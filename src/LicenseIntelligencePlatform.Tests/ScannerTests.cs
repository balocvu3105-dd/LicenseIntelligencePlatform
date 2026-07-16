using System.Runtime.InteropServices;
using LicenseIntelligencePlatform.Infrastructure.Scanners;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LicenseIntelligencePlatform.Tests;

public class ScannerTests
{
    [Fact]
    public void WindowsRegistryScanner_IsSupportedOnCurrentPlatform_ShouldMatchWindowsOS()
    {
        // Arrange
        var scanner = new WindowsRegistryScanner(NullLogger<WindowsRegistryScanner>.Instance);

        // Act
        var isSupported = scanner.IsSupportedOnCurrentPlatform();

        // Assert
        Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), isSupported);
    }

    [Fact]
    public async Task LinuxPackageScanner_WhenStatusFileProvided_ShouldParsePackagesCorrectly()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, """
                Package: curl
                Status: install ok installed
                Version: 7.88.1-10
                Maintainer: Debian Curl Maintainers

                Package: git
                Status: install ok installed
                Version: 1:2.39.2-1
                Maintainer: Git Packagers
                """);

            var scanner = new LinuxPackageScanner(NullLogger<LinuxPackageScanner>.Instance, tempFile);

            // Act
            var results = (await scanner.ScanAsync()).ToList();

            // Assert
            Assert.Equal(2, results.Count);
            Assert.Equal("curl", results[0].Name);
            Assert.Equal("7.88.1-10", results[0].Version);
            Assert.Equal("git", results[1].Name);
            Assert.Equal("1:2.39.2-1", results[1].Version);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void DeepFileSystemScanner_IsSupportedOnCurrentPlatform_ShouldMatchWindowsOS()
    {
        // Arrange
        var scanner = new DeepFileSystemScanner(NullLogger<DeepFileSystemScanner>.Instance);

        // Act
        var isSupported = scanner.IsSupportedOnCurrentPlatform();

        // Assert
        Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), isSupported);
    }
}
