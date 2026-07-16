using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Phase 3 Plugin: Verifies Web browsers, SQLite/database browsers, input method editors (UniKey), emulators, and system utilities.
/// </summary>
public class WebBrowsersAndToolsPlugin : ILicensePlugin
{
    public string PluginId => "std.browsers.tools";
    public string PluginName => "Web Browsers & System Utilities Detector";

    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.browsers.tools",
        pluginName: "Web Browsers & System Utilities Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard Team",
        description: "Detects Google Chrome, Microsoft Edge, DB Browser for SQLite, DBeaver, UniKey, LDPlayer, and Snipping Tool.",
        priority: 74,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Any"
    );

    private static readonly HashSet<string> ExactOrPrefixKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Google",
        "Google Chrome",
        "Microsoft Edge",
        "Microsoft Edge WebView2",
        "DB Browser for SQLite",
        "DBeaver",
        "UniKey",
        "LDPlayer 9",
        "Snipping Tool",
        "Microsoft GameInput",
        "Travel Guide",
        "Widgets Platform Runtime",
        "Intel(R) Computing Improvement Program",
        "Intel(R) Wireless Bluetooth(R)",
        "Intel® Driver & Support Assistant",
        "OP.GG",
        "Xbox App",
        "Antigravity IDE",
        "GameInput",
        "LicenseIntelligencePlatform"
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
        var licenseName = "Freeware / Browser Terms of Service";

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
                        description: "Found installed browser / system tool executable binary.",
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
                description: $"Matched browser or system utility name: '{software.Name}'.",
                sourceLocation: "SoftwareInfo.Name",
                rawData: software.Name
            ));
        }

        if (software.Name.Contains("DB Browser for SQLite", StringComparison.OrdinalIgnoreCase) ||
            software.Name.Contains("DBeaver", StringComparison.OrdinalIgnoreCase) ||
            software.Name.Contains("UniKey", StringComparison.OrdinalIgnoreCase))
        {
            licenseType = LicenseType.OpenSource;
            licenseName = "GPL / Apache Open Source License";
        }
        else if (software.Name.Contains("Google Chrome", StringComparison.OrdinalIgnoreCase) ||
                 software.Name.Contains("Microsoft Edge", StringComparison.OrdinalIgnoreCase))
        {
            licenseType = LicenseType.Freeware;
            licenseName = "Chromium Based Freeware Terms of Service";
        }

        return await Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: licenseType,
            licenseName: licenseName,
            confidence: confidence,
            evidences: evidences,
            notes: "Identifies web navigation clients, open source database tools, and desktop input utilities."
        ));
    }
}
