using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for identifying MathWorks MATLAB numerical computing suite and `network.lic` / `USE_SERVER` configurations.
/// </summary>
public class MatlabLicensePlugin : ILicensePlugin
{
    public string PluginId => "std.mathworks.matlab";
    public string PluginName => "MathWorks MATLAB Commercial Computing Suite Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.mathworks.matlab",
        pluginName: "MathWorks MATLAB Commercial Computing Suite Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Identifies MATLAB license.dat and FlexLM commercial license artifacts.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Any"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        var lower = software.Name.ToLowerInvariant();
        return lower.Contains("matlab") || (software.Publisher?.ToLowerInvariant().Contains("mathworks") ?? false);
    }

    public async Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        ConfidenceLevel confidence = ConfidenceLevel.High;

        if (!string.IsNullOrWhiteSpace(software.InstallPath) && Directory.Exists(software.InstallPath))
        {
            try
            {
                var licFiles = Directory.GetFiles(software.InstallPath, "*.lic", SearchOption.AllDirectories);
                foreach (var lic in licFiles)
                {
                    var content = await File.ReadAllTextAsync(lic, cancellationToken);
                    if (content.Contains("SERVER") || content.Contains("INCREMENT") || content.Contains("USE_SERVER"))
                    {
                        confidence = ConfidenceLevel.Verified;
                        evidences.Add(new Evidence(
                            evidenceType: "MatlabNetworkLic",
                            description: $"Detected MATLAB FlexLM network license token '{Path.GetFileName(lic)}'.",
                            sourceLocation: lic,
                            rawData: content.Length > 120 ? content.Substring(0, 120) + "..." : content
                        ));
                        break;
                    }
                }
            }
            catch
            {
                // Ignore file locks
            }
        }

        if (evidences.Count == 0)
        {
            evidences.Add(new Evidence(
                evidenceType: "MatlabProductRegistry",
                description: $"Identified MathWorks MATLAB scientific calculation suite: '{software.Name}'.",
                sourceLocation: "SoftwareInfo.Name",
                rawData: software.Name
            ));
        }

        return new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.Commercial,
            licenseName: "MathWorks MATLAB Commercial / FlexLM License",
            confidence: confidence,
            evidences: evidences,
            notes: "Verified via installation folder license tokens and MLM increment checks."
        );
    }
}
