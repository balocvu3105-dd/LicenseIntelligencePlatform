using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for OBS Studio broadcasting suite open source GPLv2 license.
/// </summary>
public class ObsStudioLicensePlugin : ILicensePlugin
{
    public string PluginId => "std.obs.studio";
    public string PluginName => "OBS Studio Open Source GPLv2 Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.obs.studio",
        pluginName: "OBS Studio Open Source GPLv2 Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Identifies OBS Studio GPL-2.0 open source video recording package.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Any"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        return software.Name.Contains("OBS Studio", StringComparison.OrdinalIgnoreCase);
    }

    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        evidences.Add(new Evidence(
            evidenceType: "ObsStudioPackage",
            description: $"Detected OBS Studio broadcasting software: '{software.Name}'. Distributed under open source GNU GPLv2.",
            sourceLocation: "SoftwareInfo.Name",
            rawData: software.Name
        ));

        return Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.OpenSource,
            licenseName: "GNU General Public License v2.0 (GPL-2.0)",
            confidence: ConfidenceLevel.Verified,
            evidences: evidences,
            notes: "Identified OBS Studio open source video recording package."
        ));
    }
}
