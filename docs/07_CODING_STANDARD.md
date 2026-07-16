# 07_CODING_STANDARD.md

# License Intelligence Platform (LIP)

## Coding Standard & Conventions

Version: 1.0

Status: Stable

Author: DynamiteV

---

# Purpose

Tài liệu **Coding Standard & Conventions** (Tiêu chuẩn Viết mã & Quy ước) là bộ quy tắc chuẩn mực bắt buộc dành cho toàn bộ mã nguồn thuộc **License Intelligence Platform (LIP)**.

Mục tiêu cốt lõi:
- Đảm bảo toàn bộ codebase `.NET 8.0` / `C# 12` duy trì tính nhất quán cao độ, như thể được viết bởi một kỹ sư duy nhất.
- Tối ưu hóa hiệu năng, giảm thiểu cấp phát bộ nhớ dư thừa (**Zero-Allocation Mindset**).
- Ngăn ngừa các lỗi tiềm ẩn (NullReferenceException, Deadlocks, Race Conditions, Memory Leaks) ngay từ khâu viết code.
- Định hình các ranh giới và quy tắc bắt buộc khi làm việc cùng các **AI Coding Assistants** (Antigravity IDE, GitHub Copilot, Cursor...) để ngăn chặn rủi ro làm thoái hóa kiến trúc (`Architectural Erosion`).

---

# 1. Exhaustive Naming Conventions & Lexical Rules

Sự rõ ràng và nhất quán trong cách đặt tên là yếu tố tiên quyết để đảm bảo khả năng đọc hiểu (`Readability`) và tự giải thích (`Self-Documenting Code`) của dự án.

| Thành phần mã nguồn | Quy tắc đặt tên (Convention) | Ví dụ chuẩn mực (Good) | Ví dụ vi phạm (Bad) | Giải thích lý do kỹ thuật |
| :--- | :--- | :--- | :--- | :--- |
| **Solution / Project Assembly** | `PascalCase` với phân cấp dấu chấm (`.`) phản ánh đúng 5 tầng kiến trúc | `LicenseIntelligencePlatform.Plugins.Standard` | `LIP.plugins.standard`, `LipPlugins` | Giúp trình biên dịch và công cụ kiểm thử tự động nhận diện chính xác ranh giới module. |
| **Namespace** | `PascalCase`, phải khớp chính xác với đường dẫn thư mục vật lý từ gốc Project | `LicenseIntelligencePlatform.Infrastructure.Scanners` | `LicenseIntelligencePlatform.scanners` | Đảm bảo tính ngăn nắp và tránh xung đột type khi nạp Assembly qua Reflection. |
| **Class / Record / Struct** | `PascalCase`, sử dụng danh từ hoặc cụm danh từ cụ thể | `WindowsRegistryScanner`, `PluginManifest` | `windowsRegistryScanner`, `Registry_Scanner` | Tuân thủ tiêu chuẩn thiết kế hướng đối tượng chuẩn của Microsoft .NET. |
| **Interface** | `PascalCase` bắt buộc có tiền tố chữ `I` viết hoa | `ILicensePlugin`, `IScanner`, `IReportMapper` | `LicensePluginInterface`, `Scanner` | Phân biệt rõ ràng giữa Contract (Hợp đồng) và Concrete Implementation. |
| **Public / Protected Method** | `PascalCase`, sử dụng động từ thể hiện rõ hành động/trách nhiệm | `ExecuteFullScanAsync`, `CanCheck`, `Sanitize` | `executeScan`, `doCheck`, `Scan_Data` | Giúp code đọc trôi chảy như ngôn ngữ tự nhiên: `plugin.CanCheck(software)`. |
| **Async Method** | Bắt buộc có hậu tố `Async` nếu trả về `Task` / `ValueTask` / `IAsyncEnumerable` | `ScanAsync`, `ExportAsync`, `ResolveAsync` | `Scan`, `ExportData`, `GetLicense` | Cảnh báo rõ ràng cho người gọi rằng phương thức cần từ khóa `await` để tránh `Task` bị bỏ quên. |
| **Public Property / Indexer** | `PascalCase` | `InstallPath`, `PluginVersion`, `Evidences` | `installPath`, `plugin_version`, `m_evidences` | Thể hiện thuộc tính công khai của đối tượng theo chuẩn C#. |
| **Private Field (Readonly / Mutable)** | `_camelCase` (luôn bắt đầu bằng dấu gạch dưới `_`) | `_logger`, `_scanners`, `_mergeEngine` | `logger`, `m_logger`, `Logger`, `_Logger` | Giúp phân biệt ngay lập tức biến thành viên private (`_logger`) với tham số cục bộ (`logger`) bên trong method mà không cần dùng `this.logger`. |
| **Constant (`const`) / Static ReadOnly** | `PascalCase` (cho `static readonly`) hoặc `PascalCase` / `UPPER_CASE` (cho `const` nội bộ) | `MinSdkVersion`, `DefaultTimeoutMs` | `min_sdk_version`, `default_timeout` | Định danh rõ ràng các giá trị bất biến, cấu hình tĩnh hoặc ngưỡng giới hạn của hệ thống. |
| **Local Variable & Parameter** | `camelCase` | `discoveredPackages`, `scanReport`, `options` | `DiscoveredPackages`, `sr`, `opt` | Chuẩn bị cú pháp gọn gàng trong các khối xử lý logic nội bộ method. |

