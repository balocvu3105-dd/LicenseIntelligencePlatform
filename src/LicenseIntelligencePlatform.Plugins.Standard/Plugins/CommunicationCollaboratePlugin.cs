using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Phase 3 Plugin: Verifies Communication, Collaboration, Remote Access, and AI Copilot client applications.
/// </summary>
public class CommunicationCollaboratePlugin : ILicensePlugin
{
    public string PluginId => "std.comm.collaborate";
    public string PluginName => "Communication & Collaboration Client Detector";

    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.comm.collaborate",
        pluginName: "Communication & Collaboration Client Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard Team",
        description: "Detects Discord, Zalo, OneDrive, Phone Link, VPN/Remote clients, and Copilot tools.",
        priority: 76,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Any"
    );

    private static readonly HashSet<string> ExactOrPrefixKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Discord",
        "Zalo",
        "Microsoft OneDrive",
        "Microsoft OneDrive Sync Service",
        "Microsoft Phone Link",
        "CrossDeviceService",
        "Cloudflare One Client",
        "Radmin VPN",
        "UltraViewer",
        "Copilot",
        "Microsoft 365 Copilot",
        "Virtual Assistant"
    };

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null || string.IsNullOrWhiteSpace(software.Name)) return false;
        foreach (var kw in ExactOrPrefixKeywords)
        {
            if (software.Name.Contains(kw, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    public async Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        var confidence = ConfidenceLevel.Medium;
        var licenseType = LicenseType.Freeware;
        var licenseName = "Client Application / Service Terms of Use";

        if (!string.IsNullOrWhiteSpace(software.InstallPath) && Directory.Exists(software.InstallPath))
        {
            try
            {
                var files = Directory.GetFiles(software.InstallPath, "*.exe", SearchOption.TopDirectoryOnly);
                if (files.Length > 0)
                {
                    confidence = ConfidenceLevel.High;
                    evidences.Add(new Evidence(
                        evidenceType: "FileArtifact",
                        description: "Verified installed communication / remote binary executable.",
                        sourceLocation: files[0],
                        rawData: Path.GetFileName(files[0])
                    ));
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
                evidenceType: "KeywordMatch",
                description: $"Matched communication/collaboration package: '{software.Name}'.",
                sourceLocation: "SoftwareInfo.Name",
                rawData: software.Name
            ));
        }

        if (software.Name.Contains("Microsoft 365 Copilot", StringComparison.OrdinalIgnoreCase))
        {
            licenseType = LicenseType.Commercial;
            licenseName = "Microsoft 365 Commercial Copilot Subscription";
        }
        else if (software.Name.Contains("Discord", StringComparison.OrdinalIgnoreCase) || software.Name.Contains("Zalo", StringComparison.OrdinalIgnoreCase))
        {
            licenseType = LicenseType.Freeware;
            licenseName = "Freeware / Community Client EULA";
        }

        return await Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: licenseType,
            licenseName: licenseName,
            confidence: confidence,
            evidences: evidences,
            notes: "Verifies desktop messaging, cloud synchronization, and remote assistance client tools."
        ));
    }
}
