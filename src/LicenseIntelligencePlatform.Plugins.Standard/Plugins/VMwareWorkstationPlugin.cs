using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Specialized plugin for identifying VMware commercial hypervisor products (Workstation Pro/Player, vSphere Client) and `.lic` files.
/// </summary>
public class VMwareWorkstationPlugin : ILicensePlugin
{
    private static readonly string[] VMwareKeywords = { "vmware workstation", "vmware esxi", "vmware vsphere", "vmware fusion" };

    public string PluginId => "std.vmware.workstation";
    public string PluginName => "VMware Commercial Hypervisor & Virtualization Detector";
    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.vmware.workstation",
        pluginName: "VMware Commercial Hypervisor & Virtualization Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard",
        description: "Detects VMware Workstation Pro/Player commercial licenses.",
        priority: 200,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Windows"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;
        var lower = $"{software.Name} {software.Publisher}".ToLowerInvariant();
        return VMwareKeywords.Any(k => lower.Contains(k)) || lower.Contains("vmware, inc.");
    }

    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        ConfidenceLevel confidence = ConfidenceLevel.Medium;

        try
        {
            var licPaths = new[]
            {
                @"C:\ProgramData\VMware\VMware Workstation",
                @"C:\ProgramData\VMware"
            };

            foreach (var dir in licPaths)
            {
                if (Directory.Exists(dir))
                {
                    var lics = Directory.GetFiles(dir, "license-*.lic", SearchOption.AllDirectories);
                    if (lics.Length > 0)
                    {
                        confidence = ConfidenceLevel.Verified;
                        evidences.Add(new Evidence(
                            evidenceType: "VMwareLicenseFile",
                            description: $"Detected VMware Workstation commercial license file '{Path.GetFileName(lics[0])}'.",
                            sourceLocation: lics[0],
                            rawData: $"File: {Path.GetFileName(lics[0])}"
                        ));
                        break;
                    }
                }
            }
        }
        catch
        {
            // Ignore access errors
        }

        if (evidences.Count == 0)
        {
            evidences.Add(new Evidence(
                evidenceType: "VMwareProductRegistry",
                description: $"Identified VMware hypervisor software package: '{software.Name}'.",
                sourceLocation: "SoftwareInfo.Name",
                rawData: software.Name
            ));
        }

        return Task.FromResult(new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: LicenseType.Commercial,
            licenseName: "VMware Commercial Workstation License",
            confidence: confidence,
            evidences: evidences,
            notes: "Verified via ProgramData VMware license files."
        ));
    }
}
