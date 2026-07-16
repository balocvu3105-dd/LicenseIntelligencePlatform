using System.Runtime.InteropServices;
using Microsoft.Win32;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LicenseIntelligencePlatform.Infrastructure.Scanners;

/// <summary>
/// Read-only scanner that enumerates installed software packages from the local Windows Registry.
/// Inspects both 64-bit and 32-bit uninstall hives across HKLM and HKCU.
/// Does not perform any network calls or registry writes.
/// </summary>
public class WindowsRegistryScanner : IScanner
{
    private readonly ILogger<WindowsRegistryScanner> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsRegistryScanner"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public WindowsRegistryScanner(ILogger<WindowsRegistryScanner> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string ScannerName => "WindowsRegistryScanner";

    /// <inheritdoc />
    public bool IsSupportedOnCurrentPlatform()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    /// <inheritdoc />
    public Task<IEnumerable<SoftwareInfo>> ScanAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows() || !IsSupportedOnCurrentPlatform())
        {
            _logger.LogWarning("WindowsRegistryScanner invoked on non-Windows environment. Returning empty list.");
            return Task.FromResult<IEnumerable<SoftwareInfo>>(Array.Empty<SoftwareInfo>());
        }

        var results = new List<SoftwareInfo>();

        try
        {
#pragma warning disable CA1416
            // Scan HKLM 64-bit hive
            ScanRegistryHive(RegistryHive.LocalMachine, RegistryView.Registry64, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", results, cancellationToken);

            // Scan HKLM 32-bit hive (WOW6432Node)
            ScanRegistryHive(RegistryHive.LocalMachine, RegistryView.Registry32, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", results, cancellationToken);

            // Scan HKCU hive for current user installations
            ScanRegistryHive(RegistryHive.CurrentUser, RegistryView.Default, @"Software\Microsoft\Windows\CurrentVersion\Uninstall", results, cancellationToken);
#pragma warning restore CA1416
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while reading Windows Registry hives.");
        }

        return Task.FromResult<IEnumerable<SoftwareInfo>>(results);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private void ScanRegistryHive(RegistryHive hive, RegistryView view, string subKeyPath, List<SoftwareInfo> results, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var uninstallKey = baseKey.OpenSubKey(subKeyPath, writable: false); // Read-only

            if (uninstallKey == null)
            {
                return;
            }

            foreach (var subKeyName in uninstallKey.GetSubKeyNames())
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    using var appKey = uninstallKey.OpenSubKey(subKeyName, writable: false);
                    if (appKey == null)
                    {
                        continue;
                    }

                    var displayName = appKey.GetValue("DisplayName") as string;
                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        continue; // Skip system components or hidden updates without DisplayName
                    }

                    var systemComponent = appKey.GetValue("SystemComponent");
                    if (systemComponent is int sysCompVal && sysCompVal == 1)
                    {
                        continue; // Skip internal OS components
                    }

                    var displayVersion = (appKey.GetValue("DisplayVersion") as string) ?? "Unknown";
                    var publisher = (appKey.GetValue("Publisher") as string) ?? "Unknown Publisher";
                    var installLocation = (appKey.GetValue("InstallLocation") as string) ?? "";
                    var installDate = (appKey.GetValue("InstallDate") as string) ?? "";

                    string lastModified = "";
                    var trimmedPath = installLocation.Trim().Trim('\"');
                    if (!string.IsNullOrWhiteSpace(trimmedPath))
                    {
                        try
                        {
                            if (Directory.Exists(trimmedPath))
                            {
                                var mainExe = Directory.EnumerateFiles(trimmedPath, "*.exe", SearchOption.TopDirectoryOnly).FirstOrDefault();
                                if (mainExe != null)
                                {
                                    lastModified = File.GetLastWriteTimeUtc(mainExe).ToString("yyyy/MM/dd HH:mm:ss 'UTC'");
                                }
                                else
                                {
                                    lastModified = Directory.GetLastWriteTimeUtc(trimmedPath).ToString("yyyy/MM/dd HH:mm:ss 'UTC'");
                                }
                            }
                            else if (File.Exists(trimmedPath))
                            {
                                lastModified = File.GetLastWriteTimeUtc(trimmedPath).ToString("yyyy/MM/dd HH:mm:ss 'UTC'");
                            }
                        }
                        catch { /* bỏ qua lỗi permission */ }
                    }

                    results.Add(new SoftwareInfo(
                        name: displayName.Trim(),
                        version: displayVersion.Trim(),
                        publisher: publisher.Trim(),
                        installPath: trimmedPath,
                        installDate: installDate.Trim(),
                        scanSource: $"{ScannerName} ({hive}/{view})",
                        lastModifiedDate: lastModified
                    ));
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to read registry subkey '{SubKeyName}' under '{SubKeyPath}'. Skipping.", subKeyName, subKeyPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not open base registry key {Hive}/{View}\\{SubKeyPath}.", hive, view, subKeyPath);
        }
    }
}
