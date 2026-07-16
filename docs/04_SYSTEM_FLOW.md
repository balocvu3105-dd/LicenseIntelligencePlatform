# 04_SYSTEM_FLOW.md

# License Intelligence Platform (LIP)

## System Flow

Version: 1.0

Status: Stable

Author: DynamiteV

---

# Purpose

Tài liệu này mô tả toàn bộ luồng hoạt động (System Flow) của License Intelligence Platform (LIP).

Mục tiêu:

- Giải thích cách các thành phần phối hợp với nhau.
- Chuẩn hóa luồng dữ liệu.
- Làm tài liệu tham khảo cho việc mở rộng hệ thống.
- Đảm bảo mọi thành viên đều hiểu cùng một quy trình xử lý.

Tài liệu này không mô tả chi tiết thuật toán hay implementation của từng thành phần mà chỉ tập trung vào luồng nghiệp vụ và kiến trúc.

---

# High Level Flow

```

User

↓

CLI / UI

↓

Core Engine

↓

Scanner Layer

↓

Software Collection

↓

Merge Engine

↓

Plugin Discovery

↓

Rule Engine

↓

Evidence Engine

↓

Confidence Engine

↓

Conflict Resolution

↓

License Result

↓

Report Mapper

↓

Exporter

↓

CSV / JSON / HTML / PDF

```

---

# Overall Architecture

```

┌──────────────┐
│ User │
└──────┬───────┘
│
▼
┌──────────────┐
│ CLI / UI │
└──────┬───────┘
│
▼
┌──────────────┐
│ Core Engine │
└──────┬───────┘
│
├──────────────┐
│ │
▼ ▼
Scanner Plugin Loader
│ │
└──────┬───────┘
│
▼
Rule Engine
│
▼
Evidence Engine
│
▼
Confidence Engine
│
▼
Conflict Resolution
│
▼
Scan Report
│
▼
Mapper
│
▼
Exporter

```

---

# Complete Scan Lifecycle

Một lần quét hoàn chỉnh luôn đi qua các bước sau:

1. Khởi động hệ thống
2. Kiểm tra cấu hình
3. Nạp Plugin
4. Khởi tạo Scanner
5. Quét phần mềm
6. Hợp nhất dữ liệu
7. Chọn Plugin phù hợp
8. Thu thập Evidence
9. Tính Confidence
10. Giải quyết xung đột
11. Sinh Scan Report
12. Mapping
13. Export
14. Ghi Log
15. Kết thúc

---

# Step 1 - Application Startup

```

Program.cs

↓

Dependency Injection

↓

Configuration

↓

Logging

↓

Plugin Loader

↓

Core Engine

```

Trách nhiệm

- Đọc cấu hình
- Khởi tạo Dependency Injection
- Khởi tạo Logging
- Khởi tạo Core Engine
- Nạp Plugin

---

# Step 2 - Plugin Discovery

Plugin Loader thực hiện:

```

Plugin Folder

↓

Discover DLL

↓

Validate

↓

Load Assembly

↓

Read Metadata

↓

Register

```

Kiểm tra

- SDK Version
- Signature (Future)
- Manifest
- Dependencies

Plugin không hợp lệ sẽ bị bỏ qua.

Core không được crash.

---

# Step 3 - Scanner Execution

Core Engine yêu cầu Scanner thực hiện Scan.

```

Core

↓

Composite Scanner

↓

Windows Scanner

Linux Scanner

Future Scanner

↓

SoftwareInfo[]

```

Scanner chỉ được phép:

✔ Read

Không được phép:

✘ Detect License

✘ Export

✘ Rule

✘ Network

---

# Step 4 - Software Merge

Một phần mềm có thể được phát hiện từ nhiều Scanner.

Ví dụ

Registry

+

Winget

+

Steam

↓

Software Identity

↓

Merge

↓

One Software

Merge Engine chịu trách nhiệm:

- Remove Duplicate
- Merge Metadata
- Preserve Evidence

---

# Step 5 - Plugin Selection

Rule Engine chọn Plugin.

```

Software

↓

Plugin A

Plugin B

Plugin C

↓

CanHandle()

↓

Priority

↓

Execute

```

Plugin được sắp theo:

1. Priority

