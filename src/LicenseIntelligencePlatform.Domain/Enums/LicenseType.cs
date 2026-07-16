namespace LicenseIntelligencePlatform.Domain.Enums;

/// <summary>
/// Categorizes the type of license detected for a software package.
/// </summary>
public enum LicenseType
{
    /// <summary>
    /// License could not be determined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Open source license (e.g., MIT, GPL, Apache, BSD).
    /// </summary>
    OpenSource = 1,

    /// <summary>
    /// Commercial or proprietary software license requiring paid activation or enterprise agreement.
    /// </summary>
    Commercial = 2,

    /// <summary>
    /// Freeware or free community edition software.
    /// </summary>
    Freeware = 3,

    /// <summary>
    /// Trial or evaluation version with time/feature restrictions.
    /// </summary>
    Trial = 4,

    /// <summary>
    /// Custom internal or corporate proprietary license.
    /// </summary>
    InternalCustom = 5
}
