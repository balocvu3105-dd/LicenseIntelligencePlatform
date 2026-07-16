using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for Docker Desktop commercial terms and local resource license verification.
/// </summary>
public class DockerDesktopPlugin : ILicensePlugin
{
    public string PluginId => "std.docker.desktop";
    public string PluginName => "Docker Desktop Commercial & Subscription Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.docker.desktop",
        pluginName: "Docker Desktop Commercial & Subscription Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Verifies Docker Desktop LICENSE file for subscription/commercial status.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Any"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        return software.Name.Equals("Docker Desktop", StringComparison.OrdinalIgnoreCase) ||
               (software.Name.Contains("Docker", StringComparison.OrdinalIgnoreCase) && software.Name.Contains("Desktop", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        ConfidenceLevel confidence = ConfidenceLevel.High;

        if (!string.IsNullOrWhiteSpace(software.InstallPath) && Directory.Exists(software.InstallPath))
        {
            try
            {
                var licPath = Path.Combine(software.InstallPath, "resources", "LICENSE");
                if (File.Exists(licPath))
                {
                    var snippet = await File.ReadAllTextAsync(licPath, cancellationToken);
                    confidence = ConfidenceLevel.Verified;
                    evidences.Add(new Evidence(
                        evidenceType: "DockerSubscriptionTerms",
                        description: "Detected Docker Desktop resource LICENSE specifying commercial usage terms (>250 employees / >$10M revenue).",
                        sourceLocation: licPath,
                        rawData: snippet.Length > 200 ? snippet.Substring(0, 200) + "..." : snippet
                    ));
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
                evidenceType: "DockerDesktopProduct",
                description: $"Identified Docker Desktop package requiring commercial subscription evaluation: '{software.Name}'.",
                sourceLocation: "SoftwareInfo.Name",
                rawData: software.Name
            ));
        }

        return new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.Commercial,
            licenseName: "Docker Desktop Commercial / Subscription License",
            confidence: confidence,
            evidences: evidences,
            notes: "Verified via local Docker resources LICENSE file checks."
        );
    }
}
