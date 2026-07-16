# 17_GLOSSARY.md

# License Intelligence Platform (LIP)

## Master Glossary & Domain-Driven Design Ubiquitous Language

Version: 1.0

Status: Stable (Phase 0 – Phase 4 Completed)

Author: DynamiteV

---

# 1. Purpose & Ubiquitous Language Philosophy

Tài liệu **Master Glossary & Domain-Driven Design Ubiquitous Language** là từ điển chuẩn hóa ngôn ngữ giao tiếp (`Domain Vocabulary`) cho toàn bộ dự án **License Intelligence Platform (LIP)**. Theo nguyên lý DDD, mọi Lập trình viên, Kiến trúc sư, Chuyên viên Tuân thủ (`Compliance Officers`), và **AI Coding Assistants** buộc phải sử dụng chính xác các thuật ngữ này cả trong các cuộc thảo luận, tài liệu thiết kế, và ngay bên trong tên biến, tên class của mã nguồn C#.

> *"Không bao giờ gọi một gói phần mềm là `Program` hay `App` phi tiêu chuẩn; không gọi kết luận bản quyền là `Result` chung chung; và không nhầm lẫn giữa `Scanner` (chỉ đọc dữ liệu thô) và `LicensePlugin` (phân tích nghiệp vụ)."*

---

# 2. Core Domain Vocabulary (Thuật ngữ Lõi Hệ thống)

| Thuật ngữ DDD (`Domain Term`) | Định nghĩa Chính xác trong Ngữ cảnh LIP | Ví dụ Tham chiếu C# / Kiến trúc |
| :--- | :--- | :--- |
| **`SoftwareInfo`** | Thực thể (`Entity/Record`) đại diện cho một gói phần mềm, ứng dụng, hoặc thư viện thực thi được phát hiện trên hệ thống với các trường chuẩn hóa: `Name`, `Version`, `Publisher`, `InstallLocation`, `InstallDate`. | `public sealed record SoftwareInfo(...)` trong tầng `Domain`. |
| **`IScanner`** | Thành phần thuộc tầng `Infrastructure` chịu trách nhiệm duy nhất là truy xuất hệ thống (Registry, WinGet, DPKG, File System) ở chế độ **100% Read-Only** để trả về danh sách `SoftwareInfo` thô. | `WindowsRegistryScanner`, `LinuxPackageScanner`, `CompositeScanner`. |
| **`CompositeScanner`** | Bộ quét tổng hợp (`Orchestrator Scanner`) chạy đồng bộ tất cả các `IScanner` hợp lệ trên hệ điều hành hiện tại để gom dữ liệu kiểm kê toàn diện. | `public class CompositeScanner : IScanner` trong `Infrastructure`. |
| **`ILicensePlugin`** | Thành phần thuộc tầng `Plugins.Standard` chịu trách nhiệm đánh giá và chẩn đoán tình trạng bản quyền của một (hoặc một nhóm) phần mềm cụ thể. | `AdobeCreativeCloudPlugin`, `MicrosoftOfficeCommercialPlugin`. |
| **`PluginManifest`** | Siêu dữ liệu mô tả (`Metadata Record`) của mỗi Plugin, khai báo: `PluginId`, `PluginName`, `PluginVersion`, `Author`, `Priority`, và `MinSdkVersion`. | `public sealed record PluginManifest(...)` |
| **`PluginPriority`** | Mức độ ưu tiên thực thi trong `Rule Engine 2.0`: `CommercialSpecific (100)`, `Ecosystem (75)`, `Heuristic (50)`, `Generic (25)`. | Thang điểm ưu tiên giải quyết xung đột khi nhiều plugin cùng quét 1 phần mềm. |
| **`Evidence`** | Bằng chứng kỹ thuật vật lý (`Physical Evidence Record`) như đường dẫn file `LICENSE`, header `.lic`, chữ ký số `Authenticode`, hoặc khóa registry chứng minh căn cứ của kết luận. | `public sealed record Evidence(EvidenceId, EvidenceType, ...)` |
| **`ConfidenceLevel`** | Mức độ tự tin định lượng của kết luận (`Confidence Engine 100-pt`): `None (0)`, `Low (1)`, `Medium (2)`, `High (3)`, `Verified (4)`. | Trạng thái `Verified` chỉ đạt được khi tổng trọng số bằng chứng $\ge 70\text{ pts}$. |
| **`LicenseCheckResult`** | Kết quả phân tích bất biến trả về từ một Plugin, bao gồm `PluginId`, `Software`, `LicenseType`, `ConfidenceLevel`, và danh sách `Evidences`. | `LicenseCheckResult.CreateVerified(...)`, `CreateErrorResult(...)`. |
| **`ScanReport`** | Aggregate Root chính đại diện cho toàn bộ kết quả của một phiên quét máy trạm, chứa: metadata máy chủ (`HostName`, `OSDescription`), danh sách kết quả (`Results`), và thống kê. | `public sealed record ScanReport(...)` trong `Domain`. |
| **`SoftwareMergeEngine`** | Engine thuộc tầng `Application` chịu trách nhiệm chuẩn hóa tên nhà phát hành (`Publisher Sanitization`) và gộp các bản ghi trùng lặp (`Deduplication`) theo GUID `ProductCode`. | `public class SoftwareMergeEngine : ISoftwareMergeEngine` |
| **`PluginCompatibilityValidator`** | Bộ kiểm tra tính tương thích, tự động lọc bỏ khỏi RAM các Plugin có `MinSdkVersion` cao hơn `CurrentSdkVersion` của hệ thống. | Bảo vệ sự ổn định cho `CoreEngine` trước DLL của bên thứ ba. |
| **`IReportMapper`** | Giao diện định dạng xuất dữ liệu Phase 4 thuộc `Infrastructure`, biến `ScanReport` thành các file báo cáo điều hành hoặc kỹ thuật chuyên sâu. | `AuditReportMapper`, `HtmlReportMapper`, `StatisticsReportMapper`. |
| **`Need Plugin Backlog`** | File `backlog_need_plugins.json` tự động gom các phần mềm có `LicenseType = Unknown` để phục vụ vòng lặp mở rộng Plugin ở các phiên tiếp theo. | Vòng lặp thu hoạch tri thức (`Continual Improvement Loop`). |

