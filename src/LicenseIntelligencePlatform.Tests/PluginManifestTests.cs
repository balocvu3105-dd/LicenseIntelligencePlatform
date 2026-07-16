using LicenseIntelligencePlatform.Application.Services;
using LicenseIntelligencePlatform.Domain;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Interfaces;
using LicenseIntelligencePlatform.Plugins.Standard.Plugins;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LicenseIntelligencePlatform.Tests;

/// <summary>
/// Phase 2: Tests for PluginManifest SDK compliance and PluginCompatibilityValidator.
/// </summary>
public class PluginManifestTests
{
    private static readonly ILicensePlugin[] AllStandardPlugins = new ILicensePlugin[]
    {
        new OpenSourceArtifactPlugin(),
        new CommercialKeyFilePlugin(),
        new FreewarePatternPlugin(),
        new MicrosoftOfficePlugin(),
        new AdobeCreativeCloudPlugin(),
        new AutodeskLicensePlugin(),
        new VMwareWorkstationPlugin(),
        new SqlServerLicensePlugin(),
        new JetBrainsIdesPlugin(),
        new DockerDesktopPlugin(),
        new SentinelHaspPlugin(),
        new FlexLmPlugin(),
        new MatlabLicensePlugin(),
        new GitOpenSourcePlugin(),
        new NodeJsPermissivePlugin(),
        new PythonEcosystemPlugin(),
        new PostmanCommercialPlugin(),
        new FigmaDesignPlugin(),
        new TablePlusLicensePlugin(),
        new UnityGameEnginePlugin(),
        new OracleJavaPlugin(),
        new CiscoAnyConnectPlugin(),
        new RedHatEnterprisePlugin(),
        new NvidiaEnterprisePlugin(),
        new ObsStudioLicensePlugin(),
        new VlcPlayerPlugin(),
        new MicrosoftRuntimeEcosystemPlugin(),
        new GamingPlatformsEcosystemPlugin(),
        new CommunicationCollaboratePlugin(),
        new HardwareOemUtilitiesPlugin(),
        new NvidiaConsumerSuitePlugin(),
        new WebBrowsersAndToolsPlugin(),
        new AntiCheatAndSecurityPlugin()
    };

    [Fact]
    public void AllStandardPlugins_ShouldHaveNonEmptyManifest()
    {
        foreach (var plugin in AllStandardPlugins)
        {
            var manifest = plugin.Manifest;

            Assert.NotNull(manifest);
            Assert.False(string.IsNullOrWhiteSpace(manifest.PluginId),
                $"Plugin '{plugin.GetType().Name}' has empty PluginId in Manifest.");
            Assert.False(string.IsNullOrWhiteSpace(manifest.PluginName),
                $"Plugin '{plugin.GetType().Name}' has empty PluginName in Manifest.");
            Assert.False(string.IsNullOrWhiteSpace(manifest.PluginVersion),
                $"Plugin '{plugin.GetType().Name}' has empty PluginVersion in Manifest.");
            Assert.False(string.IsNullOrWhiteSpace(manifest.Author),
                $"Plugin '{plugin.GetType().Name}' has empty Author in Manifest.");
            Assert.False(string.IsNullOrWhiteSpace(manifest.Description),
                $"Plugin '{plugin.GetType().Name}' has empty Description in Manifest.");
        }
    }

    [Fact]
    public void AllStandardPlugins_ManifestPluginId_ShouldMatchPluginId()
    {
        foreach (var plugin in AllStandardPlugins)
        {
            Assert.Equal(plugin.PluginId, plugin.Manifest.PluginId);
        }
    }

    [Fact]
    public void AllStandardPlugins_ManifestPluginName_ShouldMatchPluginName()
    {
        foreach (var plugin in AllStandardPlugins)
        {
            Assert.Equal(plugin.PluginName, plugin.Manifest.PluginName);
        }
    }

    [Fact]
    public void AllStandardPlugins_ShouldBeCompatibleWithCurrentSdk()
    {
        var validator = new PluginCompatibilityValidator(NullLogger<PluginCompatibilityValidator>.Instance);

        var compatible = validator.FilterCompatible(AllStandardPlugins);

        Assert.Equal(AllStandardPlugins.Length, compatible.Count);
    }

    [Fact]
    public void PluginCompatibilityValidator_ShouldRejectPlugin_WithTooHighMinSdkVersion()
    {
        // Arrange: simulate a plugin requiring a future SDK version
        var futurePlugin = new FakeHighMinSdkPlugin();
        var validator = new PluginCompatibilityValidator(NullLogger<PluginCompatibilityValidator>.Instance);

        // Act
        var compatible = validator.FilterCompatible(new[] { futurePlugin });

        // Assert: plugin is rejected
        Assert.Empty(compatible);
    }

    [Fact]
    public void PluginCompatibilityValidator_ShouldRejectPlugin_WithTooLowMaxSdkVersion()
    {
        // Arrange: simulate a plugin whose max SDK is older than current
        var outdatedPlugin = new FakeLowMaxSdkPlugin();
        var validator = new PluginCompatibilityValidator(NullLogger<PluginCompatibilityValidator>.Instance);

        var compatible = validator.FilterCompatible(new[] { outdatedPlugin });

        Assert.Empty(compatible);
    }

    [Fact]
    public void PluginCompatibilityValidator_ShouldOrderByDescendingPriority()
    {
        var validator = new PluginCompatibilityValidator(NullLogger<PluginCompatibilityValidator>.Instance);
        var result = validator.FilterCompatible(AllStandardPlugins).ToList();

        for (int i = 0; i < result.Count - 1; i++)
        {
            Assert.True(result[i].Manifest.Priority >= result[i + 1].Manifest.Priority,
                $"Plugins are not sorted by descending priority: '{result[i].PluginName}' (pri={result[i].Manifest.Priority}) should come before '{result[i + 1].PluginName}' (pri={result[i + 1].Manifest.Priority}).");
        }
    }
}

// ── Fake plugins for negative tests ─────────────────────────────────────────

file sealed class FakeHighMinSdkPlugin : ILicensePlugin
{
    public string PluginId => "test.fake.highmin";
    public string PluginName => "Fake High Min SDK Plugin";
    public PluginManifest Manifest => new PluginManifest(
        pluginId: PluginId, pluginName: PluginName, pluginVersion: "1.0.0",
        author: "Test", description: "Fake plugin requiring future SDK.",
        minSdkVersion: "99.0.0");

    public bool CanCheck(SoftwareInfo _) => false;
    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken ct = default)
        => throw new NotImplementedException();
}

file sealed class FakeLowMaxSdkPlugin : ILicensePlugin
{
    public string PluginId => "test.fake.lowmax";
    public string PluginName => "Fake Low Max SDK Plugin";
    public PluginManifest Manifest => new PluginManifest(
        pluginId: PluginId, pluginName: PluginName, pluginVersion: "0.9.0",
        author: "Test", description: "Fake plugin with expired max SDK.",
        minSdkVersion: "0.1.0", maxSdkVersion: "0.9.9");

    public bool CanCheck(SoftwareInfo _) => false;
    public Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken ct = default)
        => throw new NotImplementedException();
}
