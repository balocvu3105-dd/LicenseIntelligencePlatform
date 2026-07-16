namespace LicenseIntelligencePlatform.Domain.Interfaces;

/// <summary>
/// Defines a service that discovers, loads, and manages <see cref="ILicensePlugin"/> instances safely.
/// </summary>
public interface IPluginLoader
{
    /// <summary>
    /// Retrieves all loaded and active license verification plugins.
    /// </summary>
    /// <returns>Collection of active plugins.</returns>
    IEnumerable<ILicensePlugin> GetLoadedPlugins();

    /// <summary>
    /// Discovers and loads plugin assemblies located in the specified directory path.
    /// Any load errors or corrupt assemblies are logged and isolated without crashing the application.
    /// </summary>
    /// <param name="directoryPath">The directory path containing plugin DLLs.</param>
    void LoadFromDirectory(string directoryPath);
}
