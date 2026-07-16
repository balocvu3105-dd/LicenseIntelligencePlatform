using System.Runtime.InteropServices;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LicenseIntelligencePlatform.Infrastructure.Scanners;

/// <summary>
/// Read-only scanner that enumerates installed software on Linux platforms by parsing local package manager status files (DPKG).
/// Does not perform any network calls or modifications.
/// </summary>
public class LinuxPackageScanner : IScanner
{
    private readonly ILogger<LinuxPackageScanner> _logger;
    private readonly string _dpkgStatusFilePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="LinuxPackageScanner"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="dpkgStatusFilePath">Optional custom DPKG status file path for testing.</param>
    public LinuxPackageScanner(ILogger<LinuxPackageScanner> logger, string dpkgStatusFilePath = "/var/lib/dpkg/status")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dpkgStatusFilePath = dpkgStatusFilePath;
    }

    /// <inheritdoc />
    public string ScannerName => "LinuxPackageScanner";

    /// <inheritdoc />
    public bool IsSupportedOnCurrentPlatform()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || File.Exists(_dpkgStatusFilePath);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SoftwareInfo>> ScanAsync(CancellationToken cancellationToken = default)
    {
        if (!IsSupportedOnCurrentPlatform())
        {
            _logger.LogDebug("LinuxPackageScanner invoked on non-Linux environment where status file is absent. Returning empty list.");
            return Array.Empty<SoftwareInfo>();
        }

        var results = new List<SoftwareInfo>();

        if (!File.Exists(_dpkgStatusFilePath))
        {
            _logger.LogInformation("DPKG status file '{FilePath}' not found.", _dpkgStatusFilePath);
            return results;
        }

        try
        {
            _logger.LogInformation("Parsing local DPKG status file: {FilePath}", _dpkgStatusFilePath);
            var lines = await File.ReadAllLinesAsync(_dpkgStatusFilePath, cancellationToken);

            string currentPackage = "";
            string currentVersion = "";
            string currentMaintainer = "";
            string currentStatus = "";

            foreach (var line in lines)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    if (!string.IsNullOrEmpty(currentPackage) && currentStatus.Contains("installed", StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(new SoftwareInfo(
                            name: currentPackage,
                            version: string.IsNullOrEmpty(currentVersion) ? "Unknown" : currentVersion,
                            publisher: string.IsNullOrEmpty(currentMaintainer) ? "Linux Community/Maintainer" : currentMaintainer,
                            installPath: "/usr",
                            installDate: "",
                            scanSource: "LinuxDpkgStatus"
                        ));
                    }

                    currentPackage = "";
                    currentVersion = "";
                    currentMaintainer = "";
                    currentStatus = "";
                    continue;
                }

                if (line.StartsWith("Package: ", StringComparison.OrdinalIgnoreCase))
                {
                    currentPackage = line.Substring(9).Trim();
                }
                else if (line.StartsWith("Status: ", StringComparison.OrdinalIgnoreCase))
                {
                    currentStatus = line.Substring(8).Trim();
                }
                else if (line.StartsWith("Version: ", StringComparison.OrdinalIgnoreCase))
                {
                    currentVersion = line.Substring(9).Trim();
                }
                else if (line.StartsWith("Maintainer: ", StringComparison.OrdinalIgnoreCase))
                {
                    currentMaintainer = line.Substring(12).Trim();
                }
            }

            if (!string.IsNullOrEmpty(currentPackage) && currentStatus.Contains("installed", StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new SoftwareInfo(
                    name: currentPackage,
                    version: string.IsNullOrEmpty(currentVersion) ? "Unknown" : currentVersion,
                    publisher: string.IsNullOrEmpty(currentMaintainer) ? "Linux Community/Maintainer" : currentMaintainer,
                    installPath: "/usr",
                    installDate: "",
                    scanSource: "LinuxDpkgStatus"
                ));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading or parsing Linux DPKG status file '{FilePath}'.", _dpkgStatusFilePath);
        }

        return results;
    }
}