---

# 3. License Types Specification (`LicenseType Enum`)

| Giá trị Liệt kê (`Enum Value`) | Ý nghĩa & Tiêu chí Phân loại chuẩn LIP | Các Ví dụ Tiêu biểu |
| :--- | :--- | :--- |
| **`Commercial`** | Phần mềm thương mại yêu cầu mua giấy phép bản quyền (`Proprietary / Paid / Enterprise Subscription`). Rủi ro phạt tuân thủ cao nếu sử dụng không phép. | `Microsoft Office 365`, `Adobe Photoshop CC`, `Autodesk AutoCAD`, `VMware Workstation Pro`, `Microsoft SQL Server`. |
| **`OpenSource`** | Phần mềm nguồn mở hợp pháp theo các giấy phép được công nhận (`MIT, Apache 2.0, GPL, BSD`). Cho phép tự do sử dụng và kiểm toán mã nguồn. | `Git`, `Node.js Runtime`, `VLC Media Player`, `OBS Studio`, `Python Runtime`. |
| **`Freeware`** | Phần mềm miễn phí sử dụng (cho cá nhân hoặc thương mại tùy điều khoản) nhưng không công bố mã nguồn mở (`Closed-Source Free`). | `Microsoft Edge`, `Google Chrome`, `WinRAR Trial/Freeware Pattern`. |
| **`Internal`** | Phần mềm nội bộ do chính doanh nghiệp hoặc tổ chức tự phát triển, không lưu hành trên thị trường công cộng. | `Enterprise ERP Client`, `Internal VPN Connector Tool`. |
| **`Unknown`** | Phần mềm chưa xác định được tình trạng bản quyền (`Confidence = None`) do thiếu bằng chứng hoặc chưa có Plugin hỗ trợ. | Các ứng dụng đặc thù của bên thứ ba, tự động ghi nhận vào `Backlog`. |
