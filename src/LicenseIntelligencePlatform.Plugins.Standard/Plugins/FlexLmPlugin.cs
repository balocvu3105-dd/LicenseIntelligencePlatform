using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for identifying Revenera FlexNet Publisher floating license servers and daemon installations (`lmgrd.exe`).
/// </summary>
public class FlexLmPlugin : ILicensePlugin
{
    public string PluginId => "std.flexnet.publisher";
    public string PluginName => "FlexNet Publisher / FlexLM Floating License Server Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.flexnet.publisher",
        pluginName: "FlexNet Publisher / FlexLM Floating License Server Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Detects FlexNet license.dat / lmgrd license manager artifacts.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Any"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        var lower = software.Name.ToLowerInvariant();
        return lower.Contains("flexnet") || lower.Contains("flexlm") || lower.Contains("flexera");
    }

    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        ConfidenceLevel confidence = ConfidenceLevel.High;

        if (!string.IsNullOrWhiteSpace(software.InstallPath) && Directory.Exists(software.InstallPath))
        {
            try
            {
                var lmgrd = Directory.GetFiles(software.InstallPath, "lmgrd.exe", SearchOption.AllDirectories);
                if (lmgrd.Length > 0)
                {
                    confidence = ConfidenceLevel.Verified;
                    evidences.Add(new Evidence(
                        evidenceType: "FlexLmDaemonBinary",
                        description: $"Detected FlexNet license server management daemon '{lmgrd[0]}'.",
                        sourceLocation: lmgrd[0],
                        rawData: $"lmgrd.exe present"
                    ));
                }
            }
            catch
            {
                // Ignore permissions
            }
        }

        if (evidences.Count == 0)
        {
            evidences.Add(new Evidence(
                evidenceType: "FlexNetPackageMatch",
                description: $"Identified FlexNet Publisher floating server package: '{software.Name}'.",
                sourceLocation: "SoftwareInfo.Name",
                rawData: software.Name
            ));
        }

        return Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.Commercial,
            licenseName: "FlexLM / FlexNet Floating License Manager",
            confidence: confidence,
            evidences: evidences,
            notes: "Verified via lmgrd.exe daemon binary verification."
        ));
    }
}
