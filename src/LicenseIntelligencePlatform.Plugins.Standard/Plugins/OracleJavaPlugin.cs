using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for identifying Oracle Java JDK/JRE commercial OTN subscription vs OpenJDK GPLv2+CE builds.
/// </summary>
public class OracleJavaPlugin : ILicensePlugin
{
    public string PluginId => "std.oracle.java";
    public string PluginName => "Oracle Java SE Commercial Subscription Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.oracle.java",
        pluginName: "Oracle Java SE Commercial Subscription Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Detects Oracle JDK commercial release vs OpenJDK open source.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Any"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        var lower = $"{software.Name} {software.Publisher}".ToLowerInvariant();
        return lower.Contains("java se") || lower.Contains("oracle java") || lower.Contains("jdk") || lower.Contains("openjdk");
    }

    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        ConfidenceLevel confidence = ConfidenceLevel.High;
        var nameLower = software.Name.ToLowerInvariant();
        bool isOpenJdk = nameLower.Contains("openjdk") || nameLower.Contains("temurin") || nameLower.Contains("corretto");

        evidences.Add(new Evidence(
            evidenceType: "JavaDistributionPackage",
            description: $"Detected Java development/runtime environment: '{software.Name}'. OpenJDK builds use GPLv2+CE; Oracle Java SE requires commercial subscription.",
            sourceLocation: "SoftwareInfo.Name",
            rawData: software.Name
        ));

        return Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: isOpenJdk ? LicenseType.OpenSource : LicenseType.Commercial,
            licenseName: isOpenJdk ? "OpenJDK GPLv2 with Classpath Exception" : "Oracle Java SE Commercial Subscription License",
            confidence: confidence,
            evidences: evidences,
            notes: isOpenJdk ? "Identified open source OpenJDK distribution." : "Flagged Oracle Java SE commercial licensing requirement."
        ));
    }
}
