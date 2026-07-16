using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Phase 3 Plugin: Verifies Anti-Cheat frameworks and system security solutions including Riot Vanguard, AntiCheatExpert, and Windows Defender.
/// </summary>
public class AntiCheatAndSecurityPlugin : ILicensePlugin
{
    public string PluginId => "std.security.anticheat";
    public string PluginName => "Anti-Cheat & System Security Suite Detector";

    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.security.anticheat",
        pluginName: "Anti-Cheat & System Security Suite Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard Team",
        description: "Detects Riot Vanguard, AntiCheatExpert, BattlEye, EasyAntiCheat, and system defense suites.",
        priority: 79,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Windows"
    );

    private static readonly HashSet<string> ExactOrPrefixKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "AntiCheatExpert",
        "Riot Vanguard",
        "Vanguard Tray",
        "Windows Defender",
        "EasyAntiCheat",
        "BattlEye"
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
        var confidence = ConfidenceLevel.High;
        var licenseType = LicenseType.Freeware;
        var licenseName = "Proprietary Anti-Cheat / Security Driver EULA";

        if (!string.IsNullOrWhiteSpace(software.InstallPath) && Directory.Exists(software.InstallPath))
        {
            try
            {
                var files = Directory.GetFiles(software.InstallPath, "*.sys", SearchOption.AllDirectories);
                if (files.Length == 0) files = Directory.GetFiles(software.InstallPath, "*.exe", SearchOption.TopDirectoryOnly);
                if (files.Length > 0)
                {
                    confidence = ConfidenceLevel.Verified;
                    evidences.Add(new Evidence(
                        evidenceType: "FileArtifact",
                        description: "Detected kernel driver / security daemon binary in installation directory.",
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
                description: $"Matched security / anti-cheat component signature: '{software.Name}'.",
                sourceLocation: "SoftwareInfo.Name",
                rawData: software.Name
            ));
        }

        return await Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: licenseType,
            licenseName: licenseName,
            confidence: confidence,
            evidences: evidences,
            notes: "Verifies system integrity enforcement drivers and anti-tamper security platforms."
        ));
    }
}
