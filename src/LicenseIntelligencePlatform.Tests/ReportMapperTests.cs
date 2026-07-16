using System.Text;
using System.Text.Json;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Infrastructure.Exporters;
using Microsoft.Extensions.Logging.Abstractions;
using ClosedXML.Excel;
using Xunit;

namespace LicenseIntelligencePlatform.Tests;

public class ReportMapperTests
{
    [Fact]
    public async Task CsvReportMapper_ShouldExportValidCsvFormat()
    {
        // Arrange
        var mapper = new CsvReportMapper(NullLogger<CsvReportMapper>.Instance);
        var report = CreateSampleReport();
        using var stream = new MemoryStream();

        // Act
        await mapper.ExportAsync(report, stream);
        var csvContent = Encoding.UTF8.GetString(stream.ToArray());

        // Assert
        Assert.Contains("SoftwareName,Version,Publisher", csvContent);
        Assert.Contains("\"Test App\",\"1.0.0\",\"Test Vendor\"", csvContent);
        Assert.Contains("Commercial", csvContent);
    }

    [Fact]
    public async Task JsonReportMapper_ShouldExportValidJsonStructure()
    {
        // Arrange
        var mapper = new JsonReportMapper(NullLogger<JsonReportMapper>.Instance);
        var report = CreateSampleReport();
        using var stream = new MemoryStream();

        // Act
        await mapper.ExportAsync(report, stream);
        stream.Position = 0;
        using var doc = await JsonDocument.ParseAsync(stream);

        // Assert
        Assert.Equal(report.ScanId, doc.RootElement.GetProperty("ScanId").GetString());
        Assert.Equal(report.TotalSoftwareScanned, doc.RootElement.GetProperty("TotalSoftwareScanned").GetInt32());
        var resultsArray = doc.RootElement.GetProperty("Results");
        Assert.Equal(1, resultsArray.GetArrayLength());
    }

    [Fact]
    public async Task ExcelReportMapper_ShouldExportValidExcelWorkbook()
    {
        // Arrange
        var mapper = new ExcelReportMapper();
        var report = CreateSampleReport();
        using var stream = new MemoryStream();

        // Act
        await mapper.ExportAsync(report, stream);
        stream.Position = 0;
        using var workbook = new XLWorkbook(stream);

        // Assert
        Assert.Equal("XLSX", mapper.FormatName);
        Assert.Equal(4, workbook.Worksheets.Count);
        Assert.True(workbook.Worksheets.Contains("Executive Dashboard"));
        Assert.True(workbook.Worksheets.Contains("Full Inventory & Audit"));
        Assert.True(workbook.Worksheets.Contains("Commercial Licenses (Action)"));
        Assert.True(workbook.Worksheets.Contains("Open Source Compliance"));
    }

    private static ScanReport CreateSampleReport()
    {
        var software = new SoftwareInfo("Test App", "1.0.0", "Test Vendor", "/opt/test");
        var evidence = new Evidence("KeyCheck", "Found .lic file", "/opt/test/app.lic");
        var result = new LicenseCheckResult(
            pluginId: "test.plugin",
            pluginName: "Test Plugin",
            software: software,
            detectedLicenseType: LicenseType.Commercial,
            licenseName: "Commercial Suite",
            confidence: ConfidenceLevel.High,
            evidences: new[] { evidence },
            notes: "Test evaluation note."
        );

        return new ScanReport(
            hostName: "test-box",
            osDescription: "Linux 6.0",
            startedAtUtc: DateTime.UtcNow.AddMinutes(-1),
            completedAtUtc: DateTime.UtcNow,
            totalSoftwareScanned: 1,
            totalPluginsExecuted: 1,
            results: new[] { result }
        );
    }
}
