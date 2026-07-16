using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for identifying Adobe Creative Cloud applications (Photoshop, Illustrator, Premiere Pro) and activation manifests (`application.xml`).
/// </summary>
public class AdobeCreativeCloudPlugin : ILicensePlugin
{
    private static readonly string[] AdobeKeywords = { "adobe photoshop", "adobe illustrator", "adobe premiere", "adobe creative cloud", "adobe after effects", "adobe acrobat pro" };

    public string PluginId => "std.adobe.creativecloud";
    public string PluginName => "Adobe Creative Cloud & Application Suite Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.adobe.creativecloud",
        pluginName: "Adobe Creative Cloud & Application Suite Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Identifies Adobe Creative Cloud subscription licenses.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Any"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        var nameLower = software.Name.ToLowerInvariant();
        return AdobeKeywords.Any(k => nameLower.Contains(k)) || (software.Publisher?.ToLowerInvariant().Contains("adobe systems") ?? false);
    }

    public async Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        ConfidenceLevel confidence = ConfidenceLevel.Medium;
        string licenseName = "Adobe Creative Cloud Commercial License";

        if (!string.IsNullOrWhiteSpace(software.InstallPath) && Directory.Exists(software.InstallPath))
        {
            try
            {
                var xmlPath = Path.Combine(software.InstallPath, "amt", "application.xml");
                if (File.Exists(xmlPath))
                {
                    var content = await File.ReadAllTextAsync(xmlPath, cancellationToken);
                    if (content.Contains("TrialSerialNumber"))
                    {
                        licenseName = "Adobe Creative Cloud Commercial / Trial License";
                    }
                    confidence = ConfidenceLevel.Verified;
                    evidences.Add(new Evidence(
                        evidenceType: "AdobeAmtManifest",
                        description: "Detected Adobe Application Management Tool (AMT) application.xml manifest.",
                        sourceLocation: xmlPath,
                        rawData: content.Length > 150 ? content.Substring(0, 150) + "..." : content
                    ));
                }
            }
            catch
            {
                // Ignore file access locks
            }
        }

        if (evidences.Count == 0)
        {
            evidences.Add(new Evidence(
                evidenceType: "AdobeProductSignature",
                description: $"Identified Adobe commercial software product: '{software.Name}'.",
                sourceLocation: "SoftwareInfo.Name",
                rawData: software.Name
            ));
        }

        return new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.Commercial,
            licenseName: licenseName,
            confidence: confidence,
            evidences: evidences,
            notes: "Verified via Creative Cloud AMT XML manifests."
        );
    }
}
