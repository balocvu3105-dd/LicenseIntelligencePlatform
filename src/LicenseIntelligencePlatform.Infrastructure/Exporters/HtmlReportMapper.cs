using System.Text;
using System.Web;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;
using LicenseIntelligencePlatform.Infrastructure.Diagnostics;

namespace LicenseIntelligencePlatform.Infrastructure.Exporters;

/// <summary>
/// Phase 4 — Reporting Engine: HTML Visual & Printable Report Mapper.
/// Produces a self-contained, responsive, dark-mode compatible HTML5 report suitable for browser viewing or PDF printing.
/// </summary>
public sealed class HtmlReportMapper : IReportMapper
{
    /// <inheritdoc />
    public string FormatName => "HTML";

    /// <inheritdoc />
    public async Task ExportAsync(ScanReport report, Stream outputStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(outputStream);

        using var writer = new StreamWriter(outputStream, Encoding.UTF8, leaveOpen: true);
        var sb = new StringBuilder();

        var verifiedCount = report.Results.Count(r => r.IsVerified);
        var commercialCount = report.Results.Count(r => r.DetectedLicenseType == LicenseType.Commercial);
        var openSourceCount = report.Results.Count(r => r.DetectedLicenseType == LicenseType.OpenSource);

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"UTF-8\">");
        sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"    <title>LIP Executive License Report — {HtmlEncode(report.ScanId)}</title>");
        sb.AppendLine("    <style>");
        sb.AppendLine("        :root { --bg: #0f172a; --card: #1e293b; --text: #f8fafc; --muted: #94a3b8; --border: #334155; --accent: #38bdf8; --success: #22c55e; --warn: #f59e0b; --danger: #ef4444; }");
        sb.AppendLine("        * { box-sizing: border-box; }");
        sb.AppendLine("        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif; background: var(--bg); color: var(--text); margin: 0; padding: 1.5rem 2rem; line-height: 1.5; }");
        sb.AppendLine("        .container { max-width: 1840px; width: 96%; margin: 0 auto; }");
        sb.AppendLine("        header { border-bottom: 1px solid var(--border); padding-bottom: 1.25rem; margin-bottom: 1.75rem; display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 1rem; }");
        sb.AppendLine("        h1 { margin: 0; font-size: 1.8rem; color: var(--accent); font-weight: 700; letter-spacing: -0.02em; }");
        sb.AppendLine("        .meta-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 1.25rem; margin-bottom: 2rem; }");
        sb.AppendLine("        .card { background: var(--card); border: 1px solid var(--border); border-radius: 10px; padding: 1.25rem 1.5rem; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.2); transition: transform 0.2s, border-color 0.2s; }");
        sb.AppendLine("        .card:hover { border-color: var(--accent); }");
        sb.AppendLine("        .card h3 { margin: 0 0 0.5rem 0; font-size: 0.8rem; color: var(--muted); text-transform: uppercase; letter-spacing: 0.08em; font-weight: 600; }");
        sb.AppendLine("        .card .val { font-size: 2rem; font-weight: 800; line-height: 1; }");
        sb.AppendLine("        .table-container { width: 100%; overflow-x: auto; -webkit-overflow-scrolling: touch; border-radius: 10px; border: 1px solid var(--border); background: var(--card); box-shadow: 0 10px 15px -3px rgba(0,0,0,0.3); max-height: calc(100vh - 340px); }");
        sb.AppendLine("        table { width: 100%; min-width: 2800px; border-collapse: collapse; table-layout: auto; }");
        sb.AppendLine("        th, td { padding: 0.85rem 1rem; text-align: left; border-bottom: 1px solid var(--border); font-size: 0.88rem; vertical-align: top; line-height: 1.45; }");
        sb.AppendLine("        th { position: sticky; top: 0; z-index: 10; background: #0b1323; color: var(--muted); font-weight: 700; text-transform: uppercase; font-size: 0.72rem; letter-spacing: 0.08em; box-shadow: 0 1px 0 var(--border); }");
        sb.AppendLine("        tr:hover td { background: rgba(255, 255, 255, 0.03); }");
        sb.AppendLine("        .col-pkg { min-width: 220px; width: 13%; word-break: break-word; }");
        sb.AppendLine("        .col-ver { min-width: 200px; width: 10%; word-break: break-all; }");
        sb.AppendLine("        .col-pub { min-width: 180px; width: 10%; word-break: break-word; }");
        sb.AppendLine("        .col-path { min-width: 320px; width: 15%; word-break: break-all; font-family: ui-monospace, SFMono-Regular, Consolas, monospace; }");
        sb.AppendLine("        .col-dt { min-width: 130px; width: 6%; white-space: normal; }");
        sb.AppendLine("        .col-mod { min-width: 230px; width: 9%; white-space: normal; }");
        sb.AppendLine("        .col-use { min-width: 230px; width: 9%; white-space: normal; }");
        sb.AppendLine("        .col-src { min-width: 180px; width: 8%; word-break: break-word; }");
        sb.AppendLine("        .col-lic { min-width: 140px; width: 6%; text-align: center; }");
        sb.AppendLine("        .col-cnf { min-width: 140px; width: 6%; text-align: center; }");
        sb.AppendLine("        .col-det { min-width: 180px; width: 8%; word-break: break-word; }");
        sb.AppendLine("        .col-art { min-width: 380px; width: 16%; word-break: break-word; }");
        sb.AppendLine("        th.col-lic, th.col-cnf { text-align: center; }");
        sb.AppendLine("        code { background: rgba(15, 23, 42, 0.6); padding: 0.2rem 0.5rem; border-radius: 4px; font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace; font-size: 0.82rem; color: #38bdf8; word-break: break-all; display: inline-block; line-height: 1.35; }");
        sb.AppendLine("        .badge { display: inline-block; padding: 0.28rem 0.7rem; border-radius: 9999px; font-size: 0.72rem; font-weight: 700; letter-spacing: 0.03em; text-transform: uppercase; white-space: nowrap; }");
        sb.AppendLine("        .badge-verified { background: rgba(34, 197, 94, 0.15); color: #4ade80; border: 1px solid rgba(34, 197, 94, 0.4); }");
        sb.AppendLine("        .badge-comm { background: rgba(239, 68, 68, 0.15); color: #f87171; border: 1px solid rgba(239, 68, 68, 0.4); }");
        sb.AppendLine("        .badge-os { background: rgba(56, 189, 248, 0.15); color: #38bdf8; border: 1px solid rgba(56, 189, 248, 0.4); }");
        sb.AppendLine("        .badge-med { background: rgba(245, 158, 11, 0.15); color: #fbbf24; border: 1px solid rgba(245, 158, 11, 0.4); }");
        sb.AppendLine("        .badge-none { background: rgba(148, 163, 184, 0.15); color: var(--muted); border: 1px solid rgba(148, 163, 184, 0.4); }");
        sb.AppendLine("        footer { margin-top: 2.5rem; text-align: center; color: var(--muted); font-size: 0.82rem; border-top: 1px solid var(--border); padding-top: 1.5rem; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <div class=\"container\">");
        sb.AppendLine("        <header>");
        sb.AppendLine("            <div>");
        sb.AppendLine("                <h1>License Intelligence Platform (LIP)</h1>");
        sb.AppendLine("                <div style=\"color: var(--muted); font-size: 0.9rem; margin-top: 0.25rem;\">Phase 4 Executive HTML Inventory & Audit Report</div>");
        sb.AppendLine("            </div>");
        sb.AppendLine($"            <div style=\"text-align: right; color: var(--muted); font-size: 0.85rem;\">Scan ID: <strong>{HtmlEncode(report.ScanId)}</strong><br>{VietnamTime.Format(report.StartedAtUtc)}</div>");
        sb.AppendLine("        </header>");

        sb.AppendLine("        <div class=\"meta-grid\">");
        sb.AppendLine("            <div class=\"card\"><h3>Total Software Discovered</h3><div class=\"val\">" + report.TotalSoftwareScanned + "</div></div>");
        sb.AppendLine("            <div class=\"card\"><h3>Cryptographically Verified</h3><div class=\"val\" style=\"color: var(--success);\">" + verifiedCount + "</div></div>");
        sb.AppendLine("            <div class=\"card\"><h3>Commercial Proprietary</h3><div class=\"val\" style=\"color: var(--danger);\">" + commercialCount + "</div></div>");
        sb.AppendLine("            <div class=\"card\"><h3>Open Source Permissive/Copyleft</h3><div class=\"val\" style=\"color: var(--accent);\">" + openSourceCount + "</div></div>");
        sb.AppendLine("        </div>");

        sb.AppendLine("        <div class=\"table-container\">");
        sb.AppendLine("            <table>");
        sb.AppendLine("                <thead>");
        sb.AppendLine("                    <tr>");
        sb.AppendLine("                        <th class=\"col-pkg\">Software Package</th>");
        sb.AppendLine("                        <th class=\"col-ver\">Version</th>");
        sb.AppendLine("                        <th class=\"col-pub\">Publisher</th>");
        sb.AppendLine("                        <th class=\"col-path\">Install Path</th>");
        sb.AppendLine("                        <th class=\"col-dt\">Install Date</th>");
        sb.AppendLine("                        <th class=\"col-mod\">Last Updated (VN)</th>");
        sb.AppendLine("                        <th class=\"col-use\">Last Used / Active (VN)</th>");
        sb.AppendLine("                        <th class=\"col-src\">Scan Source</th>");
        sb.AppendLine("                        <th class=\"col-lic\">License Type</th>");
        sb.AppendLine("                        <th class=\"col-cnf\">Confidence</th>");
        sb.AppendLine("                        <th class=\"col-det\">Plugin Detector</th>");
        sb.AppendLine("                        <th class=\"col-art\">Verification Artifacts</th>");
        sb.AppendLine("                    </tr>");
        sb.AppendLine("                </thead>");
        sb.AppendLine("                <tbody>");

        foreach (var r in report.Results.OrderByDescending(x => x.Confidence).ThenBy(x => x.Software.Name))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var licenseClass = r.DetectedLicenseType switch
            {
                LicenseType.Commercial => "badge-comm",
                LicenseType.OpenSource => "badge-os",
                _ => "badge-none"
            };

            var confClass = r.Confidence switch
            {
                ConfidenceLevel.Verified => "badge-verified",
                ConfidenceLevel.High => "badge-verified",
                ConfidenceLevel.Medium => "badge-med",
                _ => "badge-none"
            };

            var evText = r.Evidences.Count > 0
                ? string.Join("<br>", r.Evidences.Select(e => $"• <strong style=\"color: var(--text);\">{HtmlEncode(e.EvidenceType)}:</strong> {HtmlEncode(e.Description)}"))
                : "<span style=\"color: var(--muted); font-style: italic;\">No verification artifacts recorded</span>";

            var installPath = !string.IsNullOrWhiteSpace(r.Software.InstallPath) ? HtmlEncode(r.Software.InstallPath) : "<span style=\"color: var(--muted); font-style: italic;\">N/A (System / Built-in)</span>";
            var installDate = !string.IsNullOrWhiteSpace(r.Software.InstallDate) && !r.Software.InstallDate.Equals("Unknown", StringComparison.OrdinalIgnoreCase) ? HtmlEncode(r.Software.InstallDate) : "<span style=\"color: var(--muted); font-style: italic;\">— (Pre-installed)</span>";
            var lastMod = !string.IsNullOrWhiteSpace(r.Software.LastModifiedDate) && !r.Software.LastModifiedDate.Equals("Unknown", StringComparison.OrdinalIgnoreCase) ? HtmlEncode(r.Software.LastModifiedDate) : "<span style=\"color: var(--muted); font-style: italic;\">— (Not Modified)</span>";
            var appStart = !string.IsNullOrWhiteSpace(r.Software.AppStartTime) ? HtmlEncode(r.Software.AppStartTime) : "<span style=\"color: var(--muted); font-style: italic;\">— (Inactive / Background)</span>";
            var publisher = !string.IsNullOrWhiteSpace(r.Software.Publisher) && !r.Software.Publisher.Equals("Unknown", StringComparison.OrdinalIgnoreCase) ? HtmlEncode(r.Software.Publisher) : "<span style=\"color: var(--muted); font-style: italic;\">Unknown / Independent</span>";
            var scanSrc = !string.IsNullOrWhiteSpace(r.Software.ScanSource) ? HtmlEncode(r.Software.ScanSource) : "System Scan";

            sb.AppendLine("                    <tr>");
            sb.AppendLine($"                        <td class=\"col-pkg\"><strong style=\"color: #f8fafc; font-size: 0.95rem;\">{HtmlEncode(r.Software.Name)}</strong></td>");
            sb.AppendLine($"                        <td class=\"col-ver\"><code>{HtmlEncode(r.Software.Version)}</code></td>");
            sb.AppendLine($"                        <td class=\"col-pub\" style=\"color: #cbd5e1;\">{publisher}</td>");
            sb.AppendLine($"                        <td class=\"col-path\" style=\"font-size: 0.82rem; font-family: monospace; color: #a5b4fc;\">{installPath}</td>");
            sb.AppendLine($"                        <td class=\"col-dt\" style=\"font-size: 0.82rem; color: #cbd5e1;\">{installDate}</td>");
            sb.AppendLine($"                        <td class=\"col-mod\" style=\"font-size: 0.82rem; color: #cbd5e1;\">{lastMod}</td>");
            sb.AppendLine($"                        <td class=\"col-use\" style=\"font-size: 0.82rem; color: #86efac; font-weight: 500;\">{appStart}</td>");
            sb.AppendLine($"                        <td class=\"col-src\" style=\"font-size: 0.82rem; color: #94a3b8;\">{scanSrc}</td>");
            sb.AppendLine($"                        <td class=\"col-lic\"><span class=\"badge {licenseClass}\">{HtmlEncode(r.DetectedLicenseType.ToString())}</span></td>");
            sb.AppendLine($"                        <td class=\"col-cnf\"><span class=\"badge {confClass}\">{HtmlEncode(r.Confidence.ToString())}</span></td>");
            sb.AppendLine($"                        <td class=\"col-det\" style=\"color: #94a3b8; font-weight: 500;\">{HtmlEncode(r.PluginName)}</td>");
            sb.AppendLine($"                        <td class=\"col-art\" style=\"font-size: 0.82rem; line-height: 1.4; color: #cbd5e1;\">{evText}</td>");
            sb.AppendLine("                    </tr>");
        }

        sb.AppendLine("                </tbody>");
        sb.AppendLine("            </table>");
        sb.AppendLine("        </div>");

        sb.AppendLine("        <footer>");
        sb.AppendLine($"            Generated by License Intelligence Platform v1.0 on {VietnamTime.Format(DateTime.UtcNow)} — 100% Read-Only & Air-Gapped Verification Engine");
        sb.AppendLine("        </footer>");
        sb.AppendLine("    </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        await writer.WriteAsync(sb.ToString().AsMemory(), cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string HtmlEncode(string? text) => HttpUtility.HtmlEncode(text ?? string.Empty);
}
