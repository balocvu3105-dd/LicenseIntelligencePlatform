using LicenseIntelligencePlatform.Domain.Entities;

namespace LicenseIntelligencePlatform.Domain.Interfaces;

/// <summary>
/// Defines a license detection plugin that inspects discovered software to determine license type, confidence, and supporting evidence.
/// Every plugin must expose a <see cref="PluginManifest"/> so the SDK can validate compatibility before execution.
/// </summary>
public interface ILicensePlugin
{
    /// <summary>
    /// Gets the unique identifier of the plugin.
    /// </summary>
    string PluginId { get; }

    /// <summary>
    /// Gets the human-readable name of the plugin.
    /// </summary>
    string PluginName { get; }

    /// <summary>
    /// Gets the plugin manifest containing metadata (version, author, priority, SDK compatibility).
    /// </summary>
    PluginManifest Manifest { get; }

    /// <summary>
    /// Determines whether this plugin can evaluate the specified software package.
    /// </summary>
    /// <param name="software">The software item to inspect.</param>
    /// <returns><c>true</c> if the plugin should run for this software; otherwise, <c>false</c>.</returns>
    bool CanCheck(SoftwareInfo software);

    /// <summary>
    /// Asynchronously performs license check and verification for the specified software package.
    /// </summary>
    /// <param name="software">The software package to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The detailed check result containing confidence and evidence.</returns>
    Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default);
}
