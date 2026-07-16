using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for Cisco AnyConnect Secure Client enterprise endpoint VPN licensing.
/// </summary>
public class CiscoAnyConnectPlugin : ILicensePlugin
{
    public string PluginId => "std.cisco.anyconnect";
    public string PluginName => "Cisco AnyConnect / Secure Client Enterprise License Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.cisco.anyconnect",
        pluginName: "Cisco AnyConnect / Secure Client Enterprise License Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Identifies Cisco AnyConnect VPN commercial enterprise license.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Windows"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        var lower = software.Name.ToLowerInvariant();
        return lower.Contains("anyconnect") || lower.Contains("cisco secure client");
    }

    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        evidences.Add(new Evidence(
            evidenceType: "CiscoSecureClientPackage",
            description: $"Detected Cisco AnyConnect / Secure Client: '{software.Name}'. Requires Cisco Apex/Plus enterprise subscription.",
            sourceLocation: "SoftwareInfo.Name",
            rawData: software.Name
        ));

        return Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.Commercial,
            licenseName: "Cisco AnyConnect / Secure Client Commercial Subscription",
            confidence: ConfidenceLevel.High,
            evidences: evidences,
            notes: "Flagged Cisco VPN client for Apex/Plus license seat reconciliation."
        ));
    }
}
