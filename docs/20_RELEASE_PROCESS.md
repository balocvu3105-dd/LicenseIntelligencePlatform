# 20_RELEASE_PROCESS.md

# License Intelligence Platform (LIP)

## Enterprise Release Process & Verification Blueprint

Version: 1.0

Status: Stable (Phase 0 – Phase 4 Completed)

Author: DynamiteV

---

# 1. Purpose & Release Philosophy

Tài liệu **Enterprise Release Process & Verification Blueprint** thiết lập quy trình thẩm định, đóng gói, và phát hành chuẩn xác (`Production Release Gate`) cho **License Intelligence Platform (LIP)**. Mọi phiên bản được đóng dấu chính thức (`Release Candidate -> Production Release`) bắt buộc phải vượt qua 100% các tiêu chí kiểm định khắt khe dưới đây nhằm bảo đảm không một dòng lỗi ngoại lệ hay lỗ hổng bảo mật nào có thể lọt ra môi trường máy trạm của khách hàng.

---

# 2. Pre-Release Verification Gate (Quy trình Thẩm định 3 Bước)

Release Manager hoặc Hệ thống CI/CD Pipeline phải thực hiện tuần tự 3 bước kiểm tra và tích vào 100% các hạng mục sau trước khi tạo Release Tag:

## Bước 1: Clean Build & Unit Test Verification (`Automated Gate`)
- [x] **Full Release Build Check:** Thực thi lệnh biên dịch tối ưu hóa:
  ```powershell
  dotnet clean src/LicenseIntelligencePlatform.slnx
  dotnet build src/LicenseIntelligencePlatform.slnx -c Release
  ```
  *Yêu cầu:* Đạt `0 Errors, 0 Warnings`.
- [x] **100% Test Suite Verification:** Thực thi trọn bộ kiểm thử tự động:
  ```powershell
  dotnet test src/LicenseIntelligencePlatform.slnx -c Release -v normal
  ```
  *Yêu cầu:* Toàn bộ **36/36 unit/integration tests** (`ScannerTests`, `StandardPluginsTests`, `AccuracyVerificationTests`, `ExportersTests`) phải đạt `Passed!` (Tỷ lệ pass 100%, không có test nào Skipped hay Failed).

## Bước 2: Security & Read-Only Invariance Audit (`InfoSec Gate`)
- [x] **Zero Registry Write Verification:** Kiểm tra toàn bộ mã nguồn `Infrastructure` và `Plugins.Standard`, đảm bảo tuyệt đối không có sự xuất hiện của `RegistryKey.SetValue()`, `CreateSubKey()`, hoặc `DeleteSubKey()`.
- [x] **Zero Network Capability Check:** Kiểm tra Dependency Injection Container (`Program.cs`) và các class, xác nhận không đăng ký hay khởi tạo bất kỳ `HttpClient`, `TcpClient`, `Socket`, hoặc `WebRequest` nào ra mạng.
- [x] **Sandboxed Exception Shielding Audit:** Kiểm tra 100% các Plugin mới đều có khối `try/catch` bọc bên ngoài `CheckLicenseAsync()` và tuân thủ `CancellationToken 5000ms Timeout Guard`.

## Bước 3: Execution Benchmark Target Check (`Performance Gate`)
- [x] **Scan Speed Telemetry:** Chạy thử trên máy chủ tham chiếu `.NET 8 LTS 64-bit`:
  ```powershell
  dotnet run --project src/LicenseIntelligencePlatform.Presentation.Cli -c Release --format BOTH --output "reports/release_test"
  ```
  *Yêu cầu:* Tổng thời gian thực thi tiến trình (`Total Elapsed Time`) phải **`< 300 ms`** cho `< 150 software packages`, và `Memory Delta Footprint` phải **`< 15 MB`**.

---

# 3. Phase 4 Multi-Output Artifacts Verification

Sau khi tiến trình kiểm kê hoàn tất với tham số `--format BOTH`, Release Engineer phải mở thư mục `--output` (`reports/release_test/`) và xác minh sự hiện diện trọn vẹn của **8 Production Artifacts chuẩn Phase 4 v1.0**:

