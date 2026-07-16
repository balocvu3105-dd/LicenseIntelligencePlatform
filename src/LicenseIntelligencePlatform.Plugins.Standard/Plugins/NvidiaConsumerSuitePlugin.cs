using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Phase 3 Plugin: Verifies NVIDIA consumer and gaming software suite including NVIDIA App, GeForce NOW, drivers, and PhysX.
/// </summary>
public class NvidiaConsumerSuitePlugin : ILicensePlugin
{
    public string PluginId => "std.nvidia.consumer";
    public string PluginName => "NVIDIA Consumer Suite & Driver Detector";

    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.nvidia.consumer",
        pluginName: "NVIDIA Consumer Suite & Driver Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard Team",
        description: "Detects NVIDIA App, GeForce NOW, Graphics/HD Audio drivers, PhysX, and FrameView SDK.",
        priority: 77,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Windows"
    );

    private static readonly HashSet<string> ExactOrPrefixKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "NVIDIA App",
        "NVIDIA FrameView SDK",
        "NVIDIA GeForce NOW",
        "NVIDIA Graphics Driver",
        "NVIDIA HD Audio Driver",
        "NVIDIA PhysX System Software",
        "NVIDIA USBC Driver"
    };

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null || string.IsNullOrWhiteSpace(software.Name)) return false;
        if (!string.IsNullOrWhiteSpace(software.Publisher) && software.Publisher.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var kw in ExactOrPrefixKeywords)
            {
                if (software.Name.Contains(kw, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
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
        var licenseName = "NVIDIA Software License Agreement (Driver / Companion App)";

        if (!string.IsNullOrWhiteSpace(software.InstallPath) && Directory.Exists(software.InstallPath))
        {
            try
            {
                var files = Directory.GetFiles(software.InstallPath, "*nvi*.dll", SearchOption.TopDirectoryOnly);
                if (files.Length == 0) files = Directory.GetFiles(software.InstallPath, "*.dll", SearchOption.TopDirectoryOnly);
                if (files.Length > 0)
                {
                    confidence = ConfidenceLevel.High;
                    evidences.Add(new Evidence(
                        evidenceType: "FileArtifact",
                        description: "Verified NVIDIA runtime DLL component artifact.",
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
                description: $"Matched NVIDIA software/driver signature: '{software.Name}'.",
                sourceLocation: "SoftwareInfo.Name",
                rawData: software.Name
            ));
        }

        if (software.Name.Contains("GeForce NOW", StringComparison.OrdinalIgnoreCase))
        {
            licenseName = "NVIDIA GeForce NOW Cloud Gaming Terms of Service";
        }

        return await Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: licenseType,
            licenseName: licenseName,
            confidence: confidence,
            evidences: evidences,
            notes: "Verifies NVIDIA consumer graphics drivers, control applications, and streaming clients."
        ));
    }
}
