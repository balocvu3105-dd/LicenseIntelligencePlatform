# 06_IMPLEMENTATION_GUIDE.md

# License Intelligence Platform (LIP)

## Implementation Guide

Version: 1.0

Status: Stable

Author: DynamiteV

---

# Purpose

Tài liệu **Implementation Guide** (Hướng dẫn triển khai) là bản đặc tả kỹ thuật thực hành chi tiết nhất dành cho các lập trình viên cốt lõi (Core Engineers), kỹ sư phát triển Plugin (Plugin Developers), và AI Coding Assistants.

Mục tiêu của tài liệu này:
- Cung cấp **mã nguồn mẫu chuẩn mực (Production-Ready Templates)** cho từng thành phần kiến trúc (`Scanner`, `Plugin`, `Engine`, `Exporter`).
- Chuẩn hóa quy trình từng bước (**Step-by-Step Stepwise Implementation**) từ lúc khởi tạo class mới cho đến khi tích hợp vào Dependency Injection Pipeline.
- Đảm bảo tính chống chịu lỗi tối đa (**Resilience & Fault Isolation**) theo đúng các nguyên tắc Clean Architecture và Zero-Trust Security.
- Ngăn chặn triệt để các sai lầm kiến trúc (Anti-Patterns / Technical Debts) phát sinh trong suốt vòng đời dự án.

---

# 1. Architectural Boundaries & Dependency Rules

LIP được thiết kế theo mô hình **Clean Architecture 5 tầng**. Mọi thay đổi trong mã nguồn phải tuyệt đối tuân thủ chiều phụ thuộc một chiều hướng vào trung tâm (`Domain`).

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Presentation.Cli (CLI Entry Point - Program.cs, Command Handlers)                │
└────────────────────────────────────────┬─────────────────────────────────────────┘
                                         │ (Reference)
┌────────────────────────────────────────▼─────────────────────────────────────────┐
│ Application Layer (CoreEngine, ScanCoordinator, RuleEngine, MergeEngine)         │
└────────────────────────────────────────┬─────────────────────────────────────────┘
                                         │ (Reference)
┌────────────────────────────────────────▼─────────────────────────────────────────┐
│ Domain Layer (Entities: SoftwareInfo, Evidence, LicenseCheckResult, Interfaces)  │
└────────────────────────────────────────▲─────────────────────────────────────────┘
                                         │ (Implement / Reference)
┌────────────────────────────────────────┴─────────────────────────────────────────┐
│ Infrastructure & Plugins Layer (Scanners, Exporters, Logging, 26 Standard Plugins)│
└──────────────────────────────────────────────────────────────────────────────────┘
```

## Quy tắc phụ thuộc bất di bất dịch (Dependency Invariants):

1. **Domain Layer là Độc lập tuyệt đối (Zero External Dependencies)**:
   - Trong thư mục `src/LicenseIntelligencePlatform.Domain/`, **CẤM TUYỆT ĐỐI** thêm bất kỳ tham chiếu nào tới `Microsoft.Win32.Registry`, `System.IO.File`, `System.Net`, hay các gói NuGet bên thứ ba (trừ các thư viện chuẩn thuần túy của `.NET Core`).
   - Domain chỉ định nghĩa Entities (`record`, `class`), Value Objects, Enums, và Interfaces (`IScanner`, `ILicensePlugin`, `IReportMapper`).
2. **Application Layer chỉ giao tiếp qua Interfaces**:
   - `CoreEngine` và các Service trong `Application` không bao giờ được phép dùng từ khóa `new` để khởi tạo trực tiếp các concrete class thuộc tầng Infrastructure (Ví dụ: `new WindowsRegistryScanner()` là vi phạm nghiêm trọng).
   - Mọi thành phần đều phải được tiêm phụ thuộc thông qua `IEnumerable<IScanner>`, `IEnumerable<ILicensePlugin>`, và `IEnumerable<IReportMapper>`.
3. **Infrastructure & Plugins Layer không phụ thuộc vào nhau**:
   - Tầng `Plugins.Standard` không tham chiếu đến `Infrastructure` và ngược lại. Cả hai chỉ tham chiếu đến `Domain` và giao tiếp với nhau gián tiếp thông qua sự điều phối của `Application Layer`.

---

# 2. Step-by-Step Implementation of Scanners (`IScanner`)

`IScanner` chịu trách nhiệm thu thập thông tin phần mềm thô từ hệ điều hành (Registry, Package Manager, File System) và biến đổi thành danh sách `SoftwareInfo`.

## 2.1. Nguyên tắc cốt lõi của Scanner (3 KHÔNG)

- **1. KHÔNG GHI (Read-Only Only):** Tuyệt đối không gọi `RegistryKey.SetValue()`, `RegistryKey.CreateSubKey()`, `File.WriteAllText()`, hay bất kỳ API chỉnh sửa hệ thống nào.
- **2. KHÔNG CRASH PIPELINE (Non-Throwing Resilience):** Khi gặp lỗi phân quyền (`UnauthorizedAccessException`, `SecurityException`) hoặc lỗi khóa registry bị hỏng, Scanner phải `try/catch`, log cảnh báo `LogWarning` và bỏ qua nút đó để tiếp tục quét các nút còn lại.
- **3. KHÔNG GIAO TIẾP MẠNH (Zero Network):** Không gọi REST API, HTTP request hay DNS lookup để tra cứu thông tin phần mềm trực tuyến.

## 2.2. Giao diện chuẩn `IScanner`

```csharp
namespace LicenseIntelligencePlatform.Domain.Interfaces;

