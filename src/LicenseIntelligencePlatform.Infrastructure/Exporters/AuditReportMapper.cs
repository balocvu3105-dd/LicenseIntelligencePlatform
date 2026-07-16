using System.Text;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;
using LicenseIntelligencePlatform.Infrastructure.Diagnostics;

namespace LicenseIntelligencePlatform.Infrastructure.Exporters;

/// <summary>
/// Phase 4 — Reporting Engine: Executive and Legal License Audit Report Mapper.
/// Generates a structured markdown audit report detailing evidence weights, verification status, and unverified backlogs.
/// </summary>
public sealed class AuditReportMapper : IReportMapper
{
    /// <inheritdoc />
    public string FormatName => "AUDIT";

    /// <inheritdoc />
    public async Task ExportAsync(ScanReport report, Stream outputStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(outputStream);

        using var writer = new StreamWriter(outputStream, Encoding.UTF8, leaveOpen: true);
        var sb = new StringBuilder();

        sb.AppendLine("# Executive License Audit & Compliance Report");
        sb.AppendLine();
        sb.AppendLine("## 1. Scan Session Metadata");
        sb.AppendLine($"* **Scan ID:** `{report.ScanId}`");
        sb.AppendLine($"* **Host Machine:** `{report.HostName}` ({report.OSDescription})");
        sb.AppendLine($"* **Scan Start (VN Time):** `{VietnamTime.Format(report.StartedAtUtc)}`");
        sb.AppendLine($"* **Scan Completion (VN Time):** `{VietnamTime.Format(report.CompletedAtUtc)}`");
        sb.AppendLine($"* **Total Software Discovered:** `{report.TotalSoftwareScanned}` packages");
        sb.AppendLine($"* **Total Plugins Executed:** `{report.TotalPluginsExecuted}` evaluations");
        sb.AppendLine();

        var verifiedCount = report.Results.Count(r => r.IsVerified);
        var highCount = report.Results.Count(r => r.Confidence == ConfidenceLevel.High);
        var mediumCount = report.Results.Count(r => r.Confidence == ConfidenceLevel.Medium);
        var lowOrNoneCount = report.Results.Count(r => r.Confidence == ConfidenceLevel.Low || r.Confidence == ConfidenceLevel.None);

        sb.AppendLine("## 2. Executive Confidence Breakdown");
        sb.AppendLine("| Confidence Level | Verified (Score >= 70) | High (50-69) | Medium (30-49) | Low / None (< 30) |");
        sb.AppendLine("| :---: | :---: | :---: | :---: | :---: |");
        sb.AppendLine($"| **Count** | `{verifiedCount}` | `{highCount}` | `{mediumCount}` | `{lowOrNoneCount}` |");
        sb.AppendLine();

        sb.AppendLine("## 3. Verified & Commercial Software Audit Table");
        sb.AppendLine("| Software Name | Version | Publisher | Detected License | Confidence | Plugin Identified | Evidences |");
        sb.AppendLine("| :--- | :--- | :--- | :---: | :---: | :--- | :--- |");

        foreach (var result in report.Results.OrderByDescending(r => r.Confidence).ThenBy(r => r.Software.Name))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var evidenceSummary = result.Evidences.Count > 0
                ? string.Join("; ", result.Evidences.Select(e => $"{e.EvidenceType}: {e.Description}"))
                : "No physical evidence artifacts.";

            var pub = !string.IsNullOrWhiteSpace(result.Software.Publisher) && !result.Software.Publisher.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
                ? result.Software.Publisher : "Unknown / Independent";

            sb.AppendLine($"| **{result.Software.Name}** | `{result.Software.Version}` | {pub} | `{result.DetectedLicenseType}` | **{result.Confidence}** | `{result.PluginName}` | {evidenceSummary} |");
        }

        sb.AppendLine();
        sb.AppendLine("## 4. Backlog — Unverified & Unknown Software List");
        sb.AppendLine("The following packages currently lack specific license detection plugins or cryptographic artifacts and require manual audit / future plugin development:");
        sb.AppendLine();

        var backlog = report.Results.Where(r => r.DetectedLicenseType == LicenseType.Unknown || r.Confidence == ConfidenceLevel.None).ToList();
        if (backlog.Count == 0)
        {
            sb.AppendLine("* ✅ **Zero backlog items.** All discovered software packages were successfully verified or categorized.");
        }
        else
        {
            foreach (var item in backlog.Take(50))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var backPub = !string.IsNullOrWhiteSpace(item.Software.Publisher) && !item.Software.Publisher.Equals("Unknown", StringComparison.OrdinalIgnoreCase) ? item.Software.Publisher : "Unknown / Independent";
                sb.AppendLine($"* [Need Plugin] **{item.Software.Name}** (`v{item.Software.Version}`) by `{backPub}` — *Install Location: {item.Software.InstallPath ?? "N/A (System / Built-in)"}*");
            }

            if (backlog.Count > 50)
            {
                sb.AppendLine($"* *(Plus {backlog.Count - 50} additional packages omitted for brevity)*");
            }
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine($"*Generated automatically by License Intelligence Platform (LIP) v1.0 — Phase 4 Reporting Engine on {VietnamTime.Format(DateTime.UtcNow)}.*");

        await writer.WriteAsync(sb.ToString().AsMemory(), cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
