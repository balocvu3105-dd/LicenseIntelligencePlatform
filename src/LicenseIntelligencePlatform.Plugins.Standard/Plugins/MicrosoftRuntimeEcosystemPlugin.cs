using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Phase 3 Plugin: Verifies Microsoft Runtime ecosystem components including .NET Runtimes/SDKs, Visual C++ Redistributables, ASP.NET Core, XNA, and SQL Server drivers.
/// </summary>
public class MicrosoftRuntimeEcosystemPlugin : ILicensePlugin
{
    public string PluginId => "std.microsoft.runtimes";
    public string PluginName => "Microsoft Runtime Ecosystem License Detector";

    /// <inheritdoc />
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "std.microsoft.runtimes",
        pluginName: "Microsoft Runtime Ecosystem License Detector",
        pluginVersion: "1.0.0",
        author: "LIP Standard Team",
        description: "Detects and verifies Microsoft .NET Runtimes, SDKs, Visual C++ Redistributables, and database/OLE DB drivers.",
        priority: 75,
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Windows"
    );

    private static readonly HashSet<string> ExactOrPrefixKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Microsoft .NET Runtime",
        "Microsoft .NET SDK",
        "Microsoft ASP.NET Core",
        "Microsoft Windows Desktop Runtime",
        "Microsoft Visual C++",
        "Microsoft XNA Framework",
        "Microsoft MPI",
        "Microsoft Web Deploy",
        "Microsoft ODBC Driver",
        "Microsoft OLE DB Driver",
        "vs_CoreEditorFonts",
        "Microsoft Windows Operating System",
        "Microsoft® Windows® Operating System",
        "VBCSCompiler",
        "IIS 10.0 Express",
        "IIS"
    };

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null || string.IsNullOrWhiteSpace(software.Name)) return false;
        if (!string.IsNullOrWhiteSpace(software.Publisher) && software.Publisher.Contains("Microsoft", StringComparison.OrdinalIgnoreCase))
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
        var licenseName = "Microsoft Runtime & Redistributable EULA";

        if (!string.IsNullOrWhiteSpace(software.InstallPath) && Directory.Exists(software.InstallPath))
        {
            try
            {
                var files = Directory.GetFiles(software.InstallPath, "*LICENSE*", SearchOption.TopDirectoryOnly);
                if (files.Length == 0) files = Directory.GetFiles(software.InstallPath, "*EULA*", SearchOption.TopDirectoryOnly);
                if (files.Length > 0)
                {
                    var licFile = files[0];
                    var content = await File.ReadAllTextAsync(licFile, cancellationToken);
                    confidence = ConfidenceLevel.Verified;
                    evidences.Add(new Evidence(
                        evidenceType: "FileArtifact",
                        description: "Found Microsoft EULA/LICENSE artifact in runtime directory.",
                        sourceLocation: licFile,
                        rawData: content.Length > 100 ? content[..100] : content
                    ));
                }
            }
            catch
            {
                // Ignore file system errors
            }
        }

        if (evidences.Count == 0)
        {
            evidences.Add(new Evidence(
                evidenceType: "KeywordMatch",
                description: $"Matched Microsoft Runtime package pattern: '{software.Name}'.",
                sourceLocation: "SoftwareInfo.Name",
                rawData: software.Name
            ));
        }

        if (software.Name.Contains(".NET SDK", StringComparison.OrdinalIgnoreCase) || software.Name.Contains("CoreEditorFonts", StringComparison.OrdinalIgnoreCase))
        {
            licenseType = LicenseType.OpenSource;
            licenseName = "MIT License (.NET Foundation / Microsoft Open Source)";
        }

        return new LicenseCheckResult(
            pluginId: PluginId,
            pluginName: PluginName,
            software: software,
            detectedLicenseType: licenseType,
            licenseName: licenseName,
            confidence: confidence,
            evidences: evidences,
            notes: "Verifies Microsoft system runtimes, SDKs, redistributable C++ libraries, and database drivers."
        );
    }
}