public interface IScanner
{
    /// <summary>
    /// Tên định danh của Scanner (Ví dụ: "WindowsRegistryScanner").
    /// </summary>
    string ScannerName { get; }

    /// <summary>
    /// Kiểm tra xem Scanner có hỗ trợ chạy trên hệ điều hành hiện tại hay không.
    /// </summary>
    bool IsSupportedOnCurrentPlatform();

    /// <summary>
    /// Thực hiện quét hệ thống và trả về danh sách phần mềm thu thập được.
    /// </summary>
    Task<IEnumerable<SoftwareInfo>> ScanAsync(CancellationToken cancellationToken = default);
}
```

## 2.3. Mã nguồn mẫu chuẩn cho một Custom Scanner (`WingetPackageScanner`)

Dưới đây là production-ready template khi lập trình viên cần viết một Scanner mới (Ví dụ: quét danh sách gói cài đặt qua WinGet metadata hoặc file cục bộ):

```csharp
using Microsoft.Extensions.Logging;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Infrastructure.Scanners;

/// <summary>
/// Scanner thu thập thông tin phần mềm từ hệ thống quản lý gói WinGet (Local Repository Metadata).
/// Tuân thủ nguyên tắc Read-Only và Non-Throwing.
/// </summary>
public sealed class WingetPackageScanner : IScanner
{
    private readonly ILogger<WingetPackageScanner> _logger;

