using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for identifying JetBrains IDEs (Rider, IntelliJ IDEA, PyCharm, WebStorm, Resharper) and subscription keys (`*.key`).
/// </summary>
public class JetBrainsIdesPlugin : ILicensePlugin
{
    private static readonly string[] JetBrainsKeywords = { "jetbrains", "intellij", "rider", "pycharm", "webstorm", "clion", "goland", "rubymine", "resharper" };

    public string PluginId => "std.jetbrains.ides";
    public string PluginName => "JetBrains Commercial IDE Suite Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.jetbrains.ides",
        pluginName: "JetBrains Commercial IDE Suite Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Detects JetBrains IDE toolbox subscription license files.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Any"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        var lower = $"{software.Name} {software.Publisher}".ToLowerInvariant();
        return JetBrainsKeywords.Any(k => lower.Contains(k));
    }

    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        ConfidenceLevel confidence = ConfidenceLevel.High;
        string licenseName = "JetBrains Commercial IDE License";

        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var jbDir = Path.Combine(appData, "JetBrains");
            if (Directory.Exists(jbDir))
            {
                var keys = Directory.GetFiles(jbDir, "*.key", SearchOption.AllDirectories);
                if (keys.Length > 0)
                {
                    confidence = ConfidenceLevel.Verified;
                    evidences.Add(new Evidence(
                        evidenceType: "JetBrainsLicenseKey",
                        description: $"Detected JetBrains commercial IDE subscription key file ({keys.Length} present).",
                        sourceLocation: keys[0],
                        rawData: $"Key File: {Path.GetFileName(keys[0])}"
                    ));
                }
            }
        }
        catch
        {
            // Ignore directory errors
        }

        var nameLower = software.Name.ToLowerInvariant();
        if (nameLower.Contains("community"))
        {
            licenseName = "JetBrains Community Edition / Open Source License";
            confidence = ConfidenceLevel.Verified;
        }

        if (evidences.Count == 0)
        {
            evidences.Add(new Evidence(
                evidenceType: "JetBrainsProductMatch",
                description: $"Identified JetBrains IDE product: '{software.Name}'.",
                sourceLocation: "SoftwareInfo.Name",
                rawData: software.Name
            ));
        }

        return Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: nameLower.Contains("community") ? LicenseType.Freeware : LicenseType.Commercial,
            licenseName: licenseName,
            confidence: confidence,
            evidences: evidences,
            notes: "Verified via AppData JetBrains subscription key checks and Community edition markers."
        ));
    }
}
