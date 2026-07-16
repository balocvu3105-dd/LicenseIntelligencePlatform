# 18_CHANGELOG_GUIDE.md

# License Intelligence Platform (LIP)

## Changelog Governance & Format Specification

Version: 1.0

Status: Stable (Phase 0 – Phase 4 Completed)

Author: DynamiteV

---

# 1. Purpose & Standard Principles

Tài liệu **Changelog Governance & Format Specification** thiết lập tiêu chuẩn bắt buộc cho việc ghi nhận lịch sử thay đổi (`CHANGELOG.md`) của **License Intelligence Platform (LIP)**. Việc tuân thủ tiêu chuẩn giúp Ban Giám đốc, Quản trị viên IT và các lập trình viên theo dõi chính xác sự tiến hóa của nền tảng qua từng giai đoạn (`Phase 0 -> Phase 4`).

LIP áp dụng định dạng dựa trên **[Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/)** và tuân thủ nghiêm ngặt **[Semantic Versioning 2.0.0](https://semver.org/)** (`MAJOR.MINOR.PATCH`).

### Phân loại Mục Thay đổi (`Change Categories`):
- **`Added`**: Các tính năng, scanner, plugin mới, hoặc định dạng báo cáo mới.
- **`Changed`**: Sự thay đổi về hành vi, logic điều phối, hoặc cải tiến hiệu năng của các thành phần hiện có.
- **`Deprecated`**: Các tính năng chuẩn bị loại bỏ ở các phiên bản MAJOR kế tiếp.
- **`Removed`**: Các API hoặc Plugin đã chính thức bị xóa khỏi mã nguồn.
- **`Fixed`**: Sửa lỗi logic, ngoại lệ `NullReferenceException`, hoặc khắc phục nhận diện sai (`False Positives`).
- **`Security`**: Các bản vá bảo mật, củng cố ranh giới `Read-Only` và cơ chế `Sandboxed Isolation`.

---

# 2. Production Changelog Master Record (Phase 0 – Phase 4 Completed)

```markdown
# Changelog

All notable changes to this project will be documented in this file.
The format is based on Keep a Changelog, and this project adheres to Semantic Versioning.

## [1.0.0] - 2026-07-16 (Phase 4 Milestone Completion)
### Added
- **Exporters (Phase 4 Hierarchy):** Added `AuditReportMapper` (`AUDIT`) generating Markdown legal audit reports with `Confidence Breakdown Table` and raw physical evidence citations.
- **Exporters (Phase 4 Hierarchy):** Added `HtmlReportMapper` (`HTML`) generating standalone Dark Mode visual HTML5/Printable PDF reports with executive cards (`#0f172a`) and verification status badges.
- **Exporters (Phase 4 Hierarchy):** Added `StatisticsReportMapper` (`STATS`) generating BI analytics JSON telemetry with publisher distribution and plugin contribution metrics.
- **Exporters (Phase 4 Pipeline):** Registered all 5 Phase 4 Mappers (`CSV`, `JSON`, `AUDIT`, `HTML`, `STATS`) alongside `ExecutiveSummaryMapper` and `EvidenceReportMapper` inside `Program.cs`.
- **Knowledge Harvesting:** Implemented automated `backlog_need_plugins.json` export to harvest packages with `LicenseType = Unknown` during every scan.

### Changed
- **CLI Presentation:** Upgraded `--format BOTH` pipeline to trigger the full 5-mapper export sequence in `< 50 ms`.
- **Quality Assurance:** Verified 100% test pass rate across all 36 unit/integration tests (`36/36 tests green`).

## [0.9.0] - 2026-07-15 (Phase 3 Software Discovery Completion)
### Added
- **Scanners:** Added `WingetPackageScanner` parsing local JSON repository metadata of Windows Package Manager.
- **Scanners:** Added `SteamGameScanner` extracting `appmanifest_*.acf` records inside `SteamApps\common`.
- **Scanners:** Added `DeepFileSystemScanner` inspecting RAM process execution (`AppStartTime`) and PE file metadata (`LastModifiedDate`).
- **Scanners Orchestration:** Added `CompositeScanner` combining Windows Registry (32/64-bit), Linux DPKG, and Winget seamlessly.

## [0.8.0] - 2026-07-15 (Phase 2 Plugin Ecosystem Completion)
### Added
- **Plugin SDK v1.0:** Formally launched `ILicensePlugin` interface, `PluginManifest` (`SdkVersion 1.0.0`), and dynamic `AssemblyLoadContext` loader.
- **Plugins:** Expanded standard suite to **33 Production Plugins** covering proprietary suites (`AdobeCreativeCloudPlugin`, `MicrosoftOfficeCommercialPlugin`, `VMwareWorkstationPlugin`, `AutodeskAutoCadPlugin`) and developer platforms (`DockerDesktopPlugin`, `JetBrainsIdePlugin`, `GitOpenSourcePlugin`).
- **Resilience:** Implemented Rule 9 Sandboxed Exception Shielding (`try/catch + 5000ms CancellationToken Timeout Guard`).

## [0.7.0] - 2026-07-14 (Phase 1 Intelligent Detection Completion)
### Added
- **Core Engine:** Implemented `Rule Engine 2.0` ordering plugins via `PluginPriority` (`CommercialSpecific = 100` down to `Generic = 25`) and resolving conflicts by selecting highest `ConfidenceLevel` and evidence weight.
- **Scoring Engine:** Implemented 100-Point `Confidence Engine` (+40-50 pts for license headers, +25-30 pts for Authenticode signatures, +15-20 pts for registry GUIDs).
- **Deduplication Engine:** Implemented `SoftwareMergeEngine` to merge duplicate discoveries across 32-bit/64-bit hives via GUID and `Publisher Sanitization`.

## [0.1.0] - 2026-07-10 (Phase 0 Pilot Edition)
### Added
- Core 5-Layer `Clean Architecture` layout (`Domain`, `Application`, `Infrastructure`, `Plugins.Standard`, `Presentation.Cli`).
- Read-only `WindowsRegistryScanner` and `LinuxPackageScanner`.
- Basic `CsvReportMapper` and `JsonReportMapper`.
```
