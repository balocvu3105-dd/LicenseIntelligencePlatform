# 08_ARCHITECTURE_ROADMAP.md

# License Intelligence Platform (LIP)

## Architecture Enhancement Specification (Roadmap & Technical Blueprints)

Version: 1.0

Status: Stable

Author: DynamiteV

---

# Purpose & Strategic Vision

Tài liệu **Architecture Enhancement Specification** (Đặc tả Mở rộng và Lộ trình Kiến trúc) là bản đồ định hướng kỹ thuật dài hạn, xác định rõ các đặc tả thiết kế cho từ **Phase 1 đến Phase 4** của **License Intelligence Platform (LIP)**.

Mục tiêu cốt lõi của lộ trình kiến trúc:
- Chuyển tiếp hệ thống từ kiến trúc **Pilot (Phase 0)** sang nền tảng thông minh cấp doanh nghiệp (**Enterprise-Grade Intelligent Platform**).
- Thiết lập tiêu chuẩn kiến trúc cho các Engine chuyên biệt (`Rule Engine`, `Confidence Engine`, `Evidence Engine`, `Merge Engine`, `Reporting Engine`).
- Đảm bảo tính chống chịu lỗi tối đa (**Resiliency**) và cô lập rủi ro tuyệt đối (**Fault Isolation Sandbox**) khi mở rộng quy mô lên hàng chục Scanner và hàng trăm Plugin trong hệ sinh thái.

---

# Phase 1 — Intelligent Detection Architecture (Core Engines 2.0)

Giai đoạn 1 tập trung nâng cấp độ chính xác kiểm kê bản quyền lên **≥ 97%** thông qua việc tách bạch rõ ràng 4 Engine định quyết định trong tầng `Application` và `Domain`.

## 1. Rule Engine 2.0 (Intelligent Decision & Resolution Engine)

### 1.1. Mục tiêu & Vấn đề hiện tại
Trong kiến trúc Pilot (`CoreEngine` 1.0), luồng xử lý diễn ra theo mô hình tuyến tính đơn giản:
```
SoftwareInfo ──► Plugin.CanCheck() ──► Plugin.CheckLicenseAsync() ──► LicenseCheckResult
```
Khi có nhiều Plugin cùng nhận diện một phần mềm (Ví dụ: `OpenSourceArtifactPlugin` phát hiện file `LICENSE` MIT, trong khi `GitOpenSourcePlugin` phát hiện cấu hình `.git`), hệ thống cần một cơ chế giải quyết xung đột thông minh thay vì ghi nhận trùng lặp hoặc lấy kết quả đầu tiên.

### 1.2. Kiến trúc mục tiêu (`Target Pipeline`)
```
                                        ┌──► Evidence Engine (Collect & Weight) ──┐
SoftwareInfo ──► Compatible Plugins ────┼──► Confidence Engine (Score 0-100)  ────┼──► Rule Engine 2.0 ──► Final Result
                                        └──► Conflict Resolution (Priority Match)─┘
```

### 1.3. Trách nhiệm của Rule Engine 2.0
- **Plugin Prioritization:** Nhóm và ưu tiên gọi các Plugin theo `PluginPriority` (`CommercialSpecific = 100`, `Ecosystem = 75`, `Heuristic = 50`, `Generic = 25`).
- **Conflict Arbitration (Phân xử xung đột):** Nếu có $\ge 2$ Plugin trả về kết quả cho cùng một `SoftwareInfo`:
  1. Ưu tiên kết quả có `ConfidenceLevel` cao hơn (Ví dụ: `Verified` thắng `Medium`).
  2. Nếu cùng mức `ConfidenceLevel`, ưu tiên kết quả có tổng điểm `Evidence Weight Score` cao hơn.
  3. Nếu điểm số bằng nhau, ưu tiên Plugin có `PluginPriority` cao hơn trong `PluginManifest`.
- **Result Synthesis (Hợp nhất bằng chứng):** Gộp toàn bộ danh sách `Evidences` từ các Plugin bổ trợ vào một `LicenseCheckResult` duy nhất để tăng tính minh bạch giải trình (`Explainability`).

---

## 2. Confidence Engine (100-Point Scoring System)

### 2.1. Thay thế Enums tĩnh bằng Thang điểm Động (`Dynamic Scoring Metric`)
Để loại bỏ tính chủ quan trong việc đánh giá bản quyền, `Confidence Engine` định nghĩa mô hình chấm điểm 100 điểm dựa trên trọng số thực tế của bằng chứng thu thập được:

