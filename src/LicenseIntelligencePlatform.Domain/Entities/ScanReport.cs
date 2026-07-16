namespace LicenseIntelligencePlatform.Domain.Entities;

/// <summary>
/// Aggregated report of a complete inventory scan and license verification session.
/// </summary>
public record ScanReport
{
    /// <summary>
    /// Gets the unique session identifier for this scan report.
    /// </summary>
    public string ScanId { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets the host name where the scan took place.
    /// </summary>
    public string HostName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the operating system description string.
    /// </summary>
    public string OSDescription { get; init; } = string.Empty;

    /// <summary>
    /// Gets the UTC timestamp when the scan started.
    /// </summary>
    public DateTime StartedAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the UTC timestamp when the scan completed.
    /// </summary>
    public DateTime CompletedAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the total number of software items discovered by scanners.
    /// </summary>
    public int TotalSoftwareScanned { get; init; }

    /// <summary>
    /// Gets the total number of active plugins that evaluated the software inventory.
    /// </summary>
    public int TotalPluginsExecuted { get; init; }

    /// <summary>
    /// Gets all license check results generated across the software inventory.
    /// </summary>
    public IReadOnlyList<LicenseCheckResult> Results { get; init; } = Array.Empty<LicenseCheckResult>();

    /// <summary>
    /// Creates a new instance of <see cref="ScanReport"/>.
    /// </summary>
    public ScanReport(
        string hostName,
        string osDescription,
        DateTime startedAtUtc,
        DateTime completedAtUtc,
        int totalSoftwareScanned,
        int totalPluginsExecuted,
        IEnumerable<LicenseCheckResult> results)
    {
        HostName = hostName;
        OSDescription = osDescription;
        StartedAtUtc = startedAtUtc;
        CompletedAtUtc = completedAtUtc;
        TotalSoftwareScanned = totalSoftwareScanned;
        TotalPluginsExecuted = totalPluginsExecuted;
        Results = results.ToList().AsReadOnly();
    }
}
