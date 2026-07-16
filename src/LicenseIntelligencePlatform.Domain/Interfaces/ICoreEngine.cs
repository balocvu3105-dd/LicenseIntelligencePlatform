using LicenseIntelligencePlatform.Domain.Entities;

namespace LicenseIntelligencePlatform.Domain.Interfaces;

/// <summary>
/// Defines the central orchestration engine responsible for running scanners and applying license plugins safely.
/// </summary>
public interface ICoreEngine
{
    /// <summary>
    /// Executes a full software scan across available scanners and runs all loaded license detection plugins against discovered items.
    /// Ensures isolation of plugin failures without crashing Core.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A comprehensive scan report containing results, confidence levels, and evidence.</returns>
    Task<ScanReport> ExecuteFullScanAsync(CancellationToken cancellationToken = default);
}
