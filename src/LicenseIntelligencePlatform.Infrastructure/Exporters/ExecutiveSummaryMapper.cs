using System.Text;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;
using LicenseIntelligencePlatform.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;

namespace LicenseIntelligencePlatform.Infrastructure.Exporters;

/// <summary>
/// Exporter that generates a concise executive summary report (plain text) from a scan session.
/// Designed to be readable by non-technical managers and auditors without opening CSV or JSON files.
/// </summary>
public class ExecutiveSummaryMapper : IReportMapper
{
    private readonly ILogger<ExecutiveSummaryMapper> _logger;

    /// <summary>Initializes a new instance of <see cref="ExecutiveSummaryMapper"/>.</summary>
    public ExecutiveSummaryMapper(ILogger<ExecutiveSummaryMapper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string FormatName => "ExecutiveSummary";

    /// <inheritdoc />
    public async Task ExportAsync(ScanReport report, Stream outputStream, CancellationToken cancellationToken = default)
    {
        if (report == null) throw new ArgumentNullException(nameof(report));
        if (outputStream == null) throw new ArgumentNullException(nameof(outputStream));

        _logger.LogInformation("Generating Executive Summary for scan '{ScanId}' ({ResultCount} results)...", report.ScanId, report.Results.Count);

        var sb = new StringBuilder();

        // ── Header ────────────────────────────────────────────────────────────
        sb.AppendLine("╔══════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║        LICENSE INTELLIGENCE PLATFORM  —  EXECUTIVE SUMMARY       ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        sb.AppendLine($"  Scan ID   : {report.ScanId}");
        sb.AppendLine($"  Host      : {report.HostName}");
        sb.AppendLine($"  OS        : {report.OSDescription}");
        sb.AppendLine($"  Scanned   : {VietnamTime.Format(report.StartedAtUtc)}");
        sb.AppendLine($"  Duration  : {(report.CompletedAtUtc - report.StartedAtUtc).TotalMilliseconds:F0} ms");
        sb.AppendLine($"  Generated : {VietnamTime.Format(DateTime.UtcNow)}");
        sb.AppendLine();

        // ── Totals ────────────────────────────────────────────────────────────
        var allSoftware = report.Results
            .GroupBy(r => r.Software.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(r => r.Confidence).First())
            .ToList();

        int total       = report.TotalSoftwareScanned;
        int commercial  = allSoftware.Count(r => r.DetectedLicenseType == LicenseType.Commercial);
        int openSource  = allSoftware.Count(r => r.DetectedLicenseType == LicenseType.OpenSource);
        int unknown     = allSoftware.Count(r => r.DetectedLicenseType == LicenseType.Unknown);
        int freeware    = allSoftware.Count(r => r.DetectedLicenseType == LicenseType.Freeware);
        int verified    = allSoftware.Count(r => r.IsVerified);
        int unverified  = total - verified;

        double commPct  = total > 0 ? (double)commercial / total * 100 : 0;
        double osPct    = total > 0 ? (double)openSource  / total * 100 : 0;
        double unknPct  = total > 0 ? (double)unknown     / total * 100 : 0;

        sb.AppendLine("  ┌─────────────────────────────────────────────────┐");
        sb.AppendLine("  │                  INVENTORY SUMMARY               │");
        sb.AppendLine("  ├─────────────────────────────────────────────────┤");
        sb.AppendLine($"  │  Total Software Scanned   : {total,6}              │");
        sb.AppendLine($"  │  Plugins Executed         : {report.TotalPluginsExecuted,6}              │");
        sb.AppendLine($"  │  Verified Results         : {verified,6}              │");
        sb.AppendLine($"  │  Unverified / Unknown     : {unverified,6}              │");
        sb.AppendLine("  ├─────────────────────────────────────────────────┤");
        sb.AppendLine($"  │  Commercial               : {commercial,6}  ({commPct,5:F1}%)       │");
        sb.AppendLine($"  │  Open Source              : {openSource,6}  ({osPct,5:F1}%)       │");
        sb.AppendLine($"  │  Freeware                 : {freeware,6}              │");
        sb.AppendLine($"  │  Unknown / Needs Review   : {unknown,6}  ({unknPct,5:F1}%)       │");
        sb.AppendLine("  └─────────────────────────────────────────────────┘");
        sb.AppendLine();

        // ── Top Commercial Licenses ───────────────────────────────────────────
        var topCommercial = allSoftware
            .Where(r => r.DetectedLicenseType == LicenseType.Commercial)
            .OrderByDescending(r => (int)r.Confidence)
            .Take(10)
            .ToList();

        if (topCommercial.Any())
        {
            sb.AppendLine("  TOP COMMERCIAL SOFTWARE DETECTED:");
            sb.AppendLine("  " + new string('─', 64));
            foreach (var r in topCommercial)
            {
                var conf = r.IsVerified ? "✔ Verified" : r.Confidence.ToString();
                sb.AppendLine($"  • {r.Software.Name,-38} [{conf}]");
                sb.AppendLine($"      License : {r.LicenseName}");
                if (!string.IsNullOrWhiteSpace(r.Software.LastModifiedDate))
                    sb.AppendLine($"      Updated : {r.Software.LastModifiedDate}");
                if (!string.IsNullOrWhiteSpace(r.Software.AppStartTime))
                    sb.AppendLine($"      Running : {r.Software.AppStartTime}");
            }
            sb.AppendLine();
        }

        // ── Needs Attention ───────────────────────────────────────────────────
        var needsAttention = allSoftware
            .Where(r => r.DetectedLicenseType == LicenseType.Unknown)
            .OrderBy(r => r.Software.Name)
            .Take(20)
            .ToList();

        if (needsAttention.Any())
        {
            sb.AppendLine("  ⚠  SOFTWARE REQUIRING LICENSE REVIEW:");
            sb.AppendLine("  " + new string('─', 64));
            foreach (var r in needsAttention)
            {
                sb.AppendLine($"  • {r.Software.Name,-38} [No plugin]");
                if (!string.IsNullOrWhiteSpace(r.Software.InstallPath))
                    sb.AppendLine($"      Path    : {r.Software.InstallPath}");
            }
            int remaining = unknown - needsAttention.Count;
            if (remaining > 0)
                sb.AppendLine($"  ... and {remaining} more — see license_report_*.csv for full list.");
            sb.AppendLine();
        }

        // ── Footer ────────────────────────────────────────────────────────────
        sb.AppendLine("  " + new string('─', 64));
        sb.AppendLine("  This report was generated automatically by License Intelligence Platform.");
        sb.AppendLine("  For full evidence details, see the evidence_report_*.txt file.");
        sb.AppendLine("  Confidential — For internal use only.");
        sb.AppendLine();

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        await outputStream.WriteAsync(bytes, cancellationToken);

        _logger.LogInformation("Executive Summary export completed.");
    }
}
