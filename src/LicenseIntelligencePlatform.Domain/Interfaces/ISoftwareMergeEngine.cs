using LicenseIntelligencePlatform.Domain.Entities;

namespace LicenseIntelligencePlatform.Domain.Interfaces;

/// <summary>
/// Defines the contract for merging and deduplicating software packages discovered across multiple scanners.
/// </summary>
public interface ISoftwareMergeEngine
{
    /// <summary>
    /// Merges a raw list of software items (potentially duplicated across scanners) into a
    /// single deduplicated list, preserving the richest available metadata from each source.
    /// </summary>
    /// <param name="discovered">All software items collected by all active scanners.</param>
    /// <returns>Deduplicated list of <see cref="SoftwareInfo"/> with best-available metadata.</returns>
    IReadOnlyList<SoftwareInfo> Merge(IEnumerable<SoftwareInfo> discovered);
}
