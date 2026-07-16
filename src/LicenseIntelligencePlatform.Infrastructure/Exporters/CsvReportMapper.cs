using System.Text;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Interfaces;
using LicenseIntelligencePlatform.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;

namespace LicenseIntelligencePlatform.Infrastructure.Exporters;

/// <summary>
/// Exporter responsible for mapping and formatting a <see cref="ScanReport"/> into CSV (Comma-Separated Values) format.
/// Adheres to Rule 5: Exporters only export.
/// </summary>
public class CsvReportMapper : IReportMapper
{
    private readonly ILogger<CsvReportMapper> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvReportMapper"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public CsvReportMapper(ILogger<CsvReportMapper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string FormatName => "CSV";

    /// <inheritdoc />
    public async Task ExportAsync(ScanReport report, Stream outputStream, CancellationToken cancellationToken = default)
    {
        if (report == null) throw new ArgumentNullException(nameof(report));
        if (outputStream == null) throw new ArgumentNullException(nameof(outputStream));

        _logger.LogInformation("Exporting scan report '{ScanId}' to CSV format ({ResultCount} check results)...", report.ScanId, report.Results.Count);

        using var writer = new StreamWriter(outputStream, Encoding.UTF8, bufferSize: 4096, leaveOpen: true);

        // Header
        await writer.WriteLineAsync("SoftwareName,Version,Publisher,InstallPath,InstallDate,LastModifiedDate,AppStartTime,ScanSource,DetectedLicenseType,LicenseName,ConfidenceLevel,IsVerified,EvidenceCount,Notes,PluginId,ScannedAtUtc".AsMemory(), cancellationToken);

        foreach (var result in report.Results)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var line = string.Format(
                "\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\",\"{9}\",\"{10}\",\"{11}\",{12},\"{13}\",\"{14}\",\"{15:O}\"",
                EscapeCsv(result.Software.Name),
                EscapeCsv(result.Software.Version),
                EscapeCsv(result.Software.Publisher),
                EscapeCsv(result.Software.InstallPath),
                EscapeCsv(result.Software.InstallDate),
                EscapeCsv(result.Software.LastModifiedDate),
                EscapeCsv(result.Software.AppStartTime),
                EscapeCsv(result.Software.ScanSource),
                result.DetectedLicenseType,
                EscapeCsv(result.LicenseName),
                result.Confidence,
                result.IsVerified,
                result.Evidences.Count,
                EscapeCsv(result.Notes),
                EscapeCsv(result.PluginId),
                VietnamTime.Format(result.ScannedAtUtc)
            );

            await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
        }

        await writer.FlushAsync(cancellationToken);
        _logger.LogInformation("CSV export completed successfully.");
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        // Prevent CSV Formula Injection / DDE Execution (Security Hardening)
        if (value.StartsWith("=") || value.StartsWith("+") || value.StartsWith("-") || value.StartsWith("@") || value.StartsWith("\t") || value.StartsWith("\r"))
        {
            value = "'" + value;
        }

        return value.Replace("\"", "\"\"");
    }
}