    public WingetPackageScanner(ILogger<WingetPackageScanner> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ScannerName => "WingetPackageScanner";

    public bool IsSupportedOnCurrentPlatform() => OperatingSystem.IsWindows();

    public async Task<IEnumerable<SoftwareInfo>> ScanAsync(CancellationToken cancellationToken = default)
    {
        if (!IsSupportedOnCurrentPlatform())
        {
            _logger.LogDebug("WingetPackageScanner skipped: Unsupported OS platform.");
            return Array.Empty<SoftwareInfo>();
        }

        var discoveredPackages = new List<SoftwareInfo>();

        try
        {
            // Xác định đường dẫn thư mục lưu trữ metadata của WinGet (Local Read-Only)
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string wingetPath = Path.Combine(localAppData, @"Packages\Microsoft.DesktopAppInstaller_8wekyb3d8bbwe\LocalState");

            if (!Directory.Exists(wingetPath))
            {
                _logger.LogDebug("Winget local metadata directory not found at {Path}.", wingetPath);
                return Array.Empty<SoftwareInfo>();
            }

            // Luôn kiểm tra CancellationToken trước khi thực hiện các vòng lặp nặng
            cancellationToken.ThrowIfCancellationRequested();

            // Thực hiện quét file siêu dữ liệu một cách an toàn
            var files = Directory.GetFiles(wingetPath, "*.json", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var fileInfo = new FileInfo(file);
                    
                    // Tạo đối tượng SoftwareInfo thuần trong bộ nhớ RAM
                    var software = new SoftwareInfo(
                        Name: Path.GetFileNameWithoutExtension(fileInfo.Name),
                        Version: "1.0.0.0", // Hoặc parse từ metadata an toàn
                        Publisher: "WinGet Package Ecosystem",
                        InstallLocation: wingetPath,
                        ExecutablePath: null,
                        ProductCode: $"WINGET-{Path.GetFileNameWithoutExtension(fileInfo.Name)}",
                        UpgradeCode: null,
                        InstallDate: fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-dd"),
                        InstallSource: "WinGet Local Package Repository",
                        OperatingSystem: "Windows"
                    );

                    discoveredPackages.Add(software);
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogWarning("Access denied while reading WinGet metadata file {File}: {Message}", file, ex.Message);
                }
                catch (IOException ex)
                {
                    _logger.LogWarning("IO error reading WinGet file {File}: {Message}", file, ex.Message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("WingetPackageScanner execution canceled by user/timeout.");
            throw; // Cho phép truyền OperationCanceledException lên CoreEngine
        }
        catch (Exception ex)
        {
            // Cô lập lỗi Scanner, không làm sập toàn bộ quy trình quét của CoreEngine
            _logger.LogError(ex, "Unhandled exception in WingetPackageScanner. Returning partial results ({Count}).", discoveredPackages.Count);
        }

        return await Task.FromResult(discoveredPackages);
    }
}
```

---

# 3. Step-by-Step Implementation of License Plugins (`ILicensePlugin`)

`ILicensePlugin` là trái tim của hệ thống phân tích bản quyền. Mỗi Plugin chịu trách nhiệm phát hiện, thu thập bằng chứng (`Evidence`), và tính toán `ConfidenceLevel` cho một phần mềm hoặc hệ sinh thái phần mềm cụ thể.

## 3.1. Giao diện chuẩn `ILicensePlugin`

```csharp
namespace LicenseIntelligencePlatform.Domain.Interfaces;

public interface ILicensePlugin
{
    /// <summary>
    /// Metadata mô tả đầy đủ thông tin Plugin (Version, Priority, SDK Compatibility).
    /// </summary>
    PluginManifest Manifest { get; }

    /// <summary>
    /// Kiểm tra nhanh trong RAM xem Plugin có áp dụng cho phần mềm này không (Không làm I/O nặng).
    /// </summary>
    bool CanCheck(SoftwareInfo software);

    /// <summary>
    /// Thực hiện phân tích chuyên sâu, thu thập Evidence và trả về kết quả định dạng bản quyền.
    /// </summary>
    Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default);
}
```

## 3.2. Chiến lược thu thập và định lượng bằng chứng (Evidence Weighting Strategy)

Một Plugin chuyên nghiệp không đoán mò bản quyền mà phải dựa trên điểm số trọng lượng của các bằng chứng thực tế:

| Loại bằng chứng (Evidence Type) | Nguồn thu thập (Source) | Trọng số (Weight Points) | Ý nghĩa kỹ thuật |
| :--- | :--- | :---: | :--- |
| **1. License File Header** | File `LICENSE`, `COPYING`, `.lic`, `.key` nằm trong `software.InstallLocation` (Đọc ≤ 2KB header) | **40 - 50 điểm** | Bằng chứng cao nhất, khẳng định chính xác loại giấy phép (GPL, MIT, Commercial Activation). |
| **2. Digital Signature / Executable Metadata** | Chữ ký số Authenticode của file `.exe` (`Vendor Name` khớp với Publisher thương mại) | **25 - 30 điểm** | Xác thực phần mềm thương mại chính chủ, chống giả mạo tên Registry. |
| **3. Registry / Package Key Specifics** | `ProductCode` hoặc `UpgradeCode` đặc thù của các bộ cài đặt doanh nghiệp (e.g., MS Office Volume License) | **15 - 20 điểm** | Bằng chứng định danh cấu hình cài đặt từ hệ thống. |
| **4. Publisher / Keyword Heuristic Match** | Tên `software.Publisher` hoặc `software.Name` khớp với danh sách hằng số định sẵn | **10 - 15 điểm** | Bằng chứng phụ (Heuristic), chỉ dùng để hỗ trợ khi thiếu file license. |

## 3.3. Quy tắc ánh xạ điểm số sang `ConfidenceLevel`

```csharp
// Tổng điểm Evidence = Sum(Weight Points)
// >= 70 điểm -> ConfidenceLevel.Verified (4 - Xác thực hoàn toàn bởi License Key/Signature rõ ràng)
// 50 - 69 điểm -> ConfidenceLevel.High (3 - Độ tin cậy cao, có file license hoặc chữ ký chính chủ)
// 30 - 49 điểm -> ConfidenceLevel.Medium (2 - Độ tin cậy trung bình, khớp pattern hệ sinh thái)
// 10 - 29 điểm -> ConfidenceLevel.Low (1 - Khớp từ khóa heuristic cơ bản)
// < 10 điểm  -> ConfidenceLevel.None (0 - Không đủ dữ liệu)
```

## 3.4. Mã nguồn mẫu chuẩn cho một Custom License Plugin (`SteamGameClientPlugin`)

Dưới đây là production-ready template cho một Plugin mới:

```csharp
using Microsoft.Extensions.Logging;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Plugin phát hiện và phân tích bản quyền cho nền tảng và game thuộc hệ sinh thái Steam (Valve Corporation).
/// </summary>
public sealed class SteamGameClientPlugin : ILicensePlugin
{
    private readonly ILogger<SteamGameClientPlugin> _logger;
    private static readonly HashSet<string> TargetPublishers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Valve",
        "Valve Corporation",
        "Valve L.L.C."
    };

    public SteamGameClientPlugin(ILogger<SteamGameClientPlugin> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public PluginManifest Manifest => new PluginManifest(
        PluginId: "LIP-PLG-STEAM-001",
        PluginName: "Steam Gaming Ecosystem & Client Plugin",
        PluginVersion: "1.0.0",
        Author: "LIP Core Team",
        Description: "Phát hiện và xác định bản quyền nền tảng Steam Client và các ứng dụng Valve Ecosystem.",
        Priority: PluginPriority.Normal, // Priority 70-80 cho Ecosystem
        MinSdkVersion: "1.0.0",
        MaxSdkVersion: "",
        SupportedOs: "Windows, Linux"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;

        // Kiểm tra nhanh trong RAM không tốn I/O
        return TargetPublishers.Contains(software.Publisher ?? string.Empty) ||
               (software.Name != null && software.Name.Contains("Steam", StringComparison.OrdinalIgnoreCase)) ||
               (software.InstallLocation != null && software.InstallLocation.Contains("Steam", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        int totalScore = 0;

        try
        {
            // 1. Evidence: Publisher Heuristic Match (+15 điểm)
            if (TargetPublishers.Contains(software.Publisher ?? string.Empty))
            {
                evidences.Add(new Evidence(
                    EvidenceType: "PublisherMatch",
                    Description: $"Publisher identified as official Valve Corporation ({software.Publisher}).",
                    SourceLocation: "Registry/Package Metadata",
                    RawData: software.Publisher ?? string.Empty
                ));
                totalScore += 15;
            }

            // 2. Evidence: File System & Binary Signature Verification (+35 điểm)
            if (!string.IsNullOrWhiteSpace(software.InstallLocation) && Directory.Exists(software.InstallLocation))
            {
                string steamExe = Path.Combine(software.InstallLocation, "steam.exe");
                if (File.Exists(steamExe))
                {
                    // Kiểm tra sự tồn tại của binary chính (+20 điểm)
                    evidences.Add(new Evidence(
                        EvidenceType: "ExecutableVerification",
                        Description: "Core executable steam.exe detected in install directory.",
                        SourceLocation: steamExe,
                        RawData: $"FileExists: True, Path: {steamExe}"
                    ));
                    totalScore += 20;

                    // Kiểm tra file License/Terms nếu có (Đọc <= 2KB an toàn) (+15 điểm)
                    string licenseFile = Path.Combine(software.InstallLocation, "steam_subscriber_agreement.txt");
                    if (File.Exists(licenseFile))
                    {
                        char[] buffer = new char[2048];
                        using (var reader = new StreamReader(File.OpenRead(licenseFile)))
                        {
                            await reader.ReadAsync(buffer, 0, buffer.Length);
                        }
                        string headerText = new string(buffer);

                        evidences.Add(new Evidence(
                            EvidenceType: "LicenseHeaderMatch",
                            Description: "Steam Subscriber Agreement (EULA) header detected.",
                            SourceLocation: licenseFile,
                            RawData: headerText.Substring(0, Math.Min(200, headerText.Length))
                        ));
                        totalScore += 15;
                    }
                }
            }

            // 3. Quyết định loại bản quyền và mức độ tự tin (Confidence calculation)
            LicenseType licenseType = LicenseType.ProprietaryCommercial;
            ConfidenceLevel confidence = totalScore switch
            {
                >= 50 => ConfidenceLevel.Verified,
                >= 35 => ConfidenceLevel.High,
                >= 15 => ConfidenceLevel.Medium,
                _ => ConfidenceLevel.Low
            };

            return new LicenseCheckResult(
                PluginId: Manifest.PluginId,
                PluginName: Manifest.PluginName,
                Software: software,
                DetectedLicenseType: licenseType,
                LicenseName: "Steam Subscriber Agreement (Commercial / Proprietary Client)",
                Confidence: confidence,
                Evidences: evidences,
                IsVerified: confidence == ConfidenceLevel.Verified,
                Notes: $"Steam ecosystem verified with total evidence weight of {totalScore} points.",
                ScannedAtUtc: DateTime.UtcNow
            );
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("SteamGameClientPlugin execution canceled/timed out for {SoftwareName}.", software.Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SteamGameClientPlugin while analyzing {SoftwareName}.", software.Name);
            return LicenseCheckResult.CreateErrorResult(Manifest.PluginId, Manifest.PluginName, software, ex.Message);
        }
    }
}
```

---

# 4. Core Application Engine & Pipeline Execution

`CoreEngine` (hoặc `ScanCoordinator`) là bộ điều phối trung tâm thuộc tầng `Application`. Nó nhận vào các `IScanner` và `ILicensePlugin` từ hệ thống Dependency Injection và chạy tuần tự theo luồng chuẩn hóa.

## 4.1. Luồng thực thi Pipeline (`ExecuteFullScanAsync`)

```
1. Scanner Phase: Gọi tuần tự tất cả IScanner.ScanAsync() -> Thu thập danh sách thô (Raw Software List).
2. Deduplication & Merge Phase: Truyền danh sách thô vào SoftwareMergeEngine.Merge() -> Danh sách duy nhất (Merged Software List).
3. Plugin Discovery & Filter Phase: Kiểm tra PluginCompatibilityValidator -> Lọc ra danh sách Plugin tương thích SDK.
4. License Analysis Phase: Duyệt qua từng Software -> Gọi các Plugin hợp lệ (được bọc trong Timeout & Try-Catch Sandbox).
5. Rule & Confidence Resolution Phase: Truyền danh sách kết quả của các Plugin vào RuleEngine và ConfidenceEngine -> Ra kết quả LicenseCheckResult duy nhất cho mỗi Software.
6. Report Assembly Phase: Tổng hợp thành ScanReport hoàn chỉnh.
```

## 4.2. Error Isolation Boundary (Quy tắc số 9 - Plugin Fault Resilience)

Mục tiêu số 1 của `CoreEngine`: **Không bao giờ bị sập (Crash) khi một hoặc nhiều Plugin bị lỗi I/O, lặp vô hạn hay Out-of-Memory.**

Dưới đây là mã nguồn chuẩn trong `CoreEngine` khi điều phối vòng lặp kiểm tra bản quyền:

```csharp
public async Task<ScanReport> ExecuteFullScanAsync(ScanOptions options, CancellationToken cancellationToken = default)
{
    _logger.LogInformation("Starting full system scan pipeline...");

    // 1. Chạy tất cả Scanners
    var rawSoftwareList = new List<SoftwareInfo>();
    foreach (var scanner in _scanners)
    {
        if (scanner.IsSupportedOnCurrentPlatform())
        {
            try
            {
                var scannedItems = await scanner.ScanAsync(cancellationToken);
                rawSoftwareList.AddRange(scannedItems);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Scanner {ScannerName} failed. Continuing with remaining scanners.", scanner.ScannerName);
            }
        }
    }

    // 2. Gộp phần mềm trùng lặp (Merge Engine)
    var mergedSoftwareList = _mergeEngine.Merge(rawSoftwareList);
    _logger.LogInformation("Discovered {RawCount} raw items, merged into {MergedCount} unique software entries.", 
        rawSoftwareList.Count, mergedSoftwareList.Count);

    // 3. Phân tích bản quyền từng phần mềm qua Plugin Engine
    var finalResults = new List<LicenseCheckResult>();

    foreach (var software in mergedSoftwareList)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var candidateResults = new List<LicenseCheckResult>();

        foreach (var plugin in _plugins)
        {
            // Kiểm tra nhanh CanCheck
            if (!plugin.CanCheck(software)) continue;

            // Bọc Sandbox Timeout cho từng Plugin (Tối đa 5000ms mỗi Plugin)
            using var pluginCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            pluginCts.CancelAfter(TimeSpan.FromMilliseconds(5000));

            try
            {
                var result = await plugin.CheckLicenseAsync(software, pluginCts.Token);
                if (result != null)
                {
                    candidateResults.Add(result);
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Chỉ Plugin bị Timeout, toàn bộ Scan chính vẫn tiếp tục
                _logger.LogWarning("Plugin {PluginName} timed out (5000ms guard) on software {SoftwareName}.", 
                    plugin.Manifest.PluginName, software.Name);
            }
            catch (Exception ex)
            {
                // Cô lập hoàn toàn Exception, ghi log nhưng tiếp tục chạy
                _logger.LogError(ex, "Plugin {PluginName} faulted while checking {SoftwareName}. Fault isolated.", 
                    plugin.Manifest.PluginName, software.Name);
            }
        }

        // 4. Quyết định kết quả cuối cùng qua Rule Engine
        var resolvedResult = _ruleEngine.ResolveResult(software, candidateResults);
        finalResults.Add(resolvedResult);
    }

    // 5. Trả về báo cáo hoàn chỉnh
    return new ScanReport(
        ScanId: Guid.NewGuid(),
        ScanTimeUtc: DateTime.UtcNow,
        MachineName: Environment.MachineName,
        OperatingSystem: Environment.OSVersion.ToString(),
        TotalDiscoveredSoftware: mergedSoftwareList.Count,
        Results: finalResults
    );
}
```

---

# 5. Data Merge Engine & Software Deduplication

Hệ điều hành Windows thường lưu bản ghi của cùng một phần mềm ở cả `HKLM\...\Uninstall` (64-bit), `HKLM\...\WOW6432Node\...\Uninstall` (32-bit), và `HKCU\...\Uninstall` (User level). Khi quét, `WindowsRegistryScanner` có thể thu được 2-3 bản ghi `SoftwareInfo` cho cùng một ứng dụng.

`SoftwareMergeEngine` có nhiệm vụ gộp dữ liệu lại và chuẩn hóa thông tin nhà phát hành (`Publisher Sanitization`).

## 5.1. Thuật toán nhận diện trùng lặp (`Deduplication Key`)

Một cặp phần mềm được coi là **Trùng lặp (Duplicate)** nếu thỏa mãn ít nhất một trong 3 điều kiện sau:
1. `ProductCode` (chuỗi GUID cài đặt của MSI) khớp nhau 100% không phân biệt hoa thường.
2. `Name` và `Version` khớp nhau 100% (sau khi đã loại bỏ khoảng trắng và ký tự đặc biệt ở hai đầu).
3. `InstallLocation` khớp nhau 100% (và không phải là thư mục gốc `C:\Program Files` hay `C:\Windows`).

## 5.2. Mã nguồn mẫu `SoftwareMergeEngine`

```csharp
namespace LicenseIntelligencePlatform.Application.Services;

public sealed class SoftwareMergeEngine
{
    public IReadOnlyList<SoftwareInfo> Merge(IEnumerable<SoftwareInfo> rawSoftwareList)
    {
        var mergedMap = new Dictionary<string, SoftwareInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in rawSoftwareList)
        {
            if (string.IsNullOrWhiteSpace(item.Name)) continue;

            // Tạo Deduplication Key chuẩn hóa
            string key = !string.IsNullOrWhiteSpace(item.ProductCode)
                ? item.ProductCode.Trim()
                : $"{Sanitize(item.Name)}_{Sanitize(item.Version)}";

            if (!mergedMap.TryGetValue(key, out var existing))
            {
                mergedMap[key] = SanitizePublisher(item);
            }
            else
            {
                // Nếu đã tồn tại, gộp thông tin tốt hơn (Ví dụ: ưu tiên bản ghi có InstallLocation và ExecutablePath đầy đủ hơn)
                mergedMap[key] = MergeTwoEntries(existing, item);
            }
        }

        return mergedMap.Values.ToList();
    }

    private static SoftwareInfo MergeTwoEntries(SoftwareInfo existing, SoftwareInfo newItem)
    {
        return existing with
        {
            InstallLocation = !string.IsNullOrWhiteSpace(existing.InstallLocation) ? existing.InstallLocation : newItem.InstallLocation,
            ExecutablePath = !string.IsNullOrWhiteSpace(existing.ExecutablePath) ? existing.ExecutablePath : newItem.ExecutablePath,
            Publisher = !string.IsNullOrWhiteSpace(existing.Publisher) ? existing.Publisher : newItem.Publisher
        };
    }

    private static SoftwareInfo SanitizePublisher(SoftwareInfo item)
    {
        if (string.IsNullOrWhiteSpace(item.Publisher)) return item;

        string sanitized = item.Publisher.Trim()
            .Replace("Inc.", "Inc", StringComparison.OrdinalIgnoreCase)
            .Replace("L.L.C.", "LLC", StringComparison.OrdinalIgnoreCase)
            .Replace("Corporation", "Corp", StringComparison.OrdinalIgnoreCase);

        return item with { Publisher = sanitized };
    }

    private static string Sanitize(string? input) => 
        string.IsNullOrWhiteSpace(input) ? string.Empty : input.Trim().ToLowerInvariant();
}
```

---

# 6. Exporters & Report Mapping Architecture (`IReportMapper`)

Khi người dùng chỉ định định dạng đầu ra qua cờ `--format csv,json,html` trong CLI, tầng `Infrastructure.Exporters` sẽ sử dụng `IReportMapper` tương ứng để biến đổi `ScanReport` thành file đích trong thư mục `--output`.

## 6.1. Hướng dẫn thêm một Exporter mới (`AuditMarkdownReportMapper`)

Để bổ sung một định dạng báo cáo mới mà không làm thay đổi kiến trúc hiện tại:

1. **Tạo class implementation trong `LicenseIntelligencePlatform.Infrastructure.Exporters`**:

```csharp
using System.Text;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Infrastructure.Exporters;

public sealed class AuditMarkdownReportMapper : IReportMapper
{
    public string FormatName => "MARKDOWN";

    public async Task ExportAsync(ScanReport report, string outputDirectory, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDirectory);
        string filePath = Path.Combine(outputDirectory, $"audit_report_{report.ScanId:N}.md");

        var sb = new StringBuilder();
        sb.AppendLine($"# Executive License Audit Report");
        sb.AppendLine($"**Scan ID:** `{report.ScanId}`  |  **Scan Date (UTC):** `{report.ScanTimeUtc:yyyy-MM-dd HH:mm:ss}`");
        sb.AppendLine($"**Machine Name:** `{report.MachineName}`  |  **OS:** `{report.OperatingSystem}`");
        sb.AppendLine($"**Total Discovered Software:** {report.TotalDiscoveredSoftware}");
        sb.AppendLine();
        sb.AppendLine("| Software Name | Version | Detected License Type | Confidence | Plugin Identified | Is Verified |");
        sb.AppendLine("| :--- | :--- | :--- | :---: | :--- | :---: |");

        foreach (var result in report.Results)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sb.AppendLine($"| **{result.Software.Name}** | `{result.Software.Version}` | `{result.DetectedLicenseType}` | **{result.Confidence}** | {result.PluginName} | {(result.IsVerified ? "✅ Yes" : "❌ No")} |");
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8, cancellationToken);
    }
}
```

2. **Đăng ký vào Dependency Injection Container (`Program.cs`)**:

```csharp
// Đăng ký danh sách các IReportMapper trong container
services.AddSingleton<IReportMapper, JsonReportMapper>();
services.AddSingleton<IReportMapper, CsvReportMapper>();
services.AddSingleton<IReportMapper, AuditMarkdownReportMapper>();
```

---

# 7. Performance Optimization & Memory Management

Để đáp ứng chỉ tiêu **Benchmark Target (`< 500ms` cho ~150 software packages trên ~30+ plugins)**, mọi dòng code trong Scanners, Engine và Plugins đều phải tuân thủ nguyên tắc tối ưu hóa bộ nhớ:

1. **Sử dụng `StringComparer.OrdinalIgnoreCase` trong tất cả Map / Set**:
   - Mặc định, so sánh chuỗi trong C# có thể kích hoạt văn hóa (`Culture-Sensitive Comparison`), gây tốn tài nguyên CPU nặng nề. Luôn truyền `StringComparer.OrdinalIgnoreCase` khi khởi tạo `Dictionary<string, ...>` hay `HashSet<string>`.
2. **Hạn chế cấp phát chuỗi tạm (String Allocation Reduction)**:
   - Trong các vòng lặp xử lý hàng nghìn path hay string, sử dụng `StringBuilder` hoặc `ReadOnlySpan<char>` khi bóc tách (`Slice`) version và file name thay vì gọi `string.Substring()` hay `string.Split()` liên tục.
3. **Quản lý luồng bất đồng bộ (`Async Discipline`)**:
   - Luôn sử dụng `await ... ConfigureAwait(false)` trong các thư viện tầng `Infrastructure` và `Plugins.Standard` để tránh khóa luồng UI / Context Switch không cần thiết.
   - Khi đọc header file, chỉ khởi tạo `char[] buffer = new char[2048]` và dùng `StreamReader.ReadAsync()`, không bao giờ dùng `File.ReadAllText()` đối với các file không kiểm soát kích thước.

---

# Summary of Implementation Disciplines

Tóm lại, để mã nguồn của **License Intelligence Platform (LIP)** duy trì đẳng cấp doanh nghiệp, lập trình viên phải ghi nhớ 4 quy tắc vàng:
1. **Clean Architecture 5 Tầng:** Phụ thuộc một chiều hướng vào trong (`Domain`).
2. **Scanner 3 KHÔNG:** Không ghi, không crash, không mạng.
3. **Plugin Error Sandbox:** Luôn bọc trong Timeout CancellationToken 5000ms và `try/catch`.
4. **Resilient Memory Disciplines:** Tối ưu hóa allocations, bám sát mức hiệu năng dưới `< 500ms`.
