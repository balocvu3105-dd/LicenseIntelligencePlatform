# ADR-001: Plugin Architecture

**Status:** Implemented  
**Date:** 2026-07-01  
**Authors:** DynamiteV & LIP Architecture Team

---

## Context & Problem Statement

License Intelligence Platform (LIP) cần nhận diện và phân tích bản quyền cho hàng trăm loại phần mềm khác nhau từ các nhà phát hành đa dạng (Microsoft, Adobe, JetBrains, Docker, hệ sinh thái mã nguồn mở, v.v.). Mỗi phần mềm hoặc nhà phát hành lại có cách lưu trữ thông tin kích hoạt, file giấy phép (`LICENSE`), hoặc cấu trúc registry khác nhau.

Nếu đưa toàn bộ logic kiểm tra bản quyền của mọi phần mềm vào trong `CoreEngine` hoặc `Scanner`, mã nguồn sẽ trở thành một "God Class" khổng lồ, rất khó bảo trì, dễ xảy ra xung đột khi nhiều người cùng làm việc, và vi phạm nguyên tắc `Open/Closed Principle` (mở cho mở rộng, đóng cho sửa đổi).

## Decision

Chúng tôi quyết định áp dụng **Plugin Architecture (Kiến trúc Plugin)** cho tầng phân tích bản quyền (`Application/Plugins`):
1. Định nghĩa hợp đồng chung `ILicensePlugin` trong tầng `Domain`.
2. Mỗi plugin là một class độc lập, chịu trách nhiệm cho một phần mềm hoặc một nhóm phần mềm (ecosystem) cụ thể (ví dụ: `MicrosoftOfficePlugin`, `GitOpenSourcePlugin`).
3. CoreEngine sẽ sử dụng `IPluginLoader` và `PluginCompatibilityValidator` để tự động khám phá, nạp, và thẩm định tương thích (qua `PluginManifest`) đối với các plugin.
4. Khi chạy quét, CoreEngine lần lượt truyền `SoftwareInfo` cho các plugin phù hợp (`CanCheck() == true`) theo thứ tự độ ưu tiên (`Priority DESC`).

## Consequences

### Positive
- **Tính mở rộng tuyệt đối:** Thêm phần mềm mới hoặc cải tiến logic của một phần mềm mà không cần sửa đổi dù chỉ 1 dòng code trong `CoreEngine`.
- **Phân lập lỗi (`Rule 9`):** Lỗi unhandled exception ở một plugin sẽ được catch và bọc lại thành `Unknown` result, không bao giờ làm crash toàn bộ phiên quét.
- **Dễ kiểm thử:** Có thể viết Unit Test riêng lẻ cho từng plugin một cách nhanh chóng.

### Negative
- Cần có cơ chế quản lý phiên bản SDK (`SdkVersion`, `PluginManifest`) để tránh plugin cũ chạy với CoreEngine mới gây lỗi in-memory.
- Số lượng class tăng lên (ví dụ: 26+ plugins trong `StandardPlugins`).
