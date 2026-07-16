using System.Diagnostics;
using System.Runtime.InteropServices;
using LicenseIntelligencePlatform.Application.Logging;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LicenseIntelligencePlatform.Application.Services;

/// <summary>
/// Implements the central orchestration workflow for the License Intelligence Platform.
/// Coordinates read-only inventory discovery with scanners and applies active license verification plugins.
/// Guarantees that any plugin failure is caught, isolated, and logged without crashing Core (Rule 9).
/// Phase 2: delegates software deduplication to <see cref="ISoftwareMergeEngine"/> and plugin
/// compatibility filtering to <see cref="PluginCompatibilityValidator"/>.
/// </summary>
public class CoreEngine : ICoreEngine
{
    private readonly IEnumerable<IScanner> _scanners;
    private readonly IPluginLoader _pluginLoader;
    private readonly ISoftwareMergeEngine _mergeEngine;
    private readonly PluginCompatibilityValidator _compatValidator;
    private readonly ILogger<CoreEngine> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoreEngine"/> class.
    /// </summary>
    /// <param name="scanners">The collection of available inventory scanners.</param>
    /// <param name="pluginLoader">The plugin loader service supplying verification plugins.</param>
    /// <param name="mergeEngine">Service responsible for deduplicating and merging software items.</param>
    /// <param name="compatValidator">Service responsible for filtering and ordering plugins by SDK compatibility.</param>
    /// <param name="logger">Logger for diagnostic operations.</param>
    public CoreEngine(
        IEnumerable<IScanner> scanners,
        IPluginLoader pluginLoader,
        ISoftwareMergeEngine mergeEngine,
        PluginCompatibilityValidator compatValidator,
        ILogger<CoreEngine> logger)
    {
        _scanners = scanners ?? throw new ArgumentNullException(nameof(scanners));
        _pluginLoader = pluginLoader ?? throw new ArgumentNullException(nameof(pluginLoader));
        _mergeEngine = mergeEngine ?? throw new ArgumentNullException(nameof(mergeEngine));
        _compatValidator = compatValidator ?? throw new ArgumentNullException(nameof(compatValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ScanReport> ExecuteFullScanAsync(CancellationToken cancellationToken = default)
    {
        var scanId = Guid.NewGuid().ToString("D");
        using var scanScope = _logger.BeginScanScope(scanId);

        var totalSw = Stopwatch.StartNew();
        var startedAtUtc = DateTime.UtcNow;
        var hostName = Environment.MachineName;
        var osDescription = RuntimeInformation.OSDescription;

        _logger.LogInformation("Starting License Intelligence Platform scan session [ScanId: {ScanId}] on host '{HostName}' ({OSDescription}) at {StartedAtUtc}...",
            scanId, hostName, osDescription, startedAtUtc);

        var discoveredSoftwareList = new List<SoftwareInfo>();

        // 1. Execute Scanners (Rule 3: Scanner only collects data)
        foreach (var scanner in _scanners)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Scan cancelled by user request.");
                break;
            }

            if (!scanner.IsSupportedOnCurrentPlatform())
            {
                _logger.LogDebug("Scanner '{ScannerName}' is not supported on platform '{OSDescription}'. Skipping.",
                    scanner.ScannerName, osDescription);
                continue;
            }

            _logger.LogInformation("Running software inventory scanner: {ScannerName}...", scanner.ScannerName);
            var scannerSw = Stopwatch.StartNew();
            try
            {
                var items = await scanner.ScanAsync(cancellationToken);
                var itemsList = items.ToList();
                scannerSw.Stop();

                _logger.LogInformation("Scanner '{ScannerName}' discovered {Count} software packages.", scanner.ScannerName, itemsList.Count);
                _logger.LogPerformance("Scanner", scanner.ScannerName, scannerSw.ElapsedMilliseconds, $"Discovered {itemsList.Count} packages");

                discoveredSoftwareList.AddRange(itemsList);
            }
            catch (Exception ex)
            {
                scannerSw.Stop();
                _logger.LogError(ex, "Scanner '{ScannerName}' threw an exception during scan after {DurationMs} ms. Continuing with remaining scanners.", scanner.ScannerName, scannerSw.ElapsedMilliseconds);
                _logger.LogPerformance("Scanner", scanner.ScannerName, scannerSw.ElapsedMilliseconds, $"Failed with exception: {ex.Message}");
            }
        }

        // Deduplicate and merge software items via SoftwareMergeEngine (Phase 2)
        var uniqueSoftware = _mergeEngine.Merge(discoveredSoftwareList);

        _logger.LogInformation("Inventory collection complete. Total unique software packages discovered: {Count}", uniqueSoftware.Count);

        // 2. Load and Execute Plugins — filtered and ordered by PluginCompatibilityValidator (Phase 2)
        var rawPlugins = _pluginLoader.GetLoadedPlugins();
        var activePlugins = _compatValidator.FilterCompatible(rawPlugins);
        _logger.LogInformation("Executing license verification across {PluginCount} compatible plugins...", activePlugins.Count);

        var allResults = new List<LicenseCheckResult>();
        int totalPluginExecutions = 0;
        var pluginPhaseSw = Stopwatch.StartNew();

        foreach (var software in uniqueSoftware)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var matchingPlugins = activePlugins.Where(p => SafeCanCheck(p, software)).ToList();
            if (matchingPlugins.Count == 0)
            {
                _logger.LogDebug("No matching plugins found for software '{SoftwareName}' v{Version}. Recording Unknown result.",
                    software.Name, software.Version);

                var unknownResult = new LicenseCheckResult(
                    pluginId: "core.unknown",
                    pluginName: "Core Fallback Detector",
                    software: software,
                    detectedLicenseType: LicenseType.Unknown,
                    licenseName: "Unknown License",
                    confidence: ConfidenceLevel.None,
                    evidences: Array.Empty<Evidence>(),
                    notes: "No verification plugin claimed checking capability for this software package."
                );

                allResults.Add(unknownResult);
                _logger.LogAudit(software.Name, unknownResult.PluginName, unknownResult.Confidence.ToString(), unknownResult.DetectedLicenseType.ToString(), Array.Empty<string>(), unknownResult.Notes);
                continue;
            }

            foreach (var plugin in matchingPlugins)
            {
                totalPluginExecutions++;
                LicenseCheckResult result;
                var pluginSw = Stopwatch.StartNew();
                try
                {
                    _logger.LogDebug("Running plugin '{PluginName}' ({PluginId}) on software '{SoftwareName}'...",
                        plugin.PluginName, plugin.PluginId, software.Name);

                    result = await plugin.CheckLicenseAsync(software, cancellationToken);
                    pluginSw.Stop();

                    // Ensure returned result is not null
                    if (result == null)
                    {
                        _logger.LogWarning("Plugin '{PluginName}' returned null result for software '{SoftwareName}'. Recording fallback.",
                            plugin.PluginName, software.Name);
                        result = CreateErrorResult(plugin, software, "Plugin returned null result.");
                    }
                }
                catch (Exception ex)
                {
                    pluginSw.Stop();
                    // Rule 9: Plugin failures must not crash Core
                    _logger.LogError(ex, "Plugin '{PluginName}' ({PluginId}) threw an unhandled exception while checking '{SoftwareName}'. Isolating failure.",
                        plugin.PluginName, plugin.PluginId, software.Name);

                    result = CreateErrorResult(plugin, software, $"Plugin execution failed with error: {ex.GetType().Name} - {ex.Message}");
                }

                _logger.LogPerformance("Plugin", plugin.PluginName, pluginSw.ElapsedMilliseconds, $"Evaluated '{software.Name}' v{software.Version}");
                _logger.LogAudit(
                    result.Software.Name,
                    result.PluginName,
                    result.Confidence.ToString(),
                    result.DetectedLicenseType.ToString(),
                    result.Evidences.Select(e => e.Description),
                    result.Notes);

                allResults.Add(result);
            }
        }

        pluginPhaseSw.Stop();
        _logger.LogPerformance("PluginEngine", "AllPlugins", pluginPhaseSw.ElapsedMilliseconds, $"Completed {totalPluginExecutions} plugin verifications across {uniqueSoftware.Count} packages");

        var completedAtUtc = DateTime.UtcNow;
        totalSw.Stop();

        _logger.LogInformation("Scan session completed in {Duration} ms. Total results generated: {ResultCount}. Verified licenses: {VerifiedCount}.",
            totalSw.ElapsedMilliseconds,
            allResults.Count,
            allResults.Count(r => r.IsVerified));

        _logger.LogPerformance("CoreEngine", "ExecuteFullScanAsync", totalSw.ElapsedMilliseconds, $"Total unique software: {uniqueSoftware.Count}, Verified: {allResults.Count(r => r.IsVerified)}");

        var orderedResults = allResults
            .OrderByDescending(r => r.PluginId.Equals("os.windows", StringComparison.OrdinalIgnoreCase) || r.Software.Name.Contains("Windows Operating System", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .ThenByDescending(r => r.Confidence)
            .ThenBy(r => r.Software.Name)
            .ToList();

        return new ScanReport(
            hostName: hostName,
            osDescription: osDescription,
            startedAtUtc: startedAtUtc,
            completedAtUtc: completedAtUtc,
            totalSoftwareScanned: uniqueSoftware.Count,
            totalPluginsExecuted: totalPluginExecutions,
            results: orderedResults
        )
        {
            ScanId = scanId
        };
    }

    private bool SafeCanCheck(ILicensePlugin plugin, SoftwareInfo software)
    {
        try
        {
            return plugin.CanCheck(software);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plugin '{PluginName}' threw exception during CanCheck evaluation for '{SoftwareName}'. Treating as false.",
                plugin.PluginName, software.Name);
            return false;
        }
    }

    private static LicenseCheckResult CreateErrorResult(ILicensePlugin plugin, SoftwareInfo software, string errorMessage)
    {
        return new LicenseCheckResult(
            pluginId: plugin.PluginId,
            pluginName: plugin.PluginName,
            software: software,
            detectedLicenseType: LicenseType.Unknown,
            licenseName: "Verification Failed",
            confidence: ConfidenceLevel.None,
            evidences: Array.Empty<Evidence>(),
            notes: errorMessage
        );
    }
}
