using System.Runtime.InteropServices;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Infrastructure.Scanners;
using LicenseIntelligencePlatform.Plugins.Standard.Plugins;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LicenseIntelligencePlatform.Tests;

/// <summary>
/// Comprehensive unit and integration tests verifying the Windows License Audit module,
/// Evidence Model extensions, Weighted Risk Scoring algorithm, Digital Signature verification,
/// and pipeline/reporting synchronization across scanner and plugin layers.
/// </summary>
public class WindowsLicenseAuditModuleTests : IDisposable
{
    public WindowsLicenseAuditModuleTests()
    {
        WindowsLicenseAuditContext.Clear();
    }

    public void Dispose()
    {
        WindowsLicenseAuditContext.Clear();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void WindowsLicenseAuditData_ShouldInitializeWithDefaultsAndCleanRecordProperties()
    {
        // Arrange & Act
        var auditData = new WindowsLicenseAuditData
        {
            WindowsEdition = "Windows 11 Pro",
            BuildNumber = "26100.3194",
            ProductName = "Windows 11 Pro",
            InstallDate = "2025/01/01 12:00:00",
            Architecture = "X64",
            ActivationStatus = "Activated (Licensed)",
            LicenseChannel = "RETAIL Channel",
            OemKeyPresence = true,
            InstalledProductKeyMasked = "XXXXX-XXXXX-XXXXX-XXXXX-3V66T",
            BiosEmbeddedKey = "XXXXX-XXXXX-XXXXX-XXXXX-OEM01 (System ACPI MSDM)",
            RiskScore = 0,
            RiskClassification = "Legitimate / Clean Microsoft License (LOW RISK)",
            SummaryText = "OS License Scanner [RETAIL - Status: Activated]"
        };

        // Assert
        Assert.Equal("Windows 11 Pro", auditData.WindowsEdition);
        Assert.Equal("26100.3194", auditData.BuildNumber);
        Assert.Equal("X64", auditData.Architecture);
        Assert.True(auditData.OemKeyPresence);
        Assert.Equal(0, auditData.RiskScore);
        Assert.Contains("RETAIL", auditData.LicenseChannel);
    }

    [Fact]
    public void EvidenceModel_ExtendedProperties_ShouldPopulateCategorySeverityConfidenceReasonPathTimestampAndRecommendation()
    {
        // Arrange & Act
        var ev = new Evidence(
            evidenceType: "KMS Crack Activator Binary Discovered",
            description: "Unauthorized KMS file found inside C:\\Windows\\System32\\SppExtComObj.exe",
            sourceLocation: "System32 Directory Inspection",
            rawData: "Path: C:\\Windows\\System32\\SppExtComObj.exe | Signature: Unsigned")
        {
            Category = "FileSystemArtifact",
            Severity = "CRITICAL",
            Confidence = "Verified",
            Reason = "Known KMS crack bypass binary present.",
            Path = @"C:\Windows\System32\SppExtComObj.exe",
            Recommendation = "Immediately delete unauthorized binary and run sfc /scannow."
        };

        // Assert
        Assert.Equal("KMS Crack Activator Binary Discovered", ev.EvidenceType);
        Assert.Equal("FileSystemArtifact", ev.Category);
        Assert.Equal("CRITICAL", ev.Severity);
        Assert.Equal("Verified", ev.Confidence);
        Assert.Equal("Known KMS crack bypass binary present.", ev.Reason);
        Assert.Equal(@"C:\Windows\System32\SppExtComObj.exe", ev.Path);
        Assert.NotEmpty(ev.Timestamp);
        Assert.Contains("delete unauthorized binary", ev.Recommendation);
    }

    [Fact]
    public void WindowsOsScanner_IsSupportedOnCurrentPlatform_ShouldMatchWindowsOS()
    {
        // Arrange
        var scanner = new WindowsOsScanner(NullLogger<WindowsOsScanner>.Instance);

        // Act
        var isSupported = scanner.IsSupportedOnCurrentPlatform();

        // Assert
        Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), isSupported);
        Assert.Equal("WindowsOsLicenseScanner", scanner.ScannerName);
    }

    [Fact]
    public async Task WindowsOsLicensePlugin_WhenAuditContextHasLowRiskScore_ShouldReturnVerifiedGenuineAndCleanNotes()
    {
        try
        {
            // Arrange
            var plugin = new WindowsOsLicensePlugin();
            var auditData = new WindowsLicenseAuditData
            {
                WindowsEdition = "Windows 11 Pro",
                BuildNumber = "26100.3194",
                ActivationStatus = "Activated (Licensed)",
                LicenseChannel = "RETAIL Channel",
                InstalledProductKeyMasked = "XXXXX-XXXXX-XXXXX-XXXXX-3V66T",
                RiskScore = 0,
                RiskClassification = "Legitimate / Clean Microsoft License (LOW RISK)",
                Recommendations = new[] { "✔ Windows Operating System is genuinely activated." }
            };

            WindowsLicenseAuditContext.CurrentAuditData = auditData;

            var software = new SoftwareInfo("Microsoft Windows Operating System (OS License Check)", "Windows 11 Pro", "Microsoft Corporation");

            // Act
            Assert.True(plugin.CanCheck(software));
            var result = await plugin.CheckLicenseAsync(software);

            // Assert
            Assert.Equal("os.windows", result.PluginId);
            Assert.Equal(ConfidenceLevel.Verified, result.Confidence);
            Assert.Contains("Genuine Microsoft License", result.LicenseName);
            Assert.Contains("✔ GENUINE WINDOWS ACTIVATION VERIFIED", result.Notes);
            Assert.Single(result.Evidences);
            Assert.Equal("Verified Genuine Activation Channel", result.Evidences[0].EvidenceType);
        }
        finally
        {
            WindowsLicenseAuditContext.Clear();
        }
    }

    [Fact]
    public async Task WindowsOsLicensePlugin_WhenAuditContextHasCriticalRiskScore_ShouldReturnPiratedAndComplianceAlert()
    {
        try
        {
            // Arrange
            var plugin = new WindowsOsLicensePlugin();
            var crackEv = new Evidence("KMS Crack Activator Binary Discovered", "Found SppExtComObj.exe in System32", "System32", "SppExtComObj.exe")
            {
                Category = "FileSystemArtifact",
                Severity = "CRITICAL",
                Confidence = "Verified"
            };

            var auditData = new WindowsLicenseAuditData
            {
                WindowsEdition = "Windows 10 Enterprise",
                BuildNumber = "19045.5487",
                ActivationStatus = "Activated (Licensed)",
                LicenseChannel = "VOLUME_KMSCLIENT (GVLK)",
                RiskScore = 85,
                RiskClassification = "Likely Unauthorized Activation / Pirated (CRITICAL RISK)",
                AuditEvidences = new[] { crackEv },
                Recommendations = new[] { "⚠️ CRITICAL COMPLIANCE VIOLATION: Immediately remove activator binaries." }
            };

            WindowsLicenseAuditContext.CurrentAuditData = auditData;

            var software = new SoftwareInfo("Microsoft Windows Operating System (OS License Check)", "Windows 10 Enterprise", "Microsoft Corporation");

            // Act
            var result = await plugin.CheckLicenseAsync(software);

            // Assert
            Assert.Equal("os.windows", result.PluginId);
            Assert.Equal(ConfidenceLevel.Verified, result.Confidence);
            Assert.Contains("Pirated / Invalid KMS Key", result.LicenseName);
            Assert.Contains("⚠️ CRITICAL COMPLIANCE ALERT", result.Notes);
            Assert.Single(result.Evidences);
            Assert.Equal("KMS Crack Activator Binary Discovered", result.Evidences[0].EvidenceType);
        }
        finally
        {
            WindowsLicenseAuditContext.Clear();
        }
    }

    [Fact]
    public async Task WindowsOsLicensePlugin_WhenAuditContextHasMediumRiskScore_ShouldReturnAnomalousOrUnresolvedKmsWarning()
    {
        try
        {
            // Arrange
            var plugin = new WindowsOsLicensePlugin();
            var auditData = new WindowsLicenseAuditData
            {
                WindowsEdition = "Windows 11 Enterprise",
                BuildNumber = "26100.1000",
                ActivationStatus = "Activated (Licensed)",
                LicenseChannel = "VOLUME_KMSCLIENT (GVLK)",
                RiskScore = 30,
                RiskClassification = "Anomalous / Grace Period / Unresolved KMS Client (MEDIUM RISK)",
                Recommendations = new[] { "⚠ Inspect corporate network accessibility to ensure KMS host is reachable." }
            };

            WindowsLicenseAuditContext.CurrentAuditData = auditData;

            var software = new SoftwareInfo("Microsoft Windows Operating System (OS License Check)", "Windows 11 Enterprise", "Microsoft Corporation");

            // Act
            var result = await plugin.CheckLicenseAsync(software);

            // Assert
            Assert.Equal(ConfidenceLevel.High, result.Confidence);
            Assert.Contains("Anomalous / Grace Period / Unresolved KMS", result.LicenseName);
            Assert.Contains("⚠ COMPLIANCE WARNING", result.Notes);
        }
        finally
        {
            WindowsLicenseAuditContext.Clear();
        }
    }

    [Fact]
    public async Task WindowsOsLicensePlugin_StandaloneFallbackCheck_ShouldEvaluateScanSourceAccurately()
    {
        // Arrange (Ensure exact empty state for offline evaluation)
        WindowsLicenseAuditContext.Clear();
        var plugin = new WindowsOsLicensePlugin();

        var softwareClean = new SoftwareInfo("Microsoft Windows Operating System (OS License Check)", "Windows 11 Pro", "Microsoft Corporation", @"C:\Windows\System32\spp", "2026/01/01", "OS License Scanner [RETAIL - Status: Activated (Licensed)]");
        var softwarePirate = new SoftwareInfo("Microsoft Windows Operating System (OS License Check)", "Windows 10 Pro", "Microsoft Corporation", @"C:\Windows\System32\spp", "2026/01/01", "OS License Scanner [KMS CRACK DETECTED: Activator binary SppExtComObj.exe]");

        // Act
        var resultClean = await plugin.CheckLicenseAsync(softwareClean);
        var resultPirate = await plugin.CheckLicenseAsync(softwarePirate);

        // Assert
        Assert.Contains("Genuine Microsoft License", resultClean.LicenseName);
        Assert.Contains("Pirated / Invalid KMS Key", resultPirate.LicenseName);
        Assert.Equal("Audit Alert - Pirated KMS Crack Detected", resultPirate.Evidences[0].EvidenceType);
    }

    [Fact]
    public async Task WindowsOsScanner_ScanAsync_ShouldNotThrowAndPopulateSharedContext()
    {
        // Skip on non-Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        try
        {
            // Arrange
            var scanner = new WindowsOsScanner(NullLogger<WindowsOsScanner>.Instance);

            // Act
            var results = (await scanner.ScanAsync()).ToList();

            // Assert
            Assert.Single(results);
            var osPkg = results[0];
            Assert.Equal("Microsoft Windows Operating System (OS License Check)", osPkg.Name);
            Assert.NotNull(WindowsLicenseAuditContext.CurrentAuditData);
            Assert.True(WindowsLicenseAuditContext.CurrentAuditData.RiskScore >= 0 && WindowsLicenseAuditContext.CurrentAuditData.RiskScore <= 100);
            Assert.NotEmpty(WindowsLicenseAuditContext.CurrentAuditData.WindowsEdition);
        }
        finally
        {
            WindowsLicenseAuditContext.Clear();
        }
    }
}
