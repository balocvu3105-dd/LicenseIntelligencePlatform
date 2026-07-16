# ADR-003: Read-Only Scanner Architecture

**Status:** Implemented  
**Date:** 2026-07-05  
**Authors:** DynamiteV

---

## Context & Problem Statement

Các công cụ kiểm tra phần mềm truyền thống thường yêu cầu cài đặt Agent nặng nề chạy nền với quyền Administrator cao nhất, thậm chí thực hiện ghi temporary registry keys, hook vào tiến trình (`process injection`), hoặc truyền tải dữ liệu qua mạng (`cloud calling`). Điều này gây lo ngại lớn về bảo mật, tính ổn định của máy tính người dùng, và nguy cơ vi phạm quyền riêng tư.

LIP cần được triển khai dễ dàng, an toàn, có thể chạy trên máy trạm của nhân viên mà không gây ra bất kỳ tác dụng phụ hay nghi ngờ pháp lý nào.

## Decision

Chúng tôi quyết định thiết lập quy tắc bất di bất dịch **Read-Only & Zero-Network Scanners (`ADR-003`)**:
1. Tất cả `IScanner` (`WindowsRegistryScanner`, `LinuxPackageScanner`, `DeepFileSystemScanner`) chỉ sử dụng các API đọc dữ liệu thô (`RegistryKey.OpenSubKey()`, `File.GetLastWriteTimeUtc()`, `Process.GetProcesses()`).
2. **Tuyệt đối cấm** gọi các hàm ghi hoặc sửa đổi (`SetValue`, `CreateSubKey`, `File.WriteAllText` ngoài thư mục output, `Process.Start` với payload).
3. **Tuyệt đối cấm** tích hợp bất kỳ `HttpClient`, `Socket`, hay `WebRequest` nào bên trong Scanner và Plugin để đảm bảo `100% Offline / Local-First`.

## Consequences

### Positive
- **An toàn bảo mật tuyệt đối:** Có thể vượt qua mọi bài kiểm toán bảo mật (`Security Audit`) khắt khe của doanh nghiệp.
- **Không yêu cầu Administrator:** Scanner có thể chạy bình thường dưới quyền User thông thường; nếu gặp registry key bị khóa quyền, scanner sẽ log warning và tiếp tục với các key khác.
- **Không bao giờ làm hỏng hệ điều hành:** Loại bỏ 100% nguy cơ làm hỏng Windows Registry hay xung đột tiến trình.

### Negative
- Một số phần mềm bảo mật cấp cao ẩn sâu trong kernel (`Ring 0`) không thể được phát hiện đầy đủ nếu chạy ở quyền User thông thường mà không nâng quyền.
