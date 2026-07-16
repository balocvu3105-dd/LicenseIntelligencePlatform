using System.Text;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Interfaces;
using LicenseIntelligencePlatform.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;

namespace LicenseIntelligencePlatform.Infrastructure.Exporters;

/// <summary>
/// Exporter that generates a detailed evidence report (plain text) listing all supporting evidence
/// for each verified software package, grouped by plugin.
/// Intended for license auditors and compliance officers requiring a full evidence trail.
/// </summary>
public class EvidenceReportMapper : IReportMapper
{
    private readonly ILogger<EvidenceReportMapper> _logger;

    /// <summary>Initializes a new instance of <see cref="EvidenceReportMapper"/>.</summary>
    public EvidenceReportMapper(ILogger<EvidenceReportMapper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string FormatName => "EvidenceReport";

    /// <inheritdoc />
    public async Task ExportAsync(ScanReport report, Stream outputStream, CancellationToken cancellationToken = default)
    {
        if (report == null) throw new ArgumentNullException(nameof(report));
        if (outputStream == null) throw new ArgumentNullException(nameof(outputStream));

        _logger.LogInformation("Generating Evidence Report for scan '{ScanId}' ({ResultCount} results)...", report.ScanId, report.Results.Count);

        var sb = new StringBuilder();

        sb.AppendLine("╔══════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║       LICENSE INTELLIGENCE PLATFORM  —  EVIDENCE REPORT          ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        sb.AppendLine($"  Scan ID   : {report.ScanId}");
        sb.AppendLine($"  Host      : {report.HostName}");
        sb.AppendLine($"  Scanned   : {VietnamTime.Format(report.StartedAtUtc)}");
        sb.AppendLine($"  Generated : {VietnamTime.Format(DateTime.UtcNow)}");
        sb.AppendLine();
        sb.AppendLine("  This report contains only results that have at least one piece of evidence.");
        sb.AppendLine("  For a list of unverified software, see the executive_summary_*.txt file.");
        sb.AppendLine();
        sb.AppendLine("  " + new string('═', 64));
        sb.AppendLine();

        // Group by Plugin, then list each software with its evidence
        var resultsWithEvidence = report.Results
            .Where(r => r.Evidences.Count > 0)
            .OrderBy(r => r.PluginName)
            .ThenByDescending(r => (int)r.Confidence)
            .ToList();

        if (!resultsWithEvidence.Any())
        {
            sb.AppendLine("  No evidence records found in this scan.");
        }
        else
        {
            var byPlugin = resultsWithEvidence.GroupBy(r => r.PluginName);

            foreach (var pluginGroup in byPlugin)
            {
                sb.AppendLine($"  Plugin: {pluginGroup.Key}");
                sb.AppendLine("  " + new string('─', 64));

                foreach (var result in pluginGroup)
                {
                    sb.AppendLine($"  ▸ {result.Software.Name}  v{result.Software.Version}");
                    sb.AppendLine($"    Publisher    : {result.Software.Publisher}");
                    sb.AppendLine($"    License Type : {result.DetectedLicenseType}");
                    sb.AppendLine($"    License Name : {result.LicenseName}");
                    sb.AppendLine($"    Confidence   : {result.Confidence}{(result.IsVerified ? " ✔ (Verified)" : "")}");

                    if (!string.IsNullOrWhiteSpace(result.Software.InstallPath))
                        sb.AppendLine($"    Install Path : {result.Software.InstallPath}");
                    if (!string.IsNullOrWhiteSpace(result.Software.LastModifiedDate))
                        sb.AppendLine($"    Last Updated : {result.Software.LastModifiedDate}");
                    if (!string.IsNullOrWhiteSpace(result.Software.AppStartTime))
                        sb.AppendLine($"    App Started  : {result.Software.AppStartTime}");
                    if (!string.IsNullOrWhiteSpace(result.Notes))
                        sb.AppendLine($"    Notes        : {result.Notes}");

                    sb.AppendLine($"    Evidence ({result.Evidences.Count}):");
                    foreach (var ev in result.Evidences)
                    {
                        sb.AppendLine($"      [{ev.EvidenceType}]");
                        sb.AppendLine($"        Description : {ev.Description}");
                        if (!string.IsNullOrWhiteSpace(ev.SourceLocation))
                            sb.AppendLine($"        Source      : {ev.SourceLocation}");
                        if (!string.IsNullOrWhiteSpace(ev.RawData))
                            sb.AppendLine($"        Raw Data    : {ev.RawData}");
                    }

                    sb.AppendLine();
                }

                sb.AppendLine();
            }
        }

        sb.AppendLine("  " + new string('═', 64));
        sb.AppendLine("  END OF EVIDENCE REPORT");
        sb.AppendLine($"  Total Evidenced Results: {resultsWithEvidence.Count}");
        sb.AppendLine();

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        await outputStream.WriteAsync(bytes, cancellationToken);

        _logger.LogInformation("Evidence Report export completed. {Count} evidenced results.", resultsWithEvidence.Count);
    }
}
