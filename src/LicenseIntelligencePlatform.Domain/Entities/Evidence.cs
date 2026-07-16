namespace LicenseIntelligencePlatform.Domain.Entities;

/// <summary>
/// Represents tangible evidence collected during a license verification check.
/// Required by architecture principles: Confidence + Evidence on every result.
/// </summary>
public record Evidence
{
    /// <summary>
    /// Gets the type or category of evidence (e.g., "FileArtifact", "RegistryKey", "KeywordMatch", "HeaderSignature").
    /// </summary>
    public string EvidenceType { get; init; } = string.Empty;

    /// <summary>
    /// Gets a human-readable explanation of why this evidence supports the license finding.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the raw data snippet, file header, or registry value supporting this evidence.
    /// </summary>
    public string RawData { get; init; } = string.Empty;

    /// <summary>
    /// Gets the source file path, registry key path, or location where the evidence was discovered.
    /// </summary>
    public string SourceLocation { get; init; } = string.Empty;

    /// <summary>
    /// Creates a new instance of <see cref="Evidence"/>.
    /// </summary>
    /// <param name="evidenceType">Type of evidence.</param>
    /// <param name="description">Explanation of evidence.</param>
    /// <param name="sourceLocation">Location where found.</param>
    /// <param name="rawData">Raw data found.</param>
    public Evidence(string evidenceType, string description, string sourceLocation, string rawData = "")
    {
        EvidenceType = evidenceType;
        Description = description;
        SourceLocation = sourceLocation;
        RawData = rawData;
    }
}
