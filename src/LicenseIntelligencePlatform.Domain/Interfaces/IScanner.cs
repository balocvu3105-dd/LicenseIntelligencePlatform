using LicenseIntelligencePlatform.Domain.Entities;

namespace LicenseIntelligencePlatform.Domain.Interfaces;

/// <summary>
/// Defines a read-only software inventory scanner that collects data on installed software without making modifications or hidden network calls.
/// </summary>
public interface IScanner
{
    /// <summary>
    /// Gets the unique name of this scanner source (e.g., "WindowsRegistryScanner", "LinuxPackageScanner").
    /// </summary>
    string ScannerName { get; }

    /// <summary>
    /// Determines whether this scanner is supported on the current operating system environment.
    /// </summary>
    /// <returns><c>true</c> if supported on current platform; otherwise, <c>false</c>.</returns>
    bool IsSupportedOnCurrentPlatform();

    /// <summary>
    /// Asynchronously scans the system for installed software packages.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the scan operation.</param>
    /// <returns>A collection of discovered <see cref="SoftwareInfo"/> items.</returns>
    Task<IEnumerable<SoftwareInfo>> ScanAsync(CancellationToken cancellationToken = default);
}