---

# 2. Modern C# 12 & .NET 8.0 Idioms

LIP tận dụng tối đa sức mạnh và sự an toàn của ngôn ngữ **C# 12** trên nền tảng **.NET 8.0 LTS**. Toàn bộ codebase bắt buộc áp dụng các mô hình lập trình hiện đại sau:

## 2.1. File-Scoped Namespaces
Bắt buộc sử dụng `File-Scoped Namespaces` trong toàn bộ tệp `.cs` để tiết kiệm 1 mức thụt lề (indentation), giúp code rõ ràng hơn:

```csharp
// ✅ CHUẨN MỰC: File-Scoped Namespace (Không có ngoặc nhọn {})
namespace LicenseIntelligencePlatform.Domain.Entities;

public sealed record SoftwareInfo(string Name, string Version);
```

```csharp
// ❌ VI PHẠM: Block-Scoped Namespace cũ (Gây thụt lề thừa)
namespace LicenseIntelligencePlatform.Domain.Entities
{
    public sealed record SoftwareInfo(string Name, string Version);
}
```

## 2.2. Immutable Records cho Domain Entities & DTOs
Sử dụng `record` (thay vì `class` thông thường) cho tất cả các Entities, DTOs, và Value Objects trong tầng `Domain` nhằm đảm bảo tính bất biến (`Immutability`) và so sánh theo giá trị (`Value-Equality`):

```csharp
namespace LicenseIntelligencePlatform.Domain.Entities;

/// <summary>
/// Đối tượng đại diện cho một bằng chứng bản quyền thu thập từ hệ thống.
/// </summary>
public sealed record Evidence(
    string EvidenceType,
    string Description,
    string SourceLocation,
    string RawData
);
```

## 2.3. Nullable Reference Types (NRT) Rigor
Dự án bật chế độ kiểm tra Null khắt khe trong toàn bộ các tệp `.csproj` (`<Nullable>enable</Nullable>`). Lập trình viên phải tuân thủ:
- Mọi thuộc tính/tham số có thể `null` phải được khai báo tường minh bằng dấu `?` (`string? InstallPath`).
- **CẤM TUYỆT ĐỐI** sử dụng toán tử che giấu null (`!` - Null-Forgiving Operator) mà không có bình luận giải thích lý do chính đáng xác minh qua assert/guard.
- Luôn sử dụng toán tử coalescing `??` và conditional access `?.` để xử lý null an toàn:

```csharp
string safePublisher = software.Publisher?.Trim() ?? "Unknown Publisher";
```

## 2.4. C# 12 Primary Constructors
Ưu tiên sử dụng `Primary Constructors` của C# 12 trong các Service, Scanners, và Plugins để khai báo tiêm phụ thuộc (`Dependency Injection`) một cách súc tích:

```csharp
// ✅ CHUẨN MỰC: C# 12 Primary Constructor
namespace LicenseIntelligencePlatform.Infrastructure.Scanners;

public sealed class WindowsRegistryScanner(ILogger<WindowsRegistryScanner> logger) : IScanner
{
    public string ScannerName => "WindowsRegistryScanner";
    // Tham số 'logger' có thể được sử dụng trực tiếp trong toàn bộ class
}
```

## 2.5. Pattern Matching & Collection Expressions
- Sử dụng `switch expressions` và `relational patterns` thay cho chuỗi `if-else` cồng kềnh:

```csharp
ConfidenceLevel level = totalScore switch
{
    >= 70 => ConfidenceLevel.Verified,
    >= 50 => ConfidenceLevel.High,
    >= 30 => ConfidenceLevel.Medium,
    >= 10 => ConfidenceLevel.Low,
    _     => ConfidenceLevel.None
};
```

