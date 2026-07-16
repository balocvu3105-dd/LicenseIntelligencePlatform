namespace LicenseIntelligencePlatform.Domain.Entities;

/// <summary>
/// Represents the standardised metadata descriptor for a license detection plugin.
/// Every plugin must expose a <see cref="PluginManifest"/> so the Core can validate
/// compatibility and ordering before execution.
/// </summary>
public record PluginManifest
{
    /// <summary>Gets the unique plugin identifier (e.g. "std.git.opensource").</summary>
    public string PluginId { get; init; } = string.Empty;

    /// <summary>Gets the human-readable name of the plugin.</summary>
    public string PluginName { get; init; } = string.Empty;

    /// <summary>Gets the SemVer version string of the plugin (e.g. "1.0.0").</summary>
    public string PluginVersion { get; init; } = "1.0.0";

    /// <summary>Gets the author or team that developed the plugin.</summary>
    public string Author { get; init; } = string.Empty;

    /// <summary>Gets a brief description of what the plugin detects.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the execution priority. Higher value = runs first.
    /// Default is 100 (normal). Specialised plugins should use a higher value (e.g. 200)
    /// so they can short-circuit before generic heuristic plugins run.
    /// </summary>
    public int Priority { get; init; } = 100;

    /// <summary>Gets the minimum LIP SDK version this plugin requires (inclusive).</summary>
    public string MinSdkVersion { get; init; } = "1.0.0";

    /// <summary>
    /// Gets the maximum LIP SDK version this plugin supports (inclusive).
    /// Leave as empty string to mean "no upper bound".
    /// </summary>
    public string MaxSdkVersion { get; init; } = string.Empty;

    /// <summary>Gets the operating systems this plugin can run on (e.g. "Windows", "Linux", "Any").</summary>
    public string SupportedOs { get; init; } = "Any";

    /// <summary>
    /// Creates a new instance of <see cref="PluginManifest"/>.
    /// </summary>
    public PluginManifest(
        string pluginId,
        string pluginName,
        string pluginVersion,
        string author,
        string description,
        int priority = 100,
        string minSdkVersion = "1.0.0",
        string maxSdkVersion = "",
        string supportedOs = "Any")
    {
        PluginId = pluginId;
        PluginName = pluginName;
        PluginVersion = pluginVersion;
        Author = author;
        Description = description;
        Priority = priority;
        MinSdkVersion = minSdkVersion;
        MaxSdkVersion = maxSdkVersion;
        SupportedOs = supportedOs;
    }
}
