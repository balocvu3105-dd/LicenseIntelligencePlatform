# 03_DOMAIN_MODEL.md

# License Intelligence Platform (LIP)

## Domain Model

Version: 1.0

Status: Stable

Author: DynamiteV

---

# Purpose

Tài liệu này mô tả Domain Model của License Intelligence Platform (LIP).

Domain Layer là trung tâm của toàn bộ hệ thống, chứa các quy tắc nghiệp vụ cốt lõi (Business Rules) và hoàn toàn không phụ thuộc vào bất kỳ Framework, Database, UI hay hệ điều hành nào.

Mục tiêu của Domain Layer:

- Định nghĩa các Entity và Value Object.
- Định nghĩa các hợp đồng (Contracts).
- Định nghĩa các quy tắc nghiệp vụ.
- Cung cấp nền tảng ổn định cho các tầng còn lại.
- Đảm bảo khả năng mở rộng mà không phá vỡ kiến trúc.

---

# Domain Overview

License Intelligence Platform hoạt động theo mô hình:

```
Operating System
        │
        ▼
Scanner
        │
        ▼
SoftwareInfo
        │
        ▼
Plugin
        │
        ▼
Evidence
        │
        ▼
Rule Engine
        │
        ▼
LicenseCheckResult
        │
        ▼
ScanReport
```

Domain không biết:

- Windows
- Linux
- Registry
- CSV
- JSON
- Console
- Database

Domain chỉ biết:

- Software
- License
- Evidence
- Result

---

# Ubiquitous Language

Toàn bộ dự án phải thống nhất sử dụng các thuật ngữ sau.

| Term | Meaning |
|-------|----------|
| Software | Một phần mềm được phát hiện |
| Scanner | Thành phần thu thập dữ liệu |
| Plugin | Thành phần phân tích License |
| Evidence | Một bằng chứng |
| Rule Engine | Thành phần đưa ra quyết định |
| Confidence | Mức độ tin cậy |
| Report | Báo cáo cuối |
| Scan | Một lần quét |
| Identity | Danh tính phần mềm |

Không được sử dụng các từ không thống nhất.

Ví dụ:

Không dùng

- App
- Program
- Package

nếu đang nói về Software.

---

# Domain Entities

## SoftwareInfo

Đại diện cho một phần mềm được phát hiện.

Thuộc tính:

- Name
- Version
- Publisher
- InstallLocation
- ExecutablePath
- ProductCode
- UpgradeCode
- InstallDate
- InstallSource
- OperatingSystem

Trách nhiệm:

- Chứa toàn bộ thông tin phần mềm.
- Không chứa logic License.
- Không chứa logic Scanner.

---

## Evidence

Evidence là một bằng chứng phục vụ việc xác định License.

Ví dụ:

- Registry Key
- License File
- Digital Signature
- Running Service
- Executable Metadata
- Installer Metadata

Thuộc tính:

- EvidenceId
- Type
- Source
- Description
- Weight
- Reliability
- Timestamp
- RawData

Quy tắc:

Plugin được phép tạo Evidence.

Core không được tự tạo Evidence.

---

## LicenseCheckResult

Kết quả phân tích License.

Bao gồm:

- LicenseType
- Confidence
- PluginName
- Evidence Collection
- Detection Time
- Notes

Một Software chỉ có một LicenseCheckResult cuối cùng sau khi Rule Engine xử lý.

---

## ScanReport

Đại diện cho toàn bộ kết quả của một lần Scan.

Bao gồm:

- ScanId
- ScanTime
- Machine Information
- Software Count
- Results
- Statistics

---

# Value Objects

Các Value Object không có Identity.

Ví dụ:

ConfidenceScore

OperatingSystemInfo

MachineInfo

PluginMetadata

ReportStatistics

---

# Enumerations

## LicenseType

Unknown

Commercial

OpenSource

Freeware

Internal

---

## ConfidenceLevel

None

Low

Medium

High

Verified

---

# Domain Interfaces

## IScanner

Trách nhiệm:

Thu thập dữ liệu.

Không phân tích License.

Không Export.

Không Logging.

---

## ILicensePlugin

Trách nhiệm:

Đọc SoftwareInfo.

Sinh Evidence.

Không Export.

Không Scan.

---

## IRuleEngine

Trách nhiệm:

- Chọn Plugin
- Chạy Plugin
- Gom Evidence
- Tính Confidence
- Giải quyết xung đột
- Trả kết quả cuối

---

## IPluginLoader

Trách nhiệm:

Khám phá Plugin.

Kiểm tra tương thích.

Nạp Plugin.

---

## IReportMapper

Chuyển Domain Model sang DTO.

Không sửa Domain.

---

# Aggregate

ScanReport

là Aggregate Root.

```
ScanReport

├── Scan Metadata

├── Statistics

└── Results

        ├── LicenseCheckResult

        │

        └── Evidence[]
```

---

# Domain Rules

Rule 1

Scanner chỉ đọc dữ liệu.

---

Rule 2

Plugin không được truy cập Registry trực tiếp.

---

Rule 3

Plugin không được ghi dữ liệu hệ thống.

---

Rule 4

Rule Engine luôn là nơi duy nhất đưa ra quyết định cuối cùng.

---

Rule 5

Evidence không được chỉnh sửa sau khi tạo.

---

Rule 6

Một Software chỉ có một LicenseCheckResult cuối.

---

Rule 7

Generic Plugin luôn có Priority thấp nhất.

---

Rule 8

Plugin lỗi không được làm dừng Scan.

---

Rule 9

Scanner không được gọi Network.

---

Rule 10

Domain không phụ thuộc Infrastructure.

---

# Dependency Rules

```
Presentation

↓

Application

↓

Domain

↑

Infrastructure

↑

Plugins
```

Domain không tham chiếu:

Infrastructure

Presentation

Database

Logging

CSV

JSON

Registry

---

# Domain Lifecycle

```
Scanner

↓

SoftwareInfo

↓

Plugin

↓

Evidence

↓

Rule Engine

↓

Confidence Engine

↓

LicenseCheckResult

↓

ScanReport

↓

Exporter
```

---

# Domain Invariants

Các điều sau luôn đúng.

- Evidence luôn thuộc một Software.
- ScanId là duy nhất.
- ScanReport luôn có ScanTime.
- LicenseType không được null.
- Confidence luôn nằm trong khoảng hợp lệ.
- Plugin luôn trả về LicenseCheckResult hoặc lỗi được cô lập.

---

# Design Principles

Domain tuân thủ:

- Clean Architecture
- SOLID
- Dependency Inversion
- Domain Driven Design
- Single Responsibility
- Open Closed Principle

---

# Future Extension

Domain được thiết kế để mở rộng mà không phá vỡ mã nguồn hiện tại.

Có thể bổ sung:

- Confidence Score
- Rule Engine 2.0
- Software Identity
- Data Merge Engine
- Historical Scan
- Enterprise Dashboard

mà không cần thay đổi Entity hiện có.

---

# Summary

Domain Model là nền tảng của License Intelligence Platform.

Mọi tầng khác đều phụ thuộc vào Domain, nhưng Domain không phụ thuộc bất kỳ tầng nào khác.

Đây là nguyên tắc quan trọng nhất đảm bảo hệ thống có thể mở rộng lâu dài, dễ kiểm thử, dễ bảo trì và phù hợp với kiến trúc Clean Architecture.