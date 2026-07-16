using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for Red Hat Enterprise Linux (RHEL) commercial subscription packages.
/// </summary>
public class RedHatEnterprisePlugin : ILicensePlugin
{
    public string PluginId => "std.redhat.enterprise";
    public string PluginName => "Red Hat Enterprise Linux Commercial Subscription Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.redhat.enterprise",
        pluginName: "Red Hat Enterprise Linux Commercial Subscription Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Detects RHEL entitlement certificate subscription artifacts.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Linux"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        var lower = $"{software.Name} {software.Publisher}".ToLowerInvariant();
        return lower.Contains("red hat enterprise") || lower.Contains("rhel");
    }

    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        evidences.Add(new Evidence(
            evidenceType: "RhelSubscriptionMatch",
            description: $"Detected Red Hat Enterprise Linux software: '{software.Name}'. Requires RHEL commercial support subscription.",
            sourceLocation: "SoftwareInfo.Name",
            rawData: software.Name
        ));

        return Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.Commercial,
            licenseName: "Red Hat Enterprise Linux Commercial Subscription",
            confidence: ConfidenceLevel.High,
            evidences: evidences,
            notes: "Identified Red Hat commercial package subject to RHSM subscription verification."
        ));
    }
}
