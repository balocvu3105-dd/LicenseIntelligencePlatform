namespace LicenseIntelligencePlatform.Infrastructure.Logging;

/// <summary>
/// Provides async-local storage for tracking the active <see cref="ScanId"/> across multi-threaded and asynchronous workflows.
/// Guarantees that every log entry produced during a scan session automatically correlates to the parent <see cref="ScanId"/>.
/// </summary>
public static class ScanCorrelationContext
{
    private static readonly AsyncLocal<string?> _currentScanId = new();

    /// <summary>
    /// Gets the current active correlation ScanId for the executing async task chain.
    /// </summary>
    public static string? CurrentScanId => _currentScanId.Value;

    /// <summary>
    /// Begins a correlation scope by setting the active <see cref="ScanId"/> for the current async flow.
    /// When the returned <see cref="IDisposable"/> is disposed, the previous correlation value is restored.
    /// </summary>
    /// <param name="scanId">The unique ScanId (GUID string).</param>
    /// <returns>An <see cref="IDisposable"/> scope that restores the previous correlation value upon disposal.</returns>
    public static IDisposable BeginScanScope(string scanId)
    {
        var previous = _currentScanId.Value;
        _currentScanId.Value = scanId;
        return new CorrelationScope(previous);
    }

    private sealed class CorrelationScope : IDisposable
    {
        private readonly string? _previousScanId;
        private bool _disposed;

        public CorrelationScope(string? previousScanId)
        {
            _previousScanId = previousScanId;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _currentScanId.Value = _previousScanId;
                _disposed = true;
            }
        }
    }
}
