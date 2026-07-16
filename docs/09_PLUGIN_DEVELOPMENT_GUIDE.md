# 09_PLUGIN_DEVELOPMENT_GUIDE.md

# License Intelligence Platform (LIP)

## Plugin Development Guide & SDK Architecture

Version: 1.0

Status: Stable

Author: DynamiteV

---

# Purpose & Target Audience

Tài liệu **Plugin Development Guide & SDK Architecture** là bộ hướng dẫn chính thức và chi tiết nhất dành cho các kỹ sư phát triển Plugin (`Plugin Developers`), đối tác tích hợp hệ thống (`System Integrators`), và các **AI Coding Assistants** khi xây dựng hoặc mở rộng hệ sinh thái nhận diện bản quyền cho **License Intelligence Platform (LIP)**.

Tuân thủ hướng dẫn này đảm bảo rằng:
- Mọi Plugin được phát triển mới hoàn toàn tương thích với **SDK v1.0** (`PluginManifest`, `ILicensePlugin`, `Evidence`).
- Đạt hiệu năng quét siêu tốc dưới **`< 1 ms`** trong bước lọc trước (`CanCheck`).
- Hoạt động an toàn tuyệt đối, tuân thủ nguyên tắc cách ly rủi ro (**Rule 9 Error Isolation Sandbox**) và không bao giờ gây rò rỉ bộ nhớ hay sập tiến trình Core Engine.

---

# 1. Plugin Architecture Overview & Vòng đời 5 bước (`Lifecycle`)

Hệ thống LIP phân tách hoàn toàn giữa **Cơ chế thu thập dữ liệu thô (Scanners)** và **Khối lập luận bản quyền (Plugins)**:
- `Scanners`: Chỉ đọc Registry, File System, Package Manager và trả về `SoftwareInfo` thô.
- `Plugins`: Không bao giờ tự quét hệ điều hành từ con số 0. Plugin nhận đầu vào là `SoftwareInfo`, kiểm tra dấu vết bản quyền trong `software.InstallLocation` hoặc Registry đặc thù, và trả về `LicenseCheckResult`.

```
                  ┌──► PluginDiscovery (IPluginLoader / AssemblyLoadContext)
                  ├──► Manifest Validation (PluginCompatibilityValidator - Check MinSdkVersion)
SoftwareInfo ─────┼──► CanCheck Evaluation (< 1ms RAM Check: Publisher / Keyword Match)
                  ├──► CheckLicenseAsync Execution (Timeout Guard 5000ms + Try-Catch Sandbox)
                  └──► Result Resolution (Evidence Weighting & Confidence Score Allocation)
```

---

# 2. Plugin SDK Specifications & Core Contracts

## 2.1. Khai báo Siêu dữ liệu (`PluginManifest`)
Từ Phase 2 trở đi, việc khai báo `PluginId` dạng chuỗi đơn lẻ bị thay thế hoàn toàn bởi `PluginManifest`. Thuộc tính `Manifest` cung cấp cho `CoreEngine` các thông số quan trọng để sắp xếp thứ tự thực thi và xác minh tương thích SDK:

```csharp
namespace LicenseIntelligencePlatform.Domain.Entities;

public sealed record PluginManifest(
    string PluginId,          // Quy ước: "LIP-PLG-<VENDOR>-<ID>" (Ví dụ: "LIP-PLG-ADOBE-001")
    string PluginName,        // Tên hiển thị rõ ràng cho Audit Report
    string PluginVersion,     // Semantic Versioning (e.g., "1.0.0")
    string Author,            // Tên cá nhân hoặc tổ chức (e.g., "LIP Core Team")
    string Description,       // Mô tả khả năng phát hiện của Plugin
    PluginPriority Priority,  // CommercialSpecific (100), Ecosystem (75), Heuristic (50), Generic (25)
    string MinSdkVersion,     // Phiên bản SDK tối thiểu tương thích (e.g., "1.0.0")
    string MaxSdkVersion,     // Phiên bản SDK tối đa tương thích (rỗng nếu tương thích tới tương lai)
    string SupportedOs        // "Windows", "Linux", hoặc "Any"
);
```

