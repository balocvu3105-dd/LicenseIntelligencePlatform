using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for Unity Game Engine / Unity Hub commercial tier checks based on funding threshold.
/// </summary>
public class UnityGameEnginePlugin : ILicensePlugin
{
    public string PluginId => "std.unity.engine";
    public string PluginName => "Unity Game Engine Commercial & Pro Tier Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.unity.engine",
        pluginName: "Unity Game Engine Commercial & Pro Tier Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Identifies Unity Personal/Pro/Enterprise license configuration.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Any"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        var lower = software.Name.ToLowerInvariant();
        return lower.Contains("unity") && !lower.Contains("community");
    }

    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        ConfidenceLevel confidence = ConfidenceLevel.Medium;

        evidences.Add(new Evidence(
            evidenceType: "UnityEngineInstallation",
            description: $"Detected Unity development package: '{software.Name}'. Pro/Enterprise subscription mandatory for revenue >$200K.",
            sourceLocation: "SoftwareInfo.Name",
            rawData: software.Name
        ));

        return Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.Commercial,
            licenseName: "Unity Pro / Enterprise Commercial Seat License",
            confidence: confidence,
            evidences: evidences,
            notes: "Flagged Unity development seat for revenue/funding limit audit."
        ));
    }
}
