using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Plugins.Standard.Plugins;
using Xunit;

namespace LicenseIntelligencePlatform.Tests;

public class StandardPluginsTests
{
    [Fact]
    public async Task OpenSourceArtifactPlugin_WhenKeywordMatches_ShouldReturnMediumConfidence()
    {
        // Arrange
        var plugin = new OpenSourceArtifactPlugin();
        var software = new SoftwareInfo("git-core", "2.40.0", "Open Source Maintainers");

        // Act
        var canCheck = plugin.CanCheck(software);
        var result = await plugin.CheckLicenseAsync(software);

        // Assert
        Assert.True(canCheck);
        Assert.Equal(LicenseType.OpenSource, result.DetectedLicenseType);
        Assert.Equal(ConfidenceLevel.Medium, result.Confidence);
        Assert.Single(result.Evidences);
    }

    [Fact]
    public async Task OpenSourceArtifactPlugin_WhenLicenseFileExists_ShouldReturnVerifiedConfidence()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var licPath = Path.Combine(tempDir, "LICENSE");
            await File.WriteAllTextAsync(licPath, "MIT License\nPermission is hereby granted, free of charge...");

            var plugin = new OpenSourceArtifactPlugin();
            var software = new SoftwareInfo("MyPackage", "1.0.0", "Any Publisher", installPath: tempDir);

            // Act
            var canCheck = plugin.CanCheck(software);
            var result = await plugin.CheckLicenseAsync(software);

            // Assert
            Assert.True(canCheck);
            Assert.Equal(LicenseType.OpenSource, result.DetectedLicenseType);
            Assert.Equal("MIT License", result.LicenseName);
            Assert.Equal(ConfidenceLevel.Verified, result.Confidence);
            Assert.True(result.IsVerified);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CommercialKeyFilePlugin_WhenEnterpriseName_ShouldDetectCommercialLicense()
    {
        // Arrange
        var plugin = new CommercialKeyFilePlugin();
        var software = new SoftwareInfo("Visual Studio Enterprise 2022", "17.8", "Microsoft");

        // Act
        var canCheck = plugin.CanCheck(software);
        var result = await plugin.CheckLicenseAsync(software);

        // Assert
        Assert.True(canCheck);
        Assert.Equal(LicenseType.Commercial, result.DetectedLicenseType);
        Assert.True(result.Confidence >= ConfidenceLevel.Medium);
    }

    [Fact]
    public async Task FreewarePatternPlugin_WhenCommunityEdition_ShouldDetectFreeware()
    {
        // Arrange
        var plugin = new FreewarePatternPlugin();
        var software = new SoftwareInfo("PyCharm Community Edition", "2023.3", "JetBrains");

        // Act
        var canCheck = plugin.CanCheck(software);
        var result = await plugin.CheckLicenseAsync(software);

        // Assert
        Assert.True(canCheck);
        Assert.Equal(LicenseType.Freeware, result.DetectedLicenseType);
        Assert.Equal(ConfidenceLevel.Medium, result.Confidence);
    }
}
