using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for TablePlus database management client and device license verification.
/// </summary>
public class TablePlusLicensePlugin : ILicensePlugin
{
    public string PluginId => "std.tableplus.client";
    public string PluginName => "TablePlus Database Client Commercial Device License Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.tableplus.client",
        pluginName: "TablePlus Database Client Commercial Device License Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Identifies TablePlus commercial license registration artifacts.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Any"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        return software.Name.Contains("TablePlus", StringComparison.OrdinalIgnoreCase);
    }

    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        ConfidenceLevel confidence = ConfidenceLevel.High;

        evidences.Add(new Evidence(
            evidenceType: "TablePlusPackage",
            description: $"Detected TablePlus GUI client: '{software.Name}'. Commercial seat / device license required beyond single database restriction.",
            sourceLocation: "SoftwareInfo.Name",
            rawData: software.Name
        ));

        return Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.Commercial,
            licenseName: "TablePlus Commercial Device License",
            confidence: confidence,
            evidences: evidences,
            notes: "Identified TablePlus installation requiring commercial license key."
        ));
    }
}
