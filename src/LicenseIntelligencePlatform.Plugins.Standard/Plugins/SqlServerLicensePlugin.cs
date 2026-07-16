using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for identifying Microsoft SQL Server database editions and binary instances (`sqlservr.exe`).
/// </summary>
public class SqlServerLicensePlugin : ILicensePlugin
{
    private static readonly string[] SqlKeywords = { "microsoft sql server", "sql server 20", "sql server Management studio", "mssqlserver" };

    public string PluginId => "std.microsoft.sqlserver";
    public string PluginName => "Microsoft SQL Server Database Engine Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.microsoft.sqlserver",
        pluginName: "Microsoft SQL Server Database Engine Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Verifies SQL Server engine binary presence and edition naming.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Windows"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        var lower = software.Name.ToLowerInvariant();
        return SqlKeywords.Any(k => lower.Contains(k)) && !lower.Contains("compact");
    }

    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        ConfidenceLevel confidence = ConfidenceLevel.Medium;
        string licenseName = "Microsoft SQL Server Commercial License";

        if (!string.IsNullOrWhiteSpace(software.InstallPath) && Directory.Exists(software.InstallPath))
        {
            try
            {
                var binaries = Directory.GetFiles(software.InstallPath, "sqlservr.exe", SearchOption.AllDirectories);
                if (binaries.Length > 0)
                {
                    confidence = ConfidenceLevel.Verified;
                    evidences.Add(new Evidence(
                        evidenceType: "SqlServerEngineBinary",
                        description: $"Detected Microsoft SQL Server engine binary '{binaries[0]}'.",
                        sourceLocation: binaries[0],
                        rawData: $"Binary Size: {new FileInfo(binaries[0]).Length} bytes"
                    ));
                }
            }
            catch
            {
                // Ignore permissions
            }
        }

        var lowerName = software.Name.ToLowerInvariant();
        if (lowerName.Contains("express") || lowerName.Contains("developer"))
        {
            licenseName = "Microsoft SQL Server Free / Developer Edition License";
            confidence = ConfidenceLevel.High;
        }

        if (evidences.Count == 0)
        {
            evidences.Add(new Evidence(
                evidenceType: "SqlServerProductMatch",
                description: $"Identified Microsoft SQL Server database package: '{software.Name}'.",
                sourceLocation: "SoftwareInfo.Name",
                rawData: software.Name
            ));
        }

        return Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.Commercial,
            licenseName: licenseName,
            confidence: confidence,
            evidences: evidences,
            notes: "Verified via SQL Server engine binary presence and edition naming."
        ));
    }
}
