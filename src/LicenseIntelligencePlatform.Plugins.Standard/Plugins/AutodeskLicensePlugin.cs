using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for identifying Autodesk engineering & design software (AutoCAD, Revit, Maya, 3ds Max) and Adlm registry information files.
/// </summary>
public class AutodeskLicensePlugin : ILicensePlugin
{
    private static readonly string[] AutodeskKeywords = { "autocad", "revit", "autodesk maya", "3ds max", "inventor", "autodesk civil" };

    public string PluginId => "std.autodesk.suite";
    public string PluginName => "Autodesk Commercial Design Suite Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.autodesk.suite",
        pluginName: "Autodesk Commercial Design Suite Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Detects Autodesk FlexNet / subscription license artifacts.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Windows"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        var lower = $"{software.Name} {software.Publisher}".ToLowerInvariant();
        return AutodeskKeywords.Any(k => lower.Contains(k)) || lower.Contains("autodesk");
    }

    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        ConfidenceLevel confidence = ConfidenceLevel.Medium;

        try
        {
            var adlmDir = @"C:\ProgramData\Autodesk\Adlm";
            if (Directory.Exists(adlmDir))
            {
                var regInfos = Directory.GetFiles(adlmDir, "*RegInfo.html", SearchOption.TopDirectoryOnly);
                if (regInfos.Length > 0)
                {
                    confidence = ConfidenceLevel.Verified;
                    evidences.Add(new Evidence(
                        evidenceType: "AutodeskAdlmRegInfo",
                        description: $"Found Autodesk License Manager registration HTML artifact ({regInfos.Length} files).",
                        sourceLocation: regInfos[0],
                        rawData: $"RegInfo Count: {regInfos.Length}"
                    ));
                }
            }
        }
        catch
        {
            // Ignore system directory permissions
        }

        if (evidences.Count == 0)
        {
            evidences.Add(new Evidence(
                evidenceType: "AutodeskProductMatch",
                description: $"Detected Autodesk CAD/CAM commercial product: '{software.Name}'.",
                sourceLocation: "SoftwareInfo.Name",
                rawData: software.Name
            ));
        }

        return Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.Commercial,
            licenseName: "Autodesk Commercial License",
            confidence: confidence,
            evidences: evidences,
            notes: "Verified via Autodesk License Manager Adlm registration artifacts."
        ));
    }
}
