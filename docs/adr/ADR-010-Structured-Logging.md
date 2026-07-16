# ADR-010: Structured Logging & Diagnostic Packaging

**Status:** Implemented  
**Date:** 2026-07-15  
**Authors:** DynamiteV

---

## Context & Problem Statement

Khi chạy trong môi trường thực tế tại máy khách hàng hoặc trên các máy trạm doanh nghiệp, nếu xảy ra sai lệch nhận diện (`false positive`) hay lỗi quyền truy cập, các thông báo `Console.WriteLine()` đơn giản là không thể đủ để hỗ trợ kỹ thuật hoặc chẩn đoán nguyên nhân từ xa.

## Decision

Chúng tôi triển khai **Structured File Logging & Diagnostic Packaging**:
1. Tầng `Application` và `Infrastructure` sử dụng hệ thống logging cấu trúc (`ILogger`), ghi nhận các sự kiện quan trọng kèm theo `ScanId` correlation.
2. Tạo thành phần `DiagnosticExporter` (`--diagnostic`) tự động tổng hợp toàn bộ các artifact:
   - Báo cáo CSV, JSON, Executive Summary, Evidence Report.
   - File log chi tiết của phiên quét (`scan_log.jsonl` hoặc structured log).
   - Metadata môi trường (`environment.json` với OS version, machine info).
   - Đóng gói toàn bộ thành một file `diagnostic_<scanId>.zip` duy nhất.

## Consequences

### Positive
- Khách hàng chỉ cần gửi duy nhất file `.zip` chẩn đoán cho đội ngũ hỗ trợ kỹ thuật để giải quyết mọi rắc rối.
- Log có cấu trúc giúp các hệ thống SIEM hoặc công cụ phân tích log dễ dàng phân tích tự động.

### Negative
- Cần dọn dẹp các gói zip chẩn đoán cũ trong thư mục `--output` nếu người dùng quét liên tục nhiều lần.
