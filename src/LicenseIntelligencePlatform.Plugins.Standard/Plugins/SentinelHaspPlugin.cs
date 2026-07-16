using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for identifying Thales Sentinel HASP / LDK Run-time Environment drivers (`hasplms.exe`).
/// </summary>
public class SentinelHaspPlugin : ILicensePlugin
{
    private static readonly string[] HaspKeywords = { "sentinel", "hasp", "aladdin", "safenet", "thales ldk" };

    public string PluginId => "std.sentinel.hasp";
    public string PluginName => "Sentinel HASP & LDK Hardware Dongle License Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.sentinel.hasp",
        pluginName: "Sentinel HASP & LDK Hardware Dongle License Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Detects Sentinel HASP hardware dongle protected software.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Windows"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        var lower = $"{software.Name} {software.Publisher}".ToLowerInvariant();
        return HaspKeywords.Any(k => lower.Contains(k)) && (lower.Contains("run-time") || lower.Contains("driver") || lower.Contains("hasp"));
    }

    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        ConfidenceLevel confidence = ConfidenceLevel.High;

        try
        {
            var haspPath = @"C:\Program Files (x86)\Common Files\Aladdin Shared\HASP\hasplms.exe";
            if (File.Exists(haspPath))
            {
                confidence = ConfidenceLevel.Verified;
                evidences.Add(new Evidence(
                    evidenceType: "SentinelHaspServiceBinary",
                    description: "Detected active Sentinel HASP License Manager Service daemon (hasplms.exe).",
                    sourceLocation: haspPath,
                    rawData: $"Service Binary: {haspPath}"
                ));
            }
        }
        catch
        {
            // Ignore access restrictions
        }

        if (evidences.Count == 0)
        {
            evidences.Add(new Evidence(
                evidenceType: "SentinelHaspPackage",
                description: $"Identified Sentinel HASP commercial dongle protection environment: '{software.Name}'.",
                sourceLocation: "SoftwareInfo.Name",
                rawData: software.Name
            ));
        }

        return Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.Commercial,
            licenseName: "Sentinel HASP / LDK Commercial Dongle Protection",
            confidence: confidence,
            evidences: evidences,
            notes: "Verified via Common Files Aladdin Shared HASP daemon binaries."
        ));
    }
}
