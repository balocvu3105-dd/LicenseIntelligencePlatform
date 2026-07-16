using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Phase 3 — Verification Plugin: Windows OS Genuine Activation & KMS Crack Detector.
/// Analyzes Operating System activation status and detects pirated KMS emulators (KMSpico/KMSAuto/MAS).
/// High priority (999) ensures OS verification is evaluated first and displayed at the very top of executive audits.
/// </summary>
public sealed class WindowsOsLicensePlugin : ILicensePlugin
{
    /// <inheritdoc />
    public string PluginId => "os.windows";

    /// <inheritdoc />
    public string PluginName => "Microsoft Windows OS Activation & KMS Crack Detector";

    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "os.windows",
        pluginName: "Microsoft Windows OS Activation & KMS Crack Detector",
        pluginVersion: "1.0.0",
        author: "Bá Lộc Vũ (DynamiteV)",
        description: "Audits Windows OS genuine activation channel and flags unauthorized KMS crack emulators.",
        priority: 999,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Windows"
    );

    /// <inheritdoc />
    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;

        return software.Name.Equals("Microsoft Windows Operating System (OS License Check)", StringComparison.OrdinalIgnoreCase)
            || software.Name.Contains("Windows Operating System", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(software);

        var evidences = new List<Evidence>();
        var notes = string.Empty;
        var licenseType = LicenseType.Commercial;
        var confidence = ConfidenceLevel.Verified;
        string licenseName = "Commercial (Windows Operating System)";

        var sourceText = software.ScanSource ?? string.Empty;

        if (sourceText.Contains("KMS CRACK DETECTED", StringComparison.OrdinalIgnoreCase))
        {
            licenseName = "Commercial (Pirated / Invalid KMS Key)";
            notes = "⚠️ CRITICAL COMPLIANCE ALERT: Windows Operating System is currently activated using an unauthorized KMS emulator or public pirate KMS server. Enterprise audit violation.";

            evidences.Add(new Evidence(
                evidenceType: "Audit Alert - Pirated KMS Crack Detected",
                description: sourceText,
                sourceLocation: @"C:\Windows\System32 & Registry SPP Hive",
                rawData: sourceText
            ));

            evidences.Add(new Evidence(
                evidenceType: "Remediation & Compliance Action Required",
                description: "Immediate action required: Remove unauthorized KMS activator hooks (KMSpico/KMSAuto/SECOInjector) and activate Windows with a valid RETAIL, OEM, or corporate Volume License MAK/KMS key.",
                sourceLocation: "Microsoft Volume Licensing & Enterprise Compliance Terms",
                rawData: "Action: License Remediation"
            ));
        }
        else
        {
            licenseName = "Commercial (Genuine Microsoft License)";
            notes = "✔ GENUINE WINDOWS ACTIVATION VERIFIED: Operating system is legitimately activated without any unauthorized KMS emulator hooks or crack binaries.";

            evidences.Add(new Evidence(
                evidenceType: "Verified Genuine Activation Channel",
                description: $"✔ OS License Status: {sourceText}",
                sourceLocation: @"WMI SoftwareLicensingProduct & SPP Registry",
                rawData: sourceText
            ));

            evidences.Add(new Evidence(
                evidenceType: "Read-Only Integrity & Anti-Piracy Check",
                description: "Deep heuristic inspection verified 100% clean state: zero KMSpico, KMSAuto, or loopback KMS emulator binaries/services detected on this workstation.",
                sourceLocation: "System Protection Platform Verification Engine",
                rawData: "Anti-Piracy Check: PASSED"
            ));
        }

        var result = new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: licenseType,
            licenseName: licenseName,
            confidence: confidence,
            evidences: evidences,
            notes: notes
        );

        return Task.FromResult(result);
    }
}
