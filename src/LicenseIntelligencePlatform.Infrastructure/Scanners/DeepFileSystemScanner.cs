using System.Diagnostics;
using System.Runtime.InteropServices;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Interfaces;
using LicenseIntelligencePlatform.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;

namespace LicenseIntelligencePlatform.Infrastructure.Scanners;

/// <summary>
/// A specialized read-only heuristics scanner designed to detect stealth, portable, or renamed software packages
/// by inspecting running memory processes and PE binary metadata (VersionInfo / OriginalFilename).
/// </summary>
public class DeepFileSystemScanner : IScanner
{
    private readonly ILogger<DeepFileSystemScanner> _logger;
    private readonly IEnumerable<string> _targetDirectories;

    public DeepFileSystemScanner(ILogger<DeepFileSystemScanner> logger, IEnumerable<string>? targetDirectories = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _targetDirectories = targetDirectories ?? new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs")
        };
    }

    /// <inheritdoc />
    public string ScannerName => "DeepFileSystemScanner";

    /// <inheritdoc />
    public bool IsSupportedOnCurrentPlatform()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SoftwareInfo>> ScanAsync(CancellationToken cancellationToken = default)
    {
        var discoveredSoftware = new Dictionary<string, SoftwareInfo>(StringComparer.OrdinalIgnoreCase);

        // 1. Quét từ bộ nhớ RAM (Running Processes)
        await Task.Run(() => ScanRunningProcesses(discoveredSoftware, cancellationToken), cancellationToken);

        // 2. Quét từ các thư mục rủi ro (Heuristic Directory Scan)
        await Task.Run(() => ScanTargetDirectories(discoveredSoftware, cancellationToken), cancellationToken);

        _logger.LogInformation("DeepFileSystemScanner discovered {Count} stealth/portable packages.", discoveredSoftware.Count);
        return discoveredSoftware.Values;
    }

    private void ScanRunningProcesses(Dictionary<string, SoftwareInfo> results, CancellationToken cancellationToken)
    {
        try
        {
            var processes = Process.GetProcesses();
            foreach (var proc in processes)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    var exePath = proc.MainModule?.FileName;
                    if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath)) continue;

                    // Bỏ qua các tiến trình lõi của hệ thống Windows để tối ưu hiệu năng
                    if (exePath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows), StringComparison.OrdinalIgnoreCase))
                        continue;

                    DateTime? procStartTime = null;
                    try { procStartTime = proc.StartTime.ToUniversalTime(); } catch { /* Win32Exception on protected system processes */ }

                    InspectAndAddExecutable(exePath, results, isRunningProcess: true, procStartTime);
                }
                catch
                {
                    // Bỏ qua các tiến trình system/protected không đủ quyền truy cập
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to inspect running processes in DeepFileSystemScanner.");
        }
    }

    private void ScanTargetDirectories(Dictionary<string, SoftwareInfo> results, CancellationToken cancellationToken)
    {
        foreach (var directory in _targetDirectories)
        {
            if (cancellationToken.IsCancellationRequested) break;
            if (!Directory.Exists(directory)) continue;

            try
            {
                var exeFiles = Directory.EnumerateFiles(directory, "*.exe", SearchOption.TopDirectoryOnly);
                foreach (var exePath in exeFiles.Take(50)) // Giới hạn quét 50 file mỗi thư mục để đảm bảo benchmark < 100ms
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    InspectAndAddExecutable(exePath, results, isRunningProcess: false, processStartTime: null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Directory scan access denied or failed for path: {Directory}", directory);
            }
        }
    }

    private void InspectAndAddExecutable(string exePath, Dictionary<string, SoftwareInfo> results, bool isRunningProcess, DateTime? processStartTime)
    {
        try
        {
            // Security Hardening: Block ReparsePoints/Symlink spoofing attacks
            var fileAttr = File.GetAttributes(exePath);
            if ((fileAttr & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
                return;
            }

            var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
            
            var productName = versionInfo.ProductName?.Trim();
            var companyName = versionInfo.CompanyName?.Trim();
            var originalFileName = versionInfo.OriginalFilename?.Trim();

            if (string.IsNullOrWhiteSpace(productName)) return;

            var identityKey = $"{productName}|{companyName}";
            if (results.ContainsKey(identityKey)) return;

            var version = !string.IsNullOrWhiteSpace(versionInfo.ProductVersion) 
                ? versionInfo.ProductVersion.Trim() 
                : (versionInfo.FileVersion?.Trim() ?? "Unknown");

            var publisher = !string.IsNullOrWhiteSpace(companyName) ? companyName : "Unknown Publisher";
            var installPath = Path.GetDirectoryName(exePath) ?? string.Empty;

            var scanSource = isRunningProcess 
                ? $"DeepFileSystem (RAM: {Path.GetFileName(exePath)} | Original: {originalFileName ?? "N/A"})"
                : $"DeepFileSystem (Disk: {Path.GetFileName(exePath)} | Original: {originalFileName ?? "N/A"})";

            var lastModified = VietnamTime.Format(File.GetLastWriteTimeUtc(exePath));
            var appStart = processStartTime.HasValue ? VietnamTime.Format(processStartTime.Value) : string.Empty;

            if (isRunningProcess && processStartTime.HasValue)
            {
                var uptimeMinutes = Math.Max(0, (int)(DateTime.UtcNow - processStartTime.Value).TotalMinutes);
                _logger.LogInformation("[App Usage Metering] Executable: {ExeName} ({ProductName}) | Started At: {StartTime} | Active Uptime: {Duration} min | Content Inspected: False",
                    Path.GetFileName(exePath), productName, appStart, uptimeMinutes);
            }

            var softwareInfo = new SoftwareInfo(
                productName,
                version,
                publisher,
                installPath,
                File.GetCreationTimeUtc(exePath).ToString("yyyy/MM/dd"),
                scanSource,
                lastModified,
                appStart
            );

            results[identityKey] = softwareInfo;
        }
        catch
        {
            // Bỏ qua nếu lỗi đọc metadata PE file
        }
    }
}
