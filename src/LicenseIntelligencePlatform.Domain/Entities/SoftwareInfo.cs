namespace LicenseIntelligencePlatform.Domain.Entities;

/// <summary>
/// Represents read-only software data discovered on the local host by a scanner.
/// </summary>
public record SoftwareInfo
{
    /// <summary>
    /// Gets the name of the installed software package.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the version string of the software package.
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Gets the publisher or vendor name of the software package.
    /// </summary>
    public string Publisher { get; init; } = string.Empty;

    /// <summary>
    /// Gets the installation directory path on disk, if discovered.
    /// </summary>
    public string InstallPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the installation date if recorded by the system package manager or registry.
    /// </summary>
    public string InstallDate { get; init; } = string.Empty;

    /// <summary>
    /// Gets the source of the scan (e.g., "WindowsRegistry", "LinuxDpkg", "LinuxRpm").
    /// </summary>
    public string ScanSource { get; init; } = string.Empty;

    /// <summary>
    /// Gets the last modified date of the executable/package file (File Update/Patch timestamp).
    /// </summary>
    public string LastModifiedDate { get; init; } = string.Empty;

    /// <summary>
    /// Gets the application launch timestamp if currently executing in RAM (App Start / Open time).
    /// </summary>
    public string AppStartTime { get; init; } = string.Empty;

    /// <summary>
    /// Creates a new instance of <see cref="SoftwareInfo"/>.
    /// </summary>
    public SoftwareInfo(
        string name, 
        string version, 
        string publisher, 
        string installPath = "", 
        string installDate = "", 
        string scanSource = "", 
        string lastModifiedDate = "", 
        string appStartTime = "")
    {
        Name = name;
        Version = version;
        Publisher = publisher;
        InstallPath = installPath;
        InstallDate = FormatDateWithSlash(installDate);
        ScanSource = scanSource;
        LastModifiedDate = FormatDateWithSlash(lastModifiedDate);
        AppStartTime = FormatDateWithSlash(appStartTime);
    }

    private static string FormatDateWithSlash(string dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
        {
            return string.Empty;
        }
        var trimmed = dateStr.Trim();
        if (trimmed.Length == 8 && trimmed.All(char.IsDigit))
        {
            return $"{trimmed.Substring(0, 4)}/{trimmed.Substring(4, 2)}/{trimmed.Substring(6, 2)}";
        }
        if (trimmed.Contains('-'))
        {
            return trimmed.Replace('-', '/');
        }
        return trimmed;
    }
}
