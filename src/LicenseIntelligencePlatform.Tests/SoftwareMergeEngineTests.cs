using LicenseIntelligencePlatform.Application.Services;
using LicenseIntelligencePlatform.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LicenseIntelligencePlatform.Tests;

/// <summary>
/// Phase 2: Tests for SoftwareMergeEngine deduplication and best-wins metadata merging.
/// </summary>
public class SoftwareMergeEngineTests
{
    private static SoftwareMergeEngine CreateEngine() =>
        new SoftwareMergeEngine(NullLogger<SoftwareMergeEngine>.Instance);

    [Fact]
    public void Merge_WithNoDuplicates_ShouldReturnAllItems()
    {
        var engine = CreateEngine();
        var input = new[]
        {
            new SoftwareInfo("Git",    "2.54.0", "Git Team"),
            new SoftwareInfo("Docker", "4.66.0", "Docker Inc"),
        };

        var result = engine.Merge(input);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Merge_WithDuplicates_ShouldReturnSingleItem()
    {
        var engine = CreateEngine();
        var input = new[]
        {
            new SoftwareInfo("Git", "2.54.0", "Git Team"),
            new SoftwareInfo("Git", "2.54.0", "Git Team"), // duplicate
        };

        var result = engine.Merge(input);

        Assert.Single(result);
        Assert.Equal("Git", result[0].Name);
    }

    [Fact]
    public void Merge_WhenRegistryHasInstallPath_AndRamHasAppStartTime_ShouldMergeBoth()
    {
        var engine = CreateEngine();

        // Registry scanner: has InstallPath but no AppStartTime
        var fromRegistry = new SoftwareInfo(
            name: "MyApp",
            version: "1.0.0",
            publisher: "Acme",
            installPath: @"C:\Program Files\MyApp",
            installDate: "20260101",
            scanSource: "WindowsRegistry",
            lastModifiedDate: "2026/01/01 10:00:00 UTC",
            appStartTime: "");

        // RAM scanner: has AppStartTime but no InstallPath
        var fromRam = new SoftwareInfo(
            name: "MyApp",
            version: "1.0.0",
            publisher: "Acme",
            installPath: "",
            installDate: "",
            scanSource: "DeepFileSystem",
            lastModifiedDate: "",
            appStartTime: "2026/07/15 08:00:00 UTC");

        var result = engine.Merge(new[] { fromRegistry, fromRam });

        Assert.Single(result);
        var merged = result[0];

        Assert.Equal(@"C:\Program Files\MyApp", merged.InstallPath);
        Assert.Equal("2026/07/15 08:00:00 UTC", merged.AppStartTime);
        Assert.Equal("2026/01/01 10:00:00 UTC", merged.LastModifiedDate);
        Assert.Contains("WindowsRegistry", merged.ScanSource);
        Assert.Contains("DeepFileSystem", merged.ScanSource);
    }

    [Fact]
    public void Merge_WhenVersionsDiffer_ShouldNotMerge()
    {
        var engine = CreateEngine();
        var input = new[]
        {
            new SoftwareInfo("Git", "2.53.0", "Git Team"),
            new SoftwareInfo("Git", "2.54.0", "Git Team"), // different version = different product
        };

        var result = engine.Merge(input);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Merge_ShouldPreserveBestLastModifiedDate()
    {
        var engine = CreateEngine();
        var input = new[]
        {
            new SoftwareInfo("App", "1.0.0", "Vendor", lastModifiedDate: ""),
            new SoftwareInfo("App", "1.0.0", "Vendor", lastModifiedDate: "2026/06/01 12:00:00 UTC"),
        };

        var result = engine.Merge(input);

        Assert.Single(result);
        Assert.Equal("2026/06/01 12:00:00 UTC", result[0].LastModifiedDate);
    }

    [Fact]
    public void Merge_WithEmptyInput_ShouldReturnEmpty()
    {
        var engine = CreateEngine();

        var result = engine.Merge(Array.Empty<SoftwareInfo>());

        Assert.Empty(result);
    }
}