- Sử dụng `Collection Expressions` (`[...]`) của C# 12 khi khởi tạo mảng hoặc danh sách bất biến:

```csharp
IReadOnlyList<string> supportedExtensions = [".exe", ".dll", ".lic", ".key"];
```

---

# 3. High-Performance C# & Zero-Allocation Disciplines

Để hệ thống quét hàng ngàn file và key Registry trong vòng `< 500 ms` theo yêu cầu Benchmark, các thói quen gây hao phí bộ nhớ (Memory Churn / Garbage Collection Pressure) bị cấm nghiêm ngặt:

## 3.1. `Span<T>` và `ReadOnlySpan<char>` khi bóc tách văn bản
Khi phân tích cấu trúc phiên bản (`Version parsing`), cắt đường dẫn hay kiểm tra header file, **KHÔNG ĐƯỢC** dùng `string.Substring()` hay `string.Split()` vì chúng tạo ra các đối tượng `string` mới trên Heap. Hãy dùng `ReadOnlySpan<char>`:

```csharp
// ✅ CHUẨN MỰC Zero-Allocation String Parsing
public static bool IsMajorVersion10(string version)
{
    ReadOnlySpan<char> span = version.AsSpan();
    int dotIndex = span.IndexOf('.');
    if (dotIndex > 0)
    {
        ReadOnlySpan<char> majorSpan = span.Slice(0, dotIndex);
        return majorSpan.SequenceEqual("10");
    }
    return false;
}
```

## 3.2. `StringComparer.OrdinalIgnoreCase` trong tất cả Maps / Sets
So sánh văn bản trong C# mặc định có thể nhạy cảm với ngôn ngữ hệ điều hành (`Culture-Sensitive`). Khi khởi tạo `Dictionary<string, T>` hay `HashSet<string>`, luôn truyền tham số so sánh `Ordinal`:

```csharp
// ✅ CHUẨN MỰC: So sánh byte-level siêu tốc, không bị phụ thuộc Culture
private static readonly HashSet<string> LicenseKeywords = new(StringComparer.OrdinalIgnoreCase)
{
    "GPL-3.0", "MIT", "Apache-2.0", "Proprietary Commercial"
};
```

## 3.3. `StringBuilder` cho chuỗi động & `ArrayPool<T>` cho I/O Buffer
- Khi nối chuỗi trong vòng lặp (như tạo báo cáo Markdown/CSV), bắt buộc dùng `StringBuilder`.
- Khi đọc file I/O với bộ đệm lớn hơn 4KB, cân nhắc mượn bộ đệm từ `ArrayPool<char>.Shared.Rent()` và `Return()` trong khối `finally` để tránh cấp phát mảng mới.

---

# 4. Concurrency, Async Discipline & Threading

Hệ thống điều phối các I/O Scanners và Plugins một cách bất đồng bộ. Việc viết code async không đúng cách có thể dẫn đến Deadlocks hoặc Thread Starvation.

## 4.1. Quy tắc "Async All The Way Down"
- **KHÔNG BAO GIỜ** gọi `.Result` hoặc `.Wait()` trên một `Task`. Điều này block luồng hiện tại và có thể gây Deadlock nghiêm trọng. Luôn dùng từ khóa `await`.
- **KHÔNG BAO GIỜ** viết `async void` (trừ các Event Handlers giao diện CLI/UI bắt buộc). Mọi phương thức async phải trả về `Task` hoặc `Task<T>`.

## 4.2. `ConfigureAwait(false)` trong Infrastructure & Plugins
Trong toàn bộ các thư viện tầng dưới (`LicenseIntelligencePlatform.Infrastructure`, `Plugins.Standard`), mọi lời gọi `await` đều nên đi kèm `.ConfigureAwait(false)` để tránh ép luồng phải quay lại Synchronization Context ban đầu, tối ưu hóa tốc độ thực thi thread pool:

```csharp
using var stream = File.OpenRead(filePath);
using var reader = new StreamReader(stream);
string header = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
```

## 4.3. Bắt buộc truyền & xử lý `CancellationToken`
Mọi public async API đều **phải chấp nhận `CancellationToken cancellationToken = default` là tham số cuối cùng**. Bên trong vòng lặp nặng hoặc I/O calls, phải kiểm tra định kỳ:

