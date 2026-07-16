using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for Python interpreter packages (`Python 3.x`) and Python Software Foundation (PSF) license checks.
/// </summary>
public class PythonEcosystemPlugin : ILicensePlugin
{
    public string PluginId => "std.python.runtime";
    public string PluginName => "Python Runtime & Ecosystem PSF License Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.python.runtime",
        pluginName: "Python Runtime & Ecosystem PSF License Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Identifies CPython PSF open source license artifacts.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Any"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        var lower = software.Name.ToLowerInvariant();
        return lower.StartsWith("python 3.") || lower.StartsWith("python 2.") || lower.Equals("python", StringComparison.OrdinalIgnoreCase);
    }

    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        ConfidenceLevel confidence = ConfidenceLevel.Verified;

        evidences.Add(new Evidence(
            evidenceType: "PythonInterpreterPackage",
            description: $"Identified Python runtime environment package: '{software.Name}'. Distributed under open source PSF License Agreement.",
            sourceLocation: "SoftwareInfo.Name",
            rawData: software.Name
        ));

        return Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.OpenSource,
            licenseName: "Python Software Foundation (PSF) License",
            confidence: confidence,
            evidences: evidences,
            notes: "Verified via standard Python distribution naming conventions."
        ));
    }
}
