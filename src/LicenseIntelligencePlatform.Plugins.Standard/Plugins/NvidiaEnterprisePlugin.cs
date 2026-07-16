using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for NVIDIA AI/vGPU Enterprise software license checks.
/// </summary>
public class NvidiaEnterprisePlugin : ILicensePlugin
{
    public string PluginId => "std.nvidia.enterprise";
    public string PluginName => "NVIDIA AI & vGPU Enterprise Commercial Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.nvidia.enterprise",
        pluginName: "NVIDIA AI & vGPU Enterprise Commercial Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Identifies NVIDIA vGPU / enterprise GPU driver license artifacts.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Any"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        var lower = software.Name.ToLowerInvariant();
        return lower.Contains("nvidia vgpu") || lower.Contains("nvidia ai enterprise") || lower.Contains("grid license");
    }

    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        evidences.Add(new Evidence(
            evidenceType: "NvidiaEnterprisePackage",
            description: $"Detected NVIDIA enterprise graphics/compute package: '{software.Name}'. Requires NVIDIA Enterprise licensing (NVAIE/vGPU).",
            sourceLocation: "SoftwareInfo.Name",
            rawData: software.Name
        ));

        return Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.Commercial,
            licenseName: "NVIDIA AI Enterprise / vGPU Commercial Subscription",
            confidence: ConfidenceLevel.High,
            evidences: evidences,
            notes: "Flagged NVIDIA enterprise compute software for license token audit."
        ));
    }
}
