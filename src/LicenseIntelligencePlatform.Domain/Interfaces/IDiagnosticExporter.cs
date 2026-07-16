using LicenseIntelligencePlatform.Domain.Entities;

namespace LicenseIntelligencePlatform.Domain.Interfaces;

/// <summary>
/// Defines an exporter responsible for packaging diagnostic artifacts (reports, log streams, environment context)
/// into a compressed diagnostic archive (`diagnostic.zip`) for audit and customer troubleshooting.
/// </summary>
public interface IDiagnosticExporter
{
    /// <summary>
    /// Asynchronously generates a diagnostic package zip archive for the specified scan session.
    /// </summary>
    /// <param name="report">The scan report representing the session results.</param>
    /// <param name="logsDirectory">The directory containing active log files (`application.log`, `error.log`, `performance.log`, `audit.log`).</param>
    /// <param name="outputZipPath">The target path where `diagnostic.zip` will be saved.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The absolute file path of the generated diagnostic zip package.</returns>
    Task<string> ExportDiagnosticPackageAsync(ScanReport report, string logsDirectory, string outputZipPath, CancellationToken cancellationToken = default);
}
