using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Phase 3 Plugin: Verifies Hardware, OEM, ASUS Armoury Crate, Peripheral utilities, and customization software.
/// </summary>
public class HardwareOemUtilitiesPlugin : ILicensePlugin
{
    public string PluginId => "std.hardware.oem";
    public string PluginName => "Hardware & OEM Utility Suite Detector";

    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.hardware.oem",
        pluginName: "Hardware & OEM Utility Suite Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard Team",
        description: "Detects ASUS Armoury Crate, AURA, ROG, NGENUITY, INPHIC, TUF, and system wallpaper tools.",
        priority: 73,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Windows"
    );

    private static readonly HashSet<string> ExactOrPrefixKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Armoury Crate Service",
        "AURA lighting effect add-on",
        "AURA Service",
        "Aura Wallpaper HTML",
        "Aura Wallpaper Service",
        "ROG Live Service",
        "NGENUITY",
        "INPHIC",
        "inphic driver",
        "TUF GAMING K1",
        "TranslucentTB",
        "Wallpaper Engine",
        "AOMEI Partition Assistant"
    };

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null || string.IsNullOrWhiteSpace(software.Name)) return false;
        if (!string.IsNullOrWhiteSpace(software.Publisher))
        {
            var pub = software.Publisher;
            if (pub.Contains("ASUSTeK", StringComparison.OrdinalIgnoreCase) ||
                pub.Contains("INPHIC", StringComparison.OrdinalIgnoreCase) ||
                pub.Contains("Kingston", StringComparison.OrdinalIgnoreCase) ||
                pub.Contains("HyperX", StringComparison.OrdinalIgnoreCase) ||
                pub.Contains("AOMEI", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        foreach (var kw in ExactOrPrefixKeywords)
        {
            if (software.Name.StartsWith(kw, StringComparison.OrdinalIgnoreCase) || software.Name.Contains(kw, StringComparison.OrdinalIgnoreCase))
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
        var licenseName = "Hardware Companion / OEM Bundled Software License";

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
                        description: "Found hardware utility / service binary executable.",
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
                description: $"Matched hardware/OEM utility name pattern: '{software.Name}'.",
                sourceLocation: "SoftwareInfo.Name",
                rawData: software.Name
            ));
        }

        if (software.Name.Contains("Wallpaper Engine", StringComparison.OrdinalIgnoreCase) || software.Name.Contains("AOMEI Partition Assistant", StringComparison.OrdinalIgnoreCase))
        {
            licenseType = LicenseType.Commercial;
            licenseName = "Commercial Desktop / Utility License";
        }
        else if (software.Name.Contains("TranslucentTB", StringComparison.OrdinalIgnoreCase))
        {
            licenseType = LicenseType.OpenSource;
            licenseName = "MIT License (TranslucentTB Open Source)";
        }

        return await Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: licenseType,
            licenseName: licenseName,
            confidence: confidence,
            evidences: evidences,
            notes: "Identifies motherboard, RGB lighting, peripheral control, and desktop utilities."
        ));
    }
}
