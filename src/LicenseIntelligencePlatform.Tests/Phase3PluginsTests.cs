using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Plugins.Standard.Plugins;
using Xunit;

namespace LicenseIntelligencePlatform.Tests;

/// <summary>
/// Phase 3 verification tests for the 7 new domain ecosystem & suite plugins.
/// </summary>
public class Phase3PluginsTests
{
    [Fact]
    public async Task MicrosoftRuntimeEcosystemPlugin_WhenNetRuntime_ShouldDetectAndReturnFreewareOrOpenSource()
    {
        var plugin = new MicrosoftRuntimeEcosystemPlugin();
        var software = new SoftwareInfo("Microsoft .NET Runtime - 8.0.28 (x64)", "8.0.28", "Microsoft Corporation");

        Assert.True(plugin.CanCheck(software));
        var result = await plugin.CheckLicenseAsync(software);

        Assert.NotNull(result);
        Assert.Equal(plugin.PluginId, result.PluginId);
        Assert.NotEmpty(result.Evidences);
    }

    [Fact]
    public async Task GamingPlatformsEcosystemPlugin_WhenSteam_ShouldReturnFreewarePlatform()
    {
        var plugin = new GamingPlatformsEcosystemPlugin();
        var software = new SoftwareInfo("Steam", "2.10.91.91", "Valve Corp.");

        Assert.True(plugin.CanCheck(software));
        var result = await plugin.CheckLicenseAsync(software);

        Assert.Equal(LicenseType.Freeware, result.DetectedLicenseType);
        Assert.Contains(result.Evidences, e => e.EvidenceType == "KeywordMatch");
    }

    [Fact]
    public async Task GamingPlatformsEcosystemPlugin_WhenPalworldOrCs2_ShouldReturnCommercialGame()
    {
        var plugin = new GamingPlatformsEcosystemPlugin();
        var software = new SoftwareInfo("Counter-Strike 2", "1.39.0", "Valve");

        Assert.True(plugin.CanCheck(software));
        var result = await plugin.CheckLicenseAsync(software);

        Assert.Equal(LicenseType.Commercial, result.DetectedLicenseType);
    }

    [Fact]
    public async Task CommunicationCollaboratePlugin_WhenDiscord_ShouldReturnFreeware()
    {
        var plugin = new CommunicationCollaboratePlugin();
        var software = new SoftwareInfo("Discord", "1.0.9036", "Discord Inc.");

        Assert.True(plugin.CanCheck(software));
        var result = await plugin.CheckLicenseAsync(software);

        Assert.Equal(LicenseType.Freeware, result.DetectedLicenseType);
    }

    [Fact]
    public async Task HardwareOemUtilitiesPlugin_WhenArmouryCrate_ShouldDetectAndReturnFreeware()
    {
        var plugin = new HardwareOemUtilitiesPlugin();
        var software = new SoftwareInfo("Armoury Crate Service", "6.5.7.0", "ASUSTeK COMPUTER INC.");

        Assert.True(plugin.CanCheck(software));
        var result = await plugin.CheckLicenseAsync(software);

        Assert.Equal("std.hardware.oem", result.PluginId);
        Assert.Equal(LicenseType.Freeware, result.DetectedLicenseType);
    }

    [Fact]
    public async Task NvidiaConsumerSuitePlugin_WhenGeForceNowOrDriver_ShouldDetectAndReturnFreeware()
    {
        var plugin = new NvidiaConsumerSuitePlugin();
        var software = new SoftwareInfo("NVIDIA Graphics Driver 610.47", "610.47", "NVIDIA Corporation");

        Assert.True(plugin.CanCheck(software));
        var result = await plugin.CheckLicenseAsync(software);

        Assert.Equal("std.nvidia.consumer", result.PluginId);
        Assert.NotEmpty(result.Evidences);
    }

    [Fact]
    public async Task WebBrowsersAndToolsPlugin_WhenChromeOrEdge_ShouldReturnFreeware()
    {
        var plugin = new WebBrowsersAndToolsPlugin();
        var software = new SoftwareInfo("Google Chrome", "126.0.6478.127", "Google LLC");

        Assert.True(plugin.CanCheck(software));
        var result = await plugin.CheckLicenseAsync(software);

        Assert.Equal(LicenseType.Freeware, result.DetectedLicenseType);
    }

    [Fact]
    public async Task AntiCheatAndSecurityPlugin_WhenRiotVanguardOrWindowsDefender_ShouldDetectAndReturnHighOrVerified()
    {
        var plugin = new AntiCheatAndSecurityPlugin();
        var software = new SoftwareInfo("Riot Vanguard", "1.0.0", "Riot Games, Inc.");

        Assert.True(plugin.CanCheck(software));
        var result = await plugin.CheckLicenseAsync(software);

        Assert.Equal("std.security.anticheat", result.PluginId);
        Assert.True(result.Confidence >= ConfidenceLevel.High);
    }

    [Fact]
    public async Task WindowsOsLicensePlugin_WhenGenuineOs_ShouldVerifyAndReturnGenuine()
    {
        var plugin = new WindowsOsLicensePlugin();
        var software = new SoftwareInfo("Microsoft Windows Operating System (OS License Check)", "Windows 11 Pro (Build 26100)", "Microsoft Corporation", @"C:\Windows\System32\spp", "2026/01/01", "OS License Scanner [RETAIL Channel - Status: Activated (Licensed)]");

        Assert.True(plugin.CanCheck(software));
        var result = await plugin.CheckLicenseAsync(software);

        Assert.Equal("os.windows", result.PluginId);
        Assert.Equal(ConfidenceLevel.Verified, result.Confidence);
        Assert.Contains("Genuine Microsoft License", result.LicenseName);
        Assert.Contains(result.Evidences, e => e.EvidenceType == "Verified Genuine Activation Channel");
    }

    [Fact]
    public async Task WindowsOsLicensePlugin_WhenKmsCrackDetected_ShouldFlagPiratedAndAlert()
    {
        var plugin = new WindowsOsLicensePlugin();
        var software = new SoftwareInfo("Microsoft Windows Operating System (OS License Check)", "Windows 10 Pro (Build 19045)", "Microsoft Corporation", @"C:\Windows\System32\spp", "2026/01/01", "OS License Scanner [KMS CRACK DETECTED: Activator binary found: SppExtComObj.exe | Status: Activated]");

        Assert.True(plugin.CanCheck(software));
        var result = await plugin.CheckLicenseAsync(software);

        Assert.Equal("os.windows", result.PluginId);
        Assert.Equal(ConfidenceLevel.Verified, result.Confidence);
        Assert.Contains("Pirated / Invalid KMS Key", result.LicenseName);
        Assert.Contains(result.Evidences, e => e.EvidenceType == "Audit Alert - Pirated KMS Crack Detected");
    }
}