2. Compatibility

3. Product Match

4. Generic Plugin

---

# Step 6 - Evidence Collection

Plugin tạo Evidence.

Ví dụ

Registry

↓

Evidence

License File

↓

Evidence

Digital Signature

↓

Evidence

Running Service

↓

Evidence

Core không tự tạo Evidence.

---

# Step 7 - Confidence Calculation

Evidence

↓

Confidence Engine

↓

Score

↓

Confidence Level

Ví dụ

Registry = 15

License File = 40

Signature = 20

Metadata = 15

Running Service = 10

Total = 100

Mapping

95-100 → Verified

80-94 → High

60-79 → Medium

30-59 → Low

0-29 → None

---

# Step 8 - Conflict Resolution

Nếu nhiều Plugin trả kết quả khác nhau.

Ví dụ

Plugin A

Commercial

95

Plugin B

Open Source

65

↓

Rule Engine

↓

Priority

↓

Evidence Weight

↓

Reliability

↓

Final Result

Luôn trả về:

Một LicenseCheckResult duy nhất.

---

# Step 9 - Report Generation

```

LicenseCheckResult

↓

Scan Report

↓

Mapper

↓

DTO

↓

Exporter

```

Có thể Export

CSV

JSON

HTML

PDF

Audit Report

---

# Step 10 - Logging

Trong suốt Scan

Mọi thành phần đều ghi

Performance

↓

Audit

↓

Error

↓

Application

↓

Diagnostic Package

Log phải:

- Structured
- Correlated bằng ScanId
- Không chứa dữ liệu nhạy cảm

---

# Scan Sequence Diagram

```

User

│

│ Start Scan

▼

Program

│

▼

Core Engine

│

├────────► Scanner

│ │

│ ◄──────── Software

│

├────────► Rule Engine

│ │

│ ├────► Plugin

│ │ │

│ │ ◄──── Evidence

│ │

│ ◄──────── Result

│

├────────► Report

│

◄──────── CSV

```

---

# Data Flow

```

Registry

Linux Package

Winget

Steam

Docker

↓

SoftwareInfo

↓

Evidence

↓

Rule Engine

↓

LicenseCheckResult

↓

ScanReport

↓

Mapper

↓

Exporter

↓

CSV / JSON / PDF

```

---

# Error Flow

Nếu Scanner lỗi

↓

Scanner ghi Log

↓

Core tiếp tục

Nếu Plugin lỗi

↓

Plugin Isolation

↓

Fallback Result

↓

Tiếp tục Scan

Nếu Export lỗi

↓

Ghi Error

↓

Không mất Scan Report

---

# Performance Flow

Benchmark thu thập

Scanner Time

Plugin Time

Rule Time

Memory

CPU

Throughput

Các số liệu này không ảnh hưởng tới nghiệp vụ.

---

# Future Flow

Trong các phiên bản sau sẽ bổ sung

Historical Scan

↓

Compare

↓

Change Detection

↓

Dashboard

↓

Alert

↓

Enterprise Portal

Luồng hiện tại được thiết kế để mở rộng mà không thay đổi các bước xử lý cốt lõi.

---

# Design Principles

System Flow tuân thủ các nguyên tắc sau:

- Scanner chỉ thu thập dữ liệu.
- Plugin chỉ tạo Evidence.
- Rule Engine là nơi duy nhất đưa ra quyết định.
- Mapper tách Domain khỏi Report.
- Exporter chỉ chịu trách nhiệm xuất dữ liệu.
- Logging không can thiệp nghiệp vụ.
- Mọi lỗi phải được cô lập.
- Không thành phần nào được phép phá vỡ toàn bộ Scan Pipeline.

---

# Summary

Toàn bộ License Intelligence Platform hoạt động theo mô hình Pipeline.

User

↓

Scan

↓

Collect

↓

Analyze

↓

Verify

↓

Resolve

↓

Report

↓

Export

↓

Complete

Mỗi thành phần chỉ chịu trách nhiệm một nhiệm vụ duy nhất theo nguyên tắc Single Responsibility và phối hợp với nhau thông qua Dependency Injection, giúp hệ thống dễ kiểm thử, dễ mở rộng và không phụ thuộc vào nền tảng triển khai.  