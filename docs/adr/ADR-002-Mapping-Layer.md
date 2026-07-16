# ADR-002: Mapping Layer

**Status:** Implemented  
**Date:** 2026-07-03  
**Authors:** DynamiteV

---

## Context & Problem Statement

Tầng `Domain` định nghĩa các entity cốt lõi như `ScanReport`, `LicenseCheckResult`, `SoftwareInfo`, và `Evidence` theo cấu trúc hướng đối tượng phong phú và chính xác với nghiệp vụ. Tuy nhiên, hệ thống cần xuất kết quả ra nhiều định dạng khác nhau: CSV (bảng 2D cho Excel), JSON (cấu trúc cây cho API/Hệ thống), Executive Summary (văn bản tóm tắt), và Evidence Report (báo cáo kiểm toán kỹ thuật).

Nếu đặt trực tiếp các thuộc tính hoặc annotation serialization (`[JsonProperty]`, `[CsvHeader]`) lên các entity của `Domain`, tầng `Domain` sẽ bị ô nhiễm (`polluted`) bởi các phụ thuộc về hạ tầng (`Infrastructure/Presentation`) và không còn giữ được sự thuần khiết theo Clean Architecture.

## Decision

Chúng tôi quyết định áp dụng **Mapping Layer (`IReportMapper`)** nằm tại tầng `Infrastructure/Exporters`:
1. `Domain` entity hoàn toàn không biết về cách chúng được serialize hay định dạng.
2. Tầng `Infrastructure` tạo các class `CsvReportMapper`, `JsonReportMapper`, `ExecutiveSummaryMapper`, `EvidenceReportMapper` implement `IReportMapper`.
3. Khi cần xuất báo cáo, CLI truyền `ScanReport` cho mapper; mapper sẽ chuyển đổi dữ liệu thành luồng (`Stream`) và ghi ra file.

## Consequences

### Positive
- Tầng `Domain` hoàn toàn độc lập, không phụ thuộc thư viện CSV hay JSON.
- Việc thay đổi định dạng của file CSV (đổi tên cột, format ngày tháng `yyyy/MM/dd HH:mm:ss`) không ảnh hưởng đến nghiệp vụ cốt lõi hay Unit Tests của Domain.
- Dễ dàng bổ sung thêm định dạng mới như PDF hay HTML chỉ bằng cách thêm một `IReportMapper` mới.

### Negative
- Cần viết thêm code chuyển đổi dữ liệu (`Mapping code`) trong tầng `Infrastructure`.
