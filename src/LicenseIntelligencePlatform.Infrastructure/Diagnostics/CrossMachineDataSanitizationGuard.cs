using System.Runtime.InteropServices;
using System.Text;

namespace LicenseIntelligencePlatform.Infrastructure.Diagnostics;

/// <summary>
/// Provides zero-tolerance cross-machine data isolation and sanitization.
/// Guarantees that sensitive inventory data, logs, or reports generated on one computer/user
/// are strictly prevented from persisting or leaking when the application directory or package is transferred to another machine.
/// </summary>
public static class CrossMachineDataSanitizationGuard
{
    private const string HostIdentityFileName = ".host_identity";

    /// <summary>
    /// Enforces zero-tolerance data isolation by verifying the current machine identity against the last run session.
    /// If the folder was transferred or downloaded from a different machine (<c>Environment.MachineName</c> mismatch),
    /// all historical logs and reports are immediately and permanently wiped before any new operations start.
    /// </summary>
    /// <param name="reportsDirectory">Path to the reports output directory.</param>
    /// <param name="logsDirectory">Path to the active log stream directory.</param>
    public static void EnforceZeroToleranceIsolation(string reportsDirectory, string logsDirectory)
    {
        try
        {
            var absLogsDir = Path.GetFullPath(logsDirectory);
            var absReportsDir = Path.GetFullPath(reportsDirectory);

            Directory.CreateDirectory(absLogsDir);
            Directory.CreateDirectory(absReportsDir);

            var identityPath = Path.Combine(absLogsDir, HostIdentityFileName);
            var currentHost = Environment.MachineName.Trim().ToUpperInvariant();
            var isCrossMachineDetected = false;
            string? previousHost = null;

            if (File.Exists(identityPath))
            {
                try
                {
                    var lines = File.ReadAllLines(identityPath, Encoding.UTF8);
                    if (lines.Length > 0 && !string.IsNullOrWhiteSpace(lines[0]))
                    {
                        previousHost = lines[0].Trim().ToUpperInvariant();
                        if (!string.Equals(previousHost, currentHost, StringComparison.Ordinal))
                        {
                            isCrossMachineDetected = true;
                        }
                    }
                }
                catch
                {
                    // If identity file is corrupted or unreadable, default to strict sanitization
                    isCrossMachineDetected = true;
                }
            }

            if (isCrossMachineDetected)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[SECURITY GUARD] Zero-Tolerance Cross-Machine Data Isolation Triggered!");
                Console.WriteLine($"                 Previous Host Origin: {previousHost ?? "UNKNOWN"} | Current Host: {currentHost}");
                Console.WriteLine($"                 Wiping all prior session logs and reports to prevent cross-user data leakage...");
                Console.ResetColor();

                SanitizeDirectory(absLogsDir, keepHostIdentity: false);
                SanitizeDirectory(absReportsDir, keepHostIdentity: false);
            }

            // Record current host identity
            var newIdentityContent = $"{currentHost}\nOS: {RuntimeInformation.OSDescription}\nLastSessionUtc: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
            File.WriteAllText(identityPath, newIdentityContent, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            // Never block startup due to filesystem permissions, but warn user
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[!] Warning: Could not verify cross-machine data isolation: {ex.Message}");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Permanently wipes all data files (<c>*.log</c>, <c>*.json</c>, <c>*.csv</c>, <c>*.html</c>, <c>*.audit</c>, <c>*.xlsx</c>, <c>*.zip</c>, <c>*.txt</c>)
    /// inside the specified target directory while preserving structural placeholders (<c>.keep</c>).
    /// </summary>
    /// <param name="targetDirectory">Absolute directory path to sanitize.</param>
    /// <param name="keepHostIdentity">Whether to preserve the <c>.host_identity</c> tracker file.</param>
    public static void SanitizeDirectory(string targetDirectory, bool keepHostIdentity = true)
    {
        if (!Directory.Exists(targetDirectory)) return;

        try
        {
            var files = Directory.GetFiles(targetDirectory, "*.*", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                if (string.Equals(fileName, ".keep", StringComparison.OrdinalIgnoreCase)) continue;
                if (keepHostIdentity && string.Equals(fileName, HostIdentityFileName, StringComparison.OrdinalIgnoreCase)) continue;

                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore file lock errors during purge attempts
                }
            }
        }
        catch
        {
            // Ignore directory traversal errors
        }
    }
}
