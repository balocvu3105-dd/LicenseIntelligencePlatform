using LicenseIntelligencePlatform.Domain;
using LicenseIntelligencePlatform.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LicenseIntelligencePlatform.Application.Services;

/// <summary>
/// Validates that plugins are compatible with the current LIP SDK version before they are executed.
/// Incompatible plugins are skipped with a warning — they never cause a crash (Rule 9).
/// </summary>
public class PluginCompatibilityValidator
{
    private readonly ILogger<PluginCompatibilityValidator> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="PluginCompatibilityValidator"/>.
    /// </summary>
    public PluginCompatibilityValidator(ILogger<PluginCompatibilityValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Filters the supplied list of plugins to those that are compatible with
    /// <see cref="SdkVersion.Current"/>.
    /// </summary>
    /// <param name="plugins">All registered plugins.</param>
    /// <returns>Only compatible plugins, ordered by descending Priority.</returns>
    public IReadOnlyList<ILicensePlugin> FilterCompatible(IEnumerable<ILicensePlugin> plugins)
    {
        if (plugins == null) throw new ArgumentNullException(nameof(plugins));

        var compatible = new List<ILicensePlugin>();

        foreach (var plugin in plugins)
        {
            try
            {
                var manifest = plugin.Manifest;

                if (!IsCompatible(manifest.MinSdkVersion, manifest.MaxSdkVersion))
                {
                    _logger.LogWarning(
                        "[SDK Compat] Plugin '{PluginName}' ({PluginId}) v{PluginVersion} requires SDK [{MinSdk}, {MaxSdk}] " +
                        "but current SDK is {CurrentSdk}. Plugin will be skipped.",
                        manifest.PluginName, manifest.PluginId, manifest.PluginVersion,
                        manifest.MinSdkVersion, string.IsNullOrEmpty(manifest.MaxSdkVersion) ? "∞" : manifest.MaxSdkVersion,
                        SdkVersion.Current);
                    continue;
                }

                compatible.Add(plugin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[SDK Compat] Plugin '{PluginId}' threw an exception while reading its Manifest. Plugin will be skipped.",
                    plugin.PluginId);
            }
        }

        // Order by Priority descending so specialised high-priority plugins run first
        var ordered = compatible
            .OrderByDescending(p => p.Manifest.Priority)
            .ToList()
            .AsReadOnly();

        _logger.LogInformation(
            "[SDK Compat] Plugin compatibility check complete. {CompatibleCount}/{TotalCount} plugins accepted for SDK v{CurrentSdk}.",
            ordered.Count, compatible.Count + (plugins.Count() - compatible.Count), SdkVersion.Current);

        return ordered;
    }

    private static bool IsCompatible(string minSdk, string maxSdk)
    {
        // Parse SemVer – only compare Major.Minor.Patch integers for simplicity
        if (!TryParseVersion(SdkVersion.Current, out var current)) return true;

        if (!string.IsNullOrWhiteSpace(minSdk) && TryParseVersion(minSdk, out var min))
        {
            if (current < min) return false;
        }

        if (!string.IsNullOrWhiteSpace(maxSdk) && TryParseVersion(maxSdk, out var max))
        {
            if (current > max) return false;
        }

        return true;
    }

    private static bool TryParseVersion(string versionStr, out Version version)
    {
        version = new Version(0, 0, 0);
        if (string.IsNullOrWhiteSpace(versionStr)) return false;
        return Version.TryParse(versionStr, out version!);
    }
}