| Nguồn bằng chứng (Evidence Source) | Điểm số tối đa | Tiêu chí đánh giá kỹ thuật |
| :--- | :---: | :--- |
| **1. Cryptographic / License File Header** | **+40 điểm** | Phát hiện file `LICENSE`, `.lic`, `COPYING` với header chính xác $\le 2\text{KB}$. |
| **2. Binary Authenticode Signature** | **+25 điểm** | Chữ ký số `.exe`/`.dll` khớp chứng nhận nhà phát hành (`Vendor Signature`). |
| **3. Registry / Volume Activation Key** | **+20 điểm** | Phát hiện `ProductCode`, `UpgradeCode` hoặc Registry Key bản quyền doanh nghiệp. |
| **4. Installer Metadata / Package Manifest** | **+15 điểm** | Khớp metadata của `WinGet`, `dpkg`, hoặc `Steam AppId`. |
| **5. Publisher Keyword Match (Heuristic)** | **+10 điểm** | Khớp từ khóa tên nhà phát hành (`TargetPublishers`). |

### 2.2. Ánh xạ từ Điểm số (`Score`) sang `ConfidenceLevel`
```
Score >= 85 points ──► ConfidenceLevel.Verified (4) - Bằng chứng tuyệt đối, đủ điều kiện kiểm toán pháp lý.
Score 65 - 84 pts  ──► ConfidenceLevel.High (3)     - Độ tin cậy cao, xác nhận bởi file/chữ ký rõ ràng.
Score 35 - 64 pts  ──► ConfidenceLevel.Medium (2)   - Nhận diện theo pattern hệ sinh thái hoặc metadata.
Score 10 - 34 pts  ──► ConfidenceLevel.Low (1)      - Nhận diện heuristic theo từ khóa nhà phát hành.
Score < 10 points  ──► ConfidenceLevel.None (0)     - Không đủ cơ sở dữ liệu xác thực.
```

---

## 3. Evidence Engine (First-Class Domain Entity Separation)

### 3.1. Đặc tả Entity `Evidence`
`Evidence` không chỉ là một chuỗi mô tả mà phải là một đối tượng Domain (`record`) bất biến, có định danh độc lập để phục vụ kiểm toán và truy xuất nguồn gốc:

```csharp
namespace LicenseIntelligencePlatform.Domain.Entities;

public sealed record Evidence(
    string EvidenceId,
    string EvidenceType,      // e.g., "LicenseFileHeader", "AuthenticodeSignature", "RegistryProductCode"
    string Description,
    string SourceLocation,    // Đường dẫn file tuyệt đối hoặc nhánh Registry HKCU/HKLM
    int WeightScore,          // Trọng số điểm (0-40)
    bool IsCryptographicallyVerified,
    DateTime DiscoveredAtUtc,
    string RawDataSnippet     // Đoạn text trích xuất thực tế (tối đa 256 ký tự)
);
```

### 3.2. Trách nhiệm kiến trúc
- **Zero Hallucination Guarantee:** `CoreEngine` và `RuleEngine` không bao giờ được phép tự ý tạo hay suy đoán `Evidence`. Tất cả `Evidence` phải do các `ILicensePlugin` thu thập từ hệ điều hành qua `IScanner` hoặc trực tiếp từ file system local trong khối `try/catch`.

---

# Phase 2 — Plugin Ecosystem & Lifecycle SDK

Giai đoạn 2 chuẩn hóa kiến trúc mở rộng theo mô hình **Plug-and-Play**, cho phép bên thứ ba phát triển các gói Plugin kiểm tra bản quyền độc lập mà không cần can thiệp hoặc biên dịch lại `CoreEngine`.

## 1. Plugin SDK v1.0 & Manifest Specifications

Mọi Plugin phải triển khai interface `ILicensePlugin` và cung cấp một `PluginManifest` chuẩn hóa để `PluginCompatibilityValidator` kiểm tra trước khi nạp vào RAM:

```csharp
namespace LicenseIntelligencePlatform.Domain.Entities;

public sealed record PluginManifest(
    string PluginId,          // Định danh duy nhất theo format: "LIP-PLG-<VENDOR>-<ID>"
    string PluginName,
    string PluginVersion,     // Semantic Versioning (e.g., "1.0.0")
    string Author,
    string Description,
    PluginPriority Priority,  // CommercialSpecific (100), Ecosystem (75), Heuristic (50), Generic (25)
    string MinSdkVersion,     // Phiên bản SDK tối thiểu yêu cầu (e.g., "1.0.0")
    string MaxSdkVersion,     // Phiên bản SDK tối đa tương thích (rỗng nếu tương thích tới tương lai)
    string SupportedOs        // "Windows", "Linux", hoặc "Any"
);
```

## 2. Plugin Lifecycle & Sandboxing Architecture