## 2.2. Giao diện `ILicensePlugin`
```csharp
namespace LicenseIntelligencePlatform.Domain.Interfaces;

public interface ILicensePlugin
{
    /// <summary>
    /// Metadata mô tả Plugin và chỉ định độ ưu tiên thực thi.
    /// </summary>
    PluginManifest Manifest { get; }

    /// <summary>
    /// Kiểm tra nhanh trong RAM xem Plugin có áp dụng cho phần mềm này hay không.
    /// Phải hoàn thành dưới < 1ms, không làm I/O đĩa hay network.
    /// </summary>
    bool CanCheck(SoftwareInfo software);

    /// <summary>
    /// Phân tích chuyên sâu bản quyền phần mềm, thu thập bằng chứng và trả về kết quả.
    /// </summary>
    Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default);
}
```

---

# 3. Evidence Weighting Strategy & Confidence Scoring

Một Plugin tiêu chuẩn không phán đoán tùy tiện mà phải xây dựng lập luận bản quyền từ điểm số của các bằng chứng thực tế:

## 3.1. Bảng Trọng số Bằng chứng (`Evidence Weight Allocation`)

| Loại Bằng chứng (`EvidenceType`) | Mô tả hành vi trích xuất | Trọng số (`Score`) | Ý nghĩa và Độ tin cậy |
| :--- | :--- | :---: | :--- |
| **`LicenseFileHeader`** | Đọc $\le 2\text{KB}$ đầu file `LICENSE`, `COPYING`, `.lic`, `.key` trong `software.InstallLocation`. | **40 - 50 pts** | Bằng chứng cao nhất, khẳng định chính xác loại giấy phép thương mại hay mã nguồn mở. |
| **`AuthenticodeSignature`** | Kiểm tra chữ ký số của file thực thi `.exe` (`Vendor Signature`). | **25 - 30 pts** | Xác nhận phần mềm chính chủ từ nhà sản xuất, chống giả mạo tên file. |
| **`RegistryProductCode`** | Khớp `ProductCode` hoặc `UpgradeCode` đặc thù của bộ cài đặt doanh nghiệp (e.g., Office Volume License). | **15 - 20 pts** | Xác thực định danh bản dựng trên hệ thống. |
| **`PublisherHeuristicMatch`** | Tên `software.Publisher` hoặc `software.Name` khớp với danh sách `HashSet<string>` đã khai báo. | **10 - 15 pts** | Bằng chứng phụ (`Heuristic`), dùng để hỗ trợ khi thiếu file license rõ ràng. |

## 3.2. Quy tắc chuyển đổi Tổng điểm sang `ConfidenceLevel`
```csharp
// Tổng điểm Score = Sum(Evidences.WeightScore)
ConfidenceLevel level = totalScore switch
{
    >= 70 => ConfidenceLevel.Verified, // Xác thực hoàn toàn (có file license/signature thương mại)
    >= 50 => ConfidenceLevel.High,     // Độ tin cậy cao
    >= 30 => ConfidenceLevel.Medium,   // Nhận diện theo pattern hệ sinh thái
    >= 10 => ConfidenceLevel.Low,      // Nhận diện theo từ khóa Heuristic
    _     => ConfidenceLevel.None      // Không đủ dữ liệu phán đoán
};
```

---

# 4. Step-by-Step Tutorial: Writing a Production-Ready Plugin

Dưới đây là production template chuẩn cho lập trình viên muốn phát triển một Plugin mới (Ví dụ: `AutoCadCommercialPlugin` phát hiện bản quyền cho hệ sinh thái CAD của Autodesk):

