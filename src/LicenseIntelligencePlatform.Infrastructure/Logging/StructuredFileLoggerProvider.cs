using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace LicenseIntelligencePlatform.Infrastructure.Logging;

/// <summary>
/// Implements <see cref="ILoggerProvider"/> to intercept structured log entries from all DI-registered components,
/// attaching correlation <see cref="ScanId"/> and routing events to <see cref="ILogSink"/>.
/// </summary>
public class StructuredFileLoggerProvider : ILoggerProvider
{
    private readonly ILogSink _sink;
    private readonly ConcurrentDictionary<string, ILogger> _loggers = new();

    /// <summary>
    /// Initializes a new instance of <see cref="StructuredFileLoggerProvider"/> with the specified log sink.
    /// </summary>
    /// <param name="sink">The storage destination for structured log records.</param>
    public StructuredFileLoggerProvider(ILogSink sink)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new StructuredLogger(name, _sink));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _loggers.Clear();
        _sink.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class StructuredLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly ILogSink _sink;

        public StructuredLogger(string categoryName, ILogSink sink)
        {
            _categoryName = categoryName;
            _sink = sink;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> props)
            {
                foreach (var kvp in props)
                {
                    if (string.Equals(kvp.Key, "ScanId", StringComparison.OrdinalIgnoreCase) && kvp.Value != null)
                    {
                        return ScanCorrelationContext.BeginScanScope(kvp.Value.ToString()!);
                    }
                }
            }
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter != null ? formatter(state, exception) : state?.ToString() ?? "";
            Dictionary<string, object?>? properties = null;

            if (state is IEnumerable<KeyValuePair<string, object?>> stateProperties)
            {
                foreach (var kvp in stateProperties)
                {
                    if (kvp.Key == "{OriginalFormat}") continue;
                    properties ??= new Dictionary<string, object?>();
                    properties[kvp.Key] = kvp.Value;
                }
            }

            var scanId = ScanCorrelationContext.CurrentScanId;
            if (properties != null && properties.TryGetValue("ScanId", out var propScanId) && propScanId != null)
            {
                scanId = propScanId.ToString();
            }

            var entry = new StructuredLogEntry
            {
                TimestampUtc = DateTime.UtcNow,
                LogLevel = logLevel.ToString(),
                CategoryName = _categoryName,
                EventId = eventId.Id,
                EventName = eventId.Name ?? "",
                ScanId = scanId,
                Message = message,
                Exception = exception?.ToString(),
                Properties = properties
            };

            _sink.Write(entry);
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
