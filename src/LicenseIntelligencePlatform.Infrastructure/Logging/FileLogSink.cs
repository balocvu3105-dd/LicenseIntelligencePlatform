using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LicenseIntelligencePlatform.Infrastructure.Logging;

/// <summary>
/// Implements high-performance, thread-safe, non-blocking file logging to four specialized log streams:
/// <c>application.log</c>, <c>error.log</c>, <c>performance.log</c>, and <c>audit.log</c> inside the <c>logs/</c> directory.
/// </summary>
public class FileLogSink : ILogSink
{
    private readonly string _logDirectory;
    private readonly ConcurrentQueue<StructuredLogEntry> _queue = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processTask;
    private readonly object _syncLock = new();
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of <see cref="FileLogSink"/> writing to the specified directory.
    /// </summary>
    /// <param name="logDirectory">Absolute or relative path to the target logs directory.</param>
    public FileLogSink(string logDirectory)
    {
        _logDirectory = logDirectory ?? throw new ArgumentNullException(nameof(logDirectory));
        Directory.CreateDirectory(_logDirectory);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        _processTask = Task.Run(ProcessQueueAsync);
    }

    /// <inheritdoc />
    public void Write(StructuredLogEntry entry)
    {
        if (entry == null || _cts.IsCancellationRequested) return;
        _queue.Enqueue(entry);
    }

    /// <inheritdoc />
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        while (!_queue.IsEmpty && !cancellationToken.IsCancellationRequested)
        {
            FlushBatch();
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessQueueAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                if (!_queue.IsEmpty)
                {
                    FlushBatch();
                }
                await Task.Delay(50, _cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Never let background logging task crash the process
            }
        }

        // Final flush upon shutdown
        FlushBatch();
    }

    private void FlushBatch()
    {
        if (_queue.IsEmpty) return;

        var batch = new List<StructuredLogEntry>();
        while (batch.Count < 500 && _queue.TryDequeue(out var item))
        {
            batch.Add(item);
        }

        if (batch.Count == 0) return;

        lock (_syncLock)
        {
            try
            {
                var appPath = Path.Combine(_logDirectory, "application.log");
                var errPath = Path.Combine(_logDirectory, "error.log");
                var perfPath = Path.Combine(_logDirectory, "performance.log");
                var auditPath = Path.Combine(_logDirectory, "audit.log");

                using var appWriter = new StreamWriter(appPath, append: true);
                StreamWriter? errWriter = null;
                StreamWriter? perfWriter = null;
                StreamWriter? auditWriter = null;

                try
                {
                    foreach (var entry in batch)
                    {
                        var json = JsonSerializer.Serialize(entry, _jsonOptions);

                        // 1. All logs go to application.log
                        appWriter.WriteLine(json);

                        // 2. Error & Critical logs or Exceptions go to error.log
                        if (string.Equals(entry.LogLevel, "Error", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(entry.LogLevel, "Critical", StringComparison.OrdinalIgnoreCase) ||
                            !string.IsNullOrEmpty(entry.Exception))
                        {
                            errWriter ??= new StreamWriter(errPath, append: true);
                            errWriter.WriteLine(json);
                        }

                        // 3. Performance events go to performance.log
                        if (string.Equals(entry.EventName, "Performance", StringComparison.OrdinalIgnoreCase) || entry.EventId == 1001)
                        {
                            perfWriter ??= new StreamWriter(perfPath, append: true);
                            perfWriter.WriteLine(json);
                        }

                        // 4. Audit events go to audit.log
                        if (string.Equals(entry.EventName, "Audit", StringComparison.OrdinalIgnoreCase) || entry.EventId == 1002)
                        {
                            auditWriter ??= new StreamWriter(auditPath, append: true);
                            auditWriter.WriteLine(json);
                        }
                    }
                }
                finally
                {
                    errWriter?.Dispose();
                    perfWriter?.Dispose();
                    auditWriter?.Dispose();
                }
            }
            catch
            {
                // Silently isolate file write exceptions to ensure system resilience
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            try
            {
                _processTask.Wait(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // Ignore task wait errors on dispose
            }
            FlushBatch();
        }
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            try
            {
                await _processTask.ConfigureAwait(false);
            }
            catch
            {
                // Ignore task wait errors on dispose
            }
            FlushBatch();
        }
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
