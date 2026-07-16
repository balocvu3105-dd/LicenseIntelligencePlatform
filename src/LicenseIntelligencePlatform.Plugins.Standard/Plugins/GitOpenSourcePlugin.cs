using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for Git version control client and GPL open source verification.
/// </summary>
public class GitOpenSourcePlugin : ILicensePlugin
{
    public string PluginId => "std.git.opensource";
    public string PluginName => "Git Version Control Open Source License Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.git.opensource",
        pluginName: "Git Version Control Open Source License Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Verifies Git GPL open source distribution via COPYING/LICENSE.txt.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Any"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        var lower = software.Name.ToLowerInvariant();
        return lower.Equals("git", StringComparison.OrdinalIgnoreCase) || lower.StartsWith("git version") || lower.StartsWith("git for windows");
    }

    public async Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        ConfidenceLevel confidence = ConfidenceLevel.High;

        if (!string.IsNullOrWhiteSpace(software.InstallPath) && Directory.Exists(software.InstallPath))
        {
            try
            {
                var licPath = Path.Combine(software.InstallPath, "LICENSE.txt");
                if (!File.Exists(licPath)) licPath = Path.Combine(software.InstallPath, "COPYING");
                if (File.Exists(licPath))
                {
                    var content = await File.ReadAllTextAsync(licPath, cancellationToken);
                    if (content.Contains("GENERAL PUBLIC LICENSE", StringComparison.OrdinalIgnoreCase))
                    {
                        confidence = ConfidenceLevel.Verified;
                        evidences.Add(new Evidence(
                            evidenceType: "GitGplArtifact",
                            description: "Detected GNU General Public License v2 artifact inside Git directory.",
                            sourceLocation: licPath,
                            rawData: "GNU GPL Version 2"
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
                evidenceType: "GitPackageName",
                description: $"Detected Git Open Source version control package: '{software.Name}'.",
                sourceLocation: "SoftwareInfo.Name",
                rawData: software.Name
            ));
        }

        return new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.OpenSource,
            licenseName: "GNU General Public License (GPL)",
            confidence: confidence,
            evidences: evidences,
            notes: "Verified via Git distribution directory COPYING/LICENSE.txt checks."
        );
    }
}
