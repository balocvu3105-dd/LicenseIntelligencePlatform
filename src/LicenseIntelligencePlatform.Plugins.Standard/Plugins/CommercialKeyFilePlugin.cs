using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Plugin that identifies commercial and proprietary licenses by searching for license/activation key files (`*.lic`, `*.key`) or enterprise terms.
/// Adheres to Rule 6: No hardcoded publishers.
/// </summary>
public class CommercialKeyFilePlugin : ILicensePlugin
{
    private static readonly string[] CommercialKeywords = { "enterprise", "professional", "pro edition", "ultimate", "commercial", "workstation", "server edition", "subscription", "business edition" };
    private static readonly string[] LicenseKeyExtensions = { ".lic", ".key", ".dat", ".reg" };

    /// <inheritdoc />
    public string PluginId => "std.commercial.keyfile";

    /// <inheritdoc />
    public string PluginName => "Commercial License Key & Enterprise Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.commercial.keyfile",
        pluginName: "Commercial License Key & Enterprise Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Detects commercial licenses via local key/activation file artifacts.",
        priority: 90,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Windows"
    );

    /// <inheritdoc />
    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;

        var nameLower = software.Name.ToLowerInvariant();
        if (CommercialKeywords.Any(k => nameLower.Contains(k)))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(software.InstallPath) && Directory.Exists(software.InstallPath))
        {
            try
            {
                var files = Directory.GetFiles(software.InstallPath, "*.*", SearchOption.TopDirectoryOnly);
                if (files.Any(f => LicenseKeyExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()) && Path.GetFileName(f).ToLowerInvariant().Contains("lic")))
                {
                    return true;
                }
            }
            catch
            {
                // Ignore directory access permissions
            }
        }

        return false;
    }

    /// <inheritdoc />
    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        ConfidenceLevel confidence = ConfidenceLevel.Low;
        string licenseName = "Commercial Proprietary License";

        // 1. Inspect install path for license key files
        if (!string.IsNullOrWhiteSpace(software.InstallPath) && Directory.Exists(software.InstallPath))
        {
            try
            {
                var keyFiles = Directory.GetFiles(software.InstallPath, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => LicenseKeyExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()) || Path.GetFileName(f).ToLowerInvariant().Contains("license"))
                    .ToList();

                foreach (var keyFile in keyFiles)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    confidence = ConfidenceLevel.High;
                    evidences.Add(new Evidence(
                        evidenceType: "CommercialKeyFile",
                        description: $"Discovered commercial license activation/key file '{Path.GetFileName(keyFile)}' inside installation folder.",
                        sourceLocation: keyFile,
                        rawData: $"File Name: {Path.GetFileName(keyFile)}, Size: {new FileInfo(keyFile).Length} bytes"
                    ));
                }
            }
            catch
            {
                // Ignore IO errors
            }
        }

        // 2. Check enterprise terminology in software name
        var nameLower = software.Name.ToLowerInvariant();
        foreach (var keyword in CommercialKeywords)
        {
            if (nameLower.Contains(keyword))
            {
                if (confidence < ConfidenceLevel.Medium) confidence = ConfidenceLevel.Medium;
                evidences.Add(new Evidence(
                    evidenceType: "CommercialTerm",
                    description: $"Software package name contains commercial tier designation: '{keyword}'.",
                    sourceLocation: "SoftwareInfo.Name",
                    rawData: software.Name
                ));
            }
        }

        if (confidence == ConfidenceLevel.High && evidences.Count > 0)
        {
            confidence = ConfidenceLevel.Verified;
        }

        return Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.Commercial,
            licenseName: licenseName,
            confidence: confidence,
            evidences: evidences,
            notes: $"Commercial detection evaluated via activation key check and edition descriptor analysis."
        ));
    }
}