```csharp
foreach (var registryKey in subKeys)
{
    cancellationToken.ThrowIfCancellationRequested(); // Ngắt ngay khi bị Timeout/Hủy
    // Xử lý đọc registry...
}
```

---

# 5. Code Structure, Organization & Member Order

Sự ngăn nắp cấu trúc giúp mọi lập trình viên định vị nhanh logic trong một tệp `.cs` mà không cần cuộn trang lung tung.

## 5.1. One Type Per File
Mỗi `public class`, `public interface`, `public record`, hoặc `public enum` lớn phải nằm trong một file riêng biệt có tên khớp 100% với tên type (`WindowsRegistryScanner.cs` chứa class `WindowsRegistryScanner`).

## 5.2. Thứ tự sắp xếp các thành phần bên trong Class (Member Order)
Khi viết hoặc refactor một class, phải sắp xếp các thành phần theo đúng thứ tự ưu từ trên xuống dưới:

```csharp
public sealed class ExampleClass : IExampleInterface
{
    // 1. Constants & Static ReadOnly Fields
    private const int MaxRetryCount = 3;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    // 2. Private Fields & Dependencies
    private readonly ILogger<ExampleClass> _logger;
    private readonly IScanner _scanner;

    // 3. Constructors / Primary Constructor Initialization
    public ExampleClass(ILogger<ExampleClass> logger, IScanner scanner)
    {
        _logger = logger;
        _scanner = scanner;
    }

    // 4. Public Properties
    public string Name => "ExampleClass";

    // 5. Public Methods (Implementation của Interfaces đặt trước)
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await HelperMethodAsync(cancellationToken).ConfigureAwait(false);
    }

    // 6. Private / Helper Methods (Phục vụ cho Public Methods bên trên)
    private async Task HelperMethodAsync(CancellationToken cancellationToken)
    {
        // Logic phụ trợ...
    }
}
```

---

# 6. Rigorous Error Handling & Exception Governance

Khả năng chịu lỗi (`Resilience`) của LIP phụ thuộc hoàn toàn vào tính kỷ luật khi xử lý Exceptions.

## 6.1. Cấm tuyệt đối Magic Catch (`swallowing exceptions`)
- **KHÔNG BAO GIỜ** viết khối `catch (Exception)` rỗng để nuốt lỗi:
```csharp
// ❌ VI PHẠM TRẦM TRỌNG: Nuốt lỗi không dấu vết
try { ReadKey(); } catch { }
```
- Nếu bắt buộc phải bỏ qua một ngoại lệ (như khi Scanner gặp `UnauthorizedAccessException` trên nhánh Registry bị khóa của hệ thống), **PHẢI GHI LOG WARNING/DEBUG** hoặc có comment giải thích rõ ràng lý do kiến trúc:
```csharp
// ✅ CHUẨN MỰC: Catch cụ thể và ghi log, giữ cho pipeline tiếp tục chạy
try 
{ 
    using var key = Registry.LocalMachine.OpenSubKey(subPath); 
} 
catch (SecurityException ex) 
{ 
    _logger.LogDebug("Security exception on subkey {Path}: {Message}", subPath, ex.Message); 
}
```

## 6.2. Exception Specificity (Tính cụ thể của ngoại lệ)
- Ném các ngoại lệ cụ thể có ý nghĩa: `ArgumentNullException`, `InvalidOperationException`, `OperationCanceledException`.
- **KHÔNG BAO GIỜ** ném `throw new Exception("Error message");` chung chung. Nếu lỗi thuộc nghiệp vụ Domain, hãy tạo Custom Exception kế thừa từ `Exception` (Ví dụ: `InvalidPluginManifestException`).

---

# 7. Documentation, XML Comments & Maintainability

Mã nguồn sạch là mã nguồn tự giải thích (`Self-Documenting Code`). Tuy nhiên, tài liệu API vẫn là bắt buộc để hỗ trợ IDE IntelliSense và sinh tài liệu tự động.

## 7.1. Bắt buộc XML Documentation (`///`) cho Public Contracts
Toàn bộ `public interface`, `public class`, `public record` và public methods bên trong tầng `Domain` và `Application` bắt buộc phải có XML Comments đầy đủ:

```csharp
/// <summary>
/// Thực hiện quét toàn bộ hệ thống để tìm kiếm các phần mềm đã cài đặt theo các tùy chọn cấu hình.
/// </summary>
/// <param name="options">Các tùy chọn lọc và tham số điều khiển quá trình quét.</param>
/// <param name="cancellationToken">Token dùng để hủy bỏ tiến trình quét khi hết giờ hoặc người dùng yêu cầu.</param>
/// <returns>Báo cáo tổng hợp chứa danh sách phần mềm và kết quả phân tích bản quyền.</returns>
/// <exception cref="ArgumentNullException">Ném ra nếu <paramref name="options"/> là null.</exception>
public Task<ScanReport> ExecuteFullScanAsync(ScanOptions options, CancellationToken cancellationToken = default);
```

