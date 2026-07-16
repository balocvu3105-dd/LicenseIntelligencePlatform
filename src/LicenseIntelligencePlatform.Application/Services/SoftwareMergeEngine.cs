using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LicenseIntelligencePlatform.Application.Services;

/// <summary>
/// Default implementation of <see cref="ISoftwareMergeEngine"/>.
/// Deduplicates software packages discovered by multiple scanners using Name+Version as the identity key,
/// and applies a "best-wins" strategy to merge metadata fields (InstallPath, InstallDate,
/// LastModifiedDate, AppStartTime, ScanSource) across all sources.
///
/// Separation of concerns: this class owns all merge logic so <see cref="CoreEngine"/> stays focused
/// on orchestration only.
/// </summary>
public class SoftwareMergeEngine : ISoftwareMergeEngine
{
    private readonly ILogger<SoftwareMergeEngine> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SoftwareMergeEngine"/>.
    /// </summary>
    public SoftwareMergeEngine(ILogger<SoftwareMergeEngine> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IReadOnlyList<SoftwareInfo> Merge(IEnumerable<SoftwareInfo> discovered)
    {
        if (discovered == null) throw new ArgumentNullException(nameof(discovered));

        var list = discovered.ToList();

        var merged = list
            .GroupBy(s => $"{s.Name.Trim()}||{s.Version.Trim()}", StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var first = g.First();

                // Best-wins: pick first non-empty value for each metadata field
                var bestInstallPath  = g.Select(x => x.InstallPath).FirstOrDefault(p => !string.IsNullOrWhiteSpace(p))  ?? first.InstallPath;
                var bestInstallDate  = g.Select(x => x.InstallDate).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d))  ?? first.InstallDate;
                var bestLastModified = g.Select(x => x.LastModifiedDate).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? first.LastModifiedDate;
                var bestAppStart     = g.Select(x => x.AppStartTime).FirstOrDefault(t => !string.IsNullOrWhiteSpace(t)) ?? first.AppStartTime;
                var combinedSource   = string.Join(" + ", g.Select(x => x.ScanSource).Distinct());

                return new SoftwareInfo(
                    first.Name,
                    first.Version,
                    first.Publisher,
                    bestInstallPath,
                    bestInstallDate,
                    combinedSource,
                    bestLastModified,
                    bestAppStart
                );
            })
            .ToList()
            .AsReadOnly();

        var duplicatesRemoved = list.Count - merged.Count;
        if (duplicatesRemoved > 0)
        {
            _logger.LogInformation(
                "[MergeEngine] Deduplicated {OriginalCount} raw items → {UniqueCount} unique packages ({DuplicatesRemoved} duplicates merged).",
                list.Count, merged.Count, duplicatesRemoved);
        }
        else
        {
            _logger.LogDebug(
                "[MergeEngine] No duplicates found. {UniqueCount} unique packages.", merged.Count);
        }

        return merged;
    }
}
