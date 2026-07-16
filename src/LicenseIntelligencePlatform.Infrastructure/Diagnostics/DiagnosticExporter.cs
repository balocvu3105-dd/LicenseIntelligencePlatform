using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LicenseIntelligencePlatform.Infrastructure.Diagnostics;

/// <summary>
/// Implements <see cref="IDiagnosticExporter"/> to create compressed diagnostic zip archives (`diagnostic.zip`)
/// containing scan results, active log streams, runtime environment context, and version metadata.
/// </summary>
public class DiagnosticExporter : IDiagnosticExporter
{
    private readonly IReportMapper _jsonMapper;
    private readonly ILogger<DiagnosticExporter> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DiagnosticExporter"/>.
    /// </summary>
    /// <param name="jsonMapper">The JSON report mapper for exporting scan-result.json.</param>
    /// <param name="logger">Logger for recording diagnostic package generation events.</param>
    public DiagnosticExporter(IReportMapper jsonMapper, ILogger<DiagnosticExporter> logger)
    {
        _jsonMapper = jsonMapper ?? throw new ArgumentNullException(nameof(jsonMapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<string> ExportDiagnosticPackageAsync(ScanReport report, string logsDirectory, string outputZipPath, CancellationToken cancellationToken = default)
    {
        if (report == null) throw new ArgumentNullException(nameof(report));
        if (string.IsNullOrWhiteSpace(outputZipPath)) throw new ArgumentNullException(nameof(outputZipPath));

        _logger.LogInformation("Generating diagnostic support package at '{OutputZipPath}'...", outputZipPath);

        var outputDir = Path.GetDirectoryName(outputZipPath);
        if (!string.IsNullOrEmpty(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var tempDir = Path.Combine(Path.GetTempPath(), $"lip_diag_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // 1. Export scan-result.json
            var scanResultPath = Path.Combine(tempDir, "scan-result.json");
            using (var fs = new FileStream(scanResultPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await _jsonMapper.ExportAsync(report, fs, cancellationToken).ConfigureAwait(false);
            }

            // 2. Copy active log files from logsDirectory
            if (Directory.Exists(logsDirectory))
            {
                var logFiles = new[] { "application.log", "error.log", "performance.log", "audit.log" };
                foreach (var logFile in logFiles)
                {
                    var sourcePath = Path.Combine(logsDirectory, logFile);
                    if (File.Exists(sourcePath))
                    {
                        var destPath = Path.Combine(tempDir, logFile);
                        // Use FileShare.ReadWrite so active file sink locks don't block copying
                        using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        using var destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);
                        await sourceStream.CopyToAsync(destStream, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            // 3. Export environment.json
            var envInfo = new
            {
                MachineName = Environment.MachineName,
                OSDescription = RuntimeInformation.OSDescription,
                OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
                ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                ProcessorCount = Environment.ProcessorCount,
                FrameworkDescription = RuntimeInformation.FrameworkDescription,
                WorkingSetBytes = Environment.WorkingSet,
                TimestampUtc = DateTime.UtcNow
            };

            var envPath = Path.Combine(tempDir, "environment.json");
            var envJson = JsonSerializer.Serialize(envInfo, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(envPath, envJson, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

            // 4. Export version.txt
            var versionPath = Path.Combine(tempDir, "version.txt");
            var versionText = "License Intelligence Platform (LIP) v1.0.0 - Pilot Freeze\nDiagnostic Support Package\nGenerated At: " + DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss 'UTC'");
            await File.WriteAllTextAsync(versionPath, versionText, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

            // 5. Create diagnostic zip package
            if (File.Exists(outputZipPath))
            {
                File.Delete(outputZipPath);
            }

            ZipFile.CreateFromDirectory(tempDir, outputZipPath, CompressionLevel.Optimal, includeBaseDirectory: false);

            _logger.LogInformation("Diagnostic package successfully exported to '{OutputZipPath}'.", outputZipPath);
            return Path.GetFullPath(outputZipPath);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch
            {
                // Ignore temporary directory cleanup issues
            }
        }
    }
}
