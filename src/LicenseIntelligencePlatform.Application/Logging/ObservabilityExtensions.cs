using Microsoft.Extensions.Logging;

namespace LicenseIntelligencePlatform.Application.Logging;

/// <summary>
/// Provides extension methods for recording structured observability events (Performance metrics, Audit trail, Scan correlation)
/// across Application, Infrastructure, and Plugin layers without coupling to physical log sinks.
/// </summary>
public static class ObservabilityExtensions
{
    private static readonly EventId PerformanceEventId = new(1001, "Performance");
    private static readonly EventId AuditEventId = new(1002, "Audit");

    /// <summary>
    /// Begins a structured logging correlation scope attaching the specified <see cref="ScanId"/> to all enclosed log records.
    /// </summary>
    public static IDisposable BeginScanScope(this ILogger logger, string scanId)
    {
        if (logger == null || string.IsNullOrWhiteSpace(scanId))
        {
            return NullScope.Instance;
        }

        return logger.BeginScope(new Dictionary<string, object>
        {
            ["ScanId"] = scanId
        }) ?? NullScope.Instance;
    }

    /// <summary>
    /// Records a structured performance timing entry (<c>EventId = 1001</c>, <c>Name = "Performance"</c>).
    /// Intercepted by infrastructure log providers and routed to <c>performance.log</c>.
    /// </summary>
    public static void LogPerformance(this ILogger logger, string component, string action, long durationMs, string details = "")
    {
        if (logger == null) return;

        logger.Log(
            LogLevel.Information,
            PerformanceEventId,
            "Performance: [{Component}] {Action} completed in {DurationMs} ms. {Details}",
            component,
            action,
            durationMs,
            details);
    }

    /// <summary>
    /// Records a structured audit decision entry (<c>EventId = 1002</c>, <c>Name = "Audit"</c>) explaining why the system reached a conclusion.
    /// Intercepted by infrastructure log providers and routed to <c>audit.log</c>.
    /// </summary>
    public static void LogAudit(this ILogger logger, string softwareName, string pluginUsed, string confidence, string licenseResult, IEnumerable<string> evidences, string notes = "")
    {
        if (logger == null) return;

        var evidenceList = evidences != null ? string.Join("; ", evidences) : "";

        logger.Log(
            LogLevel.Information,
            AuditEventId,
            "Audit: Software '{SoftwareName}' verified by [{PluginUsed}] as {LicenseResult} (Confidence: {Confidence}). Evidences: [{Evidences}]. Notes: {Notes}",
            softwareName,
            pluginUsed,
            licenseResult,
            confidence,
            evidenceList,
            notes);
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
