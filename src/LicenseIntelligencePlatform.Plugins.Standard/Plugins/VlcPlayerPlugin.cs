using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for VLC Media Player open source GPL license verification.
/// </summary>
public class VlcPlayerPlugin : ILicensePlugin
{
    public string PluginId => "std.videolan.vlc";
    public string PluginName => "VLC Media Player Open Source GPL Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.videolan.vlc",
        pluginName: "VLC Media Player Open Source GPL Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Identifies VLC LGPL/GPL open source media player.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Any"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        var lower = software.Name.ToLowerInvariant();
        return lower.Contains("vlc media player") || (software.Publisher?.ToLowerInvariant().Contains("videolan") ?? false);
    }

    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        evidences.Add(new Evidence(
            evidenceType: "VlcPlayerPackage",
            description: $"Detected VLC Media Player: '{software.Name}'. Distributed under open source GNU GPLv2+.",
            sourceLocation: "SoftwareInfo.Name",
            rawData: software.Name
        ));

        return Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.OpenSource,
            licenseName: "GNU General Public License v2.0+ (GPL-2.0+)",
            confidence: ConfidenceLevel.Verified,
            evidences: evidences,
            notes: "Identified VideoLAN VLC Media Player open source package."
        ));
    }
}
