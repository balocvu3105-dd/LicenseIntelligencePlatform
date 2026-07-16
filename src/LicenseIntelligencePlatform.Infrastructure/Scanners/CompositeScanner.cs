using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LicenseIntelligencePlatform.Infrastructure.Scanners;

/// <summary>
/// A composite scanner that wraps multiple OS-specific scanners and executes only those supported on the current running platform.
/// </summary>
public class CompositeScanner : IScanner
{
    private readonly IEnumerable<IScanner> _innerScanners;
    private readonly ILogger<CompositeScanner> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeScanner"/> class.
    /// </summary>
    /// <param name="innerScanners">Collection of underlying scanners.</param>
    /// <param name="logger">Logger instance.</param>
    public CompositeScanner(IEnumerable<IScanner> innerScanners, ILogger<CompositeScanner> logger)
    {
        _innerScanners = innerScanners ?? throw new ArgumentNullException(nameof(innerScanners));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string ScannerName => "CompositeScanner";

    /// <inheritdoc />
    public bool IsSupportedOnCurrentPlatform()
    {
        return _innerScanners.Any(s => s.IsSupportedOnCurrentPlatform());
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SoftwareInfo>> ScanAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<SoftwareInfo>();

        foreach (var scanner in _innerScanners)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (!scanner.IsSupportedOnCurrentPlatform())
            {
                _logger.LogDebug("CompositeScanner skipping inner scanner '{ScannerName}' due to platform support check.", scanner.ScannerName);
                continue;
            }

            try
            {
                _logger.LogInformation("CompositeScanner delegating to '{ScannerName}'...", scanner.ScannerName);
                var items = await scanner.ScanAsync(cancellationToken);
                results.AddRange(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inner scanner '{ScannerName}' failed inside CompositeScanner.", scanner.ScannerName);
            }
        }

        return results;
    }
}
