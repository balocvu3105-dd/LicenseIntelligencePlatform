namespace LicenseIntelligencePlatform.Infrastructure.Logging;

/// <summary>
/// Defines a storage destination sink for structured log entries.
/// Abstraction enables drop-in replacement of file logging with SQLite, PostgreSQL, Seq, Elasticsearch,
/// or Grafana Loki without altering the Core Engine or Logger Provider.
/// </summary>
public interface ILogSink : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Enqueues or writes a structured log entry to the underlying storage mechanism.
    /// </summary>
    /// <param name="entry">The structured log entry to record.</param>
    void Write(StructuredLogEntry entry);

    /// <summary>
    /// Asynchronously flushes any buffered log entries to permanent storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
