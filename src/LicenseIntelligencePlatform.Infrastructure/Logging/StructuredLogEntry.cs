using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace LicenseIntelligencePlatform.Infrastructure.Logging;

/// <summary>
/// Represents a structured, JSON-serializable log record captured across any layer of the application.
/// Supports correlation via <see cref="ScanId"/> and rich structured attributes in <see cref="Properties"/>.
/// </summary>
public record StructuredLogEntry
{
    /// <summary>
    /// Gets the UTC timestamp when the log event occurred.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the log level string representation (e.g., "Information", "Error", "Warning").
    /// </summary>
    public string LogLevel { get; init; } = string.Empty;

    /// <summary>
    /// Gets the logger category name (e.g., "Scanner", "Plugin", "CoreEngine", "RuleEngine").
    /// </summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the numeric event identifier.
    /// </summary>
    public int EventId { get; init; }

    /// <summary>
    /// Gets the event name (e.g., "Performance", "Audit").
    /// </summary>
    public string EventName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the unique scan session correlation identifier (<see cref="Guid"/> string), if available.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ScanId { get; init; }

    /// <summary>
    /// Gets the formatted message string.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the string representation of any captured exception, including stack trace.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Exception { get; init; }

    /// <summary>
    /// Gets optional structured key-value attributes associated with the event (e.g., DurationMs, SoftwareName, Confidence).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object?>? Properties { get; init; }
}
