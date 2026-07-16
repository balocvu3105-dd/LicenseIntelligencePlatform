using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LicenseIntelligencePlatform.Infrastructure.Diagnostics;

/// <summary>
/// Security & Anti-Tamper enforcement engine for exported audit artifacts.
/// 1. Computes cryptographic SHA-256 integrity checksums across all generated report files.
/// 2. Generates an immutable report_integrity_manifest_<scanId>.sha256 containing exact signatures.
/// 3. Sets OS FileAttributes.ReadOnly lock on all generated reports to block unauthorized post-scan modifications.
/// </summary>
public static class ReportSecurityLocker
{
    public static async Task LockAndSignReportsAsync(string scanId, string outputDirectory, IEnumerable<string> reportFilePaths, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory) || !Directory.Exists(outputDirectory))
        {
            return;
        }

        var manifestPath = Path.Combine(outputDirectory, $"report_integrity_manifest_{scanId}.sha256");
        var sb = new StringBuilder();
        sb.AppendLine("==========================================================================================");
        sb.AppendLine("        LICENSE INTELLIGENCE PLATFORM - CRYPTOGRAPHIC REPORT INTEGRITY MANIFEST           ");
        sb.AppendLine("==========================================================================================");
        sb.AppendLine($"Scan ID      : {scanId}");
        sb.AppendLine($"Signed At    : {VietnamTime.Format(DateTime.UtcNow)}");
        sb.AppendLine($"Hash Algorithm: SHA-256 (FIPS-180-4 Standard)");
        sb.AppendLine($"Security Lock : OS FileAttributes.ReadOnly Enforced");
        sb.AppendLine("==========================================================================================");
        sb.AppendLine("SHA-256 Hash                                                     File Name");
        sb.AppendLine("---------------------------------------------------------------- -------------------------");

        var validPaths = new List<string>();

        foreach (var filePath in reportFilePaths)
        {
            if (cancellationToken.IsCancellationRequested) break;

            if (File.Exists(filePath))
            {
                validPaths.Add(filePath);
                try
                {
                    // Ensure write lock is cleared before computing if previously set
                    var currentAttr = File.GetAttributes(filePath);
                    if ((currentAttr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        File.SetAttributes(filePath, currentAttr & ~FileAttributes.ReadOnly);
                    }

                    using var sha256 = SHA256.Create();
                    using var stream = File.OpenRead(filePath);
                    var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
                    var hashHex = Convert.ToHexString(hashBytes).ToLowerInvariant();
                    var fileName = Path.GetFileName(filePath);
                    sb.AppendLine($"{hashHex}  {fileName}");
                }
                catch
                {
                    // Ignore transient lock issues during hash calculation
                }
            }
        }

        sb.AppendLine("==========================================================================================");
        sb.AppendLine("END OF INTEGRITY MANIFEST. ANY MODIFICATION TO REPORT FILES WILL INVALIDATE SHA-256 HASH.");

        // Write the manifest file
        await File.WriteAllTextAsync(manifestPath, sb.ToString(), Encoding.UTF8, cancellationToken);
        validPaths.Add(manifestPath);

        // Lock all report files + manifest as Read-Only to prevent tampering
        foreach (var filePath in validPaths)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var attr = File.GetAttributes(filePath);
                    File.SetAttributes(filePath, attr | FileAttributes.ReadOnly);
                }
            }
            catch
            {
                // Fallback if filesystem permissions prevent attribute modification
            }
        }
    }
}
