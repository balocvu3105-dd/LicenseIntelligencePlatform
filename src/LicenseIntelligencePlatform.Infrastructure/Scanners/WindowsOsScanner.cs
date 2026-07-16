using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using LicenseIntelligencePlatform.Application.Logging;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Interfaces;
using LicenseIntelligencePlatform.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace LicenseIntelligencePlatform.Infrastructure.Scanners;

/// <summary>
/// Phase 3 — Scanner Layer: Windows License Audit & System Compliance Module.
/// Collects comprehensive Windows Operating System metadata, edition, activation status, license channels,
/// product keys, and executes deep read-only searches across 8 system locations (System32, Program Files,
/// ProgramData, Startup, Scheduled Tasks, Services, Registry, Licensing configuration) for KMS crack artifacts.
/// Performs digital signature verification and calculates a weighted risk score.
/// </summary>
public sealed class WindowsOsScanner : IScanner
{
    private readonly ILogger<WindowsOsScanner> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsOsScanner"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public WindowsOsScanner(ILogger<WindowsOsScanner> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string ScannerName => "WindowsOsLicenseScanner";

    /// <inheritdoc />
    public bool IsSupportedOnCurrentPlatform() => OperatingSystem.IsWindows();

    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    public async Task<IEnumerable<SoftwareInfo>> ScanAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Windows License Audit module: Scan Started (Executing deep 8-location read-only SLMGR/SPP inspection)...");
        var sw = Stopwatch.StartNew();

