using LicenseIntelligencePlatform.Application.Services;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LicenseIntelligencePlatform.Tests;

public class CoreEngineTests
{
    // Phase 2 helpers — reuse across tests
    private static SoftwareMergeEngine CreateMergeEngine() =>
        new SoftwareMergeEngine(NullLogger<SoftwareMergeEngine>.Instance);

    private static PluginCompatibilityValidator CreateCompatValidator() =>
        new PluginCompatibilityValidator(NullLogger<PluginCompatibilityValidator>.Instance);

    [Fact]
    public async Task ExecuteFullScanAsync_ShouldAggregateSoftwareFromAllScanners()
    {
        // Arrange
        var mockScanner1 = new Mock<IScanner>();
        mockScanner1.Setup(s => s.ScannerName).Returns("MockScanner1");
        mockScanner1.Setup(s => s.IsSupportedOnCurrentPlatform()).Returns(true);
        mockScanner1.Setup(s => s.ScanAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[]
        {
            new SoftwareInfo("App A", "1.0", "Vendor A"),
            new SoftwareInfo("App B", "2.0", "Vendor B")
        });

        var mockScanner2 = new Mock<IScanner>();
        mockScanner2.Setup(s => s.ScannerName).Returns("MockScanner2");
        mockScanner2.Setup(s => s.IsSupportedOnCurrentPlatform()).Returns(true);
        mockScanner2.Setup(s => s.ScanAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[]
        {
            new SoftwareInfo("App C", "3.0", "Vendor C")
        });

        var mockLoader = new Mock<IPluginLoader>();
        mockLoader.Setup(l => l.GetLoadedPlugins()).Returns(Array.Empty<ILicensePlugin>());

        var engine = new CoreEngine(
            new[] { mockScanner1.Object, mockScanner2.Object },
            mockLoader.Object,
            CreateMergeEngine(),
            CreateCompatValidator(),
            NullLogger<CoreEngine>.Instance
        );

        // Act
        var report = await engine.ExecuteFullScanAsync();

        // Assert
        Assert.NotNull(report);
        Assert.Equal(3, report.TotalSoftwareScanned);
    }

    [Fact]
    public async Task ExecuteFullScanAsync_Rule9_PluginFailuresMustNotCrashCore()
    {
        // Arrange: Create a scanner that returns 1 software item
        var mockScanner = new Mock<IScanner>();
        mockScanner.Setup(s => s.ScannerName).Returns("TestScanner");
        mockScanner.Setup(s => s.IsSupportedOnCurrentPlatform()).Returns(true);
        mockScanner.Setup(s => s.ScanAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[]
        {
            new SoftwareInfo("Faulty App", "1.0", "Vendor X")
        });

        // Create a faulty plugin that throws an unhandled exception when invoked
        var faultyPlugin = new Mock<ILicensePlugin>();
        faultyPlugin.Setup(p => p.PluginId).Returns("test.faulty");
        faultyPlugin.Setup(p => p.PluginName).Returns("Crashy Plugin");
        faultyPlugin.Setup(p => p.Manifest).Returns(new PluginManifest(
            pluginId: "test.faulty",
            pluginName: "Crashy Plugin",
            pluginVersion: "1.0.0",
            author: "Test",
            description: "Faulty plugin for testing Rule 9."
        ));
        faultyPlugin.Setup(p => p.CanCheck(It.IsAny<SoftwareInfo>())).Returns(true);
        faultyPlugin.Setup(p => p.CheckLicenseAsync(It.IsAny<SoftwareInfo>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated catastrophic plugin failure!"));

        var mockLoader = new Mock<IPluginLoader>();
        mockLoader.Setup(l => l.GetLoadedPlugins()).Returns(new[] { faultyPlugin.Object });

        var engine = new CoreEngine(
            new[] { mockScanner.Object },
            mockLoader.Object,
            CreateMergeEngine(),
            CreateCompatValidator(),
            NullLogger<CoreEngine>.Instance
        );

        // Act & Assert (Must not throw)
        var report = await engine.ExecuteFullScanAsync();

        Assert.NotNull(report);
        Assert.Single(report.Results);
        var result = report.Results[0];
        Assert.Equal("test.faulty", result.PluginId);
        Assert.Equal(ConfidenceLevel.None, result.Confidence);
        Assert.Contains("Simulated catastrophic plugin failure!", result.Notes);
    }

    [Fact]
    public async Task ExecuteFullScanAsync_WhenNoPluginsMatch_ShouldCreateFallbackUnknownResult()
    {
        // Arrange
        var mockScanner = new Mock<IScanner>();
        mockScanner.Setup(s => s.IsSupportedOnCurrentPlatform()).Returns(true);
        mockScanner.Setup(s => s.ScanAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[]
        {
            new SoftwareInfo("Custom Tool", "9.9", "Internal Team")
        });

        var mockPlugin = new Mock<ILicensePlugin>();
        mockPlugin.Setup(p => p.PluginId).Returns("test.nomatch");
        mockPlugin.Setup(p => p.PluginName).Returns("No Match Plugin");
        mockPlugin.Setup(p => p.Manifest).Returns(new PluginManifest(
            pluginId: "test.nomatch",
            pluginName: "No Match Plugin",
            pluginVersion: "1.0.0",
            author: "Test",
            description: "Plugin that never claims any software."
        ));
        mockPlugin.Setup(p => p.CanCheck(It.IsAny<SoftwareInfo>())).Returns(false);

        var mockLoader = new Mock<IPluginLoader>();
        mockLoader.Setup(l => l.GetLoadedPlugins()).Returns(new[] { mockPlugin.Object });

        var engine = new CoreEngine(
            new[] { mockScanner.Object },
            mockLoader.Object,
            CreateMergeEngine(),
            CreateCompatValidator(),
            NullLogger<CoreEngine>.Instance
        );

        // Act
        var report = await engine.ExecuteFullScanAsync();

        // Assert
        Assert.Single(report.Results);
        Assert.Equal("core.unknown", report.Results[0].PluginId);
        Assert.Equal(LicenseType.Unknown, report.Results[0].DetectedLicenseType);
    }
}

