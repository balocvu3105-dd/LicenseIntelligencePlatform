using System.Text.Json;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;
using LicenseIntelligencePlatform.Plugins.Standard.Plugins;
using Xunit;
using Xunit.Abstractions;

namespace LicenseIntelligencePlatform.Tests;

/// <summary>
/// Verifies the detection accuracy KPI across real-world software packages in TestData.
/// Ensures overall platform accuracy meets or exceeds the 96% threshold required for v1.0 Pilot release.
/// </summary>
public class AccuracyVerificationTests
{
    private readonly ITestOutputHelper _output;

    public AccuracyVerificationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task VerifyPlatformAccuracy_ShouldMeetOrExceed96PercentKpi()
    {
        // 1. Setup all 26 standard and vendor plugins
        var plugins = new ILicensePlugin[]
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
            new VlcPlayerPlugin()
        };

        // 2. Locate TestData/Software directory
        var baseDir = AppContext.BaseDirectory;
        var testDataDir = Path.Combine(baseDir, "..", "..", "..", "TestData", "Software");
        if (!Directory.Exists(testDataDir))
        {
            testDataDir = Path.Combine(Directory.GetCurrentDirectory(), "src", "LicenseIntelligencePlatform.Tests", "TestData", "Software");
        }
        if (!Directory.Exists(testDataDir))
        {
            testDataDir = Path.Combine(Directory.GetCurrentDirectory(), "tests", "TestData", "Software");
        }

        Assert.True(Directory.Exists(testDataDir), $"TestData directory not found at {testDataDir}");

        var softwareDirs = Directory.GetDirectories(testDataDir);
        Assert.NotEmpty(softwareDirs);

        int totalTested = 0;
        int totalCorrect = 0;

        _output.WriteLine(string.Format("{0,-20} | {1,-15} | {2,-15} | {3,-10}", "Software", "Expected", "LIP Result", "Status"));
        _output.WriteLine(new string('-', 68));

        foreach (var dir in softwareDirs)
        {
            var regFile = Path.Combine(dir, "registry.json");
            var expectedFile = Path.Combine(dir, "expected-result.json");

            if (!File.Exists(regFile) || !File.Exists(expectedFile)) continue;

            var regJson = await File.ReadAllTextAsync(regFile);
            var regInfo = JsonSerializer.Deserialize<Dictionary<string, string>>(regJson);
            if (regInfo == null) continue;

            var expectedJson = await File.ReadAllTextAsync(expectedFile);
            var expectedInfo = JsonSerializer.Deserialize<Dictionary<string, string>>(expectedJson);
            if (expectedInfo == null) continue;

            var software = new SoftwareInfo(
                name: regInfo.GetValueOrDefault("DisplayName", Path.GetFileName(dir)),
                version: regInfo.GetValueOrDefault("DisplayVersion", "1.0"),
                publisher: regInfo.GetValueOrDefault("Publisher", ""),
                installPath: regInfo.GetValueOrDefault("InstallLocation", dir),
                scanSource: "AccuracyTest"
            );

            // Execute plugins on software
            LicenseCheckResult? bestResult = null;
            foreach (var plugin in plugins)
            {
                if (plugin.CanCheck(software))
                {
                    var res = await plugin.CheckLicenseAsync(software);
                    if (bestResult == null || res.Confidence > bestResult.Confidence)
                    {
                        bestResult = res;
                    }
                }
            }

            totalTested++;
            var detectedType = bestResult?.DetectedLicenseType ?? LicenseType.Unknown;
            var expectedTypeStr = expectedInfo.GetValueOrDefault("ExpectedLicenseType", "Unknown");
            Enum.TryParse<LicenseType>(expectedTypeStr, true, out var expectedType);

            bool isCorrect = (detectedType == expectedType);
            if (isCorrect) totalCorrect++;

            _output.WriteLine(string.Format("{0,-20} | {1,-15} | {2,-15} | {3,-10}",
                Path.GetFileName(dir), expectedType, detectedType, isCorrect ? "✅ Correct" : "❌ Incorrect"));
        }

        _output.WriteLine(new string('-', 68));
        double accuracy = totalTested > 0 ? ((double)totalCorrect / totalTested) * 100.0 : 0;
        _output.WriteLine($"Total Tested: {totalTested} | Correct: {totalCorrect} | Accuracy: {accuracy:F2}%");

        Assert.True(totalTested >= 10, "Should test at least 10 software dataset samples.");
        Assert.True(accuracy >= 96.0, $"Platform accuracy KPI failure! Expected >= 96%, got {accuracy:F2}%");
    }
}
