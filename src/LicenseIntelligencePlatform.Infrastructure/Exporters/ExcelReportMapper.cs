using ClosedXML.Excel;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;
using LicenseIntelligencePlatform.Infrastructure.Diagnostics;

namespace LicenseIntelligencePlatform.Infrastructure.Exporters;

/// <summary>
/// Phase 4 — Reporting Engine: Enterprise Multi-Sheet Excel Spreadsheet (.xlsx) Exporter.
/// Generates a rich, interactive Excel workbook with Executive Dashboard, Full Inventory, Commercial Audit, and Open Source Compliance sheets.
/// </summary>
public sealed class ExcelReportMapper : IReportMapper
{
    /// <inheritdoc />
    public string FormatName => "XLSX";

    /// <inheritdoc />
    public async Task ExportAsync(ScanReport report, Stream outputStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(outputStream);

        // Run ClosedXML generation on CPU thread to keep async interface responsive
        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();

            // ── Sheet 1: Executive Dashboard ────────────────────────────────────────────────────────
            var dashSheet = workbook.Worksheets.Add("Executive Dashboard");
            dashSheet.ShowGridLines = true;

            // Title block
            dashSheet.Range("A1:E1").Merge();
            var titleCell = dashSheet.Cell("A1");
            titleCell.Value = "LICENSE INTELLIGENCE PLATFORM (LIP) — EXECUTIVE DASHBOARD";
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 16;
            titleCell.Style.Font.FontColor = XLColor.FromHtml("#F8FAFC");
            titleCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F172A");
            titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            dashSheet.Row(1).Height = 40;

            // Subtitle metadata
            dashSheet.Range("A2:E2").Merge();
            dashSheet.Cell("A2").Value = $"Scan ID: {report.ScanId} | Host: {report.HostName} ({report.OSDescription})";
            dashSheet.Cell("A2").Style.Font.FontSize = 11;
            dashSheet.Cell("A2").Style.Font.FontColor = XLColor.FromHtml("#475569");
            dashSheet.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            dashSheet.Range("A3:E3").Merge();
            dashSheet.Cell("A3").Value = $"Scan Start (VN Time): {VietnamTime.Format(report.StartedAtUtc)} | Total Packages: {report.TotalSoftwareScanned}";
            dashSheet.Cell("A3").Style.Font.FontSize = 11;
            dashSheet.Cell("A3").Style.Font.Bold = true;
            dashSheet.Cell("A3").Style.Font.FontColor = XLColor.FromHtml("#0284C7");
            dashSheet.Cell("A3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Summary Table Header
            dashSheet.Cell("B5").Value = "Key Performance Indicator (KPI)";
            dashSheet.Cell("C5").Value = "Package Count";
            dashSheet.Cell("D5").Value = "Percentage of Total";
            var kpiHeaderRange = dashSheet.Range("B5:D5");
            kpiHeaderRange.Style.Font.Bold = true;
            kpiHeaderRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
            kpiHeaderRange.Style.Font.FontColor = XLColor.FromHtml("#F8FAFC");
            kpiHeaderRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            dashSheet.Row(5).Height = 25;

            var verifiedCount = report.Results.Count(r => r.IsVerified);
            var commercialCount = report.Results.Count(r => r.DetectedLicenseType == LicenseType.Commercial);
            var openSourceCount = report.Results.Count(r => r.DetectedLicenseType == LicenseType.OpenSource);
            var freewareCount = report.Results.Count(r => r.DetectedLicenseType == LicenseType.Freeware);
            var unknownCount = report.Results.Count(r => r.DetectedLicenseType == LicenseType.Unknown);
            var totalCount = Math.Max(1, report.Results.Count);

            AddDashboardRow(dashSheet, 6, "Total Software Discovered", report.TotalSoftwareScanned, 100.0, "#0F172A", true);
            AddDashboardRow(dashSheet, 7, "Cryptographically Verified Packages", verifiedCount, (double)verifiedCount / totalCount * 100.0, "#166534", false);
            AddDashboardRow(dashSheet, 8, "Commercial Proprietary (Subscription Audit Req)", commercialCount, (double)commercialCount / totalCount * 100.0, "#991B1B", false);
            AddDashboardRow(dashSheet, 9, "Open Source Permissive/Copyleft", openSourceCount, (double)openSourceCount / totalCount * 100.0, "#075985", false);
            AddDashboardRow(dashSheet, 10, "Freeware / Royalty-Free System Utilities", freewareCount, (double)freewareCount / totalCount * 100.0, "#334155", false);
            AddDashboardRow(dashSheet, 11, "Unknown / Need Plugin Evaluation Backlog", unknownCount, (double)unknownCount / totalCount * 100.0, "#B45309", false);

            var kpiBorderRange = dashSheet.Range("B5:D11");
            kpiBorderRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            kpiBorderRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dashSheet.Columns().AdjustToContents(10, 50);

            // ── Sheet 2: Full Software Inventory ────────────────────────────────────────────────────
            var fullSheet = workbook.Worksheets.Add("Full Inventory & Audit");
            PopulateTableSheet(fullSheet, report.Results.OrderByDescending(x => x.Confidence).ThenBy(x => x.Software.Name));

            // ── Sheet 3: Commercial Audit ───────────────────────────────────────────────────────────
            var commSheet = workbook.Worksheets.Add("Commercial Licenses (Action)");
            PopulateTableSheet(commSheet, report.Results.Where(r => r.DetectedLicenseType == LicenseType.Commercial).OrderBy(x => x.Software.Name));

            // ── Sheet 4: Open Source Compliance ─────────────────────────────────────────────────────
            var osSheet = workbook.Worksheets.Add("Open Source Compliance");
            PopulateTableSheet(osSheet, report.Results.Where(r => r.DetectedLicenseType == LicenseType.OpenSource).OrderBy(x => x.Software.Name));

            workbook.SaveAs(outputStream);
        }, cancellationToken);
    }

    private static void AddDashboardRow(IXLWorksheet sheet, int row, string label, int count, double percentage, string colorHex, bool isHeaderTotal)
    {
        sheet.Cell(row, 2).Value = label;
        sheet.Cell(row, 3).Value = count;
        sheet.Cell(row, 4).Value = percentage / 100.0;
        sheet.Cell(row, 4).Style.NumberFormat.Format = "0.0%";

        if (isHeaderTotal)
        {
            sheet.Range(row, 2, row, 4).Style.Font.Bold = true;
            sheet.Range(row, 2, row, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
        }
        else
        {
            sheet.Cell(row, 3).Style.Font.Bold = true;
            sheet.Cell(row, 3).Style.Font.FontColor = XLColor.FromHtml(colorHex);
        }
        sheet.Row(row).Height = 22;
    }

    private static void PopulateTableSheet(IXLWorksheet sheet, IEnumerable<LicenseCheckResult> results)
    {
        sheet.ShowGridLines = true;

        // Header Row
        var headers = new[]
        {
            "Software Package", "Version", "Publisher", "Install Path",
            "License Type", "Confidence", "Plugin Detector",
            "Verification Evidence", "Scan Source", "Last Modified (VN Time)"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0B1323");
            cell.Style.Font.FontColor = XLColor.FromHtml("#F8FAFC");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        sheet.Row(1).Height = 26;
        sheet.SheetView.FreezeRows(1);

        int row = 2;
        foreach (var r in results)
        {
            sheet.Cell(row, 1).Value = r.Software.Name;
            sheet.Cell(row, 1).Style.Font.Bold = true;

            sheet.Cell(row, 2).Value = r.Software.Version;
            sheet.Cell(row, 3).Value = r.Software.Publisher ?? "Unknown";
            sheet.Cell(row, 4).Value = r.Software.InstallPath;

            // License Type Cell with conditional badge tint
            var licCell = sheet.Cell(row, 5);
            licCell.Value = r.DetectedLicenseType.ToString();
            licCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            licCell.Style.Font.Bold = true;
            switch (r.DetectedLicenseType)
            {
                case LicenseType.Commercial:
                    licCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEE2E2");
                    licCell.Style.Font.FontColor = XLColor.FromHtml("#991B1B");
                    break;
                case LicenseType.OpenSource:
                    licCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E0F2FE");
                    licCell.Style.Font.FontColor = XLColor.FromHtml("#075985");
                    break;
                case LicenseType.Freeware:
                    licCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                    licCell.Style.Font.FontColor = XLColor.FromHtml("#334155");
                    break;
            }

            // Confidence Cell
            var confCell = sheet.Cell(row, 6);
            confCell.Value = r.Confidence.ToString();
            confCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            confCell.Style.Font.Bold = true;
            if (r.Confidence == ConfidenceLevel.Verified || r.Confidence == ConfidenceLevel.High)
            {
                confCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCFCE7");
                confCell.Style.Font.FontColor = XLColor.FromHtml("#166534");
            }
            else if (r.Confidence == ConfidenceLevel.Medium)
            {
                confCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
                confCell.Style.Font.FontColor = XLColor.FromHtml("#92400E");
            }

            sheet.Cell(row, 7).Value = r.PluginName;

            // Evidence combined text
            var evText = r.Evidences.Count > 0
                ? string.Join(" | ", r.Evidences.Select(e => $"[{e.EvidenceType}] {e.Description}"))
                : "No artifacts";
            sheet.Cell(row, 8).Value = evText;

            sheet.Cell(row, 9).Value = r.Software.ScanSource;
            sheet.Cell(row, 10).Value = r.Software.LastModifiedDate;

            sheet.Row(row).Height = 22;
            row++;
        }

        if (row > 2)
        {
            var dataRange = sheet.Range(1, 1, row - 1, 10);
            dataRange.SetAutoFilter();
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        }

        // Auto-fit column widths with logical limits so long paths don't make columns super wide
        sheet.Columns(1, 10).AdjustToContents(10, 65);
    }
}
