# ADR-009: Software Identity Composite Key

**Status:** Accepted  
**Date:** 2026-07-14  
**Authors:** DynamiteV

---

## Context & Problem Statement

Tên hiển thị (`DisplayName`) trong Registry hay tên file thực thi (`ExecutableName`) thường không đủ duy nhất hoặc dễ bị thay đổi qua các bản phát hành phụ (patch releases). Để `SoftwareMergeEngine` và các plugin nhận dạng đúng một phần mềm mà không bị nhầm lẫn giữa các phiên bản hoặc các bản cài tùy chỉnh, hệ thống cần một cơ chế định danh nhất quán.

## Decision

Chúng tôi định nghĩa **Software Identity Composite Key**:
1. Khóa nhận diện chuẩn của `SoftwareInfo` được tổ hợp từ các yếu tố: `Name` + `Version` + `InstallPath` (khi có).
2. Khi tiến hành kiểm tra tương thích plugin, plugin được phép so sánh linh hoạt dựa trên cả `ProductCode` / `GUID` trong registry (nếu có) hoặc tên nhà phát hành `Publisher`.

## Consequences

### Positive
- Ngăn chặn hoàn toàn việc gộp nhầm 2 phần mềm khác nhau nhưng trùng tên chung chung (ví dụ: "Client" hay "Updater").
- Tạo nền tảng vững chắc cho việc lưu trữ và so sánh lịch sử quét (`Historical Scan Repository` - Phase 5).

### Negative
- Cần chuẩn hóa chuỗi đường dẫn (loại bỏ dấu `\` thừa ở cuối, chuẩn hóa chữ hoa/chữ thường) trước khi tạo composite key.