### 2.1. Vòng đời 5 bước của một Plugin (`Lifecycle States`)
```
[1. Discovered] ──► [2. Validated (SDK & OS Check)] ──► [3. Loaded into RAM] ──► [4. Executing (Timeout Guard)] ──► [5. Unloaded / Completed]
```

### 2.2. Kỷ luật Sandbox & Error Isolation boundary (Rule 9 Enforcement)
Mặc dù ở Phase 2 các Standard Plugin chạy trong cùng AppDomain / Process với Core Engine, việc bảo vệ hệ thống khỏi lỗi của Plugin vẫn là bất di bất dịch:
- **CancellationToken Guard (5000ms Timeout):** Mỗi lời gọi `plugin.CheckLicenseAsync(software, token)` được bọc bởi `CancellationTokenSource.CreateLinkedTokenSource` với giới hạn tối đa `5000 ms`. Nếu vượt quá thời gian (do lặp vô hạn hoặc treo I/O), token sẽ tự động hủy, ghi nhận kết quả `Timeout` và tiếp tục quét phần mềm kế tiếp.
- **Exception Shielding:** Khối `try/catch (Exception ex)` bao bọc riêng biệt cho từng Plugin. Lỗi I/O, `NullReferenceException` hay `OutOfMemoryException` cục bộ từ Plugin sẽ bị bắt lại, cô lập thành một `LicenseCheckResult.CreateErrorResult(...)` mà không làm sập `CoreEngine`.

---

## 3. Data Merge Engine & Deduplication Strategy (`SoftwareMergeEngine`)

Khi nhiều `IScanner` (Registry 32-bit, Registry 64-bit, WinGet, Deep File System) cùng quét hệ thống, cùng một phần mềm sẽ xuất hiện nhiều lần dưới các góc nhìn khác nhau.

### 3.1. Chiến lược Gộp và Chuẩn hóa (`Sanitization & Merge Invariants`)
`SoftwareMergeEngine` áp dụng thuật toán Deduplication theo 3 cấp độ ưu tiên:
1. **Khớp tuyệt đối GUID ProductCode:** Nếu `softwareA.ProductCode == softwareB.ProductCode` (và không rỗng), gộp thành 1 bản ghi duy nhất.
2. **Khớp cặp Name + Version:** Nếu chuẩn hóa `Sanitize(softwareA.Name) == Sanitize(softwareB.Name)` và `softwareA.Version == softwareB.Version`, gộp bản ghi.
3. **Chuẩn hóa Publisher (`Publisher Sanitization`):** Tự động hợp nhất các biến thể tên công ty:
   - `"Microsoft Corporation"`, `"Microsoft Corp."`, `"Microsoft"` $\to$ Chuẩn hóa thành `"Microsoft Corporation"`.
   - `"Adobe Systems Incorporated"`, `"Adobe Inc."`, `"Adobe"` $\to$ Chuẩn hóa thành `"Adobe Inc."`.

---

# Phase 3 — Software Discovery Expansion (Multi-Source Scanners)

Giai đoạn 3 mở rộng khả năng thu thập dữ liệu thô của hệ thống trên mọi hệ điều hành phổ biến thông qua kiến trúc **Scanner SDK**.

## 1. Kiến trúc Scanner SDK (`IScanner` & `CompositeScanner`)

Hệ thống không phụ thuộc vào một nguồn dữ liệu duy nhất. `CompositeScanner` điều phối danh sách các `IScanner` độc lập:

```
                                  ┌──► WindowsRegistryScanner (HKLM & HKCU - 32/64 bit)
                                  ├──► LinuxPackageScanner (/var/lib/dpkg/status)
CompositeScanner (Orchestrator) ──┼──► WingetPackageScanner (Local Repository Metadata)
                                  ├──► SteamGameScanner (SteamApps / appmanifest_*.acf)
                                  └──► DeepFileSystemScanner (Running Processes & PE Headers)
```

## 2. Tiêu chuẩn Kỹ thuật của các Scanner Mới
Mọi Scanner mới trong Phase 3 phải tuân thủ nghiêm ngặt 3 nguyên tắc bất di bất dịch của kiến trúc LIP:
- **100% Read-Only:** Cấm tuyệt đối quyền ghi/sửa/xóa trên file system, Registry hoặc cấu hình gói của hệ điều hành.
- **Zero-Network Connectivity:** Quét hoàn toàn offline dựa trên metadata cục bộ (`.json`, `.acf`, `.txt`, Registry hives). Không gọi API mạng để tra cứu tên phần mềm.
- **Platform-Aware Execution:** Luôn kiểm tra `OperatingSystem.IsWindows()`, `IsLinux()`, hoặc `IsMacOS()` trong hàm `IsSupportedOnCurrentPlatform()` để tự động bỏ qua các Scanner không tương thích mà không ném ngoại lệ.

