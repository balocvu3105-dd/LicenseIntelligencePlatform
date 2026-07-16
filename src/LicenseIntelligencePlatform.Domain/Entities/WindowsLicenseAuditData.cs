using System.Text.Json.Serialization;

namespace LicenseIntelligencePlatform.Domain.Entities;

/// <summary>
/// Represents comprehensive audit findings collected by the Windows License Audit scan module.
/// Captures OS metadata, activation channels, product keys, digital signatures, KMS crack artifacts, and risk scoring.
/// </summary>
public record WindowsLicenseAuditData
{
    /// <summary>Gets the exact Windows operating system edition (e.g., "Windows 11 Pro", "Windows 10 Enterprise").</summary>
    public string WindowsEdition { get; init; } = string.Empty;

    /// <summary>Gets the full Windows build number including UBR revision (e.g., "26100.3194" or "19045.5487").</summary>
    public string BuildNumber { get; init; } = string.Empty;

    /// <summary>Gets the product name registered in WMI / registry.</summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>Gets the installation timestamp formatted as "yyyy/MM/dd HH:mm:ss".</summary>
    public string InstallDate { get; init; } = string.Empty;

    /// <summary>Gets the system kernel architecture (e.g., "X64", "Arm64").</summary>
    public string Architecture { get; init; } = string.Empty;

    /// <summary>Gets the activation status text (e.g., "Activated (Licensed)", "Out-of-Box Grace Period", "Unlicensed").</summary>
    public string ActivationStatus { get; init; } = string.Empty;

    /// <summary>Gets the detected license channel (e.g., "RETAIL", "OEM", "VOLUME_MAK", "VOLUME_KMSCLIENT", "GVLK", "Evaluation").</summary>
    public string LicenseChannel { get; init; } = string.Empty;

    /// <summary>Gets whether a genuine OEM BIOS/UEFI embedded key or MSDM ACPI table is present.</summary>
    public bool OemKeyPresence { get; init; }

    /// <summary>Gets the installed product key in masked format (e.g., "XXXXX-XXXXX-XXXXX-XXXXX-3V66T").</summary>
    public string InstalledProductKeyMasked { get; init; } = string.Empty;

    /// <summary>Gets the embedded BIOS OEM product key string or descriptive identifier if present.</summary>
    public string BiosEmbeddedKey { get; init; } = string.Empty;

    /// <summary>Gets diagnostic summary of WMI SoftwareLicensingProduct class.</summary>
    public string SoftwareLicensingProductSummary { get; init; } = string.Empty;

    /// <summary>Gets diagnostic summary of WMI SoftwareLicensingService class.</summary>
    public string SoftwareLicensingServiceSummary { get; init; } = string.Empty;

    /// <summary>Gets the calculated weighted risk score (0 to 100).</summary>
    public int RiskScore { get; init; }

    /// <summary>Gets the risk classification label based on the score (e.g., "Legitimate", "Likely Unauthorized Activation").</summary>
    public string RiskClassification { get; init; } = "Legitimate";

    /// <summary>Gets a concise summary text for scanner inventory listings.</summary>
    public string SummaryText { get; init; } = string.Empty;

    /// <summary>Gets the collection of detailed audit evidence items discovered across system paths.</summary>
    public IReadOnlyList<Evidence> AuditEvidences { get; init; } = Array.Empty<Evidence>();

    /// <summary>Gets actionable compliance and remediation recommendations based on the audit findings.</summary>
    public IReadOnlyList<string> Recommendations { get; init; } = Array.Empty<string>();

    /// <summary>Gets the UTC timestamp of the audit execution.</summary>
    public DateTime AuditedAtUtc { get; init; } = DateTime.UtcNow;
}
