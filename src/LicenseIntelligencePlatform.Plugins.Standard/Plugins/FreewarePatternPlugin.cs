using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Plugin that identifies freeware, community, and non-commercial software tiers via naming conventions and descriptors.
/// Adheres to Rule 6: No hardcoded publishers.
/// </summary>
public class FreewarePatternPlugin : ILicensePlugin
{
    private static readonly string[] FreewareKeywords = { "free edition", "freeware", "community edition", "personal edition", "lite edition", "home edition", "free viewer", "free reader" };

    /// <inheritdoc />
    public string PluginId => "std.freeware.pattern";

    /// <inheritdoc />
    public string PluginName => "Freeware & Community Edition Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.freeware.pattern",
        pluginName: "Freeware & Community Edition Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Detects freeware patterns by name and publisher keywords.",
        priority: 70,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Any"
    );

    /// <inheritdoc />
    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        var lower = software.Name.ToLowerInvariant();
        return FreewareKeywords.Any(k => lower.Contains(k));
    }

    /// <inheritdoc />
    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        var lower = software.Name.ToLowerInvariant();

        foreach (var kw in FreewareKeywords)
        {
            if (lower.Contains(kw))
            {
                evidences.Add(new Evidence(
                    evidenceType: "FreewareDescriptor",
                    description: $"Software name includes community or free edition descriptor: '{kw}'.",
                    sourceLocation: "SoftwareInfo.Name",
                    rawData: software.Name
                ));
            }
        }

        return Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.Freeware,
            licenseName: "Freeware / Community Edition License",
            confidence: evidences.Count > 0 ? ConfidenceLevel.Medium : ConfidenceLevel.Low,
            evidences: evidences,
            notes: "Identified via standard freeware tier descriptors without publisher lock-in."
        ));
    }
}
