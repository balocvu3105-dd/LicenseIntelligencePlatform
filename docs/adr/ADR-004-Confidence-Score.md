# ADR-004: Confidence Score & Evidence Weighting

**Status:** Accepted  
**Date:** 2026-07-08  
**Authors:** DynamiteV

---

## Context & Problem Statement

Việc nhận diện bản quyền không bao giờ là trắng-đen (`Binary/Boolean 100%`). Một phần mềm có thể có tên là "Visual Studio Code" trong Registry nhưng đường dẫn cài đặt lại là một bản build tùy chỉnh từ mã nguồn mở (VSCodium) hoặc bản thương mại. Nếu hệ thống chỉ trả về kết luận `Commercial` hoặc `OpenSource` mà không giải thích mức độ tin cậy, người quản trị sẽ không dám đưa ra quyết định kiểm toán.

## Decision

Chúng tôi định nghĩa mô hình **Confidence Level & Evidence Collection**:
1. Mọi `LicenseCheckResult` đều đi kèm trường `ConfidenceLevel` (`None`, `Low`, `Medium`, `High`, `Verified`) và danh sách bằng chứng `Evidences`.
2. Định lượng bằng `Evidence Weight`:
   - `FileArtifact` (`LICENSE`, `COPYING` khớp từ khóa bản quyền): Weight `High/Verified`.
   - `DigitalSignature` (exe được ký bởi Microsoft/Adobe): Weight `High`.
   - `RegistryKey` / `InstallPath pattern`: Weight `Medium`.
   - `KeywordMatch` (chỉ khớp tên nhà phát hành): Weight `Low`.
3. Chỉ những kết quả đạt `ConfidenceLevel >= High` mới được đánh dấu `IsVerified = true`.

## Consequences

### Positive
- **Minh bạch & Có thể giải thích (`Explainable AI / Logic`):** Mọi báo cáo đều đưa ra căn cứ kỹ thuật rõ ràng để kiểm toán viên xác minh lại khi cần.
- **Tránh sai lệch (`False Positive`):** Những phần mềm đoán mò theo từ khóa chỉ bị đánh giá `Low`, giúp admin biết cần kiểm tra thủ công thêm.

### Negative
- Lập trình viên viết plugin phải suy nghĩ kỹ và tạo ra các object `Evidence` cụ thể thay vì chỉ return enum `LicenseType`.
