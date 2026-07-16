# 14_ENGINEERING_HANDBOOK.md

# License Intelligence Platform (LIP)

## Enterprise Engineering Handbook & Governance Standard

Version: 1.0

Status: Stable (Phase 0 – Phase 4 Completed)

Author: DynamiteV

---

# 1. Purpose & Engineering Philosophy

Tài liệu **Enterprise Engineering Handbook & Governance Standard** là cẩm nang quản trị kỹ thuật tối cao cho toàn bộ tổ chức phát triển **License Intelligence Platform (LIP)**. Mọi Kỹ sư Phần mềm (`Software Engineers`), Kiến trúc sư (`Architects`), và **AI Coding Assistants** khi đóng góp mã nguồn vào dự án bắt buộc phải tuân thủ nghiêm ngặt cẩm nang này.

### Triết lý Kỹ thuật (`Engineering Axioms`):
- **Readability & Maintainability Over Premature Optimization:** Code được đọc nhiều gấp 10 lần so với viết mới. Luôn ưu tiên sự rõ ràng, tường minh trong đặt tên và cấu trúc hơn là các thủ thuật tối micro vô nghĩa khiến người sau không thể đọc hiểu.
- **Strict Clean Architecture Enforcement:** Phân định ranh giới tuyệt đối. Tầng `Domain` là bất khả xâm phạm (`Zero Dependencies`), tầng `Application` điều phối nghiệp vụ, `Infrastructure` xử lý ngoại vi I/O, `Plugins.Standard` chứa logic nhận diện, và `Presentation.Cli` là điểm 진 nhập duy nhất.
- **Resiliency & Fault Tolerance by Design:** Trong môi trường thực tế với quyền người dùng giới hạn và các file bị khóa, hệ thống không bao giờ được phép sập (`Zero Crash Policy`). Luôn áp dụng `try/catch` bẫy ngoại lệ và `CancellationToken 5000ms Timeout Guard` khi gọi sang Plugin hoặc I/O ngoại vi.
- **Test-Driven Reliability:** Mọi commit vào nhánh chính phải đảm bảo 100% bộ Unit Test (`36/36 tests`) vượt qua thành công (`Pass cleanly`).

---

# 2. Architectural Guardrails & Layering Invariants

## 2.1. Quy tắc phụ thuộc tầng (`Strict Layer Dependencies`)
```
[Presentation.Cli] ──► [Application] ──► [Domain] ◄── [Infrastructure]
                                            ▲
                                            └── [Plugins.Standard (33 Plugins)]
```

- **Quy tắc 1 (Domain Integrity):** Cấm tuyệt đối thêm tham chiếu ngoại vi (`NuGet` bên thứ ba, `System.IO.File`, `Microsoft.Win32.Registry`, `HttpClient`) vào `LicenseIntelligencePlatform.Domain`.
- **Quy tắc 2 (Application Pure Logic):** `CoreEngine`, `SoftwareMergeEngine`, `PluginCompatibilityValidator` thuộc `Application` chỉ giao tiếp qua `IScanner` và `ILicensePlugin`.
- **Quy tắc 3 (Scanner Read-Only):** `IScanner` (`WindowsRegistryScanner`, `LinuxPackageScanner`, `WingetPackageScanner`, `DeepFileSystemScanner`) thuộc `Infrastructure` cấm tuyệt đối thực thi thao tác `Write/Delete` trên đĩa hoặc Registry.

