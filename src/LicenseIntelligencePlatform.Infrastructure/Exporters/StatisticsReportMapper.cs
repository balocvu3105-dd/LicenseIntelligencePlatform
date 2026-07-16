using System.Text;
using System.Text.Json;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Infrastructure.Exporters;

/// <summary>
/// Phase 4 — Reporting Engine: Statistical and Metrics Analytics Report Mapper.
/// Exports aggregated statistical telemetry (verification ratios, publisher breakdown, confidence distribution) in clean JSON format.
/// </summary>
public sealed class StatisticsReportMapper : IReportMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <inheritdoc />
    public string FormatName => "STATS";

    /// <inheritdoc />
    public async Task ExportAsync(ScanReport report, Stream outputStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(outputStream);

        var total = report.Results.Count;
        var verified = report.Results.Count(r => r.IsVerified);

        var statsObj = new
        {
            SessionId = report.ScanId,
            StartedAtUtc = report.StartedAtUtc,
            CompletedAtUtc = report.CompletedAtUtc,
            DurationSeconds = (report.CompletedAtUtc - report.StartedAtUtc).TotalSeconds,
            HostName = report.HostName,
            OSDescription = report.OSDescription,
            TotalSoftwareScanned = report.TotalSoftwareScanned,
            TotalPluginEvaluations = report.TotalPluginsExecuted,
            OverallVerificationRatio = total > 0 ? Math.Round((double)verified / total * 100, 2) : 0.0,
            ConfidenceDistribution = new
            {
                Verified = report.Results.Count(r => r.Confidence == ConfidenceLevel.Verified),
                High = report.Results.Count(r => r.Confidence == ConfidenceLevel.High),
                Medium = report.Results.Count(r => r.Confidence == ConfidenceLevel.Medium),
                Low = report.Results.Count(r => r.Confidence == ConfidenceLevel.Low),
                None = report.Results.Count(r => r.Confidence == ConfidenceLevel.None)
            },
            LicenseTypeDistribution = report.Results
                .GroupBy(r => r.DetectedLicenseType.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            TopPublishersDiscovered = report.Results
                .GroupBy(r => string.IsNullOrWhiteSpace(r.Software.Publisher) ? "Unknown Publisher" : r.Software.Publisher.Trim())
                .OrderByDescending(g => g.Count())
                .Take(15)
                .ToDictionary(g => g.Key, g => g.Count()),
            PluginContributionMetrics = report.Results
                .GroupBy(r => r.PluginName)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => new
                {
                    TotalIdentified = g.Count(),
                    VerifiedCount = g.Count(x => x.IsVerified)
                })
        };

        await JsonSerializer.SerializeAsync(outputStream, statsObj, JsonOptions, cancellationToken).ConfigureAwait(false);
    }
}
