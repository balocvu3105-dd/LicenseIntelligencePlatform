using System.Reflection;
using LicenseIntelligencePlatform.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LicenseIntelligencePlatform.Application.Services;

/// <summary>
/// Service responsible for discovering and loading <see cref="ILicensePlugin"/> implementations safely.
/// Isolates assembly load errors and plugin instantiations so that failures never crash the Core application.
/// </summary>
public class PluginLoaderService : IPluginLoader
{
    private readonly List<ILicensePlugin> _plugins = new();
    private readonly ILogger<PluginLoaderService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginLoaderService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance used for diagnostics.</param>
    /// <param name="preloadedPlugins">Optional pre-registered plugin instances (e.g., from DI).</param>
    public PluginLoaderService(ILogger<PluginLoaderService> logger, IEnumerable<ILicensePlugin>? preloadedPlugins = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (preloadedPlugins != null)
        {
            foreach (var plugin in preloadedPlugins)
            {
                RegisterPluginSafe(plugin);
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<ILicensePlugin> GetLoadedPlugins()
    {
        return _plugins.AsReadOnly();
    }

    /// <inheritdoc />
    public void LoadFromDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            _logger.LogWarning("Plugin load directory path is empty or null. Skipping external directory scan.");
            return;
        }

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogInformation("Plugin directory '{DirectoryPath}' does not exist. No external plugins loaded from this location.", directoryPath);
            return;
        }

        _logger.LogInformation("Scanning directory '{DirectoryPath}' for license detection plugin assemblies...", directoryPath);

        string[] dllFiles;
        try
        {
            dllFiles = Directory.GetFiles(directoryPath, "*.dll", SearchOption.TopDirectoryOnly);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate files in plugin directory '{DirectoryPath}'. Core will continue without external plugins.", directoryPath);
            return;
        }

        foreach (var dllPath in dllFiles)
        {
            LoadAssemblyPluginsSafe(dllPath);
        }
    }

    private void LoadAssemblyPluginsSafe(string dllPath)
    {
        try
        {
            _logger.LogDebug("Attempting to load plugin assembly from '{DllPath}'...", dllPath);
            var assembly = Assembly.LoadFrom(dllPath);
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(ILicensePlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();

            foreach (var type in pluginTypes)
            {
                try
                {
                    if (Activator.CreateInstance(type) is ILicensePlugin instance)
                    {
                        RegisterPluginSafe(instance);
                        _logger.LogInformation("Successfully discovered and loaded plugin '{PluginName}' ({PluginId}) from '{DllPath}'.", instance.PluginName, instance.PluginId, dllPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to instantiate plugin type '{TypeName}' from assembly '{DllPath}'. Skipping this plugin.", type.FullName, dllPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load assembly '{DllPath}'. Assembly may be corrupt, incompatible, or locked. Core engine remains unaffected.", dllPath);
        }
    }

    /// <summary>
    /// Safely registers a plugin if not already loaded.
    /// </summary>
    /// <param name="plugin">Plugin instance to register.</param>
    public void RegisterPluginSafe(ILicensePlugin plugin)
    {
        if (plugin == null)
        {
            return;
        }

        if (_plugins.Any(p => p.PluginId.Equals(plugin.PluginId, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogDebug("Plugin '{PluginId}' is already registered. Skipping duplicate registration.", plugin.PluginId);
            return;
        }

        _plugins.Add(plugin);
        _logger.LogDebug("Registered plugin: {PluginName} ({PluginId})", plugin.PluginName, plugin.PluginId);
    }
}
