using LicenseIntelligencePlatform.Domain.Entities;

namespace LicenseIntelligencePlatform.Domain.Interfaces;

/// <summary>
/// Defines a mapper/exporter responsible for formatting and exporting a <see cref="ScanReport"/> to an external representation (e.g., CSV, JSON).
/// </summary>
public interface IReportMapper
{
    /// <summary>
    /// Gets the format name handled by this mapper (e.g., "CSV", "JSON").
    /// </summary>
    string FormatName { get; }

    /// <summary>
    /// Asynchronously exports the scan report to the specified target stream.
    /// </summary>
    /// <param name="report">The scan report to export.</param>
    /// <param name="outputStream">The stream where the formatted report will be written.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExportAsync(ScanReport report, Stream outputStream, CancellationToken cancellationToken = default);
}
