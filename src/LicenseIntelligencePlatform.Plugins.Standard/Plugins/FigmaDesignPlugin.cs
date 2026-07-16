using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for identifying Figma desktop application and cloud UX commercial license subscriptions.
/// </summary>
public class FigmaDesignPlugin : ILicensePlugin
{
    public string PluginId => "std.figma.design";
    public string PluginName => "Figma Commercial UX Design License Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.figma.design",
        pluginName: "Figma Commercial UX Design License Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Detects Figma agent commercial subscription license.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Any"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        return software.Name.Contains("Figma", StringComparison.OrdinalIgnoreCase);
    }

    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        ConfidenceLevel confidence = ConfidenceLevel.Medium;

        evidences.Add(new Evidence(
            evidenceType: "FigmaClientPackage",
            description: $"Detected Figma design client installation: '{software.Name}'. Cloud workspace seat licenses apply.",
            sourceLocation: "SoftwareInfo.Name",
            rawData: software.Name
        ));

        return Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.Commercial,
            licenseName: "Figma Commercial Organization / Enterprise License",
            confidence: confidence,
            evidences: evidences,
            notes: "Identified Figma desktop client tied to cloud organization seat management."
        ));
    }
}
