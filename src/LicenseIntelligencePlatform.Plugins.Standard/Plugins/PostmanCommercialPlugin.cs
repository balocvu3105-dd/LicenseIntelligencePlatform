using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for Postman API testing suite commercial & team enterprise subscription license evaluation.
/// </summary>
public class PostmanCommercialPlugin : ILicensePlugin
{
    public string PluginId => "std.postman.client";
    public string PluginName => "Postman API Platform Commercial & Subscription Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.postman.client",
        pluginName: "Postman API Platform Commercial & Subscription Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Identifies Postman commercial subscription license.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Any"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        return software.Name.Contains("Postman", StringComparison.OrdinalIgnoreCase);
    }

    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        ConfidenceLevel confidence = ConfidenceLevel.Medium;

        evidences.Add(new Evidence(
            evidenceType: "PostmanClientInstallation",
            description: $"Detected Postman API client installation: '{software.Name}'. Requires evaluation against Postman Team/Enterprise seat count restrictions.",
            sourceLocation: "SoftwareInfo.Name",
            rawData: software.Name
        ));

        return Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.Commercial,
            licenseName: "Postman Commercial / Enterprise Subscription License",
            confidence: confidence,
            evidences: evidences,
            notes: "Flagged as commercial subscription evaluation required for organizational seat usage."
        ));
    }
}
