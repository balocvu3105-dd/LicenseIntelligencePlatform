# License Intelligence Platform

<div align="center">

![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean%20%2B%20SOLID-008080?style=for-the-badge)
![Security: Read-Only](https://img.shields.io/badge/Security-Read--Only%20%2F%20Air--Gapped-10B981?style=for-the-badge)
![Integrity: SHA-256 Checksum](https://img.shields.io/badge/Integrity-SHA--256%20Checksum-3B82F6?style=for-the-badge)
![Unit Tests: 49/49 Passed](https://img.shields.io/badge/Tests-49%2F49%20Passed-success?style=for-the-badge)

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
<img width="1825" height="792" alt="image" src="https://github.com/user-attachments/assets/85fc1e67-a562-47ef-aa60-494c31c1990e" />

*(CLI interactive progress bar & summary dashboard placeholder)*

### Widescreen HTML Report & Multi-Sheet Excel Workbook
- **🎨 Executive HTML Dashboard (`2800px+ Super Widescreen`)**: Giao diện Dark Mode sang trọng, hiển thị thẻ chỉ số tổng quan và bảng dữ liệu siêu rộng. Tự động ngắt dòng thông minh (`break-all`), đảm bảo các chuỗi phiên bản (`Version`), mã commit SHA hay đường dẫn thư mục không bao giờ bị ngắt lẻ ký tự.
- **📊 Goldilocks Deluxe Excel Workbook (`4 Tabs + Center/Center Alignment`)**: Toàn bộ 12 cột (`Software Package`, `Version`, `Install Path`, `Last Modified`, `Evidence`...) được thiết lập **căn chính giữa theo cả 2 trục ngang và dọc (`Center / Center`)** kết hợp lề đệm dọc (`+14pt cushion capped at 85pt`) và chiều rộng mở rộng tối đa (`Width = 28 — 140`), tuyệt đối không xén chân chữ hay phình dòng.

</div>

---

## Key Features

- **🛡️ Module Kiểm Toán Bản Quyền Windows OS Chuyên Sâu (8-Location Deep Scan & Risk Scoring Engine):**
  - **Quét 8 khu vực trọng yếu:** Kiểm tra toàn bộ dịch vụ WMI `SoftwareLicensingProduct`, Registry SPP/SLMGR `SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform`, ACPI MSDM BIOS OEM Key, thư mục hệ thống `C:\Windows\System32`, tiến trình bộ nhớ, Scheduled Tasks, hosts file (`C:\Windows\System32\drivers\etc\hosts`) và Windows Services (`osppsvc`, `sppsvc`).
  - **Phát hiện và cảnh báo KMS Lậu / Emulators:** Nhận dạng chính xác các bộ giả lập máy chủ KMS bất hợp pháp (`KMSpico`, `KMSAuto`, `KMS_VL_ALL`, `SppExtComObj`, `Veloce`, loopback `127.0.0.1:1688`, `0.0.0.0:1688`) cũng như chữ ký số lạ/chưa được ký (Authenticode Verification).
  - **Thang điểm rủi ro tuân thủ động (Weighted Risk Score 0 — 100):** Tự động phân chia 3 cấp độ: `✔ Clean / Legitimate` (Score < 16), `⚠ Anomalous / Grace Period / Unresolved KMS` (16 - 49), `⚠️ CRITICAL PIRACY / KMS CRACK DETECTED` (Score >= 50).
  - **Ghim đầu bảng (Top Priority 999):** Kết quả kiểm kê và xác minh bản quyền Windows OS luôn được hiển thị độc quyền trong **hộp chuyên đề CLI** và **Ghim ở dòng số 1** trong mọi báo cáo Excel (.xlsx) và Web Widescreen (.html).
- **Tự động hóa rà soát giấy phép:** Phân tích và phân loại chính xác các loại giấy phép (Commercial, Open Source, Freeware, Custom) thông qua hệ sinh thái 34 sandboxed plugins chuẩn hóa.
- **Bảo mật cục bộ (Read-Only & Air-Gapped):** Mọi thao tác quét Registry, hệ thống tập tin và chữ ký Authenticode diễn ra hoàn toàn trên RAM cục bộ, không ghi hoặc thay đổi cấu hình máy trạm.
- **Chống giả mạo dữ liệu (SHA-256 & Read-Only Lock):** Báo cáo xuất ra được tự động ký mã băm SHA-256 và khóa thuộc tính chỉ đọc nhằm bảo vệ tính toàn vẹn cho hồ sơ kiểm toán.
- **Thu thập đa nguồn (Multi-Source Scanners):** Tổng hợp dữ liệu từ Windows OS Activation (`SLMGR/SPP`), Windows Registry (32/64-bit), Windows Package Manager (WinGet), Linux dpkg/rpm và deep binary scanning.
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

## Quick Start

### 1. Chạy trực tiếp từ bản phát hành (Windows x64)

Bạn không cần cài đặt `.NET SDK` trên máy trạm khi sử dụng bản `Self-Contained`:

```powershell
# 1. Tải và giải nén gói phát hành
# 2. Mở Command Prompt hoặc PowerShell tại thư mục giải nén
.\LicenseIntelligencePlatform.Presentation.Cli.exe --no-pause
```
*Sau khi hoàn tất (~500ms), toàn bộ báo cáo (.xlsx, .html, .md, .csv, .json) được tự động xuất ra thư mục `reports/`.*

### 2. Tùy chọn dòng lệnh tổng hợp

```powershell
# Xem danh sách toàn bộ tham số hỗ trợ
.\LicenseIntelligencePlatform.Presentation.Cli.exe --help
```

---

## Multi-Format Report Generators

Hệ thống cho phép tạo và xuất báo cáo rà soát bản quyền tự động qua 4 định dạng độc lập (hoặc xuất đồng thời khi dùng `--format XLSX,HTML,MD,JSON`). Dưới đây là hướng dẫn chi tiết cho từng định dạng xuất:

### Generate HTML Report

Xuất báo cáo trực quan **Executive HTML Dashboard (`2800px+ Super Widescreen`)** với giao diện Dark Mode sang trọng (`#0F172A`), hiển thị thẻ chỉ số tổng quan (`Summary Cards`) và bảng dữ liệu siêu rộng. Tự động ngắt dòng thông minh (`break-all`), đảm bảo các chuỗi phiên bản dài (`Version`), mã commit SHA hay đường dẫn không bao giờ bị ngắt lẻ ký tự. Đặc biệt, kết quả rà soát bản quyền Windows OS & KMS Lậu luôn được **ghim tại dòng #1**.

```powershell
.\LicenseIntelligencePlatform.Presentation.Cli.exe --format HTML --output "reports/html" --no-pause
```

### Generate Excel Workbook

Xuất bảng tính **Goldilocks Deluxe Excel Workbook (`4 Tabs + Center/Center Alignment`)** gồm 4 trang tính chuyên sâu: *Executive Summary*, *Full Inventory & Audit*, *Commercial Licenses (Action Required)*, và *Open Source Compliance*. Toàn bộ 12 cột được thiết lập **căn chính giữa theo cả 2 trục ngang và dọc (`Center / Center`)**, lề đệm dọc (`+14pt cushion capped at 85pt`), độ rộng tự động mở rộng tối đa (`Width = 28 — 140`) và ghim dòng #1 bản quyền Windows OS.

```powershell
.\LicenseIntelligencePlatform.Presentation.Cli.exe --format XLSX --output "reports/excel" --no-pause
```

### Generate Markdown

Xuất hồ sơ kiểm toán tài sản công nghệ thông tin chuẩn **GitHub Flavored Markdown (`.md`)**. Phù hợp để tích hợp trực tiếp vào hệ thống tài liệu nội bộ, Git wiki, pull requests hoặc lưu trữ tĩnh kèm đầy đủ bảng tổng hợp bằng chứng (`Evidences`) cho từng gói phần mềm.

```powershell
.\LicenseIntelligencePlatform.Presentation.Cli.exe --format MD --output "reports/markdown" --no-pause
```

### Generate JSON

Xuất toàn bộ cấu trúc dữ liệu thô **Structured Raw JSON Data (`CamelCase + SHA-256 Checksum`)**. Phù hợp để tích hợp vào các đường ống CI/CD Pipeline, SIEM, Dashboard quản trị tài sản (ITAM) hoặc gọi qua hệ thống API tự động.

```powershell
.\LicenseIntelligencePlatform.Presentation.Cli.exe --format JSON --output "reports/json" --no-pause
```

---

## Project Structure

```text
License-Intelligence-Platform-Docs/
├── docs/                                  # Tài liệu kiến trúc, đặc tả hệ thống và hướng dẫn phát triển
├── src/
│   ├── LicenseIntelligencePlatform.Domain/              # Core Domain: Entities, Enums, Immutable Records & Interfaces
│   ├── LicenseIntelligencePlatform.Application/         # Use Cases: CoreEngine, MergeEngine, RuleEngine
│   ├── LicenseIntelligencePlatform.Plugins.Standard/    # Tập hợp 34 Sandboxed Standard Plugins
│   ├── LicenseIntelligencePlatform.Infrastructure/      # Multi-source Scanners & Multi-format Exporters
│   ├── LicenseIntelligencePlatform.Presentation.Cli/    # CLI Entry Point & Dependency Injection Container
│   └── LicenseIntelligencePlatform.Tests/               # Bộ kiểm thử tự động Unit Tests (41/41 passed)
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

## Future Roadmap & Planned Enhancements

Hệ thống hiện tại đã hoàn tất trọn vẹn Giai đoạn 0–4 (`Phase 0–4 Completed` với **49/49 Unit Tests Passed**). Các định hướng nâng cấp tiếp theo trong tương lai (**Phase 5 & Phase 6**) đã được ghi nhận vào kế hoạch phát triển:

- **👑 Executive Card Sync (HTML & Excel):** Đồng bộ khối thẻ chuyên đề **Windows License Audit & Risk Score** lên vị trí trên cùng của Báo cáo Web HTML Widescreen (`HtmlReportMapper`) và Bảng tính Excel Goldilocks Deluxe (`ExcelReportMapper`).
- **🛡️ Deep Office & Adobe Scanners:** Mở rộng quét chuyên sâu dịch vụ WMI `OfficeSoftwareProtectionProduct` và SPP Registry, bóc tách giấy phép Office 365, Office LTSC Pro Plus và phát hiện bẻ khóa KMS lậu.
- **📊 Inline SVG Analytics Charts:** Tích hợp biểu đồ phân bổ giấy phép (`Commercial vs Open Source vs Freeware`) và tỷ lệ độ tin cậy trực tiếp bên trong HTML Dashboard mà không cần kết nối internet/JS ngoại vi.
- **🔐 Asymmetric Digital Signatures (RSA/Ed25519):** Ký số báo cáo bằng khóa riêng tư (`--sign-key <key.pem>`), cho phép bộ phận Kiểm toán Nội bộ dùng Public Key xác minh tính nguyên bản tuyệt đối của hồ sơ kiểm toán.
- **📦 SBOM Exporters (CycloneDX / SPDX):** Xuất dữ liệu chuẩn Software Bill of Materials quốc tế phục vụ tích hợp SIEM / SOC (`ServiceNow`, `Microsoft Sentinel`, `Dependency-Track`).
- **⏳ Snapshot Diff Engine:** Thêm cờ `--compare <previous-report.json>` tự động đối chiếu các lần rà soát để cảnh báo phần mềm mới cài đặt, phần mềm bị gỡ bỏ hoặc chuyển đổi trạng thái bản quyền bất thường.

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
