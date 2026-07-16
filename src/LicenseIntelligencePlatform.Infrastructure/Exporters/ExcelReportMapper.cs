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

            // Explicit column widths for perfect dashboard layout without clipping or ### overflows
            dashSheet.Column(1).Width = 4;  // Left margin space
            dashSheet.Column(2).Width = 48; // KPI Label column
            dashSheet.Column(3).Width = 20; // Package Count column
            dashSheet.Column(4).Width = 25; // Percentage column
            dashSheet.Column(5).Width = 12;
            dashSheet.Column(6).Width = 12;

            // Title block (merged A1:F1 to guarantee ample room for full title)
            dashSheet.Range("A1:F1").Merge();
            var titleCell = dashSheet.Cell("A1");
            titleCell.Value = "LICENSE INTELLIGENCE PLATFORM (LIP) — EXECUTIVE DASHBOARD";
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 16;
            titleCell.Style.Font.FontColor = XLColor.FromHtml("#F8FAFC");
            titleCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F172A");
            titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            dashSheet.Row(1).Height = 42;

            // Subtitle metadata
            dashSheet.Range("A2:F2").Merge();
            dashSheet.Cell("A2").Value = $"Scan ID: {report.ScanId} | Host: {report.HostName} ({report.OSDescription})";
            dashSheet.Cell("A2").Style.Font.FontSize = 11;
            dashSheet.Cell("A2").Style.Font.FontColor = XLColor.FromHtml("#475569");
            dashSheet.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            dashSheet.Range("A3:F3").Merge();
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
            dashSheet.Row(5).Height = 26;

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
        sheet.Row(row).Height = 24;
    }

    private static void PopulateTableSheet(IXLWorksheet sheet, IEnumerable<LicenseCheckResult> results)
    {
        sheet.ShowGridLines = true;

        // Header Row
        var headers = new[]
        {
            "Software Package", "Version", "Publisher", "Install Path", "Install Date",
            "Last Modified (VN Time)", "Last Used / Active (VN Time)", "Scan Source",
            "License Type", "Confidence", "Plugin Detector", "Verification Evidence"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0B1323");
            cell.Style.Font.FontColor = XLColor.FromHtml("#F8FAFC");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        sheet.Row(1).Height = 28;
        sheet.SheetView.FreezeRows(1);

        int row = 2;
        foreach (var r in results)
        {
            // Set vertical alignment for the entire data row so multiline wrapped text aligns all cells crisply at the top
            sheet.Range(row, 1, row, 12).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

            // Col 1: Software Package (WrapText true so long names don't spill over)
            var pkgCell = sheet.Cell(row, 1);
            pkgCell.Value = r.Software.Name;
            pkgCell.Style.Font.Bold = true;
            pkgCell.Style.Alignment.WrapText = true;

            // Col 2: Version
            var verCell = sheet.Cell(row, 2);
            verCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            FormatCellOrPlaceholder(verCell, r.Software.Version, "—");

            // Col 3: Publisher
            var pubCell = sheet.Cell(row, 3);
            pubCell.Style.Alignment.WrapText = true;
            var pub = !string.IsNullOrWhiteSpace(r.Software.Publisher) && !r.Software.Publisher.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
                ? r.Software.Publisher : null;
            FormatCellOrPlaceholder(pubCell, pub, "Unknown / Independent");

            // Col 4: Install Path (WrapText true is MANDATORY to prevent path strings from overlapping into Install Date)
            var pathCell = sheet.Cell(row, 4);
            pathCell.Style.Alignment.WrapText = true;
            FormatCellOrPlaceholder(pathCell, r.Software.InstallPath, "N/A (System / Built-in)");

            // Col 5: Install Date
            var dtCell = sheet.Cell(row, 5);
            dtCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            FormatCellOrPlaceholder(dtCell, r.Software.InstallDate, "— (Pre-installed)");

            // Col 6: Last Modified Date
            var modCell = sheet.Cell(row, 6);
            modCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            FormatCellOrPlaceholder(modCell, r.Software.LastModifiedDate, "— (Not Modified)");

            // Col 7: Last Used / Active
            var useCell = sheet.Cell(row, 7);
            useCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            FormatCellOrPlaceholder(useCell, r.Software.AppStartTime, "— (Inactive / Background)");

            // Col 8: Scan Source
            var srcCell = sheet.Cell(row, 8);
            srcCell.Style.Alignment.WrapText = true;
            FormatCellOrPlaceholder(srcCell, r.Software.ScanSource, "System Scan");

            // Col 9: License Type Cell with conditional badge tint
            var licCell = sheet.Cell(row, 9);
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
                default:
                    licCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
                    licCell.Style.Font.FontColor = XLColor.FromHtml("#92400E");
                    break;
            }

            // Col 10: Confidence Cell
            var confCell = sheet.Cell(row, 10);
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
            else
            {
                confCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                confCell.Style.Font.FontColor = XLColor.FromHtml("#64748B");
            }

            // Col 11: Plugin Detector
            var detCell = sheet.Cell(row, 11);
            detCell.Style.Alignment.WrapText = true;
            FormatCellOrPlaceholder(detCell, r.PluginName, "Standard Detector");

            // Col 12: Evidence combined text (WrapText true is MANDATORY to prevent long evidence strings from spilling out of table right border)
            var evCell = sheet.Cell(row, 12);
            evCell.Style.Alignment.WrapText = true;
            var evText = r.Evidences.Count > 0
                ? string.Join(" | ", r.Evidences.Select(e => $"[{e.EvidenceType}] {e.Description}"))
                : null;
            FormatCellOrPlaceholder(evCell, evText, "No verification artifacts recorded");

            row++;
        }

        if (row > 2)
        {
            var dataRange = sheet.Range(1, 1, row - 1, 12);
            dataRange.SetAutoFilter();
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        }

        // Apply generous, perfectly balanced and ultra-comfortable column widths (mở rộng ô ra cho thoải mái)
        for (int i = 1; i <= 12; i++)
        {
            var col = sheet.Column(i);
            var headerLen = headers[i - 1].Length;
            var minHeaderWidth = headerLen + 6.0; // +6 padding for bold font and filter icon space

            double explicitWidth = i switch
            {
                1 => Math.Max(minHeaderWidth, 40), // Software Package (wide enough for long titles)
                2 => Math.Max(minHeaderWidth, 20), // Version
                3 => Math.Max(minHeaderWidth, 32), // Publisher
                4 => Math.Max(minHeaderWidth, 80), // Install Path (Mở rộng cực kỳ thoải mái để đường dẫn dài nằm trọn trên 1 hoặc 2 dòng thoáng đãng)
                5 => Math.Max(minHeaderWidth, 24), // Install Date
                6 => Math.Max(minHeaderWidth, 30), // Last Modified (VN Time)
                7 => Math.Max(minHeaderWidth, 32), // Last Used / Active (VN Time)
                8 => Math.Max(minHeaderWidth, 36), // Scan Source
                9 => Math.Max(minHeaderWidth, 22), // License Type
                10 => Math.Max(minHeaderWidth, 22), // Confidence
                11 => Math.Max(minHeaderWidth, 42), // Plugin Detector
                12 => Math.Max(minHeaderWidth, 110), // Verification Evidence (Mở rộng tối đa 110 ký tự để chuỗi bằng chứng dài chạy thoải mái không bao giờ bị xén chân chữ)
                _ => 30
            };
            col.Width = explicitWidth;
        }

        // Calculate safe, spacious row heights to prevent ClosedXML's AdjustToContents from clipping/slicing the bottom of multiline wrapped text
        for (int rRow = 2; rRow < row; rRow++)
        {
            int maxLinesInRow = 1;
            for (int colIdx = 1; colIdx <= 12; colIdx++)
            {
                var val = sheet.Cell(rRow, colIdx).GetString();
                if (!string.IsNullOrEmpty(val))
                {
                    double colW = sheet.Column(colIdx).Width;
                    // Count lines needed based on length / width ratio + explicit newlines
                    int estimatedLines = (int)Math.Ceiling(val.Length / Math.Max(1.0, colW - 3.0));
                    int newlineCount = val.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
                    int lines = Math.Max(estimatedLines, newlineCount);
                    if (lines > maxLinesInRow) maxLinesInRow = lines;
                }
            }

            // Set generous height: 26pt for 1 line, +24pt for each additional wrapped line (48pt for 2 lines, 72pt for 3 lines) to guarantee zero bottom-clipping (đảm bảo không bao giờ xén chữ)
            sheet.Row(rRow).Height = Math.Max(26.0, maxLinesInRow * 24.0);
        }
    }

    private static void FormatCellOrPlaceholder(IXLCell cell, string? value, string placeholder)
    {
        if (!string.IsNullOrWhiteSpace(value) && !value.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
        {
            cell.Value = value;
            cell.Style.Font.FontColor = XLColor.FromHtml("#0F172A");
        }
        else
        {
            cell.Value = placeholder;
            cell.Style.Font.FontColor = XLColor.FromHtml("#64748B");
            cell.Style.Font.Italic = true;
        }
    }
}
