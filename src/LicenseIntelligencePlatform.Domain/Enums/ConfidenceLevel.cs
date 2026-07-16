namespace LicenseIntelligencePlatform.Domain.Enums;

/// <summary>
/// Represents the confidence level of a license verification check.
/// </summary>
public enum ConfidenceLevel
{
    /// <summary>
    /// No confidence or verification could be performed.
    /// </summary>
    None = 0,

    /// <summary>
    /// Low confidence based on heuristic rules or file names without content check.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium confidence based on pattern matching inside files or metadata.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High confidence based on explicit license headers, signatures, or registration keys.
    /// </summary>
    High = 3,

    /// <summary>
    /// Fully verified license through definitive proof or cryptographically validated evidence.
    /// </summary>
    Verified = 4
}
