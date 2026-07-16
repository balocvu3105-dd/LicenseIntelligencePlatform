# ADR-005: Generic & Fallback Plugins

**Status:** Implemented  
**Date:** 2026-07-09  
**Authors:** DynamiteV

---

## Context & Problem Statement

Mặc dù chúng tôi liên tục bổ sung các Plugin chuyên biệt (`Standard & Ecosystem Plugins`), một máy tính thực tế luôn có thể chứa hàng trăm phần mềm nhỏ lẻ, tiện ích driver, hoặc phần mềm nội bộ mà chúng tôi chưa từng viết plugin cho chúng.

Nếu CoreEngine chỉ xử lý những phần mềm khớp với các plugin chuyên biệt, danh sách kết quả sẽ bỏ sót tất cả các phần mềm còn lại (`Unknown / Unhandled`), khiến báo cáo kiểm kê tài sản không phản ánh đầy đủ 100% phần mềm thực tế trên máy tính.

## Decision

Chúng tôi áp dụng mô hình **Generic Heuristic Plugins & Core Fallback (`ADR-005`)**:
1. Xây dựng các heuristic plugins có `Priority` trung bình/thấp (`50-60`):
   - `CommercialKeyFilePlugin`: Quét tìm từ khóa "Enterprise", "Professional", "License Key" trong tên và registry.
   - `OpenSourceArtifactPlugin`: Quét tìm file `LICENSE`, `COPYING`, `gpl.txt` trong `InstallPath`.
   - `FreewarePatternPlugin`: Quét tìm từ khóa "Community", "Free", "Viewer".
2. Nếu sau khi chạy tất cả plugin chuyên biệt và heuristic mà `CanCheck()` vẫn là `false`, CoreEngine sẽ kích hoạt `Core Fallback Detector` (`core.unknown`) với `Priority = 0`, trả về `LicenseType = Unknown`, `Confidence = None`.
3. Toàn bộ kết quả có `Confidence = None` sẽ được xuất riêng vào file `backlog_need_plugins.json`.

## Consequences

### Positive
- **Độ bao phủ 100% (`Zero Software Left Behind`):** Mọi phần mềm quét thấy trong Registry hay RAM đều xuất hiện trong báo cáo, không bị rơi rụng.
- **Tự động sinh Backlog (`Data-Driven Roadmap`):** File `backlog_need_plugins.json` chính là nguồn dữ liệu vàng để xác định cần viết plugin nào tiếp theo cho Phase 3 & Phase 4.

### Negative
- Cần lọc kỹ trong báo cáo để phân biệt giữa phần mềm đã verify (`Confidence >= High`) và phần mềm chưa có plugin (`Confidence = None`).