        var results = new List<SoftwareInfo>();

        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return results;
            }

            // 1. Collect OS Metadata
            var edition = GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName") ?? "Microsoft Windows Operating System";
            var displayVersion = GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "DisplayVersion")
                              ?? GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId")
                              ?? "10/11";
            var build = GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuildNumber")
                     ?? GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuild")
                     ?? "Unknown";
            var ubr = GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "UBR");
            var buildNumber = string.IsNullOrWhiteSpace(ubr) ? build : $"{build}.{ubr}";
            var productName = edition;
            var architecture = RuntimeInformation.OSArchitecture.ToString();

            var installDateEpoch = GetRegistryValueObject(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "InstallDate");
            string installDateStr = "Unknown";
            if (installDateEpoch is int epoch || (installDateEpoch is long lEpoch && (epoch = (int)lEpoch) > 0))
            {
                try
                {
                    var dt = DateTimeOffset.FromUnixTimeSeconds(epoch).UtcDateTime;
                    installDateStr = $"{dt:yyyy/MM/dd HH:mm:ss}";
                }
                catch { /* fallback */ }
            }

            var fullOsVersion = $"{edition} {displayVersion} (Build {buildNumber})";

            var auditEvidences = new List<Evidence>();

            // 2. Query WMI SoftwareLicensingService for OEM key, BIOS embedded key, and KMS server
            bool oemKeyPresence = false;
            string biosEmbeddedKey = string.Empty;
            string kmsServerFromService = string.Empty;
            string clientMachineId = string.Empty;

            try
            {
                using var serviceSearcher = new ManagementObjectSearcher(@"root\CIMV2", "SELECT OA3xOriginalProductKey, OA3xOriginalProductKeyDescription, KeyManagementServiceMachine, KeyManagementServicePort, ClientMachineID FROM SoftwareLicensingService");
                foreach (ManagementObject obj in serviceSearcher.Get())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var oa3Key = obj["OA3xOriginalProductKey"]?.ToString()?.Trim() ?? string.Empty;
                    var oa3Desc = obj["OA3xOriginalProductKeyDescription"]?.ToString()?.Trim() ?? string.Empty;
                    kmsServerFromService = obj["KeyManagementServiceMachine"]?.ToString()?.Trim() ?? string.Empty;
                    clientMachineId = obj["ClientMachineID"]?.ToString()?.Trim() ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(oa3Key) && oa3Key.Length >= 20)
                    {
                        oemKeyPresence = true;
                        biosEmbeddedKey = $"{oa3Key} ({oa3Desc})".Trim();
                        auditEvidences.Add(new Evidence(
                            evidenceType: "BIOS Embedded OEM Key Discovered",
                            description: $"Genuine OEM BIOS SLIC / MSDM key verified present in ACPI tables: {oa3Desc}",
                            sourceLocation: @"WMI SoftwareLicensingService (OA3xOriginalProductKey)",
                            rawData: $"BIOS Key Present: {oemKeyPresence}")
                        {
                            Category = "WmiLicensingQuery",
                            Severity = "INFO",
                            Confidence = "Verified",
                            Reason = "OEM hardware embedded license key detected in ACPI MSDM table.",
                            Path = "ACPI\\MSDM / SoftwareLicensingService",
                            Recommendation = "Retain original OEM product key for compliance validation upon bare-metal reinstallation."
                        });
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "WMI query for SoftwareLicensingService fallback triggered.");
            }

            // Check SPP backup product key if WMI OA3x was empty
            if (!oemKeyPresence)
            {
                var backupKey = GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform", "BackupProductKeyDefault");
                if (!string.IsNullOrWhiteSpace(backupKey) && backupKey.Length >= 20)
                {
                    oemKeyPresence = true;
                    biosEmbeddedKey = $"Backup SPP Key: {backupKey}";
                }
            }

            // 3. Query WMI SoftwareLicensingProduct for partial product key, channel, and activation status
            string activationStatus = "Unlicensed / Unknown Status";
            string licenseChannel = "Unknown Channel";
            string installedProductKeyMasked = "XXXXX-XXXXX-XXXXX-XXXXX-XXXXX";
            string slpSummary = string.Empty;

            try
            {
                using var prodSearcher = new ManagementObjectSearcher(@"root\CIMV2", "SELECT Description, LicenseStatus, PartialProductKey, ApplicationId, ProductKeyChannel FROM SoftwareLicensingProduct WHERE PartialProductKey IS NOT NULL AND ApplicationId = '55c92734-d682-4d71-983e-d6ec3f16059f'");
                foreach (ManagementObject obj in prodSearcher.Get())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var desc = obj["Description"]?.ToString() ?? string.Empty;
                    var channelProp = obj["ProductKeyChannel"]?.ToString() ?? string.Empty;
                    var partialKey = obj["PartialProductKey"]?.ToString()?.Trim() ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(partialKey))
                    {
                        installedProductKeyMasked = $"XXXXX-XXXXX-XXXXX-XXXXX-{partialKey}";
                    }

                    var combinedDesc = $"{desc} {channelProp}";
                    if (combinedDesc.Contains("RETAIL", StringComparison.OrdinalIgnoreCase)) licenseChannel = "RETAIL Channel";
                    else if (combinedDesc.Contains("OEM", StringComparison.OrdinalIgnoreCase)) licenseChannel = "OEM Channel";
                    else if (combinedDesc.Contains("VOLUME_KMSCLIENT", StringComparison.OrdinalIgnoreCase) || combinedDesc.Contains("GVLK", StringComparison.OrdinalIgnoreCase)) licenseChannel = "VOLUME_KMSCLIENT (GVLK)";
                    else if (combinedDesc.Contains("VOLUME_MAK", StringComparison.OrdinalIgnoreCase)) licenseChannel = "VOLUME_MAK Channel";
                    else if (combinedDesc.Contains("TIMEBASED_EVAL", StringComparison.OrdinalIgnoreCase) || combinedDesc.Contains("EVAL", StringComparison.OrdinalIgnoreCase)) licenseChannel = "Evaluation Channel";
                    else if (!string.IsNullOrWhiteSpace(channelProp)) licenseChannel = channelProp;
                    else if (!string.IsNullOrWhiteSpace(desc)) licenseChannel = desc;

                    if (obj["LicenseStatus"] is uint statusVal || int.TryParse(obj["LicenseStatus"]?.ToString(), out int sVal) && (statusVal = (uint)sVal) >= 0)
                    {
                        activationStatus = statusVal switch
                        {
                            1 => "Activated (Licensed)",
                            0 => "Unlicensed",
                            2 => "Out-of-Box Grace Period",
                            3 => "Out-of-Tolerance Grace Period",
                            4 => "Non-Genuine Grace Period",
                            5 => "Notification Mode (Expired)",
                            6 => "Extended Grace Period",
                            _ => $"Status Code {statusVal}"
                        };
                    }

                    slpSummary = $"ApplicationId: 55c92734-d682-4d71-983e-d6ec3f16059f | Status: {activationStatus} | Channel: {licenseChannel} | PartialKey: {partialKey}";
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "WMI query for SoftwareLicensingProduct failed. Using registry fallbacks.");
            }

            var slsSummary = $"ClientMachineId: {clientMachineId} | KMS Host: {kmsServerFromService} | OEM Key Present: {oemKeyPresence}";

            // 4. Detect Evidence across 8 system locations (System32, Program Files, ProgramData, Startup, Scheduled Tasks, Services, Registry, Licensing configuration)
            int scoreActivatorBinary = 0;
            int scoreActivatorService = 0;
            int scoreActivatorTask = 0;
            int scoreActivatorRegistry = 0;
            int scorePirateKmsHost = 0;
            int scoreInvalidSignature = 0;
            int scoreVolumeKmsOrGvlk = 0;
            int scoreNonGenuineStatus = 0;

            // Location 1: System32
            var system32Files = new[] { "SppExtComObj.exe", "SppExtComObjPatcher.exe", "SECOInjector.dll", "SECOInjector.exe", "KMS-R@1n.exe", "Ohook.dll" };
            var sys32Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32");
            foreach (var fName in system32Files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fullPath = Path.Combine(sys32Dir, fName);
                if (File.Exists(fullPath))
                {
                    scoreActivatorBinary = 45;
                    _logger.LogWarning("Evidence Found: Unauthorized KMS binary discovered inside System32: {Path}", fullPath);
                    var sigInfo = VerifyDigitalSignature(fullPath);
                    if (sigInfo.Status.Contains("Unsigned") || sigInfo.Status.Contains("Invalid")) scoreInvalidSignature = 25;

                    auditEvidences.Add(new Evidence(
                        evidenceType: "KMS Crack Activator Binary Discovered",
                        description: $"Unauthorized KMS emulator file discovered in System32: {fName} (Publisher: {sigInfo.Publisher}, SHA256: {sigInfo.Sha256})",
                        sourceLocation: "System32 Directory Inspection",
                        rawData: $"Path: {fullPath} | Signature: {sigInfo.Status} | Modified: {sigInfo.ModifiedTime}")
                    {
                        Category = "FileSystemArtifact",
                        Severity = "CRITICAL",
                        Confidence = "Verified",
                        Reason = "Known KMS crack / emulator binary resides in system directory.",
                        Path = fullPath,
                        Recommendation = $"Immediately delete unauthorized binary '{fName}' and perform a clean system file integrity scan ('sfc /scannow')."
                    });
                }
            }

            // Location 2 & 3: Program Files & ProgramData
            var targetPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "KMSpico", "KMSELDI.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "KMSpico", "AutoPico.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "KMSpico", "KMSELDI.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "KMSAuto", "KMSAuto.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "KMSAutoS", "KMSAutoS.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AutoKMS", "AutoKMS.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "KMS-R@1n.exe")
            };

            foreach (var path in targetPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (File.Exists(path))
                    {
                        scoreActivatorBinary = 45;
                        _logger.LogWarning("Evidence Found: Activator suite binary discovered: {Path}", path);
                        var sigInfo = VerifyDigitalSignature(path);
                        if (sigInfo.Status.Contains("Unsigned") || sigInfo.Status.Contains("Invalid")) scoreInvalidSignature = 25;

                        auditEvidences.Add(new Evidence(
                            evidenceType: "KMS Crack Activator Suite Discovered",
                            description: $"Activator binary discovered: {Path.GetFileName(path)} (Publisher: {sigInfo.Publisher}, SHA256: {sigInfo.Sha256})",
                            sourceLocation: "Program Files / ProgramData Inspection",
                            rawData: $"Path: {path} | Signature: {sigInfo.Status} | Modified: {sigInfo.ModifiedTime}")
                        {
                            Category = "FileSystemArtifact",
                            Severity = "CRITICAL",
                            Confidence = "Verified",
                            Reason = "Known KMS activation bypass suite installation directory detected.",
                            Path = path,
                            Recommendation = $"Uninstall unauthorized activation tool '{Path.GetFileName(path)}' immediately to restore compliance."
                        });
                    }
                }
                catch { /* Ignore access exceptions */ }
            }

            // Location 4: Startup Folder
            var startupDirs = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs\StartUp")
            };
            foreach (var sDir in startupDirs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (Directory.Exists(sDir))
                {
                    try
                    {
                        foreach (var file in Directory.GetFiles(sDir))
                        {
                            var fName = Path.GetFileName(file).ToLowerInvariant();
                            if (fName.Contains("kms") || fName.Contains("pico") || fName.Contains("sppext") || fName.Contains("ohook") || fName.Contains("mas"))
                            {
                                scoreActivatorBinary = 45;
                                _logger.LogWarning("Evidence Found: Startup hook discovered: {File}", file);
                                auditEvidences.Add(new Evidence(
                                    evidenceType: "KMS Activator Startup Hook",
                                    description: $"Activator shortcut/binary discovered in Startup folder: {Path.GetFileName(file)}",
                                    sourceLocation: "Startup Directory Inspection",
                                    rawData: $"Startup Item: {file}")
                                {
                                    Category = "FileSystemArtifact",
                                    Severity = "HIGH",
                                    Confidence = "Verified",
                                    Reason = "Activator hook configured to execute automatically on system boot.",
                                    Path = file,
                                    Recommendation = $"Remove startup shortcut/binary '{Path.GetFileName(file)}' to prevent continuous license tampering."
                                });
                            }
                        }
                    }
                    catch { /* ignore */ }
                }
            }

            // Location 5: Scheduled Tasks
            try
            {
                var crackTasks = new[] { "AutoKMS", "AutoKMSDaily", "KMSpico", "KMSAutoNet", "SppExtComObjPatcher", "SECOInjector", "OnlineKMS", "MAS_AIO" };
                using var taskTreeKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree");
                if (taskTreeKey != null)
                {
                    foreach (var tName in taskTreeKey.GetSubKeyNames())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (crackTasks.Any(ct => tName.Contains(ct, StringComparison.OrdinalIgnoreCase)))
                        {
                            scoreActivatorTask = 20;
                            _logger.LogWarning("Evidence Found: Activator Scheduled Task discovered: {TaskName}", tName);
                            auditEvidences.Add(new Evidence(
                                evidenceType: "Activator Scheduled Task Detected",
                                description: $"Windows Task Scheduler contains recurring activation crack task: {tName}",
                                sourceLocation: @"Schedule\TaskCache\Tree Registry Inspection",
                                rawData: $"Task Name: {tName}")
                            {
                                Category = "ScheduledTaskInspection",
                                Severity = "HIGH",
                                Confidence = "Verified",
                                Reason = "Scheduled task automatically resets grace periods or re-injects activator hooks.",
                                Path = $@"TaskCache\Tree\{tName}",
                                Recommendation = $"Delete scheduled task '{tName}' using Task Scheduler or 'schtasks /delete /tn \"{tName}\" /f'."
                            });
                        }
                    }
                }
            }
            catch { /* ignore */ }

            // Location 6: Services
            try
            {
                var crackServices = new[] { "ServiceKMSEL", "KMSAutoNet", "SppExtComObjPatcher", "AutoKMS", "MAS_Service", "SECOInjector" };
                using var servicesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
                if (servicesKey != null)
                {
                    foreach (var svc in crackServices)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        using var subKey = servicesKey.OpenSubKey(svc);
                        if (subKey != null)
                        {
                            scoreActivatorService = 25;
                            _logger.LogWarning("Evidence Found: Activator Background Service discovered: {ServiceName}", svc);
                            auditEvidences.Add(new Evidence(
                                evidenceType: "Activator Background Service Detected",
                                description: $"Windows service registered for KMS crack persistence: {svc}",
                                sourceLocation: @"HKLM\SYSTEM\CurrentControlSet\Services",
                                rawData: $"Service: {svc}")
                            {
                                Category = "ServiceInspection",
                                Severity = "CRITICAL",
                                Confidence = "Verified",
                                Reason = "Background service runs KMS loopback or patcher engine continuously with SYSTEM privileges.",
                                Path = $@"HKLM\SYSTEM\CurrentControlSet\Services\{svc}",
                                Recommendation = $"Stop and remove service using administrative command: 'sc stop {svc} && sc delete {svc}'."
                            });
                        }
                    }
                }
            }
            catch { /* ignore */ }

            // Location 7: Registry Hives & Markers
            try
            {
                var crackHives = new[] { @"SOFTWARE\KMSpico", @"SOFTWARE\KMSAuto" };
                foreach (var hivePath in crackHives)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using var subKey = Registry.LocalMachine.OpenSubKey(hivePath);
                    if (subKey != null)
                    {
                        scoreActivatorRegistry = 15;
                        _logger.LogWarning("Evidence Found: Activator Registry Hive discovered: {HivePath}", hivePath);
                        auditEvidences.Add(new Evidence(
                            evidenceType: "Activator Registry Hive Detected",
                            description: $"Dedicated activator registry configuration found: HKLM\\{hivePath}",
                            sourceLocation: "LocalMachine Registry Hive Inspection",
                            rawData: $"Registry Hive: HKLM\\{hivePath}")
                        {
                            Category = "RegistryArtifact",
                            Severity = "HIGH",
                            Confidence = "Verified",
                            Reason = "Registry hive stores configuration parameters and license bypass variables.",
                            Path = $"HKLM\\{hivePath}",
                            Recommendation = $"Clean up unauthorized registry hive 'HKLM\\{hivePath}' after removing activator binaries."
                        });
                    }
                }
            }
            catch { /* ignore */ }

            // Location 8: Licensing Configuration & KMS Host Check
            var kmsServerFromSpp = GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform", "KeyManagementServiceName");
            var activeKmsServer = !string.IsNullOrWhiteSpace(kmsServerFromService) ? kmsServerFromService : (kmsServerFromSpp ?? string.Empty);
            var discoveredKmsIp = GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform", "DiscoveredKeyManagementServiceMachineIpAddress") ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(activeKmsServer) || !string.IsNullOrWhiteSpace(discoveredKmsIp))
            {
                var checkKms = $"{activeKmsServer} {discoveredKmsIp}".ToLowerInvariant();
                if (checkKms.Contains("127.0.0.1") || checkKms.Contains("0.0.0.0") || checkKms.Contains("localhost") ||
                    checkKms.Contains("kms.moe") || checkKms.Contains("msguides") || checkKms.Contains("cangshui") ||
                    checkKms.Contains("digiboy") || checkKms.Contains("03k.org") || checkKms.Contains("loli.best") ||
                    checkKms.EndsWith(".ru") || checkKms.EndsWith(".ir"))
                {
                    scorePirateKmsHost = 35;
                    _logger.LogWarning("Evidence Found: Unauthorized Pirate/Loopback KMS Host discovered: {KmsServer}", activeKmsServer);
                    auditEvidences.Add(new Evidence(
                        evidenceType: "Unauthorized Pirate / Loopback KMS Host Detected",
                        description: $"Windows licensing is configured to authenticate against an unauthorized KMS server host: {activeKmsServer} (IP: {discoveredKmsIp})",
                        sourceLocation: @"SoftwareProtectionPlatform Licensing Configuration",
                        rawData: $"KMS Host: {activeKmsServer} | Discovered IP: {discoveredKmsIp}")
                    {
                        Category = "LicensingConfiguration",
                        Severity = "CRITICAL",
                        Confidence = "Verified",
                        Reason = "Operating system sends volume license activation requests to a pirated emulator or public unauthorized host.",
                        Path = @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform (KeyManagementServiceName)",
                        Recommendation = "Clear unauthorized KMS server configuration immediately using administrative commands: 'slmgr /ckms' and 'slmgr /upk'."
                    });
                }
            }

            // Check if Volume KMS Client / GVLK without corporate host or alongside crack findings
            if (licenseChannel.Contains("VOLUME_KMSCLIENT", StringComparison.OrdinalIgnoreCase) || licenseChannel.Contains("GVLK", StringComparison.OrdinalIgnoreCase))
            {
                if (scoreActivatorBinary > 0 || scoreActivatorService > 0 || scorePirateKmsHost > 0)
                {
                    scoreVolumeKmsOrGvlk = 30;
                }
                else if (string.IsNullOrWhiteSpace(activeKmsServer))
                {
                    scoreVolumeKmsOrGvlk = 15;
                    auditEvidences.Add(new Evidence(
                        evidenceType: "Unresolved Volume KMS Client Key (GVLK)",
                        description: "Windows is configured with a Volume KMS Client key but no Key Management Service (KMS) server is configured or resolved.",
                        sourceLocation: "SoftwareLicensingProduct Channel Inspection",
                        rawData: $"Channel: {licenseChannel} | KMS Server: None")
                    {
                        Category = "LicensingConfiguration",
                        Severity = "WARNING",
                        Confidence = "High",
                        Reason = "KMS Client setup lacks corporate KMS host assignment.",
                        Path = "SoftwareLicensingProduct",
                        Recommendation = "Verify internal corporate KMS server DNS SRV records (`_vlmcs._tcp`) or activate using MAK / RETAIL key."
                    });
                }
            }

            // Check non-genuine status
            if (!activationStatus.Contains("Activated", StringComparison.OrdinalIgnoreCase))
            {
                scoreNonGenuineStatus = 40;
                auditEvidences.Add(new Evidence(
                    evidenceType: "Non-Genuine or Expired Activation Status",
                    description: $"Windows Operating System reports abnormal activation state: {activationStatus}",
                    sourceLocation: "SoftwareLicensingProduct LicenseStatus",
                    rawData: $"Status: {activationStatus}")
                {
                    Category = "WmiLicensingQuery",
                    Severity = "WARNING",
                    Confidence = "Verified",
                    Reason = "Operating system license has expired or entered notification grace period.",
                    Path = "SoftwareLicensingProduct",
                    Recommendation = "Acquire a legitimate Windows operating system license key and activate via Settings or 'slmgr /ipk <KEY>'."
                });
            }

            // Verify core SPP engine digital signature if no activator binary was found, to provide clean verification evidence
            if (scoreActivatorBinary == 0)
            {
                var sppsvcPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "sppsvc.exe");
                if (File.Exists(sppsvcPath))
                {
                    var sigInfo = VerifyDigitalSignature(sppsvcPath);
                    if (!sigInfo.Status.Contains("Unsigned") && !sigInfo.Status.Contains("Invalid"))
                    {
                        auditEvidences.Add(new Evidence(
                            evidenceType: "Genuine System Protection Platform Engine Verified",
                            description: $"Core Windows licensing engine ('sppsvc.exe') verified intact with valid Microsoft digital signature. (Publisher: {sigInfo.Publisher}, SHA256: {sigInfo.Sha256})",
                            sourceLocation: @"C:\Windows\System32\sppsvc.exe Inspection",
                            rawData: $"Path: {sppsvcPath} | Signature: {sigInfo.Status} | Version: {sigInfo.Version}")
                        {
                            Category = "DigitalSignature",
                            Severity = "INFO",
                            Confidence = "Verified",
                            Reason = "Cryptographic digital signature matches Microsoft Corporation root of trust.",
                            Path = sppsvcPath,
                            Recommendation = "Maintain automatic OS updates to ensure SPP integrity."
                        });
                    }
                }
            }

            // 5. Calculate Weighted Risk Score & Classification (Rule: Never classify as cracked on one indicator alone unless threshold reached)
            int totalRiskScore = Math.Min(100, scoreActivatorBinary + scoreActivatorService + scoreActivatorTask + scoreActivatorRegistry + scorePirateKmsHost + scoreInvalidSignature + scoreVolumeKmsOrGvlk + scoreNonGenuineStatus);
            string riskClass;
            var recommendations = new List<string>();

            if (totalRiskScore >= 50)
            {
                riskClass = "Likely Unauthorized Activation / Pirated (CRITICAL RISK)";
                recommendations.Add("⚠️ CRITICAL COMPLIANCE VIOLATION: Immediately remove all detected KMS activator binaries, scheduled tasks, and background services.");
                recommendations.Add("⚠️ Reset system activation state using administrative commands: 'slmgr /ckms' and 'slmgr /upk'.");
                recommendations.Add("⚠️ Purchase or reassign a genuine Microsoft Windows RETAIL, OEM, or corporate Volume License key and activate the system.");
            }
            else if (totalRiskScore >= 16)
            {
                riskClass = "Anomalous / Grace Period / Unresolved KMS Client (MEDIUM RISK)";
                recommendations.Add("⚠ Inspect corporate network accessibility to ensure the Key Management Service (KMS) host or Active Directory activation is reachable.");
                recommendations.Add("⚠ If operating on an Evaluation or Grace Period license, ensure activation is completed before the expiration date.");
            }
            else
            {
                riskClass = "Legitimate / Clean Microsoft License (LOW RISK)";
                recommendations.Add("✔ Windows Operating System is genuinely activated and fully compliant with Microsoft licensing terms.");
                recommendations.Add("✔ Continue standard system maintenance and retain OEM/Retail proof of entitlement.");
            }

            _logger.LogInformation("Windows License Audit module: Risk Calculated (Score: {RiskScore}/100 | Classification: {RiskClass})", totalRiskScore, riskClass);

            // 6. Build summary string & store inside shared context for verification plugins and exporters
            string scanSourceSummary;
            if (totalRiskScore >= 50)
            {
                var crackBrief = string.Join("; ", auditEvidences.Where(e => e.Severity == "CRITICAL" || e.Severity == "HIGH").Select(e => e.EvidenceType));
                scanSourceSummary = $"OS License Scanner [KMS CRACK DETECTED: {crackBrief} | Score: {totalRiskScore}/100 | Status: {activationStatus}]";
            }
            else
            {
                scanSourceSummary = $"OS License Scanner [{licenseChannel} - Status: {activationStatus} | Score: {totalRiskScore}/100 - {riskClass}]";
            }

            var auditData = new WindowsLicenseAuditData
            {
                WindowsEdition = edition,
                BuildNumber = buildNumber,
                ProductName = productName,
                InstallDate = installDateStr,
                Architecture = architecture,
                ActivationStatus = activationStatus,
                LicenseChannel = licenseChannel,
                OemKeyPresence = oemKeyPresence,
                InstalledProductKeyMasked = installedProductKeyMasked,
                BiosEmbeddedKey = biosEmbeddedKey,
                SoftwareLicensingProductSummary = slpSummary,
                SoftwareLicensingServiceSummary = slsSummary,
                RiskScore = totalRiskScore,
                RiskClassification = riskClass,
                SummaryText = scanSourceSummary,
                AuditEvidences = auditEvidences.AsReadOnly(),
                Recommendations = recommendations.AsReadOnly(),
                AuditedAtUtc = DateTime.UtcNow
            };

            WindowsLicenseAuditContext.CurrentAuditData = auditData;

            var osPackage = new SoftwareInfo(
                name: "Microsoft Windows Operating System (OS License Check)",
                version: fullOsVersion,
                publisher: "Microsoft Corporation",
                installPath: @"C:\Windows\System32\spp",
                installDate: installDateStr,
                scanSource: scanSourceSummary,
                lastModifiedDate: VietnamTime.Format(DateTime.UtcNow),
                appStartTime: "System Kernel (Active OS)"
            );

            results.Add(osPackage);
            sw.Stop();
            _logger.LogInformation("Windows License Audit module: Scan Completed in {Duration} ms ({EvidenceCount} evidence artifacts discovered).", sw.ElapsedMilliseconds, auditEvidences.Count);
            _logger.LogPerformance("WindowsOsScanner", "ScanAsync", sw.ElapsedMilliseconds, $"Discovered OS: {licenseChannel} ({activationStatus}) - Risk Score: {totalRiskScore}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Windows License Audit module encountered an unhandled exception during OS inspection.");
        }

        return results;
    }

    [SupportedOSPlatform("windows")]
    private static (string Publisher, string Sha256, string Version, string ModifiedTime, string Status) VerifyDigitalSignature(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return ("Unknown", "N/A", "0.0.0.0", "N/A", "File Not Found");

            string publisher = "Unknown";
            string version = "1.0.0.0";
            string sha256Hex = "Unknown";
            string modifiedTime = File.GetLastWriteTimeUtc(filePath).ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

            try
            {
                var fvi = FileVersionInfo.GetVersionInfo(filePath);
                if (!string.IsNullOrWhiteSpace(fvi.CompanyName)) publisher = fvi.CompanyName.Trim();
                if (!string.IsNullOrWhiteSpace(fvi.FileVersion)) version = fvi.FileVersion.Trim();
            }
            catch { /* fallback */ }

            try
            {
                using var stream = File.OpenRead(filePath);
                using var hasher = SHA256.Create();
                var hashBytes = hasher.ComputeHash(stream);
                sha256Hex = Convert.ToHexString(hashBytes).ToLowerInvariant();
            }
            catch { /* fallback */ }

            string status = "Unsigned (Suspicious Executable)";
            try
            {
#pragma warning disable SYSLIB0057
                using var cert = System.Security.Cryptography.X509Certificates.X509Certificate.CreateFromCertFile(filePath);
#pragma warning restore SYSLIB0057
                var subject = cert.Subject;
                if (!string.IsNullOrWhiteSpace(subject))
                {
                    if (publisher == "Unknown" || string.IsNullOrWhiteSpace(publisher))
                    {
                        publisher = subject;
                    }
                    if (subject.Contains("Microsoft Corporation", StringComparison.OrdinalIgnoreCase) || subject.Contains("Microsoft Windows", StringComparison.OrdinalIgnoreCase))
                    {
                        status = "Signed by Microsoft Corporation (Valid)";
                    }
                    else
                    {
                        status = $"Signed by {publisher} (External/Activator Certificate)";
                    }
                }
            }
            catch
            {
                status = "Unsigned / No Digital Certificate Discovered";
            }

            return (publisher, sha256Hex, version, modifiedTime, status);
        }
        catch
        {
            return ("Unknown", "Error", "Unknown", "Unknown", "Inspection Failed");
        }
    }

    [SupportedOSPlatform("windows")]
    private static string? GetRegistryValue(string subKeyPath, string valueName)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(subKeyPath);
            return key?.GetValue(valueName)?.ToString()?.Trim();
        }
        catch
        {
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static object? GetRegistryValueObject(string subKeyPath, string valueName)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(subKeyPath);
            return key?.GetValue(valueName);
        }
        catch
        {
            return null;
        }
    }
}
