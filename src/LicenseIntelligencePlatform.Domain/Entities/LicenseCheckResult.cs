using LicenseIntelligencePlatform.Domain.Enums;

namespace LicenseIntelligencePlatform.Domain.Entities;

/// <summary>
/// Represents the outcome of checking a software package against a license detection plugin.
/// Includes required confidence level and supporting evidence.
/// </summary>
public record LicenseCheckResult
{
    /// <summary>
    /// Gets the unique identifier of the plugin that performed the evaluation.
    /// </summary>
    public string PluginId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the display name of the plugin that performed the evaluation.
    /// </summary>
    public string PluginName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the software info package evaluated.
    /// </summary>
    public SoftwareInfo Software { get; init; } = new SoftwareInfo("", "", "");

    /// <summary>
    /// Gets the detected license category type.
    /// </summary>
    public LicenseType DetectedLicenseType { get; init; } = LicenseType.Unknown;

    /// <summary>
    /// Gets the name of the license if identified (e.g., "MIT License", "GNU General Public License v3.0", "Commercial Enterprise License").
    /// </summary>
    public string LicenseName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the confidence level assigned to this detection.
    /// </summary>
    public ConfidenceLevel Confidence { get; init; } = ConfidenceLevel.None;

    /// <summary>
    /// Gets the read-only collection of evidence supporting this verification result.
    /// </summary>
    public IReadOnlyList<Evidence> Evidences { get; init; } = Array.Empty<Evidence>();

    /// <summary>
    /// Gets whether this result has high or verified confidence with concrete evidence.
    /// </summary>
    public bool IsVerified => Confidence >= ConfidenceLevel.High && Evidences.Count > 0;

    /// <summary>
    /// Gets diagnostic or explanatory notes about the detection check.
    /// </summary>
    public string Notes { get; init; } = string.Empty;

    /// <summary>
    /// Gets the UTC timestamp when this check was executed.
    /// </summary>
    public DateTime ScannedAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a new instance of <see cref="LicenseCheckResult"/>.
    /// </summary>
    public LicenseCheckResult(
        string pluginId,
        string pluginName,
        SoftwareInfo software,
        LicenseType detectedLicenseType,
        string licenseName,
        ConfidenceLevel confidence,
        IEnumerable<Evidence> evidences,
        string notes = "")
    {
        PluginId = pluginId;
        PluginName = pluginName;
        Software = software;
        DetectedLicenseType = detectedLicenseType;
        LicenseName = licenseName;
        Confidence = confidence;
        Evidences = evidences.ToList().AsReadOnly();
        Notes = notes;
        ScannedAtUtc = DateTime.UtcNow;
    }
}
