using System.Diagnostics;
using System.Management;
using System.Runtime.Versioning;
using LicenseIntelligencePlatform.Application.Logging;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Interfaces;
using LicenseIntelligencePlatform.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace LicenseIntelligencePlatform.Infrastructure.Scanners;

/// <summary>
/// Phase 3 — Scanner Layer: Windows OS Genuine Activation & KMS Crack Scanner.
/// Inspects Windows Operating System licensing channels (Retail, OEM, Volume KMS) and performs
/// deep heuristic read-only checks for unauthorized KMS activator tools (e.g., KMSpico, KMSAuto, SppExtComObj emulators).
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
    public Task<IEnumerable<SoftwareInfo>> ScanAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing Windows OS Activation & KMS Crack Scanner (100% Read-Only SLMGR/SPP inspection)...");
        var sw = Stopwatch.StartNew();

        var results = new List<SoftwareInfo>();

        try
        {
            var osName = GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName") ?? "Microsoft Windows Operating System";
            var displayVersion = GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "DisplayVersion") 
                              ?? GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId") 
                              ?? "10/11";
            var buildNumber = GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuildNumber") 
                           ?? GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuild") 
                           ?? "Unknown";
            var installDateEpoch = GetRegistryValueObject(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "InstallDate");

            string installDateStr = "Unknown";
            if (installDateEpoch is int epoch || (installDateEpoch is long lEpoch && (epoch = (int)lEpoch) > 0))
            {
                try
                {
                    var dt = DateTimeOffset.FromUnixTimeSeconds(epoch).UtcDateTime;
                    installDateStr = $"{dt:yyyy/MM/dd}";
                }
                catch { /* fallback */ }
            }

            var fullOsVersion = $"{osName} {displayVersion} (Build {buildNumber})";

            // 1. Check for KMS Crack files, registry keys, and services
            var crackFindings = DetectKmsCrackArtifacts();

            // 2. Query WMI SoftwareLicensingProduct for precise channel and license status
            string channel = "Unknown Channel";
            string licenseStatusText = "Unlicensed / Unknown";
            string kmsServer = string.Empty;

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    @"root\CIMV2",
                    "SELECT Description, LicenseStatus, KeyManagementServiceMachine FROM SoftwareLicensingProduct WHERE PartialProductKey IS NOT NULL AND ApplicationId = '55c92734-d682-4d71-983e-d6ec3f16059f'");

                foreach (ManagementObject obj in searcher.Get())
                {
                    var desc = obj["Description"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(desc))
                    {
                        if (desc.Contains("RETAIL", StringComparison.OrdinalIgnoreCase)) channel = "RETAIL Channel";
                        else if (desc.Contains("OEM", StringComparison.OrdinalIgnoreCase)) channel = "OEM Channel";
                        else if (desc.Contains("VOLUME_KMSCLIENT", StringComparison.OrdinalIgnoreCase)) channel = "VOLUME_KMSCLIENT Channel";
                        else if (desc.Contains("VOLUME_MAK", StringComparison.OrdinalIgnoreCase)) channel = "VOLUME_MAK Channel";
                        else channel = desc;
                    }

                    if (obj["LicenseStatus"] is uint statusVal || int.TryParse(obj["LicenseStatus"]?.ToString(), out int sVal) && (statusVal = (uint)sVal) >= 0)
                    {
                        licenseStatusText = statusVal switch
                        {
                            1 => "Activated (Licensed)",
                            0 => "Unlicensed",
                            2 => "OOB Grace Period",
                            3 => "OOT Grace Period",
                            4 => "Non-Genuine Grace Period",
                            5 => "Notification Mode (Expired)",
                            6 => "Extended Grace Period",
                            _ => $"Status Code {statusVal}"
                        };
                    }

                    kmsServer = obj["KeyManagementServiceMachine"]?.ToString()?.Trim() ?? string.Empty;
                    break;
                }
            }
            catch (Exception wmiEx)
            {
                _logger.LogDebug(wmiEx, "WMI query for SoftwareLicensingProduct threw exception. Falling back to SPP registry diagnostics.");
                var sppKeyName = GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform", "KeyManagementServiceName");
                if (!string.IsNullOrWhiteSpace(sppKeyName)) kmsServer = sppKeyName;
            }

            // Check if KMS Server points to pirated / local emulator host
            bool isPirateKmsHost = false;
            if (!string.IsNullOrWhiteSpace(kmsServer))
            {
                var lowerKms = kmsServer.ToLowerInvariant();
                if (lowerKms.Contains("127.0.0.1") || lowerKms.Contains("0.0.0.0") || lowerKms.Contains("localhost") ||
                    lowerKms.Contains("msguides") || lowerKms.Contains("loli.best") || lowerKms.Contains("digiboy.ir") ||
                    lowerKms.Contains("cangshui") || lowerKms.Contains("03k.org") || lowerKms.Contains("kms.moe") ||
                    lowerKms.EndsWith(".ru") || lowerKms.EndsWith(".ir"))
                {
                    isPirateKmsHost = true;
                    crackFindings.Add($"Unauthorized Pirate KMS Server Host detected: {kmsServer}:1688");
                }
                else if (channel.Contains("VOLUME_KMSCLIENT", StringComparison.OrdinalIgnoreCase))
                {
                    channel += $" (KMS Host: {kmsServer})";
                }
            }

            string scanSourceSummary;
            if (crackFindings.Count > 0 || isPirateKmsHost)
            {
                var details = string.Join("; ", crackFindings);
                scanSourceSummary = $"OS License Scanner [KMS CRACK DETECTED: {details} | Status: {licenseStatusText}]";
            }
            else
            {
                scanSourceSummary = $"OS License Scanner [{channel} - Status: {licenseStatusText}]";
            }

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
            _logger.LogPerformance("WindowsOsScanner", "ScanAsync", sw.ElapsedMilliseconds, $"Discovered OS activation: {channel} ({licenseStatusText})");
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "WindowsOsScanner encountered an error while auditing Windows license state.");
        }

        return Task.FromResult<IEnumerable<SoftwareInfo>>(results);
    }

    [SupportedOSPlatform("windows")]
    private static List<string> DetectKmsCrackArtifacts()
    {
        var findings = new List<string>();

        // Check common KMS activator / emulator files in Windows directory
        string[] crackFilePaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "SppExtComObj.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "SppExtComObjPatcher.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "SECOInjector.dll"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SECOInjector.dll"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "KMS-R@1n.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "KMSpico", "KMSELDI.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "KMSAuto", "KMSAuto.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "KMSAutoS", "KMSAutoS.exe")
        };

        foreach (var path in crackFilePaths)
        {
            try
            {
                if (File.Exists(path))
                {
                    findings.Add($"Activator binary found: {Path.GetFileName(path)} ({path})");
                }
            }
            catch { /* Ignore access exceptions */ }
        }

        // Check registry markers
        try
        {
            using var kmspicoKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\KMSpico");
            if (kmspicoKey != null)
            {
                findings.Add("Registry hive HKLM\\SOFTWARE\\KMSpico detected");
            }
        }
        catch { /* ignore */ }

        // Check Windows services for KMS activators
        try
        {
            string[] crackServices = new[] { "ServiceKMSEL", "KMSAutoNet", "SppExtComObjPatcher" };
            using var servicesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
            if (servicesKey != null)
            {
                foreach (var svc in crackServices)
                {
                    using var subKey = servicesKey.OpenSubKey(svc);
                    if (subKey != null)
                    {
                        findings.Add($"Activator background service detected: {svc}");
                    }
                }
            }
        }
        catch { /* ignore */ }

        return findings;
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
