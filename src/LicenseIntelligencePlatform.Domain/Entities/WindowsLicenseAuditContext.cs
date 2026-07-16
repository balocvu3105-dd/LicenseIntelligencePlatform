namespace LicenseIntelligencePlatform.Domain.Entities;

/// <summary>
/// Thread-safe shared context between the Windows License Audit scanner layer and verification plugin layer.
/// Guarantees that scan state and evidence flow upward and downward through the engine pipeline without data loss.
/// </summary>
public static class WindowsLicenseAuditContext
{
    private static WindowsLicenseAuditData? _currentAuditData;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets or sets the audit data captured by the Windows License Audit scanner.
    /// </summary>
    public static WindowsLicenseAuditData? CurrentAuditData
    {
        get
        {
            lock (_lock) return _currentAuditData;
        }
        set
        {
            lock (_lock) _currentAuditData = value;
        }
    }

    /// <summary>
    /// Clears the cached audit state cleanly.
    /// </summary>
    public static void Clear()
    {
        lock (_lock) _currentAuditData = null;
    }
}
