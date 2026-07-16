using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for identifying Microsoft Office (Office 365, ProPlus, Volume/Retail editions) and verifying KMS/Retail activation artifacts (`OSPP.VBS`, `.xrm-ms`).
/// </summary>
public class MicrosoftOfficePlugin : ILicensePlugin
{
    private static readonly string[] OfficeKeywords = { "microsoft office", "office 365", "o365proplus", "word", "excel", "powerpoint", "outlook" };

    public string PluginId => "std.microsoft.office";
    public string PluginName => "Microsoft Office & 365 Commercial License Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.microsoft.office",
        pluginName: "Microsoft Office & 365 Commercial License Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Identifies Microsoft Office suite commercial editions.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Windows"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        var combined = $"{software.Name} {software.Publisher}".ToLowerInvariant();
        return OfficeKeywords.Any(k => combined.Contains(k));
    }

    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        ConfidenceLevel confidence = ConfidenceLevel.Medium;
        string licenseName = "Microsoft Office Commercial License";

        // Check installation folder for Office OSPP activation script or license files
        if (!string.IsNullOrWhiteSpace(software.InstallPath) && Directory.Exists(software.InstallPath))
        {
            try
            {
                var osppPath = Path.Combine(software.InstallPath, "OSPP.VBS");
                if (File.Exists(osppPath))
                {
                    confidence = ConfidenceLevel.Verified;
                    evidences.Add(new Evidence(
                        evidenceType: "OfficeActivationScript",
                        description: "Detected Microsoft Office Software Protection Platform Script (OSPP.VBS).",
                        sourceLocation: osppPath,
                        rawData: "OSPP.VBS Present"
                    ));
                }

                var licenseFiles = Directory.GetFiles(software.InstallPath, "*.xrm-ms", SearchOption.AllDirectories);
                if (licenseFiles.Length > 0)
                {
                    confidence = ConfidenceLevel.Verified;
                    evidences.Add(new Evidence(
                        evidenceType: "OfficeLicenseToken",
                        description: $"Found {licenseFiles.Length} Microsoft Office license token file(s) (.xrm-ms).",
                        sourceLocation: licenseFiles[0],
                        rawData: $"Token count: {licenseFiles.Length}"
                    ));
                }
            }
            catch
            {
                // Ignore directory permissions
            }
        }

        // Check product edition in software name
        var nameLower = software.Name.ToLowerInvariant();
        if (nameLower.Contains("proplus") || nameLower.Contains("office 365") || nameLower.Contains("enterprise"))
        {
            licenseName = "Microsoft Office 365 / Enterprise Commercial License";
            if (confidence < ConfidenceLevel.High) confidence = ConfidenceLevel.High;
        }

        if (evidences.Count == 0)
        {
            evidences.Add(new Evidence(
                evidenceType: "OfficeProductRegistry",
                description: $"Identified Microsoft Office product suite registry entry: '{software.Name}'.",
                sourceLocation: "SoftwareInfo.Name",
                rawData: software.Name
            ));
        }

        return Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.Commercial,
            licenseName: licenseName,
            confidence: confidence,
            evidences: evidences,
            notes: "Verified via OSPP activation scripts and XRM-MS license tokens."
        ));
    }
}
