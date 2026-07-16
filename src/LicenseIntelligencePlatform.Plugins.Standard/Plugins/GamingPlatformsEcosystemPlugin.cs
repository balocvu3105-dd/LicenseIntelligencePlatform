using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Phase 3 Plugin: Verifies Gaming platform clients, launchers, anti-cheat services, and game distribution licenses.
/// </summary>
public class GamingPlatformsEcosystemPlugin : ILicensePlugin
{
    public string PluginId => "std.gaming.ecosystem";
    public string PluginName => "Gaming Platforms & Distribution Ecosystem Detector";

    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.gaming.ecosystem",
        pluginName: "Gaming Platforms & Distribution Ecosystem Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard Team",
        description: "Detects Steam, Epic Games, Riot, Rockstar, Overwolf, and popular game packages.",
        priority: 78,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Windows"
    );

    private static readonly HashSet<string> ExactOrPrefixKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Steam",
        "Steam Client WebHelper",
        "Epic Games Launcher",
        "Epic Online Services",
        "Riot Client",
        "Rockstar Games Launcher",
        "Rockstar Games SDK",
        "Overwolf",
        "TFTactics",
        "Paradox Launcher v2",
        "Cities: Skylines",
        "Counter-Strike 2",
        "Grand Theft Auto V Legacy",
        "Green Hell",
        "Palworld",
        "Stardew Valley",
        "Sid Meier's Civilization VI",
        "Wuthering Waves",
        "GameSDK Service",
        "Liên Minh Huyền Thoại",
        "Source SDK Base 2007"
    };

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null || string.IsNullOrWhiteSpace(software.Name)) return false;
        if (!string.IsNullOrWhiteSpace(software.Publisher))
        {
            var pub = software.Publisher;
            if (pub.Contains("Valve", StringComparison.OrdinalIgnoreCase) ||
                pub.Contains("Epic Games", StringComparison.OrdinalIgnoreCase) ||
                pub.Contains("Riot Games", StringComparison.OrdinalIgnoreCase) ||
                pub.Contains("Rockstar Games", StringComparison.OrdinalIgnoreCase) ||
                pub.Contains("Overwolf", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
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
        var licenseName = "Game Client / Launcher EULA";

        if (!string.IsNullOrWhiteSpace(software.InstallPath) && Directory.Exists(software.InstallPath))
        {
            try
            {
                var files = Directory.GetFiles(software.InstallPath, "*steam_appid.txt", SearchOption.AllDirectories);
                if (files.Length == 0) files = Directory.GetFiles(software.InstallPath, "*eula*.txt", SearchOption.TopDirectoryOnly);
                if (files.Length > 0)
                {
                    confidence = ConfidenceLevel.High;
                    evidences.Add(new Evidence(
                        evidenceType: "FileArtifact",
                        description: "Detected game client distribution metadata artifact in install directory.",
                        sourceLocation: files[0],
                        rawData: "Game Distribution Identifier / EULA"
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
                description: $"Matched gaming launcher / application signature: '{software.Name}'.",
                sourceLocation: "SoftwareInfo.Name",
                rawData: software.Name
            ));
        }

        var lower = software.Name.ToLowerInvariant();
        if (lower.Contains("counter-strike") || lower.Contains("grand theft auto") || lower.Contains("cities: skylines") ||
            lower.Contains("palworld") || lower.Contains("stardew") || lower.Contains("civilization") || lower.Contains("green hell") || lower.Contains("wuthering waves"))
        {
            licenseType = LicenseType.Commercial;
            licenseName = "Commercial Digital Game License (Steam / Epic / Store)";
        }
        else if (lower.Contains("steam") || lower.Contains("epic games") || lower.Contains("riot client") || lower.Contains("overwolf") || lower.Contains("launcher"))
        {
            licenseType = LicenseType.Freeware;
            licenseName = "Free Client Launcher / Platform Service EULA";
        }

        return await Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: licenseType,
            licenseName: licenseName,
            confidence: confidence,
            evidences: evidences,
            notes: "Analyzes digital distribution clients, launchers, and licensed entertainment software."
        ));
    }
}