```csharp
using System.Text;
using Microsoft.Extensions.Logging;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

/// <summary>
/// Plugin nhận diện bản quyền thương mại cho phần mềm Autodesk AutoCAD và các công cụ kỹ thuật CAD.
/// Tuân thủ nghiêm ngặt nguyên tắc Read-Only và Sandboxed Error Isolation.
/// </summary>
public sealed class AutoCadCommercialPlugin : ILicensePlugin
{
    private readonly ILogger<AutoCadCommercialPlugin> _logger;
    
    // Khai báo HashSet với OrdinalIgnoreCase ở đầu file để tối ưu bộ nhớ và tốc độ < 1ms
    private static readonly HashSet<string> AutodeskPublishers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Autodesk",
        "Autodesk, Inc.",
        "Autodesk Inc."
    };

    public AutoCadCommercialPlugin(ILogger<AutoCadCommercialPlugin> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public PluginManifest Manifest => new PluginManifest(
        PluginId: "LIP-PLG-AUTODESK-001",
        PluginName: "Autodesk AutoCAD Commercial License Detector",
        PluginVersion: "1.0.0",
        Author: "LIP Core Team",
        Description: "Phát hiện và xác định bản quyền các ứng dụng AutoCAD và CAD Engineering Suite.",
        Priority: PluginPriority.CommercialSpecific, // Priority cao nhất 100 cho bộ ứng dụng thương mại nặng
        MinSdkVersion: "1.0.0",
        MaxSdkVersion: "",
        SupportedOs: "Windows"
    );

    public bool CanCheck(SoftwareInfo software)
    {
        if (software == null) return false;

        // Lọc nhanh trong RAM < 1ms
        return AutodeskPublishers.Contains(software.Publisher ?? string.Empty) ||
               (software.Name != null && software.Name.Contains("AutoCAD", StringComparison.OrdinalIgnoreCase)) ||
               (software.Name != null && software.Name.Contains("Autodesk", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        int totalScore = 0;

        try
        {
            // 1. Evidence: Publisher Match (+15 điểm)
            if (AutodeskPublishers.Contains(software.Publisher ?? string.Empty))
            {
                evidences.Add(new Evidence(
                    EvidenceType: "PublisherHeuristicMatch",
                    Description: $"Publisher verified as official Autodesk entity ({software.Publisher}).",
                    SourceLocation: "SoftwareInfo.Publisher",
                    RawData: software.Publisher ?? string.Empty
                ));
                totalScore += 15;
            }

            // 2. Evidence: Kiểm tra file license hay AdskLicensing service trong InstallLocation (+45 điểm)
            if (!string.IsNullOrWhiteSpace(software.InstallLocation) && Directory.Exists(software.InstallLocation))
            {
                cancellationToken.ThrowIfCancellationRequested();

                string licPath = Path.Combine(software.InstallLocation, "AdskLicensing", "AdskLicensingAgent.exe");
                if (File.Exists(licPath))
                {
                    evidences.Add(new Evidence(
                        EvidenceType: "AuthenticodeSignature",
                        Description: "Autodesk Licensing Agent executable detected.",
                        SourceLocation: licPath,
                        RawData: $"Agent Exists: {licPath}"
                    ));
                    totalScore += 25;
                }

                // Kiểm tra file giấy phép (.lic / .data) an toàn
                var licFiles = Directory.GetFiles(software.InstallLocation, "*.lic", SearchOption.TopDirectoryOnly);
                if (licFiles.Length > 0)
                {
                    string licFile = licFiles[0];
                    char[] buffer = new char[1024];
                    using (var reader = new StreamReader(File.OpenRead(licFile), Encoding.UTF8))
                    {
                        await reader.ReadAsync(buffer, 0, buffer.Length);
                    }
                    string header = new string(buffer);

                    evidences.Add(new Evidence(
                        EvidenceType: "LicenseFileHeader",
                        Description: $"Commercial license file detected: {Path.GetFileName(licFile)}.",
                        SourceLocation: licFile,
                        RawData: header.Substring(0, Math.Min(150, header.Length))
                    ));
                    totalScore += 30;
                }
            }

            // 3. Quyết định loại bản quyền
            ConfidenceLevel confidence = totalScore switch
            {
                >= 70 => ConfidenceLevel.Verified,
                >= 50 => ConfidenceLevel.High,
                >= 30 => ConfidenceLevel.Medium,
                >= 10 => ConfidenceLevel.Low,
                _     => ConfidenceLevel.None
            };

            return new LicenseCheckResult(
                PluginId: Manifest.PluginId,
                PluginName: Manifest.PluginName,
                Software: software,
                DetectedLicenseType: LicenseType.ProprietaryCommercial,
                LicenseName: "Autodesk Commercial / Engineering Subscription License",
                Confidence: confidence,
                Evidences: evidences,
                IsVerified: confidence == ConfidenceLevel.Verified,
                Notes: $"Autodesk license status resolved with total evidence weight of {totalScore} points.",
                ScannedAtUtc: DateTime.UtcNow
            );
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("AutoCadCommercialPlugin check cancelled/timed out for {SoftwareName}.", software.Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception inside AutoCadCommercialPlugin for {SoftwareName}.", software.Name);
            return LicenseCheckResult.CreateErrorResult(Manifest.PluginId, Manifest.PluginName, software, ex.Message);
        }
    }
}
```

