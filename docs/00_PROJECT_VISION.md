# 00_PROJECT_VISION.md

# License Intelligence Platform (LIP)

## 00 · Project Vision & Strategic Foundation

Version: 1.0

Status: Stable

Author: DynamiteV

Last Updated: 2026-07-16

---

# 1. Vision Statement

**License Intelligence Platform (LIP)** là nền tảng kiểm kê và thông minh hóa giấy phép phần mềm tự động hàng đầu trên kiến trúc `.NET 8 Clean Architecture`. Hệ thống hoạt động hoàn toàn ở chế độ chỉ đọc (`Read-Only`), siêu tốc (`Ultra-Fast`), không cần kết nối internet (`Air-Gapped & Offline-First`), và không bao giờ cài đặt Agent thường trú hay can thiệp vào tiến trình của hệ điều hành.

> *"Biết chính xác mọi tài sản phần mềm trong hệ thống, đánh giá rủi ro pháp lý và giải trình bằng chứng bản quyền độ chuẩn xác 100% — ngay trên máy trạm, trong vỏn vẹn dưới 200 mili-giây."*

---

# 2. Problem Statement (Thách thức Ngành Quản trị IT)

Các doanh nghiệp và nhà quản trị hạ tầng CNTT (`IT Asset Managers`, `CIOs`, `Compliance Auditors`) luôn phải đối mặt với 4 điểm nghẽn nghiêm trọng:
1. **Rủi ro phạt vi phạm bản quyền khổng lồ:** Các hãng phần mềm thương mại lớn (Microsoft, Adobe, Autodesk, Oracle, VMware) liên tục kiểm toán ngẫu nhiên. Việc thiếu kiểm soát giấy phép dẫn đến các khoản phạt từ hàng chục nghìn đến hàng triệu USD.
2. **Lãng phí chi phí giấy phép (`Shelfware`):** Hàng ngàn giấy phép đắt đỏ được mua nhưng nhân viên không bao giờ mở hoặc chỉ dùng các tính năng miễn phí cơ bản.
3. **Thao tác thủ công, thiếu bằng chứng giải trình:** Các công cụ quét thông thường chỉ trả về danh sách chuỗi văn bản thô mà không đưa ra được bằng chứng vật lý (`Physical Evidence`) như header file `.lic` hay chữ ký số `Authenticode`.
4. **Nguy cơ bảo mật từ các Agent nặng nề:** Công cụ Asset Management truyền thống yêu cầu quyền `SYSTEM/root` cao nhất, cài đặt service chạy ngầm ngốn RAM và gửi dữ liệu nhạy cảm lên Cloud của bên thứ ba.

---

# 3. Mission & Value Proposition

Sứ mệnh của LIP là **democratize (dân chủ hóa) quy trình kiểm toán bản quyền phần mềm**, biến công việc phức tạp kéo dài hàng tuần của các chuyên gia pháp lý thành một lệnh CLI siêu tốc trong `0.17 giây`.

### 4 Giá trị Cốt lõi của LIP v1.0 (Phase 0 – Phase 4 Completed):
- **Deterministic 100-Point Scoring:** Chấm dứt phán đoán cảm tính bằng `Confidence Engine 100 điểm`. Mỗi phần mềm được phân loại (`Commercial`, `OpenSource`, `Freeware`) kèm điểm số chuẩn xác dính liền với bằng chứng file header hoặc chữ ký số.
- **Multi-Source Deduplication:** Quét đồng bộ từ Registry 32/64-bit, WinGet, và Deep File System. `SoftwareMergeEngine` tự động chuẩn hóa nhà phát hành (`Publisher Sanitization`) và gộp GUID để không bao giờ báo cáo trùng lặp.
- **Air-Gapped Privacy & Rule 9 Isolation:** Hoạt động 100% offline. Khối `try/catch + 5000ms Timeout Guard` bảo vệ Core Engine, đảm bảo `Zero-Crash` trước mọi lỗi I/O hay kẹt đĩa.
- **5-Output Executive Reporting Hierarchy:** Xuất trọn bộ 5 định dạng báo cáo song song: **HTML Visual Report** (Dark Mode, có thể in PDF cho C-Suite), **Audit Markdown** (trích dẫn bằng chứng pháp lý), **Statistics JSON** (cho hệ thống BI), **CSV** và **Executive Summary**.

---

# 4. Target Users & Personas

| Persona | Vai trò trong Doanh nghiệp | Nhu cầu và Cách LIP Giải quyết |
| :--- | :--- | :--- |
| **CIO / IT Director** | Giám đốc Công nghệ | Cần tầm nhìn toàn cảnh (`Executive Overview`) về rủi ro pháp lý và chi phí tài sản phần mềm. Sử dụng `HtmlReportMapper` và `ExecutiveSummaryMapper`. |
| **IT Asset & Compliance Manager** | Quản lý Tài sản CNTT & Tuân thủ | Cần danh sách kiểm kê chính xác từng endpoint, xác minh phần mềm thương mại và lãng phí. Sử dụng `AuditReportMapper` và `CsvReportMapper`. |
| **Legal / Licensing Auditor** | Kiểm toán viên Bản quyền | Cần trích dẫn bằng chứng gốc (`Evidence SourceLocation`, `RawDataSnippet`) để chứng minh tính hợp pháp khi làm việc với vendor. Sử dụng `AuditReportMapper`. |
| **DevOps / System Engineer** | Kỹ sư Hạ tầng & Triển khai | Cần công cụ nhẹ portable, chạy trong CI/CD pipeline hoặc qua PowerShell script không cần network. Sử dụng `StatisticsReportMapper` và `EvidenceReportMapper`. |

---

# 5. Core Architectural Pillars (5 Trụ cột Kiến trúc)

1. **Clean Architecture:** Phân tách rõ ràng 5 tầng: `Presentation.Cli` $\to$ `Application` $\to$ `Domain` $\leftarrow$ `Infrastructure` & `Plugins.Standard`. Tầng Domain hoàn toàn không chứa dependency I/O hay framework ngoại vi.
2. **SOLID & Dependency Injection:** Mọi thành phần từ `IScanner`, `ILicensePlugin` đến `IReportMapper` đều giao tiếp qua Interface, được quản lý trọn vẹn trong `Microsoft.Extensions.DependencyInjection`.
3. **Zero-Allocation Idioms:** Tối ưu hóa tối đa tốc độ với `ReadOnlySpan<char>`, `StringComparer.OrdinalIgnoreCase`, giúp xử lý hàng trăm gói phần mềm với `Memory Delta < 5 MB`.
4. **Sandboxed Plugin Ecosystem:** Đăng ký 33 Plugins chuẩn (`Standard Plugins`) bao phủ từ Adobe, Microsoft Office, Autodesk, SQL Server đến Docker, Steam, JetBrains.
5. **Living Backlog Learning Loop:** Tự động thu thập phần mềm chưa có plugin riêng vào file `backlog_need_plugins.json`, tạo vòng lặp phát triển liên tục (`Continual Improvement Loop`).
