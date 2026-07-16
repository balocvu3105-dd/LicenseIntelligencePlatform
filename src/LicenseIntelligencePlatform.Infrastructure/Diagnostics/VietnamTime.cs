using System;

namespace LicenseIntelligencePlatform.Infrastructure.Diagnostics;

/// <summary>
/// Standardized time utility to convert and format timestamps to exact Vietnam Standard Time (UTC+7 / ICT).
/// Enforces consistency across all logs, visual reports, and audit artifacts.
/// </summary>
public static class VietnamTime
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    /// <summary>
    /// Gets the current local date and time in Vietnam Standard Time (UTC+7).
    /// </summary>
    public static DateTime Now => DateTime.UtcNow.Add(VietnamOffset);

    /// <summary>
    /// Converts a UTC DateTime to Vietnam Standard Time DateTime.
    /// </summary>
    public static DateTime ToVietnamTime(DateTime utcDateTime)
    {
        return utcDateTime.Kind == DateTimeKind.Utc
            ? utcDateTime.Add(VietnamOffset)
            : utcDateTime;
    }

    /// <summary>
    /// Formats a UTC DateTime string to explicit Vietnam Time string format: 'yyyy/MM/dd HH:mm:ss VN Time (UTC+7)'.
    /// </summary>
    public static string Format(DateTime utcDateTime)
    {
        var vnTime = ToVietnamTime(utcDateTime);
        return vnTime.ToString("yyyy/MM/dd HH:mm:ss 'VN Time (UTC+7)'");
    }

    /// <summary>
    /// Formats a UTC DateTime string to compact Vietnam Time format for CSV/JSON/Log headers.
    /// </summary>
    public static string FormatCompact(DateTime utcDateTime)
    {
        var vnTime = ToVietnamTime(utcDateTime);
        return vnTime.ToString("yyyy-MM-dd HH:mm:ss '+07:00'");
    }
}
