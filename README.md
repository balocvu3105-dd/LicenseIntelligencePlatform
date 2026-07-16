# License Intelligence Platform

<div align="center">

![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean%20%2B%20SOLID-008080?style=for-the-badge)
![Security: Read-Only](https://img.shields.io/badge/Security-Read--Only%20%2F%20Air--Gapped-10B981?style=for-the-badge)
![Integrity: SHA-256 Checksum](https://img.shields.io/badge/Integrity-SHA--256%20Checksum-3B82F6?style=for-the-badge)
![Unit Tests: 37/37 Passed](https://img.shields.io/badge/Tests-37%2F37%20Passed-success?style=for-the-badge)

[![Download Release (.zip)](https://img.shields.io/badge/📥_TẢI_VỀ_TRỌN_BỘ_.ZIP_(14_MB)-008080?style=for-the-badge&logo=github)](https://github.com/balocvu3105-dd/LicenseIntelligencePlatform/raw/main/dist/LicenseIntelligencePlatform-v1.0.0-win-x64.zip)
[![Download CLI (.exe)](https://img.shields.io/badge/🚀_TẢI_TRỰC_TIẾP_FILE_.EXE-3B82F6?style=for-the-badge&logo=windows)](https://github.com/balocvu3105-dd/LicenseIntelligencePlatform/raw/main/dist/LicenseIntelligencePlatform-v1.0.0-win-x64/LicenseIntelligencePlatform.Presentation.Cli.exe)

</div>

---

## Overview

**License Intelligence Platform (LIP)** là hệ thống tự động hóa rà soát giấy phép bản quyền và kiểm kê tài nguyên phần mềm được xây dựng trên nền tảng **.NET 8 Clean Architecture**. Dự án cung cấp công cụ phân tích sâu danh mục phần mềm đã cài đặt trên máy trạm Windows và Linux, xác minh tuân thủ mã nguồn mở và rủi ro giấy phép thương mại một cách minh bạch, chính xác.

Hệ thống hoạt động theo nguyên tắc **Read-Only** (không ghi đè hay chỉnh sửa hệ thống) và **Air-Gapped** (xử lý dữ liệu hoàn toàn cục bộ, không gửi thông tin ra ngoài mạng), đảm bảo các yêu cầu khắt khe về bảo mật và kiểm toán tài sản công nghệ thông tin.

---

## Screenshots

<div align="center">

### CLI Execution & Summary
```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│                 LICENSE INTELLIGENCE PLATFORM (LIP) v1.0 AUDIT                   │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Total Scanned: 132 | Commercial: 42 | Open Source: 68 | Freeware: 22           │
│ Scan Status  : Completed (482 ms) | SHA-256 Checksum Signed                      │
└──────────────────────────────────────────────────────────────────────────────────┘
```
*(CLI interactive progress bar & summary dashboard placeholder)*

### Widescreen HTML Report & Multi-Sheet Excel Workbook
`[ HTML Report Screenshot Placeholder — 1920x1080 Widescreen Dashboard ]`

`[ Excel Report Screenshot Placeholder — 4 Tabs: Dashboard, Inventory, Commercial, OpenSource ]`

</div>

---

## Key Features

- **Tự động hóa rà soát giấy phép:** Phân tích và phân loại chính xác các loại giấy phép (Commercial, Open Source, Freeware, Custom) thông qua hệ sinh thái 33 sandboxed plugins chuẩn hóa.
- **Bảo mật cục bộ (Read-Only & Air-Gapped):** Mọi thao tác quét Registry, hệ thống tập tin và chữ ký Authenticode diễn ra hoàn toàn trên RAM cục bộ, không ghi hoặc thay đổi cấu hình máy trạm.
- **Chống giả mạo dữ liệu (SHA-256 & Read-Only Lock):** Báo cáo xuất ra được tự động ký mã băm SHA-256 và khóa thuộc tính chỉ đọc nhằm bảo vệ tính toàn vẹn cho hồ sơ kiểm toán.
- **Thu thập đa nguồn (Multi-Source Scanners):** Tổng hợp dữ liệu từ Windows Registry (32/64-bit), Windows Package Manager (WinGet), Linux dpkg/rpm và deep binary scanning.
- **Deduplication & Merge Engine:** Loại bỏ dữ liệu trùng lặp giữa các kiến trúc hệ thống và chuẩn hóa tên nhà xuất bản.
- **Rule Engine & Confidence Scoring:** Định lượng độ tin cậy bằng chứng rà soát theo thang điểm động và xử lý xung đột mức độ ưu tiên giữa các plugin tự động.
- **Xuất báo cáo đa định dạng:** Hỗ trợ đồng thời Bảng tính Excel (.xlsx 4 trang tính), Báo cáo trực quan HTML Widescreen, Hồ sơ Markdown (.md), CSV và JSON.
- **Mở rộng dễ dàng qua Plugin SDK:** Kiến trúc dạng plug-and-play, cho phép phát triển và tích hợp các module kiểm tra bản quyền riêng lẻ mà không sửa đổi core engine.

---

## Architecture

Hệ thống được thiết kế theo mô hình **Clean Architecture 5 tầng**, tách biệt hoàn toàn giữa Core Domain, Application logic và các chi tiết hạ tầng (Scanners, Exporters, Plugins).

```
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
│  • Scanners: WinRegistry, Winget,    │     │  • 33 Sandboxed Standard Plugins    │
│    Linux dpkg, Deep PE Scanner       │     │  • Commercial / OpenSource / Steam  │
│  • Exporters: XLSX, HTML, MD, JSON   │     │  • Heuristic & License Manifests    │
└──────────────────┬───────────────────┘     └──────────────────┬──────────────────┘
                   ▼                                            ▼
┌──────────────────────────────────────────────────────────────────────────────────┐
│                         Domain Layer (Core Abstractions)                         │
│         [ Entities | Enums | Immutable Records | Interfaces (IScanner...) ]      │
└──────────────────────────────────────────────────────────────────────────────────┘
```

---

## Quick Start

### 1. Chạy trực tiếp từ bản phát hành (Windows x64)

Bạn không cần cài đặt `.NET SDK` trên máy trạm khi sử dụng bản `Self-Contained`:

```powershell
# 1. Tải và giải nén gói phát hành
# 2. Mở Command Prompt hoặc PowerShell tại thư mục giải nén
.\LicenseIntelligencePlatform.Presentation.Cli.exe --no-pause
```
*Sau khi hoàn tất (~500ms), toàn bộ báo cáo (.xlsx, .html, .md, .csv, .json) được tự động xuất ra thư mục `reports/`.*

### 2. Tùy chọn dòng lệnh nâng cao

```powershell
# Chỉ xuất báo cáo định dạng Excel và HTML tại thư mục chỉ định
.\LicenseIntelligencePlatform.Presentation.Cli.exe --format XLSX,HTML --output "D:\AuditReports" --no-pause

# Xem danh sách toàn bộ tham số hỗ trợ
.\LicenseIntelligencePlatform.Presentation.Cli.exe --help
```

---

## Project Structure

```text
License-Intelligence-Platform-Docs/
├── docs/                                  # Tài liệu kiến trúc, đặc tả hệ thống và hướng dẫn phát triển
├── src/
│   ├── LicenseIntelligencePlatform.Domain/              # Core Domain: Entities, Enums, Immutable Records & Interfaces
│   ├── LicenseIntelligencePlatform.Application/         # Use Cases: CoreEngine, MergeEngine, RuleEngine
│   ├── LicenseIntelligencePlatform.Plugins.Standard/    # Tập hợp 33 Sandboxed Standard Plugins
│   ├── LicenseIntelligencePlatform.Infrastructure/      # Multi-source Scanners & Multi-format Exporters
│   ├── LicenseIntelligencePlatform.Presentation.Cli/    # CLI Entry Point & Dependency Injection Container
│   └── LicenseIntelligencePlatform.Tests/               # Bộ kiểm thử tự động Unit Tests (37/37 passed)
└── dist/                                  # Bản phát hành đóng gói sẵn (Windows x64 Self-Contained)
```

---

## Documentation

Mọi thông tin chi tiết về thiết kế hệ thống, thuật toán và hướng dẫn mở rộng được phân chia thành các tài liệu chuyên sâu bên trong thư mục [docs/](file:///docs):

- **[Project Vision](file:///docs/00_PROJECT_VISION.md)** – Tầm nhìn dự án, mục tiêu giải pháp và các nguyên tắc thiết kế.
- **[Architecture](file:///docs/02_ARCHITECTURE.md)** – Đặc tả Clean Architecture 5 tầng, SOLID và nguyên lý phụ thuộc.
- **[Domain Model](file:///docs/03_DOMAIN_MODEL.md)** – Mô hình thực thể nghiệp vụ (`ScanReport`, `SoftwareInfo`, `Evidence`, `ConfidenceLevel`).
- **[Security Model](file:///docs/05_SECURITY_MODEL.md)** – Mô hình cơ chế rà soát Read-Only, Air-Gapped và tính toàn vẹn SHA-256.
- **[Plugin Development Guide](file:///docs/09_PLUGIN_DEVELOPMENT_GUIDE.md)** – Hướng dẫn xây dựng và tích hợp Plugin mới theo quy chuẩn SDK v1.0.
- **[Product Roadmap](file:///docs/13_PRODUCT_ROADMAP.md)** – Kế hoạch phát triển sản phẩm và tiến trình hoàn thiện các giai đoạn.

---

## Build from Source

Yêu cầu môi trường: **.NET 8.0 SDK** trở lên.

```powershell
# 1. Clone kho lưu trữ về máy
git clone https://github.com/balocvu3105-dd/LicenseIntelligencePlatform.git
cd LicenseIntelligencePlatform

# 2. Chạy toàn bộ bộ kiểm thử tự động
dotnet test src/LicenseIntelligencePlatform.slnx -c Release

# 3. Biên dịch và chạy trực tiếp qua CLI
dotnet run --project src/LicenseIntelligencePlatform.Presentation.Cli -- --format XLSX,HTML --no-pause

# 4. Đóng gói bản chạy độc lập cho Windows x64 (Single-File Self-Contained)
dotnet publish src/LicenseIntelligencePlatform.Presentation.Cli -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o ./dist/win-x64
```

---

## License

Dự án được phát hành dưới giấy phép **MIT License**. Bạn có quyền tự do sử dụng, chỉnh sửa và phân phối cho cả mục đích cá nhân lẫn thương mại.

Bản quyền © **Bá Lộc Vũ (DynamiteV)**.
