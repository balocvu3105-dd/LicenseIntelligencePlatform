using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for Node.js runtime and MIT license verification.
/// </summary>
public class NodeJsPermissivePlugin : ILicensePlugin
{
    public string PluginId => "std.nodejs.runtime";
    public string PluginName => "Node.js Runtime MIT License Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.nodejs.runtime",
        pluginName: "Node.js Runtime MIT License Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Identifies Node.js MIT permissive open source license.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Any"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        return software.Name.Equals("Node.js", StringComparison.OrdinalIgnoreCase) || software.Name.StartsWith("Node.js ");
    }

    public async Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        ConfidenceLevel confidence = ConfidenceLevel.High;

        if (!string.IsNullOrWhiteSpace(software.InstallPath) && Directory.Exists(software.InstallPath))
        {
            try
            {
                var lic = Path.Combine(software.InstallPath, "LICENSE");
                if (File.Exists(lic))
                {
                    var text = await File.ReadAllTextAsync(lic, cancellationToken);
                    if (text.Contains("MIT License", StringComparison.OrdinalIgnoreCase))
                    {
                        confidence = ConfidenceLevel.Verified;
                        evidences.Add(new Evidence(
                            evidenceType: "NodeJsMitArtifact",
                            description: "Detected Node.js runtime MIT License text inside root folder.",
                            sourceLocation: lic,
                            rawData: "MIT License confirmed"
                        ));
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        if (evidences.Count == 0)
        {
            evidences.Add(new Evidence(
                evidenceType: "NodeJsPackageMatch",
                description: $"Detected Node.js JavaScript runtime engine package: '{software.Name}'.",
                sourceLocation: "SoftwareInfo.Name",
                rawData: software.Name
            ));
        }

        return new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.OpenSource,
            licenseName: "MIT License",
            confidence: confidence,
            evidences: evidences,
            notes: "Verified via Node.js local distribution directory LICENSE checks."
        );
    }
}