---

# 5. Dynamic Loading & Plugin Compatibility Validation

## 5.1. Cơ chế `PluginLoaderService` & `AssemblyLoadContext`
Để nạp Plugin động từ thư mục bên ngoài (`--plugins <folder>`) mà không làm kẹt file trên đĩa hay xung đột assembly, LIP sử dụng `AssemblyLoadContext`:
- Mỗi file DLL plugin trong thư mục `--plugins` được kiểm tra chữ ký số hoặc nạp vào một `AssemblyLoadContext` riêng biệt.
- Nếu Plugin không hợp lệ hoặc chứa mã độc vi phạm Sandboxing, context có thể được `Unload()` ra khỏi bộ nhớ RAM mà không làm ảnh hưởng tới Core Engine.

## 5.2. `PluginCompatibilityValidator`
Trước khi nạp Plugin vào `CoreEngine`, `PluginCompatibilityValidator` thực hiện chẩn đoán:
- So sánh `Manifest.MinSdkVersion` với `CoreEngine.CurrentSdkVersion` (`"1.0.0"`).
- Nếu `MinSdkVersion` lớn hơn SDK hiện tại, Plugin lập tức bị từ chối và ghi nhận cảnh báo `LogWarning`: *"Plugin X skipped: Requires higher SDK version"*.

---

# 6. Testing & Quality Assurance Disciplines for Plugins

Trước khi phát hành một Plugin mới, lập trình viên bắt buộc phải chạy và xác minh 100% qua 3 bước kiểm thử:

1. **Unit Test với Mock Data:** Sử dụng `xUnit` và `Moq` để khởi tạo các đối tượng `SoftwareInfo` giả lập (`Fake Software Info`) và kiểm tra logic điểm số `CheckLicenseAsync`.
2. **Benchmark CanCheck Speed:** Xác nhận phương thức `CanCheck` chạy với tốc độ trung bình dưới `0.05 ms` trên `BenchmarkDotNet` khi truyền 1000 `SoftwareInfo` khác nhau.
3. **Resiliency & Fault Isolation Verification:** Cố tình truyền `software.InstallLocation` trỏ đến một thư mục bị khóa phân quyền (`Access Denied`) hoặc gây `OutOfMemoryException` giả lập bên trong `CheckLicenseAsync` để chứng minh rằng khối `try/catch` Sandbox chặn đứng ngoại lệ và trả về `CreateErrorResult` chính xác, không làm sập `dotnet test`.

---

# Summary of Plugin Developer Disciplines

1. **Luôn khai báo `PluginManifest` đầy đủ:** Chỉ rõ `PluginPriority`, `MinSdkVersion`, và `SupportedOs`.
2. **Tuân thủ 3 KHÔNG của Plugin:** Không sửa OS, Không gọi Network, Không bỏ qua `try/catch` khi làm I/O.
3. **Áp dụng Thang điểm Bằng chứng:** Đọc file license header $\le 2\text{KB}$, gán trọng số 40-50 pts để đạt `ConfidenceLevel.Verified`.
4. **Giữ `CanCheck` siêu tốc `< 1ms`:** Chỉ dùng `HashSet.Contains` với `StringComparer.OrdinalIgnoreCase`.