## 7.2. Nguyên tắc "Explain WHY, not WHAT" cho Internal Comments
Bên trong logic thân hàm (`Method Body`), tên biến và cấu trúc code đã nói lên **Nó đang làm gì (WHAT)**. Comment chỉ được phép xuất hiện khi cần giải thích **Tại sao quyết định kỹ thuật đó lại được thực hiện (WHY)**:

```csharp
// ✅ CHUẨN MỰC: Giải thích TẠI SAO (Lý do nghiệp vụ/kỹ thuật)
// Đọc tối đa 2048 bytes vì các giấy phép nguồn mở (GPL/MIT) luôn đặt thông tin bản quyền ở Header.
// Việc hạn chế số bytes giúp bảo vệ RAM và ngăn rủi ro OOM khi gặp file log khổng lồ đặt nhầm tên LICENSE.
char[] buffer = new char[2048];

// ❌ VI PHẠM: Comment thừa thãi nói lại WHAT code đang làm
// Khởi tạo mảng ký tự có kích thước 2048
char[] buffer = new char[2048];
```

---

# 8. AI Coding Assistants & Automated Refactoring Governance

Khi các **AI Coding Assistants** (như Antigravity IDE, GitHub Copilot, Cursor) hoặc các công cụ tự động hóa tham gia chỉnh sửa mã nguồn, chúng **BẮT BUỘC** phải tuân theo 4 quy ước quản trị sau:

1. **Never Remove or Mutilate Passing Tests (`Zero Test Deletion`)**:
   - Khi AI thực hiện refactor hoặc thêm tính năng, **TUYỆT ĐỐI KHÔNG ĐƯỢC XÓA** hoặc comment out bất kỳ Unit Test đang pass nào trong `LicenseIntelligencePlatform.Tests` chỉ để làm cho `dotnet test` xanh.
   - Nếu một thay đổi kiến trúc hợp lệ làm hỏng test cũ, AI phải cập nhật logic của test đó và giải thích chi tiết trong Pull Request/Commit Description.
2. **Strict Comment & Docstring Preservation (`Documentation Integrity`)**:
   - AI **KHÔNG ĐƯỢC XÓA** các khối XML Comments (`///`) hay các comment giải thích kiến trúc (`// WHY: ...`) hiện hữu khi chỉnh sửa một file, trừ khi đoạn logic tương ứng bị thay thế hoàn toàn.
3. **No Hardcoded Magic Strings / Numbers**:
   - Khi AI tạo ra một Plugin hoặc Scanner mới, không được cắm cứng (`hardcode`) tên nhà phát hành, từ khóa EULA hay magic offsets nằm rải rác giữa logic method. Tất cả phải được định nghĩa rõ ràng thành `private static readonly HashSet<string>` hoặc `private const` ở phần đầu của class.
4. **No Unsolicited Dependency Injection or Layer Violations**:
   - AI **KHÔNG ĐƯỢC TỰ Ý THÊM** các gói NuGet bên ngoài (`Newtonsoft.Json`, `RestSharp`, `Polly`...) hay phá vỡ ranh giới 5 tầng Clean Architecture mà không có sự đồng ý tường minh (`Explicit Approval`) từ người dùng. Mọi tính năng phải được xây dựng dựa trên các thư viện chuẩn hiện có của `.NET 8.0`.

---

# Summary of Coding Standards

Việc tuân thủ 8 chương quy chuẩn viết mã trên giúp **License Intelligence Platform (LIP)** đạt được 3 giá trị kỹ thuật cao nhất:
1. **Hiệu năng & Tối ưu (High Performance & Zero-Allocation):** Tối đa hóa `Span<T>`, `OrdinalIgnoreCase`, và bộ nhớ đệm.
2. **Độ ổn định tuyệt đối (Strict Resilience & Safety):** Kỷ luật Nullable, CancellationToken Timeout, và Exception handling rõ ràng.
3. **Khả năng duy trì lâu dài (Long-term Clean Architecture):** Cấu trúc đồng nhất, tài liệu rõ ràng, và quản trị nghiêm ngặt đối với AI Coding Assistants.