| Tên Artifact Đầu ra (`Output Artifact File`) | Định dạng (`Format`) | Mục đích & Đối tượng Nhu cầu | Trạng thái Thẩm định |
| :--- | :--- | :--- | :---: |
| **`license_report_<scanId>.html`** | Standalone HTML5 / PDF | Báo cáo trực quan Dark Mode (`#0f172a`), thẻ Card thống kê, cho C-Suite / CIO. | ✅ **Required** |
| **`audit_report_<scanId>.md`** | GitHub Markdown | Báo cáo kiểm toán pháp lý trích dẫn chi tiết từng dòng bằng chứng gốc. | ✅ **Required** |
| **`statistics_report_<scanId>.json`** | Structured JSON | Báo cáo số liệu BI Telemetry (tỷ lệ nhà phát hành, mili-giây từng plugin). | ✅ **Required** |
| **`license_report_<scanId>.csv`** | Comma-Separated CSV | Bảng biểu phẳng cho IT Admin mở trên Microsoft Excel. | ✅ **Required** |
| **`license_report_<scanId>.json`** | Standard JSON | Cấu trúc dữ liệu đầy đủ cho hệ thống tích hợp tự động hóa. | ✅ **Required** |
| **`executive_summary_<scanId>.txt`** | Plain Text | Văn bản tóm tắt nhanh cho Giám đốc hạ tầng. | ✅ **Required** |
| **`evidence_report_<scanId>.txt`** | Plain Text | Nhật ký truy vết bằng chứng thô phục vụ tra cứu kỹ thuật. | ✅ **Required** |
| **`backlog_need_plugins.json`** | JSON Array | Danh sách phần mềm Unknown thu hoạch cho vòng lặp phát triển Phase sau. | ✅ **Required** |

---

# 4. Packaging & Self-Contained Deployment Distribution

Để bảo đảm tính khả di (`Portability`) cực đại trong các môi trường doanh nghiệp không có mạng (`Air-Gapped / Offline`), bản phát hành chính thức được đóng gói dưới dạng **Self-Contained Single File Executable**:

```powershell
# Đóng gói portable file thực thi duy nhất cho Windows x64 (không cần cài trước .NET Runtime)
dotnet publish src/LicenseIntelligencePlatform.Presentation.Cli `
    -c Release `
    -r win-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true `
    -o "./dist/win-x64"
```

### Kiểm tra gói phát hành (`Distribution Verification`):
1. Vào thư mục `dist/win-x64/` (hoặc `dist/LicenseIntelligencePlatform-v1.0.0-win-x64/`).
2. Xác nhận sự tồn tại của file `LicenseIntelligencePlatform.Presentation.Cli.exe` (kích thước khoảng `60 MB - 80 MB` vì đã nhúng trọn bộ `.NET 8 Runtime` và `33 Plugins`) cùng tài liệu `HUONG_DAN_SU_DUNG.md`.
3. Copy sang một máy trạm hoàn toàn trắng (`Clean Windows 11 / Windows Server VM không có .NET SDK`) và chạy lệnh `.\LicenseIntelligencePlatform.Presentation.Cli.exe --format BOTH` để xác nhận hoạt động hoàn hảo!

> [!IMPORTANT]
> **Quy trình Làm sạch Dữ liệu Trước Đóng Gói (`Pre-Distribution Data Clean Check`):**
> Trước khi nén thành file `.zip` phát hành cho người dùng (`dist/LicenseIntelligencePlatform-v1.0.0-win-x64.zip`) hoặc đẩy lên Git/Release Tag, **bắt buộc phải xóa toàn bộ dữ liệu kiểm kê thử nghiệm** sinh ra trên máy trạm của lập trình viên:
> - Xóa sạch các file nhật ký trong thư mục `logs/` (`application.log`, `audit.log`, `performance.log`, `error.log`).
> - Xóa sạch toàn bộ báo cáo trong thư mục `reports/` (`*.html`, `*.audit`, `*.csv`, `*.json`, `*.xlsx`, `*.txt`).
> - Chỉ để lại các thư mục `logs/` và `reports/` rỗng kèm file `.keep` để giữ cấu trúc thư mục sạch sẽ, bảo đảm người dùng khi tải file về **hoàn toàn không chứa bất kỳ dữ liệu hay vết tích kiểm kê nội bộ nào** của đội ngũ phát triển!


---

# 5. Release Tagging & Semantic Versioning

Khi tất cả các tiêu chí trên được tích `Pass`, tiến hành gắn nhãn (`Git Tagging`):
```powershell
git tag -a v1.0.0 -m "Release v1.0.0 - Stable Production Build (Phase 0 - Phase 4 Completed)"
git push origin v1.0.0
```
Mọi bản vá lỗi (`Patch`) sau này sẽ tăng số cuối (`v1.0.1`), mở rộng tính năng không phá vỡ cũ tăng số giữa (`v1.1.0`), và thay đổi kiến trúc lớn sẽ tăng số đầu (`v2.0.0`).
