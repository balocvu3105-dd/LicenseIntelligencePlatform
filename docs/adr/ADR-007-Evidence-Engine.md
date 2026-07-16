# ADR-007: Immutable Evidence Engine Domain Entity Record

**Status:** Implemented (Phase 1)  
**Date:** 2026-07-14  
**Author:** DynamiteV  

---

## 1. Context & Problem Statement

Trong các giải pháp Asset Management truyền thống, bằng chứng nhận diện giấy phép (`Evidence`) thường chỉ là một chuỗi văn bản tự do kiểu `"Found Adobe in registry"`. Khi hệ thống bước vào các đợt kiểm toán pháp lý thực tế (`Compliance Audits`), các chuyên gia kiểm toán không thể chấp nhận chuỗi văn bản thô thiếu cấu trúc vì không thể truy vết (`Traceability`) file gốc, không có trọng số định lượng, và không rõ loại bằng chứng (`EvidenceType`).

## 2. Considered Options

- **Option A:** Giữ nguyên `List<string> Evidences` trong DTO kết quả quét.
  - *Nhược điểm:* Không thể phân nhóm hay lọc bằng chứng theo trọng số (`WeightScore`) hay đường dẫn file (`SourceLocation`).
- **Option B:** Tách `Evidence` thành một **First-Class Domain Entity/Record** bất biến thuộc tầng `Domain`, có cấu trúc rõ ràng và bất biến sau khi khởi tạo (`init-only properties`).
  - *Ưu điểm:* Chuẩn hóa mô hình dữ liệu kiểm toán. Trở thành nền tảng tính điểm định lượng cho `Confidence Engine` (ADR-004).

## 3. Decision

Chúng tôi quyết định áp dụng **Option B — Immutable Evidence Engine Domain Entity Record** ngay trong Phase 1:
1. **Cấu trúc Domain Record chuẩn:**
   ```csharp
   public sealed record Evidence(
       string EvidenceId,
       EvidenceType EvidenceType,
       string Description,
       string SourceLocation,
       int WeightScore,
       string RawDataSnippet
   );
   ```
2. **Quy tắc Bất biến (`Domain Invariants`):**
   - Bất kỳ `LicenseCheckResult` nào có `ConfidenceLevel > None` đều bắt buộc phải chứa ít nhất 1 đối tượng `Evidence` hợp lệ (`evidences.Count > 0`).
   - `SourceLocation` phải chỉ định chính xác nhánh Registry (`HKLM\...`) hoặc đường dẫn file (`C:\Program Files\...`).
   - `RawDataSnippet` chứa trích đoạn ≤ 2KB nội dung file header (ví dụ: `"COPYRIGHT (C) MICROSOFT CORP"`) để kiểm toán viên xác minh không cần mở đĩa.

## 4. Consequences

### Positive
- **Giải trình minh bạch 100% (`Explainable AI / Engine`):** Báo cáo `AuditReportMapper` (`AUDIT`) có thể trích dẫn trực tiếp bảng bằng chứng rạch ròi cho từng ứng dụng.
- **Tính toán trọng số chính xác:** `Confidence Engine` dễ dàng cộng gộp các `WeightScore` của danh sách `Evidences` để quyết định thăng hạng lên `Verified` ($\ge 70\text{ pts}$).

### Negative / Trade-offs
- Các Plugin cần bổ sung thêm vài dòng khởi tạo đối tượng `new Evidence(...)` thay vì nối chuỗi đơn giản.