## 2.2. Plugin Development Constraints (`Rule 6 & Rule 9 Enforcement`)
- **Không tự ý quét toàn bộ hệ thống:** `ILicensePlugin` nhận đầu vào `SoftwareInfo` đã gộp lọc, cấm tự ý duyệt toàn bộ đĩa `C:\` hoặc gọi `RegOpenKeyEx` bên ngoài đường dẫn `software.InstallLocation`.
- **Không gọi Network / Internet:** Các Plugin trong `Plugins.Standard` hoàn toàn chạy offline.
- **Thang điểm 100 bằng chứng (`Confidence Engine`):** Trả về `ConfidenceLevel.Verified` chỉ khi thu thập được `Evidence` có trọng số tối thiểu `70 pts` (nhờ file license header hoặc chữ ký số Authenticode).

---

# 3. .NET 8 Coding Standards & Idioms

## 3.1. Immutability & Modern C# 12
- Luôn sử dụng `sealed class` hoặc `sealed record` cho các đối tượng DTO, Entity và Plugin không có nhu cầu kế thừa.
- Sử dụng `record` và `init-only properties` (`public string Name { get; init; } = string.Empty;`) cho các đối tượng `SoftwareInfo`, `Evidence`, `PluginManifest`, `LicenseCheckResult`.
- Tránh `null` rủi ro bằng cách khởi tạo chuỗi rỗng `string.Empty` hoặc `Array.Empty<T>()` làm giá trị mặc định.

## 3.2. Performance & Memory Optimization (`Zero-Allocation Idioms`)
- **String Comparison:** Khi so sánh tên phần mềm hay nhà phát hành, **luôn luôn** sử dụng `StringComparison.OrdinalIgnoreCase` hoặc khởi tạo `HashSet<string>(StringComparer.OrdinalIgnoreCase)`. Cấm dùng `.ToLower()` hay `.ToUpper()` trong vòng lặp vì tạo ra string rác trên Heap gây áp lực cho Garbage Collector (`GC Churn`).
- **Span & Memory:** Sử dụng `ReadOnlySpan<char>` và `StreamReader` buffer nhỏ (`<= 2KB`) khi đọc header file `.lic` hoặc `.key` để đảm bảo `Memory Footprint Delta < 5 MB`.

## 3.3. Asynchronous Programming Disciplines
- Mọi phương thức I/O phải là `async Task` hoặc `async Task<T>` kèm theo tham số `CancellationToken cancellationToken = default`.
- **Luôn luôn** truyền `cancellationToken` xuyên suốt chuỗi gọi và gọi `cancellationToken.ThrowIfCancellationRequested()` trong các vòng lặp lớn.
- **Cấm tuyệt đối** sử dụng `.Result`, `.Wait()`, hoặc `Task.Run(...)` một cách tùy tiện gây `Deadlock` hoặc `Thread Pool Starvation`.
- Luôn thêm `.ConfigureAwait(false)` khi viết code trong thư viện `Infrastructure` hoặc `Plugins`.

---

# 4. Exception Handling & Logging Standards

## 4.1. Structured Logging (`ILogger<T>`)
- Cấm sử dụng `Console.WriteLine()` trong các thư viện (`Domain`, `Application`, `Infrastructure`, `Plugins`). `Console.WriteLine()` chỉ được phép sử dụng duy nhất tại tầng `Presentation.Cli` để hiển thị bảng UI cho người dùng.
- Sử dụng cấu trúc Structured Logging với Message Templates:
  ```csharp
  // ✅ ĐÚNG: Structured Logging (Tham số hóa)
  _logger.LogInformation("Scanning complete for host {HostName}. Discovered {Count} packages.", report.HostName, report.TotalSoftwareScanned);

  // ❌ SAI: String Interpolation gây mất cấu trúc Log và tốn chi phí format string
  _logger.LogInformation($"Scanning complete for host {report.HostName}. Discovered {report.TotalSoftwareScanned} packages.");
  ```

## 4.2. Exception Shielding & Sandbox Isolation
- Khi bắt ngoại lệ trong `CoreEngine` hoặc các Plugin, phải cô lập vào đối tượng kết quả thay vì ném ra ngoài:
  ```csharp
  try
  {
      return await plugin.CheckLicenseAsync(software, linkedCts.Token);
  }
  catch (OperationCanceledException)
  {
      _logger.LogWarning("Plugin {PluginName} timed out after 5000ms on package {SoftwareName}.", plugin.Manifest.PluginName, software.Name);
      return LicenseCheckResult.CreateErrorResult(plugin.Manifest.PluginId, plugin.Manifest.PluginName, software, "Timeout after 5000ms");
  }
  catch (Exception ex)
  {
      _logger.LogError(ex, "Plugin {PluginName} crashed while evaluating {SoftwareName}.", plugin.Manifest.PluginName, software.Name);
      return LicenseCheckResult.CreateErrorResult(plugin.Manifest.PluginId, plugin.Manifest.PluginName, software, ex.Message);
  }
  ```

---

# 5. Git Workflow & Review Governance

## 5.1. Trunk-Based & Feature Branching Workflow
```
[main (Protected / Stable)] ◄── PR / Review ── [feature/phase4-audit-mapper]
                                          ── [bugfix/winget-scanner-path]
```
- **Nhánh `main`:** Luôn ở trạng thái hoàn hảo (`Production Ready`). Không bao giờ commit trực tiếp lên `main`.
- **Tên nhánh (`Branch Naming convention`):**
  - `feature/<short-description>` (Ví dụ: `feature/html-visual-report`)
  - `bugfix/<short-description>` (Ví dụ: `bugfix/registry-64bit-access`)
  - `refactor/<short-description>` (Ví dụ: `refactor/evidence-scoring`)
- **Tên commit (`Semantic Commit Messages`):**
  - `feat: implement HtmlReportMapper with dark mode styling and badges`
  - `fix: resolve CS1061 property mismatch in AuditReportMapper`
  - `test: verify 36/36 unit tests pass clean across all 33 plugins`
  - `docs: upgrade 14_ENGINEERING_HANDBOOK.md to Version 1.0 Stable`

---

# 6. Definition of Done (DoD) & Review Checklist

Trước khi mở Pull Request (PR) hoặc gộp code, lập trình viên và AI Coding Assistant phải tự kiểm định qua bảng checklist 8 tiêu chí sau:

| Tiêu chí DoD (`Definition of Done Item`) | Yêu cầu Kỹ thuật Thẩm định (`Technical Verification`) | Trạng thái bắt buộc |
| :--- | :--- | :---: |
| **1. Clean Build Verification** | `dotnet build src/LicenseIntelligencePlatform.slnx -c Release` thành công, **0 Errors, 0 Warnings**. | ✅ **Bắt buộc** |
| **2. 100% Unit Test Pass** | `dotnet test src/LicenseIntelligencePlatform.slnx` đạt **36/36 tests green** (`Pass rate: 100%`). | ✅ **Bắt buộc** |
| **3. Architectural Compliance** | Không vi phạm ranh giới tầng, không tham chiếu I/O vào `Domain`, tuân thủ 100% Read-Only `IScanner`. | ✅ **Bắt buộc** |
| **4. Zero-Allocation & String Rules** | Đã dùng `StringComparison.OrdinalIgnoreCase` khi so sánh chuỗi, không `.ToLower()` trong vòng lặp. | ✅ **Bắt buộc** |
| **5. Rule 9 Exception Sandbox** | Mọi lời gọi file/đĩa hoặc plugin mới đều bọc trong `try/catch` an toàn. | ✅ **Bắt buộc** |
| **6. CancellationToken Passthrough** | Đã truyền `cancellationToken` vào tất cả các hàm async I/O. | ✅ **Bắt buộc** |
| **7. Structured Logging Checks** | Sử dụng đúng message templates `{ParameterName}`, không dùng string interpolation `$""`. | ✅ **Bắt buộc** |
| **8. Documentation Synchronized** | Đã cập nhật `CHANGELOG` và tài liệu markdown liên quan nếu thay đổi kiến trúc/tính năng. | ✅ **Bắt buộc** |