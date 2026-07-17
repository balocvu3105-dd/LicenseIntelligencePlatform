# License Intelligence Platform (LIP)

<div align="center">

![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean%20%2B%20SOLID-008080?style=for-the-badge)
![Security: Read-Only](https://img.shields.io/badge/Security-Read--Only%20%2F%20Air--Gapped-10B981?style=for-the-badge)
![Integrity: SHA-256 Checksum](https://img.shields.io/badge/Integrity-SHA--256%20Checksum-3B82F6?style=for-the-badge)
![Unit Tests: 49/49 Passed](https://img.shields.io/badge/Tests-49%2F49%20Passed-success?style=for-the-badge)

[![Download Release (.zip)](https://img.shields.io/badge/Download_Release_.ZIP_(14_MB)-008080?style=for-the-badge&logo=github)](https://github.com/balocvu3105-dd/LicenseIntelligencePlatform/raw/main/dist/LicenseIntelligencePlatform-v1.0.0-win-x64.zip)
[![Download CLI (.exe)](https://img.shields.io/badge/Download_CLI_.EXE-3B82F6?style=for-the-badge&logo=windows)](https://github.com/balocvu3105-dd/LicenseIntelligencePlatform/raw/main/dist/LicenseIntelligencePlatform-v1.0.0-win-x64/LicenseIntelligencePlatform.Presentation.Cli.exe)

</div>

---

## Executive Summary

**License Intelligence Platform (LIP)** là engine kiểm kê tài nguyên phần mềm (`Software Asset Management - SAM`) và rà soát tuân thủ bản quyền tự động dành cho môi trường doanh nghiệp, được xây dựng trên kiến trúc **.NET 8 Clean Architecture & Domain-Driven Design (DDD)**.

Dự án giải quyết bài toán kiểm toán tài sản công nghệ thông tin phức tạp bằng giải pháp xử lý cục bộ, tốc độ cao mà không phụ thuộc vào các Agent thường trú nặng nề hay dịch vụ đám mây bên thứ ba. Hệ thống tuân thủ 2 nguyên tắc thiết kế cốt lõi:
- **100% Read-Only & Air-Gapped Execution:** Mọi thao tác quét dữ liệu từ Registry, WMI, ACPI BIOS, hệ thống tập tin và chữ ký số `Authenticode` diễn ra hoàn toàn trên RAM cục bộ. Hệ thống tuyệt đối không ghi đè, không thay đổi cấu hình hệ điều hành và không tạo ra bất kỳ kết nối mạng ngoại vi nào (`Zero Network Telemetry`), đáp ứng tiêu chuẩn bảo mật khắt khe nhất của các tổ chức tài chính và chính phủ.
- **Deterministic & Audit-Ready Output:** Dữ liệu thu thập được chuẩn hóa, đánh giá độ tin cậy qua thuật toán chấm điểm động (`Confidence Scoring Engine 0–100`) và tự động đóng gói sang nhiều định dạng báo cáo (`XLSX`, `HTML`, `Markdown`, `JSON`) kèm chữ ký mã băm **SHA-256 Checksum** nhằm ngăn chặn giả mạo hồ sơ kiểm toán.

---

## Architectural & Technical Highlights

### 1. Clean Architecture & Domain-Driven Design (DDD)
Hệ thống áp dụng mô hình phân tách 5 tầng nghiêm ngặt (`Domain`, `Application`, `Plugins`, `Infrastructure`, `Presentation.Cli`), tuân thủ nguyên lý đảo ngược phụ thuộc (`Dependency Inversion Principle`):
- **Domain Layer:** Lõi nghiệp vụ thuần túy không chứa dependency ngoại vi hay logic I/O. Quản lý các thực thể bất biến (`Immutable Records`), định nghĩa các hợp đồng giao diện (`IScanner`, `ISoftwarePlugin`, `IReportExporter`) và các quy tắc kiểm toán.
- **Application Layer:** Đóng gói các Use Case (`CoreEngine`), bộ gộp dữ liệu (`SoftwareMergeEngine`), và bộ xử lý quy tắc (`RuleEngine`).
- **Infrastructure Layer:** Triển khai các Scanner giao tiếp với hệ điều hành (WMI, Registry, PE Parser, Linux `dpkg`/`rpm`) và bộ tạo báo cáo đa định dạng.

### 2. Multi-Source Discovery & Deduplication Engine (`SoftwareMergeEngine`)
- **Quét đa kênh:** Tổng hợp thông tin phần mềm từ WMI (`SoftwareLicensingProduct`), Windows Registry (32/64-bit hives), Windows Package Manager (`WinGet`), Linux (`dpkg`/`rpm`) và phân tích sâu cấu trúc nhị phân (`PE/Portable Executable Analysis`).
- **Thuật toán Deduplication:** Giải quyết triệt để tình trạng trùng lặp gói phần mềm giữa các kiến trúc hệ thống (x86/x64) hoặc các phương thức cài đặt khác nhau thông qua cơ chế chuẩn hóa tên nhà xuất bản (`Publisher Sanitization`) và đối chiếu chữ ký tập tin.

### 3. Sandboxed Plugin Ecosystem & Confidence Scoring Engine
- **Kiến trúc Plugin chuẩn hóa (`34 Sandboxed Standard Plugins`):** Phân loại tự động giấy phép (`Commercial`, `Open Source`, `Freeware`, `Custom`) dựa trên tập ký hiệu (`Signatures`) và bằng chứng thu thập.
- **Rule Engine & Weighted Confidence Score (`0–100`):** Định lượng mức độ tin cậy cho từng bằng chứng rà soát (`Evidence`), tự động xử lý và phân xử xung đột khi nhiều Scanner hoặc Plugin cùng nhận diện một gói phần mềm.

### 4. Deep OS Compliance & KMS Anomaly Detection
- **Rà soát 8 điểm trọng yếu:** Quét chuyên sâu WMI, Registry SPP/SLMGR, khóa ACPI MSDM BIOS OEM Key, thư mục hệ thống `C:\Windows\System32`, tiến trình bộ nhớ (`RAM Processes`), Scheduled Tasks, dịch vụ Windows (`osppsvc`, `sppsvc`) và tập tin `hosts`.
- **Nhận diện giả lập KMS trái phép:** Phát hiện chính xác các bộ giả lập máy chủ KMS nội bộ (`KMSpico`, `KMSAuto`, `KMS_VL_ALL`, loopback `127.0.0.1:1688`, `0.0.0.0:1688`) cũng như chữ ký số lạ/chưa được xác thực.
- **Dynamic Risk Scoring:** Hệ thống tự động phân loại mức độ rủi ro tuân thủ (`Clean/Legitimate`, `Anomalous/Grace Period`, `Critical Piracy/KMS Crack Detected`) và ghim kết quả rà soát hệ điều hành ở hàng đầu tiên trong mọi báo cáo đầu ra.

### 5. Fault Isolation & Timeout Guards
- Toàn bộ các Plugin và Scanner I/O được bao bọc trong cơ chế cô lập (`Sandboxed Execution`) kết hợp giới hạn thời gian (`5000ms Timeout Guard`), ngăn chặn triệt để rủi ro rò rỉ bộ nhớ hay treo hệ thống khi gặp tập tin hỏng, bị khóa hoặc truy xuất I/O chậm.

---

## System Architecture

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│                          Presentation Layer (CLI / API)                          │
│         [ Program.cs | CliOptions | Summary Rendering | DI Container ]           │
└────────────────────────────────────────┬─────────────────────────────────────────┘
                                         ▼
┌──────────────────────────────────────────────────────────────────────────────────┐
│                      Application Layer (Use Cases & Engines)                     │
│         [ CoreEngine | SoftwareMergeEngine | Rule Engine | Confidence ]          │
└──────────────────┬─────────────────────────────────────────────┬─────────────────┘
                   ▼                                             ▼
┌──────────────────────────────────────┐     ┌─────────────────────────────────────┐
│         Infrastructure Layer         │     │           Plugins Layer             │
│  • Scanners: WinOS Activation (SPP), │     │  • 34 Sandboxed Standard Plugins    │
│    WinRegistry, Linux dpkg, Deep PE  │     │  • Commercial / OpenSource / Steam  │
│  • Exporters: XLSX, HTML, MD, JSON   │     │  • Heuristic & License Manifests    │
└──────────────────┬───────────────────┘     └──────────────────┬──────────────────┘
                   ▼                                            ▼
┌──────────────────────────────────────────────────────────────────────────────────┐
│                         Domain Layer (Core Abstractions)                         │
│         [ Entities | Enums | Immutable Records | Interfaces (IScanner...) ]      │
└──────────────────────────────────────────────────────────────────────────────────┘
```

---

## Engineering Rigor & Quality Standards

Dự án được xây dựng với các chuẩn mực cao nhất của công nghệ phần mềm hiện đại (`Software Engineering Excellence`), đảm bảo độ tin cậy tuyệt đối khi triển khai trong môi trường doanh nghiệp:

- **Architectural Decision Records (ADRs):** Toàn bộ các quyết định thiết kế quan trọng (`Design Trade-offs & Rationales`) đều được lưu trữ minh bạch trong thư mục [`docs/adr/`](file:///docs/adr) (`ADR-001` đến `ADR-010`), bao gồm kiến trúc Plugin, cơ chế ánh xạ dữ liệu, logic chấm điểm tin cậy (`Confidence Score`), và cơ chế structured logging.
- **Automated Verification:** Bộ kiểm thử tự động đạt **49/49 Unit Tests Passed**, bao phủ toàn bộ logic lõi (`CoreEngine`), bộ gộp dữ liệu (`SoftwareMergeEngine`), xử lý quy tắc (`RuleEngine`), và bộ định dạng báo cáo.
- **Comprehensive Technical Documentation:** Hệ thống 20 tài liệu kỹ thuật chuyên sâu ([`docs/`](file:///docs)) mô tả chi tiết tầm nhìn, mô hình rủi ro bảo mật (`Security Model`), chuẩn viết mã (`Coding Standards`) và đặc tả SDK cho nhà phát triển Plugin (`Plugin Development Guide`).
- **DevOps & CI/CD Readiness:** Hỗ trợ xuất bản dưới dạng tập tin thực thi độc lập (`Single-File Self-Contained Binary`) cho Windows x64. Có thể dễ dàng nhúng vào các script quản trị hạ tầng, đường ống CI/CD, hoặc tích hợp cùng SIEM/SOC mà không yêu cầu cài đặt `.NET Runtime` trên máy client.

---

## Quick Start & CLI Usage

### 1. Chạy trực tiếp từ gói phát hành (Self-Contained Binary)
Không yêu cầu cài đặt `.NET SDK` hay dependencies hệ thống:

```powershell
# Mở PowerShell tại thư mục chứa file thực thi
.\LicenseIntelligencePlatform.Presentation.Cli.exe --no-pause
```
*Hệ thống tự động quét, tổng hợp dữ liệu và xuất trọn bộ báo cáo (`.xlsx`, `.html`, `.md`, `.csv`, `.json`) vào thư mục `reports/` chỉ trong ~500ms.*

### 2. Các tham số dòng lệnh quan trọng
```powershell
# Xem toàn bộ tham số hỗ trợ
.\LicenseIntelligencePlatform.Presentation.Cli.exe --help

# Chỉ định định dạng xuất cụ thể và thư mục lưu trữ báo cáo
.\LicenseIntelligencePlatform.Presentation.Cli.exe --format XLSX,HTML,JSON --output "audit_results/2026_Q3" --no-pause

# Chạy ở chế độ xem nhanh tóm tắt trên CLI
.\LicenseIntelligencePlatform.Presentation.Cli.exe --summary-only
```

---

## Multi-Format Report Exporters

LIP tích hợp 4 bộ xuất dữ liệu độc lập, được thiết kế tối ưu hóa cho từng giai đoạn và đối tượng trong quy trình kiểm toán tài sản CNTT:

| Định dạng | Engine Exporter | Mục tiêu & Đặc điểm Kỹ thuật |
| :--- | :--- | :--- |
| **HTML** | `HtmlReportMapper` | **Self-Contained Executive Dashboard:** Báo cáo trực quan trang đơn độc lập (`Dark Mode`), không phụ thuộc thư viện ngoại vi/Internet. Hiển thị thẻ chỉ số tổng quan (`Summary Cards`) và bảng dữ liệu đầy đủ, hỗ trợ ngắt dòng chuỗi SHA/đường dẫn và in ấn sang định dạng PDF phục vụ giải trình cấp quản lý. |
| **XLSX** | `ExcelReportMapper` | **Standardized Audit Workbook:** Bảng tính Excel phân vùng 4 trang tính (*Executive Summary*, *Full Inventory*, *Commercial Action Required*, và *Open Source Compliance*). Các cột dữ liệu được tự động căn chỉnh lề và tối ưu độ rộng (`Column Auto-Fit`), lưu trữ rà soát Windows OS ở hàng đầu tiên. |
| **Markdown** | `MarkdownReportMapper`| **Git-Ready Audit Trail:** Định dạng chuẩn **GitHub Flavored Markdown (`.md`)**, lưu trữ toàn bộ chuỗi bằng chứng (`Evidence Traceability`) cho từng gói phần mềm. Phù hợp để nhúng trực tiếp vào Git Wiki, Pull Requests hoặc hệ thống tài liệu nội bộ. |
| **JSON** | `JsonReportMapper` | **Structured Raw Data:** Dữ liệu thô cấu trúc hóa (`CamelCase JSON`) kèm mã băm **SHA-256 Checksum** tại header. Tối ưu cho việc tích hợp vào đường ống CI/CD, SIEM/SOC, hoặc gọi tự động qua các API quản lý tài sản (ITAM). |

---

## Project Structure

```text
License-Intelligence-Platform-Docs/
├── docs/                                              # Tài liệu kỹ thuật, ADRs và đặc tả kiến trúc
│   └── adr/                                           # 10 Architectural Decision Records (ADR-001 -> ADR-010)
├── src/
│   ├── LicenseIntelligencePlatform.Domain/            # Core Domain Layer: Entities, Enums, Interfaces & Immutable Records
│   ├── LicenseIntelligencePlatform.Application/       # Application Layer: CoreEngine, MergeEngine & RuleEngine
│   ├── LicenseIntelligencePlatform.Plugins.Standard/  # Plugins Layer: 34 Sandboxed Standard Plugins
│   ├── LicenseIntelligencePlatform.Infrastructure/    # Infrastructure Layer: Scanners & Multi-format Exporters
│   ├── LicenseIntelligencePlatform.Presentation.Cli/  # Presentation Layer: CLI Entry Point & DI Container
│   └── LicenseIntelligencePlatform.Tests/             # Unit Tests Suite (49/49 Passed)
└── dist/                                              # Bản phát hành đóng gói sẵn (Windows x64 Self-Contained Binary)
```

---

## Core Documentation & ADR Reference

Toàn bộ tài liệu thiết kế hệ thống được phân vùng khoa học trong thư mục [`docs/`](file:///docs):

- **[00_PROJECT_VISION.md](file:///docs/00_PROJECT_VISION.md)** – Tầm nhìn dự án, bài toán kiểm toán bản quyền và nguyên tắc thiết kế cốt lõi.
- **[02_ARCHITECTURE.md](file:///docs/02_ARCHITECTURE.md)** – Đặc tả Clean Architecture 5 tầng, nguyên lý SOLID và sơ đồ phụ thuộc.
- **[03_DOMAIN_MODEL.md](file:///docs/03_DOMAIN_MODEL.md)** – Mô hình thực thể nghiệp vụ (`ScanReport`, `SoftwareInfo`, `Evidence`, `ConfidenceLevel`).
- **[05_SECURITY_MODEL.md](file:///docs/05_SECURITY_MODEL.md)** – Cơ chế bảo mật Air-Gapped, rà soát Read-Only và xác thực tính toàn vẹn SHA-256.
- **[09_PLUGIN_DEVELOPMENT_GUIDE.md](file:///docs/09_PLUGIN_DEVELOPMENT_GUIDE.md)** – Đặc tả SDK và quy chuẩn tích hợp Plugin mới vào hệ sinh thái LIP.
- **[15_ADR_INDEX.md](file:///docs/15_ADR_INDEX.md)** – Chỉ mục tổng hợp các Quyết định Kiến trúc (`Architectural Decision Records`).

---

## Build from Source

Yêu cầu môi trường: **.NET 8.0 SDK** trở lên.

```powershell
# 1. Clone kho lưu trữ về máy cục bộ
git clone https://github.com/balocvu3105-dd/LicenseIntelligencePlatform.git
cd LicenseIntelligencePlatform

# 2. Chạy toàn bộ bộ kiểm thử tự động (Unit Tests Suite)
dotnet test src/LicenseIntelligencePlatform.slnx -c Release

# 3. Biên dịch và thực thi trực tiếp qua CLI
dotnet run --project src/LicenseIntelligencePlatform.Presentation.Cli -- --format XLSX,HTML --no-pause

# 4. Đóng gói bản thực thi độc lập cho Windows x64 (Single-File Self-Contained)
dotnet publish src/LicenseIntelligencePlatform.Presentation.Cli -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o ./dist/win-x64
```

---

## Future Roadmap & Planned Enhancements

Hệ thống hiện tại đã hoàn tất Giai đoạn 0–4 (`Phase 0–4 Completed` với **49/49 Unit Tests Passed**). Các định hướng phát triển kiến trúc tiếp theo (`Phase 5 & Phase 6`) tập trung vào khả năng mở rộng tích hợp sâu:

- **Deep Office & Adobe Scanners:** Mở rộng module phân tích WMI `OfficeSoftwareProtectionProduct` và SPP Registry, định danh chính xác giấy phép Office 365, Office LTSC Pro Plus và phát hiện bẻ khóa KMS trái phép.
- **Asymmetric Digital Signatures (RSA/Ed25519):** Ký số báo cáo đầu ra bằng khóa riêng tư (`--sign-key <key.pem>`), cho phép hệ thống kiểm toán xác minh tính nguyên bản của dữ liệu qua Public Key.
- **SBOM Exporters (CycloneDX / SPDX):** Xuất cấu trúc dữ liệu chuẩn Software Bill of Materials (SBOM) quốc tế, phục vụ tích hợp trực tiếp với hệ thống SIEM/SOC (`ServiceNow`, `Microsoft Sentinel`, `Dependency-Track`).
- **Snapshot Diff Engine:** Bổ sung cờ `--compare <previous-report.json>` tự động đối chiếu các lần quét liên tiếp để phát hiện biến động cài đặt phần mềm và rủi ro thay đổi giấy phép bất thường.

---

## License

Dự án được phát hành dưới giấy phép **MIT License**.

Bản quyền © **Bá Lộc Vũ (DynamiteV)**.
