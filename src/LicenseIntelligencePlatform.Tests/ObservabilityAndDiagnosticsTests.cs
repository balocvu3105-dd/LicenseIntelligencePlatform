using System.IO.Compression;
using System.Text.Json;
using LicenseIntelligencePlatform.Application.Logging;
using LicenseIntelligencePlatform.Application.Services;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;
using LicenseIntelligencePlatform.Infrastructure.Diagnostics;
using LicenseIntelligencePlatform.Infrastructure.Exporters;
using LicenseIntelligencePlatform.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LicenseIntelligencePlatform.Tests;

public class ObservabilityAndDiagnosticsTests
{
    [Fact]
    public async Task StructuredFileLogger_RoutesEventsAndAttachesScanId()
    {
        var tempLogDir = Path.Combine(Path.GetTempPath(), $"lip_test_logs_{Guid.NewGuid():N}");
        try
        {
            var sink = new FileLogSink(tempLogDir);
            using var provider = new StructuredFileLoggerProvider(sink);
            var logger = provider.CreateLogger("TestCategory");

            var testScanId = Guid.NewGuid().ToString("D");
            using (logger.BeginScanScope(testScanId))
            {
                logger.LogInformation("Test info message inside scope");
                logger.LogPerformance("Scanner", "WinReg", 15, "10 items");
                logger.LogAudit("Docker", "CommercialPlugin", "Verified", "Commercial", new[] { "Key found" }, "Valid license");
                logger.LogError(new InvalidOperationException("Simulated error"), "Error inside scope");
            }

            await sink.FlushAsync();
            sink.Dispose();

            var appPath = Path.Combine(tempLogDir, "application.log");
            var errPath = Path.Combine(tempLogDir, "error.log");
            var perfPath = Path.Combine(tempLogDir, "performance.log");
            var auditPath = Path.Combine(tempLogDir, "audit.log");

            Assert.True(File.Exists(appPath), "application.log should exist");
            Assert.True(File.Exists(errPath), "error.log should exist");
            Assert.True(File.Exists(perfPath), "performance.log should exist");
            Assert.True(File.Exists(auditPath), "audit.log should exist");

            var appLines = await File.ReadAllLinesAsync(appPath);
            Assert.Equal(4, appLines.Length);
            foreach (var line in appLines)
            {
                Assert.Contains(testScanId, line);
            }

            var perfLines = await File.ReadAllLinesAsync(perfPath);
            Assert.Single(perfLines);
            Assert.Contains("Performance", perfLines[0]);
            Assert.Contains(testScanId, perfLines[0]);

            var auditLines = await File.ReadAllLinesAsync(auditPath);
            Assert.Single(auditLines);
            Assert.Contains("Audit", auditLines[0]);
            Assert.Contains("Docker", auditLines[0]);

            var errLines = await File.ReadAllLinesAsync(errPath);
            Assert.Single(errLines);
            Assert.Contains("Simulated error", errLines[0]);
        }
        finally
        {
            if (Directory.Exists(tempLogDir)) Directory.Delete(tempLogDir, true);
        }
    }

    [Fact]
    public async Task DiagnosticExporter_CreatesZipPackageWithAllArtifacts()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"lip_diag_test_{Guid.NewGuid():N}");
        var logsDir = Path.Combine(tempDir, "logs");
        Directory.CreateDirectory(logsDir);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(logsDir, "application.log"), "{\"msg\":\"app\"}");
            await File.WriteAllTextAsync(Path.Combine(logsDir, "error.log"), "{\"msg\":\"err\"}");
            await File.WriteAllTextAsync(Path.Combine(logsDir, "performance.log"), "{\"msg\":\"perf\"}");
            await File.WriteAllTextAsync(Path.Combine(logsDir, "audit.log"), "{\"msg\":\"audit\"}");

            var software = new SoftwareInfo("SoftA", "1.0", "TestPublisher", "C:/App", "Test App", "hash123");
            var report = new ScanReport(
                hostName: "TEST-PC",
                osDescription: "Test OS",
                startedAtUtc: DateTime.UtcNow.AddSeconds(-2),
                completedAtUtc: DateTime.UtcNow,
                totalSoftwareScanned: 1,
                totalPluginsExecuted: 1,
                results: new[]
                {
                    new LicenseCheckResult("p.1", "TestPlugin", software, LicenseType.Commercial, "Comm", ConfidenceLevel.High, Array.Empty<Evidence>())
                }
            );

            var zipPath = Path.Combine(tempDir, "diagnostic.zip");
            var mapperLogger = NullLogger<JsonReportMapper>.Instance;
            var mapper = new JsonReportMapper(mapperLogger);
            var exporterLogger = NullLogger<DiagnosticExporter>.Instance;
            var exporter = new DiagnosticExporter(mapper, exporterLogger);

            var exportedPath = await exporter.ExportDiagnosticPackageAsync(report, logsDir, zipPath);

            Assert.True(File.Exists(exportedPath));
            using var archive = ZipFile.OpenRead(exportedPath);
            var entryNames = archive.Entries.Select(e => e.Name).ToList();

            Assert.Contains("scan-result.json", entryNames);
            Assert.Contains("application.log", entryNames);
            Assert.Contains("error.log", entryNames);
            Assert.Contains("performance.log", entryNames);
            Assert.Contains("audit.log", entryNames);
            Assert.Contains("environment.json", entryNames);
            Assert.Contains("version.txt", entryNames);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }


    [Fact]
    public void CrossMachineDataSanitizationGuard_WipesOldDataWhenHostChanges()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"lip_guard_test_{Guid.NewGuid():N}");
        var logsDir = Path.Combine(tempRoot, "logs");
        var reportsDir = Path.Combine(tempRoot, "reports");
        Directory.CreateDirectory(logsDir);
        Directory.CreateDirectory(reportsDir);

        try
        {
            // Simulate foreign machine data transferred or downloaded from another person's computer
            File.WriteAllText(Path.Combine(logsDir, "application.log"), "User A secret sensitive logs");
            File.WriteAllText(Path.Combine(reportsDir, "license_report.csv"), "User A scanned software");
            File.WriteAllText(Path.Combine(logsDir, ".host_identity"), "FOREIGN-USER-MACHINE-PC\nOS: Windows 10");

            // Execute zero-tolerance cross-machine guard
            CrossMachineDataSanitizationGuard.EnforceZeroToleranceIsolation(reportsDir, logsDir);

            // Verify all previous session data was completely wiped to prevent cross-user leakage
            Assert.False(File.Exists(Path.Combine(logsDir, "application.log")), "Old logs from another machine must be wiped");
            Assert.False(File.Exists(Path.Combine(reportsDir, "license_report.csv")), "Old reports from another machine must be wiped");

            // Verify host identity is updated to current machine
            Assert.True(File.Exists(Path.Combine(logsDir, ".host_identity")), ".host_identity tracker must exist");
            var newIdentity = File.ReadAllText(Path.Combine(logsDir, ".host_identity"));
            Assert.Contains(Environment.MachineName.Trim().ToUpperInvariant(), newIdentity);
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
        }
    }
}

