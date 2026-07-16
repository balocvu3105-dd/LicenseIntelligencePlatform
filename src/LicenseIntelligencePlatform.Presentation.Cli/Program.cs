using System.Text;
using LicenseIntelligencePlatform.Application.Services;
using LicenseIntelligencePlatform.Domain;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;
using LicenseIntelligencePlatform.Infrastructure.Diagnostics;
using LicenseIntelligencePlatform.Infrastructure.Exporters;
using LicenseIntelligencePlatform.Infrastructure.Logging;
using LicenseIntelligencePlatform.Infrastructure.Scanners;
using LicenseIntelligencePlatform.Plugins.Standard.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LicenseIntelligencePlatform.Presentation.Cli;

/// <summary>
/// Main console entry point for the License Intelligence Platform CLI application.
/// Sets up Dependency Injection across layers and orchestrates inventory and license check operations.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        PrintHeader();

        // 1. Parse CLI Options
        var options = ParseCliArguments(args);

        // Interactive Welcome Card (Hold before run - UX Feedback #1)
        if (!options.NoPause && Environment.UserInteractive && !Console.IsInputRedirected && !Console.IsOutputRedirected)
        {
            Console.WriteLine(" [Giới Thiệu]: Hệ thống kiểm kê toàn bộ phần mềm & rà soát bản quyền tự động trên máy tính.");
            Console.WriteLine(" [Cam Kết Bảo Mật]:");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("    ✔ 100% Read-Only (Chỉ đọc, tuyệt đối không ghi/sửa/xóa bất kỳ file/registry nào của bạn).");
            Console.WriteLine("    ✔ 100% Offline   (Hoạt động cục bộ trên máy, không kết nối internet/gửi dữ liệu ra ngoài).");
            Console.WriteLine("    ✔ Anti-Tamper    (Tự động khóa chỉ đọc & ký mã băm SHA-256 chống giả mạo kết quả).");
            Console.ResetColor();
            Console.WriteLine("==========================================================================================");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(" 👉 Nhấn phím [ENTER] để bắt đầu kiểm kê hệ thống... (Hoặc phím [ESC] để thoát): ");
            Console.ResetColor();
            try
            {
                while (Console.KeyAvailable) Console.ReadKey(true);
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine("\n[+] Đã hủy lệnh kiểm kê. Hẹn gặp lại!");
                    return 0;
                }
                Console.WriteLine();
                Console.WriteLine();
            }
            catch { /* Fallback */ }
        }

        // [ZERO-TOLERANCE SECURITY] Enforce Cross-Machine Data Isolation Guard
        // Guarantees that any historical logs or reports generated on one computer are immediately wiped if transferred to a new computer.
        CrossMachineDataSanitizationGuard.EnforceZeroToleranceIsolation(options.OutputDirectory, Path.Combine(Directory.GetCurrentDirectory(), "logs"));

        // 2. Configure Dependency Injection (Clean Architecture Layer Wiring)

        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddStructuredFileLogging(Path.Combine(Directory.GetCurrentDirectory(), "logs"));
                if (options.Verbose)
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Debug);
                }
            })
            .ConfigureServices(services =>
            {
                // Domain & Application Services
                services.AddSingleton<IPluginLoader>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<PluginLoaderService>>();
                    var standardPlugins = new ILicensePlugin[]
                    {
                        new WindowsOsLicensePlugin(),
                        new OpenSourceArtifactPlugin(),
                        new CommercialKeyFilePlugin(),
                        new FreewarePatternPlugin(),
                        new MicrosoftOfficePlugin(),
                        new AdobeCreativeCloudPlugin(),
                        new AutodeskLicensePlugin(),
                        new VMwareWorkstationPlugin(),
                        new SqlServerLicensePlugin(),
                        new JetBrainsIdesPlugin(),
                        new DockerDesktopPlugin(),
                        new SentinelHaspPlugin(),
                        new FlexLmPlugin(),
                        new MatlabLicensePlugin(),
                        new GitOpenSourcePlugin(),
                        new NodeJsPermissivePlugin(),
                        new PythonEcosystemPlugin(),
                        new PostmanCommercialPlugin(),
                        new FigmaDesignPlugin(),
                        new TablePlusLicensePlugin(),
                        new UnityGameEnginePlugin(),
                        new OracleJavaPlugin(),
                        new CiscoAnyConnectPlugin(),
                        new RedHatEnterprisePlugin(),
                        new NvidiaEnterprisePlugin(),
                        new ObsStudioLicensePlugin(),
                        new VlcPlayerPlugin(),
                        new MicrosoftRuntimeEcosystemPlugin(),
                        new GamingPlatformsEcosystemPlugin(),
                        new CommunicationCollaboratePlugin(),
                        new HardwareOemUtilitiesPlugin(),
                        new NvidiaConsumerSuitePlugin(),
                        new WebBrowsersAndToolsPlugin(),
                        new AntiCheatAndSecurityPlugin()
                    };
                    var loader = new PluginLoaderService(logger, standardPlugins);

                    if (!string.IsNullOrWhiteSpace(options.PluginsDirectory))
                    {
                        loader.LoadFromDirectory(options.PluginsDirectory);
                    }
                    return loader;
                });

                // Infrastructure Scanners
                services.AddSingleton<WindowsOsScanner>();
                services.AddSingleton<WindowsRegistryScanner>();
                services.AddSingleton<LinuxPackageScanner>();
                services.AddSingleton<DeepFileSystemScanner>();
                services.AddSingleton<IScanner>(sp =>
                {
                    var winOs = sp.GetRequiredService<WindowsOsScanner>();
                    var win = sp.GetRequiredService<WindowsRegistryScanner>();
                    var linux = sp.GetRequiredService<LinuxPackageScanner>();
                    var deep = sp.GetRequiredService<DeepFileSystemScanner>();
                    var logger = sp.GetRequiredService<ILogger<CompositeScanner>>();
                    return new CompositeScanner(new IScanner[] { winOs, win, linux, deep }, logger);
                });

                // Infrastructure Exporters / Mappers & Diagnostics
                services.AddSingleton<IReportMapper, CsvReportMapper>();
                services.AddSingleton<IReportMapper, JsonReportMapper>();
                services.AddSingleton<IReportMapper, AuditReportMapper>();
                services.AddSingleton<IReportMapper, HtmlReportMapper>();
                services.AddSingleton<IReportMapper, ExcelReportMapper>();
                services.AddSingleton<IReportMapper, StatisticsReportMapper>();
                services.AddSingleton<IDiagnosticExporter, DiagnosticExporter>();

                // Phase 2 & Phase 4 — Reporting Engine Mappers
                services.AddSingleton<ExecutiveSummaryMapper>();
                services.AddSingleton<EvidenceReportMapper>();
                services.AddSingleton<AuditReportMapper>();
                services.AddSingleton<HtmlReportMapper>();
                services.AddSingleton<ExcelReportMapper>();
                services.AddSingleton<StatisticsReportMapper>();

                // Phase 2 — Plugin SDK: Software Merge Engine + Compatibility Validator
                services.AddSingleton<ISoftwareMergeEngine, SoftwareMergeEngine>();
                services.AddSingleton<PluginCompatibilityValidator>();

                // Core Engine Orchestrator
                services.AddSingleton<ICoreEngine, CoreEngine>();
            })
            .Build();

        // 3. Execute Core Engine Scan
        var coreEngine = host.Services.GetRequiredService<ICoreEngine>();
        using var cts = new CancellationTokenSource();
        
        Console.WriteLine("[*] Initializing system inventory scanners and verifying plugins...");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var startMem = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
        
        ScanReport report;
        try
        {
            report = await coreEngine.ExecuteFullScanAsync(cts.Token);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[!] Critical engine error: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
        sw.Stop();
        var endMem = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;

        // 4. Print Rich Summary Table & Benchmark
        PrintSummaryTable(report, sw.ElapsedMilliseconds, startMem, endMem);

        // 5. Export Reports and Backlog if required
        await ExportReportsAndBacklogAsync(host.Services, report, options, cts.Token);

        // 6. Interactive Post-Scan Summary & Auto-Open Check (UX Feedback #2 & #3)
        if (Environment.UserInteractive && !Console.IsInputRedirected && !Console.IsOutputRedirected && !options.NoPause)
        {
            var timestampStr = VietnamTime.Now.ToString("yyyy-MM-dd_HH'h'mm'm'");
            var displayDir = string.IsNullOrEmpty(options.MasterWorkspaceDirectory) ? options.OutputDirectory : options.MasterWorkspaceDirectory;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("==========================================================================================");
            Console.WriteLine("  🎉 QUÁ TRÌNH KIỂM KÊ HOÀN TẤT 100% & TOÀN BỘ KẾT QUẢ ĐÃ ĐƯỢC BẢO MẬT CHỈ ĐỌC (READ-ONLY)!");
            Console.WriteLine("==========================================================================================");
            Console.ResetColor();
            Console.WriteLine($" 📁 Toàn bộ hồ sơ kiểm toán được lưu trọn gói tại:\n    -> {displayDir}");
            Console.WriteLine();
            Console.WriteLine(" 📊 2 Báo cáo trực quan chính:");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"    ✔ Excel: Bao_Cao_Tong_Hop_Ban_Quen_{timestampStr}.xlsx");
            Console.WriteLine($"    ✔ Web:   Bao_Cao_Truc_Quan_Web_{timestampStr}.html");
            Console.ResetColor();
            Console.WriteLine("==========================================================================================");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(" 👉 Nhấn phím [ENTER / Y] để mở ngay Báo cáo Web HTML (hoặc phím khác để đóng cửa sổ): ");
            Console.ResetColor();
            try
            {
                while (Console.KeyAvailable) Console.ReadKey(true);
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Y)
                {
                    var htmlPath = Path.Combine(displayDir, $"Bao_Cao_Truc_Quan_Web_{timestampStr}.html");
                    if (!File.Exists(htmlPath)) htmlPath = Path.Combine(options.OutputDirectory, $"license_report_{report.ScanId}.html");
                    if (File.Exists(htmlPath))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(htmlPath) { UseShellExecute = true });
                    }
                }
            }
            catch
            {
                // Fallback for non-interactive window handles
            }
        }

        return 0;
    }

    private static void PrintHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==========================================================================================");
        Console.WriteLine("                  LICENSE INTELLIGENCE PLATFORM v1.0 (Clean Architecture)                 ");
        Console.WriteLine("    Read-Only Software Inventory & Automatic License Verification Engine (SOLID + DI)     ");
        Console.WriteLine("                 Copyright (c) 2026 Bá Lộc Vũ (DynamiteV) • All Rights Reserved           ");
        Console.WriteLine("==========================================================================================");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static CliOptions ParseCliArguments(string[] args)
    {
        var options = new CliOptions();
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLowerInvariant();
            if (arg == "--output" || arg == "-o")
            {
                if (i + 1 < args.Length) options.OutputDirectory = args[++i];
            }
            else if (arg == "--format" || arg == "-f")
            {
                if (i + 1 < args.Length) options.Format = args[++i].ToUpperInvariant();
            }
            else if (arg == "--plugins" || arg == "-p")
            {
                if (i + 1 < args.Length) options.PluginsDirectory = args[++i];
            }
            else if (arg == "--verbose" || arg == "-v")
            {
                options.Verbose = true;
            }
            else if (arg == "--diagnostic" || arg == "-d")
            {
                options.Diagnostic = true;
            }
            else if (arg == "--no-pause" || arg == "--batch")
            {
                options.NoPause = true;
            }
        }
        return options;
    }

    private static void PrintSummaryTable(ScanReport report, long totalElapsedMs, long startMemBytes, long endMemBytes)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"Scan Complete across Host: {report.HostName} ({report.OSDescription})");
        Console.WriteLine($"Total Software Scanned: {report.TotalSoftwareScanned} | Plugins Executed: {report.TotalPluginsExecuted} | Check Results: {report.Results.Count}");
        
        // Print Performance & System Benchmark
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        var memDiffMb = Math.Max(0, (endMemBytes - startMemBytes) / (1024.0 * 1024.0));
        var engineDurationMs = (report.CompletedAtUtc - report.StartedAtUtc).TotalMilliseconds;
        Console.WriteLine($"[BENCHMARK] Total Elapsed Time: {totalElapsedMs} ms ({engineDurationMs:F2} ms engine) | Memory Footprint Delta: {memDiffMb:F2} MB | Throughput: {(report.TotalSoftwareScanned > 0 ? (report.TotalSoftwareScanned / (totalElapsedMs / 1000.0 + 0.001)): 0):F1} pkgs/sec");
        Console.ResetColor();
        Console.WriteLine();

        var auditData = WindowsLicenseAuditContext.CurrentAuditData;
        if (auditData != null)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==========================================================================================");
            Console.WriteLine("             WINDOWS LICENSE AUDIT & SYSTEM COMPLIANCE INSPECTION MODULE                  ");
            Console.WriteLine("==========================================================================================");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"  OS Edition & Build : {auditData.WindowsEdition} (Build {auditData.BuildNumber} - {auditData.Architecture})");
            Console.WriteLine($"  Activation Status  : {auditData.ActivationStatus} [{auditData.LicenseChannel}]");
            Console.WriteLine($"  Installed Key      : {auditData.InstalledProductKeyMasked} | OEM BIOS Key Present: {auditData.OemKeyPresence}");
            if (!string.IsNullOrWhiteSpace(auditData.BiosEmbeddedKey))
                Console.WriteLine($"  OEM Embedded Key   : {auditData.BiosEmbeddedKey}");
            
            Console.Write("  Audit Risk Score   : ");
            if (auditData.RiskScore >= 50) Console.ForegroundColor = ConsoleColor.Red;
            else if (auditData.RiskScore >= 16) Console.ForegroundColor = ConsoleColor.Yellow;
            else Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{auditData.RiskScore} / 100 ({auditData.RiskClassification})");
            Console.ForegroundColor = ConsoleColor.White;

            if (auditData.AuditEvidences.Count > 0)
            {
                Console.WriteLine($"  Discovered Artifacts ({auditData.AuditEvidences.Count}):");
                foreach (var ev in auditData.AuditEvidences.Take(5))
                {
                    Console.ForegroundColor = ev.Severity == "CRITICAL" || ev.Severity == "HIGH" ? ConsoleColor.Red : ConsoleColor.DarkGray;
                    Console.WriteLine($"   • [{ev.EvidenceType}] {ev.Description}");
                }
                if (auditData.AuditEvidences.Count > 5)
                    Console.WriteLine($"   ... (plus {auditData.AuditEvidences.Count - 5} more evidence items detailed in export reports)");
            }

            if (auditData.Recommendations.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                foreach (var rec in auditData.Recommendations)
                {
                    Console.WriteLine($"  {rec}");
                }
            }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==========================================================================================");
            Console.ResetColor();
            Console.WriteLine();
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(string.Format("{0,-35} | {1,-15} | {2,-16} | {3,-10} | {4,-5}", 
            "Software Name", "Version", "License Type", "Confidence", "Evid"));
        Console.WriteLine(new string('-', 90));
        Console.ResetColor();

        // Print top 30 results or all if small
        var displayResults = report.Results.Take(40).ToList();
        foreach (var r in displayResults)
        {
            var name = r.Software.Name.Length > 34 ? r.Software.Name.Substring(0, 31) + "..." : r.Software.Name;
            var ver = r.Software.Version.Length > 14 ? r.Software.Version.Substring(0, 11) + "..." : r.Software.Version;
            var lic = r.DetectedLicenseType.ToString().Length > 16 ? r.DetectedLicenseType.ToString().Substring(0, 13) + "..." : r.DetectedLicenseType.ToString();

            if (r.Confidence >= Domain.Enums.ConfidenceLevel.High) Console.ForegroundColor = ConsoleColor.Green;
            else if (r.Confidence == Domain.Enums.ConfidenceLevel.Medium) Console.ForegroundColor = ConsoleColor.Cyan;
            else if (r.Confidence == Domain.Enums.ConfidenceLevel.Low) Console.ForegroundColor = ConsoleColor.DarkYellow;
            else Console.ForegroundColor = ConsoleColor.Gray;

            Console.WriteLine(string.Format("{0,-35} | {1,-15} | {2,-16} | {3,-10} | {4,-5}",
                name, ver, lic, r.Confidence, r.Evidences.Count));
            Console.ResetColor();
        }

        if (report.Results.Count > displayResults.Count)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"... and {report.Results.Count - displayResults.Count} more items exported to report files.");
            Console.ResetColor();
        }
        Console.WriteLine(new string('-', 90));

        // Print Unknown Software Backlog Summary
        var unknowns = report.Results.Where(r => r.DetectedLicenseType == LicenseType.Unknown || r.Confidence == Domain.Enums.ConfidenceLevel.None).ToList();
        if (unknowns.Count > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[BACKLOG - NEED PLUGIN] {unknowns.Count} software packages require dedicated license detection plugins:");
            Console.ResetColor();
            foreach (var u in unknowns.Take(15))
            {
                Console.WriteLine($"  -> [Need Plugin] {u.Software.Name} ({u.Software.Version}) by {u.Software.Publisher ?? "Unknown Publisher"}");
            }
            if (unknowns.Count > 15) Console.WriteLine($"  ... plus {unknowns.Count - 15} more logged to backlog_need_plugins.json.");
            Console.WriteLine();
        }

        Console.WriteLine();
    }

    private static async Task ExportReportsAndBacklogAsync(IServiceProvider services, ScanReport report, CliOptions options, CancellationToken cancellationToken)
    {
        var timestampStr = VietnamTime.Now.ToString("yyyy-MM-dd_HH'h'mm'm'");
        string masterDir;
        string rawDataDir;

        if (string.IsNullOrWhiteSpace(options.OutputDirectory) || options.OutputDirectory == "reports")
        {
            masterDir = Path.Combine(Directory.GetCurrentDirectory(), $"License_Audit_Results_{timestampStr}");
            rawDataDir = Path.Combine(masterDir, "raw_data");
        }
        else
        {
            masterDir = options.OutputDirectory;
            rawDataDir = Path.Combine(masterDir, "raw_data");
        }

        options.MasterWorkspaceDirectory = masterDir;
        options.OutputDirectory = rawDataDir;

        Directory.CreateDirectory(masterDir);
        Directory.CreateDirectory(rawDataDir);

        // Clear Read-Only locks from existing report files before overwriting
        try
        {
            foreach (var existingFile in Directory.GetFiles(rawDataDir))
            {
                var attr = File.GetAttributes(existingFile);
                if ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly) File.SetAttributes(existingFile, attr & ~FileAttributes.ReadOnly);
            }
            foreach (var existingFile in Directory.GetFiles(masterDir))
            {
                var attr = File.GetAttributes(existingFile);
                if ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly) File.SetAttributes(existingFile, attr & ~FileAttributes.ReadOnly);
            }
        }
        catch { /* Ignore access issues during cleanup attempt */ }

        var exportedFiles = new List<string>();

        var mappers = services.GetServices<IReportMapper>();
        foreach (var mapper in mappers)
        {
            if (options.Format != "BOTH" && !mapper.FormatName.Equals(options.Format, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var extension = mapper.FormatName.ToLowerInvariant();
            var filePath = Path.Combine(rawDataDir, $"license_report_{report.ScanId}.{extension}");

            Console.WriteLine($"[+] Exporting {mapper.FormatName} report to: {filePath}");
            using var fileStream = File.Create(filePath);
            await mapper.ExportAsync(report, fileStream, cancellationToken);
            exportedFiles.Add(filePath);

            // Create Master Workspace copy for Excel and HTML reports in the root folder for easy user access
            if (mapper.FormatName == "XLSX" || mapper.FormatName == "EXCEL")
            {
                var rootExcelPath = Path.Combine(masterDir, $"Bao_Cao_Tong_Hop_Ban_Quen_{timestampStr}.xlsx");
                using var rootExcelStream = File.Create(rootExcelPath);
                await mapper.ExportAsync(report, rootExcelStream, cancellationToken);
                exportedFiles.Add(rootExcelPath);
            }
            else if (mapper.FormatName == "HTML")
            {
                var rootHtmlPath = Path.Combine(masterDir, $"Bao_Cao_Truc_Quan_Web_{timestampStr}.html");
                using var rootHtmlStream = File.Create(rootHtmlPath);
                await mapper.ExportAsync(report, rootHtmlStream, cancellationToken);
                exportedFiles.Add(rootHtmlPath);
            }
        }

        // Phase 2 — Reporting Engine: Executive Summary
        var execSummaryMapper = services.GetRequiredService<ExecutiveSummaryMapper>();
        var execSummaryPath = Path.Combine(rawDataDir, $"executive_summary_{report.ScanId}.txt");
        Console.WriteLine($"[+] Exporting Executive Summary to: {execSummaryPath}");
        using (var execStream = File.Create(execSummaryPath))
        {
            await execSummaryMapper.ExportAsync(report, execStream, cancellationToken);
        }
        exportedFiles.Add(execSummaryPath);

        // Phase 2 — Reporting Engine: Evidence Report
        var evidenceMapper = services.GetRequiredService<EvidenceReportMapper>();
        var evidencePath = Path.Combine(rawDataDir, $"evidence_report_{report.ScanId}.txt");
        Console.WriteLine($"[+] Exporting Evidence Report to: {evidencePath}");
        using (var evidenceStream = File.Create(evidencePath))
        {
            await evidenceMapper.ExportAsync(report, evidenceStream, cancellationToken);
        }
        exportedFiles.Add(evidencePath);

        // Phase 4 — Reporting Engine: Executive Audit Report
        var auditMapper = services.GetRequiredService<AuditReportMapper>();
        var auditPath = Path.Combine(rawDataDir, $"audit_report_{report.ScanId}.md");
        Console.WriteLine($"[+] Exporting Executive Audit Report to: {auditPath}");
        using (var auditStream = File.Create(auditPath))
        {
            await auditMapper.ExportAsync(report, auditStream, cancellationToken);
        }
        exportedFiles.Add(auditPath);

        // Phase 4 — Reporting Engine: Statistics Analytics Report
        var statsMapper = services.GetRequiredService<StatisticsReportMapper>();
        var statsPath = Path.Combine(rawDataDir, $"statistics_report_{report.ScanId}.json");
        Console.WriteLine($"[+] Exporting Statistics Report to: {statsPath}");
        using (var statsStream = File.Create(statsPath))
        {
            await statsMapper.ExportAsync(report, statsStream, cancellationToken);
        }
        exportedFiles.Add(statsPath);

        // Export Unknown Software Backlog
        var unknowns = report.Results
            .Where(r => r.DetectedLicenseType == LicenseType.Unknown || r.Confidence == Domain.Enums.ConfidenceLevel.None)
            .Select(r => new
            {
                SoftwareName = r.Software.Name,
                Version = r.Software.Version,
                Publisher = r.Software.Publisher,
                InstallPath = r.Software.InstallPath,
                Status = "Need Plugin",
                LoggedAt = VietnamTime.Format(DateTime.UtcNow)
            }).ToList();

        if (unknowns.Count > 0)
        {
            var backlogPath = Path.Combine(rawDataDir, "backlog_need_plugins.json");
            var json = System.Text.Json.JsonSerializer.Serialize(unknowns, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(backlogPath, json, cancellationToken);
            Console.WriteLine($"[+] Exported {unknowns.Count} unhandled software items to backlog file: {backlogPath}");
            exportedFiles.Add(backlogPath);
        }

        // Flush LogSink and Move system logs inside master workspace folder to prevent outside clutter
        var logSink = services.GetService<ILogSink>();
        if (logSink != null)
        {
            await logSink.FlushAsync(cancellationToken);
        }

        try
        {
            var localLogsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            if (Directory.Exists(localLogsDir))
            {
                var sysLogsDir = Path.Combine(masterDir, "system_logs");
                Directory.CreateDirectory(sysLogsDir);
                foreach (var logFile in Directory.GetFiles(localLogsDir))
                {
                    if (Path.GetFileName(logFile).Equals(".keep", StringComparison.OrdinalIgnoreCase)) continue;
                    var destPath = Path.Combine(sysLogsDir, Path.GetFileName(logFile));
                    File.Copy(logFile, destPath, true);
                    exportedFiles.Add(destPath);
                }
            }
        }
        catch { /* Ignore log copy lock errors */ }

        // Export Diagnostic Package if requested
        if (options.Diagnostic)
        {
            var diagExporter = services.GetRequiredService<IDiagnosticExporter>();
            var zipPath = Path.Combine(rawDataDir, $"diagnostic_{report.ScanId}.zip");
            var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            var createdZip = await diagExporter.ExportDiagnosticPackageAsync(report, logsDir, zipPath, cancellationToken);
            Console.WriteLine($"[+] Diagnostic support package exported to: {createdZip}");
            exportedFiles.Add(createdZip);
        }

        // Security Hardening: Compute SHA-256 Checksums and enforce Read-Only lock on all exported reports
        Console.WriteLine("[+] Enforcing Anti-Tamper SHA-256 Checksum Signature & Read-Only Lock on all exported reports...");
        await ReportSecurityLocker.LockAndSignReportsAsync(report.ScanId, rawDataDir, exportedFiles, cancellationToken);

        // Clean up temporary local logs directory after copying into master workspace
        try
        {
            var localLogsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            if (Directory.Exists(localLogsDir))
            {
                foreach (var logFile in Directory.GetFiles(localLogsDir))
                {
                    if (Path.GetFileName(logFile).Equals(".keep", StringComparison.OrdinalIgnoreCase)) continue;
                    var attr = File.GetAttributes(logFile);
                    if ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly) File.SetAttributes(logFile, attr & ~FileAttributes.ReadOnly);
                    File.Delete(logFile);
                }
            }
        }
        catch { /* Ignore cleanup issues */ }

        Console.WriteLine();
    }

    private class CliOptions
    {
        public string OutputDirectory { get; set; } = "reports";
        public string MasterWorkspaceDirectory { get; set; } = "";
        public string Format { get; set; } = "BOTH";
        public string PluginsDirectory { get; set; } = "";
        public bool Verbose { get; set; } = false;
        public bool Diagnostic { get; set; } = false;
        public bool NoPause { get; set; } = false;
    }
}
