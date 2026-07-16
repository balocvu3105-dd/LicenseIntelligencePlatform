namespace LicenseIntelligencePlatform.Domain;

/// <summary>
/// Provides the current LIP Plugin SDK version constant.
/// All plugins are validated against this version at load time.
/// Increment this when introducing breaking changes to the plugin interface.
/// </summary>
public static class SdkVersion
{
    /// <summary>
    /// The current LIP SDK version string (SemVer).
    /// Plugins with <c>MinSdkVersion</c> greater than this value will be rejected.
    /// Plugins with a non-empty <c>MaxSdkVersion</c> less than this value will be rejected.
    /// </summary>
    public const string Current = "1.0.0";
}