---

# Phase 4 — Reporting & Diagnostic Engine Hierarchy

Giai đoạn 4 đa dạng hóa các định dạng đầu ra của nền tảng, biến các dữ liệu kỹ thuật thô thành các báo cáo điều hành và báo cáo kiểm toán có giá trị pháp lý và quản trị cao cho doanh nghiệp.

## 1. Kiến trúc IReportMapper Multi-Output Pipeline

```
                                   ┌──► JsonReportMapper (Machine-readable JSON schema v1.0)
                                   ├──► CsvReportMapper (Flat tabular representation)
ScanReport (Immutable Aggregate) ──┼──► ExecutiveSummaryMapper (High-level C-Suite overview text)
                                   ├──► AuditMarkdownReportMapper (Detailed markdown tables with evidence)
                                   └──► EvidenceReportMapper (Cryptographic & raw evidence trace log)
```

## 2. Đặc tả các định dạng Báo cáo Phase 4

### 2.1. Executive Summary Report (`ExecutiveSummaryMapper`)
- **Đối tượng sử dụng:** Giám đốc CNTT (CIO/IT Director), Quản lý tài sản (IT Asset Manager).
- **Nội dung trọng tâm:**
  - Tổng số phần mềm phát hiện trên hệ thống vs Số lượng phần mềm đã xác minh bản quyền (`IsVerified = true`).
  - Phân bổ tỷ lệ theo `LicenseType` (Thương mại Proprietary, Open Source Permissive, Freeware, Unknown).
  - Danh sách top các phần mềm thương mại trọng yếu có rủi ro pháp lý cao (Adobe, Microsoft Office, Autodesk, SQL Server) kèm tình trạng tuân thủ.

### 2.2. Detailed Audit Report (`AuditMarkdownReportMapper`)
- **Đối tượng sử dụng:** Kiểm toán viên bản quyền (Licensing Auditor), Chuyên viên Bảo mật & Tuân thủ.
- **Nội dung trọng tâm:**
  - Bảng chi tiết toàn bộ phần mềm kèm `ConfidenceLevel` và điểm `Confidence Score`.
  - Trích dẫn trực tiếp nguồn gốc bằng chứng (`Evidence Source Location`) và đoạn text tiêu đề giấy phép thu thập được (`RawDataSnippet`).
  - Phân định rõ các phần mềm thuộc danh sách **Backlog - Need Plugin** (phần mềm chưa có plugin chuyên biệt để hệ thống tiếp tục học hỏi và phát triển ở các Phase tiếp theo).

### 2.3. Evidence Trace Log (`EvidenceReportMapper`)
- **Đối tượng sử dụng:** Kỹ sư hệ thống (DevOps / System Engineers), Đội ngũ hỗ trợ kỹ thuật LIP.
- **Nội dung trọng tâm:**
  - Nhật ký thô (`Diagnostic Trace`) ghi nhận toàn bộ quá trình thực thi của từng Scanner và Plugin.
  - Thời gian thực thi `Performance Stopwatch (ms)` cho từng nút kiểm tra, giúp phát hiện ngay nút cổ chai (`Bottlenecks`) hoặc các plugin có hành vi đọc file quá chậm.

---

# Quality Attributes & Architectural Health Governance

Để lộ trình từ **Phase 1 đến Phase 4** diễn ra suôn sẻ, toàn bộ mã nguồn phải duy trì 5 thuộc tính chất lượng (`Quality Attributes`) tối thượng:

1. **Performance Requirement:** Thời gian hoàn thành trọn vẹn quy trình `ExecuteFullScanAsync` (quét ~150 phần mềm và chạy ~33+ plugins) phải luôn duy trì ở mức **`< 500 ms`** trên cấu hình máy tính tiêu chuẩn.
2. **Strict Immutability:** Các Aggregate Roots (`ScanReport`, `SoftwareInfo`, `LicenseCheckResult`) sau khi được sinh ra bởi Core Engine phải ở trạng thái chỉ đọc (`ReadOnly / Immutable Records`), ngăn chặn việc bất kỳ Exporter nào vô tình hay cố ý thay đổi dữ liệu kết quả trước khi xuất file.
3. **Automated Architectural Testing:** Hệ thống duy trì bộ Unit Test 100% pass (`dotnet test`), trong đó các bài test kiểm tra kiến trúc (`Dependency Enforcement Tests`) sẽ tự động đánh trượt CI/CD nếu phát hiện tầng `Domain` hoặc `Application` có chứa tham chiếu I/O hoặc ngoại vi phi chuẩn.
