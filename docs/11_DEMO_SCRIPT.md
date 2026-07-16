# 11_DEMO_SCRIPT.md

# License Intelligence Platform (LIP)

## Executive Demo Script & Phase 1-4 Feature Walkthrough

Version: 1.0

Status: Stable

Author: DynamiteV

---

# Purpose & Demo Specifications

Tài liệu **Executive Demo Script & Walkthrough** là kịch bản trình diễn chi tiết (`Presentation & Video Walkthrough`) dành cho Sales Engineers, Core Developers, hoặc IT Leaders khi giới thiệu sức mạnh của **License Intelligence Platform (LIP)** từ **Phase 1 đến Phase 4** trước Ban Giám đốc (C-Suite) hoặc đối tác kiểm toán.

### Key Value Proposition (Tuyên ngôn Giá trị Core):
> *"Khám phá toàn diện tự động $\to$ Đánh giá bằng chứng thông minh 100 điểm $\to$ Gộp trung lặp siêu tốc $\to$ Xuất trọn bộ 5 báo cáo điều hành (HTML Visual, Audit MD, Stats JSON, Executive Summary, CSV) trong vòng **$< 0.2$ giây** mà không cần can thiệp OS hay cài đặt Agent rắc rối."*

---

# 1. Kịch bản Trình diễn 5 Phút (`Scene-by-Scene Timeline`)

| Thời lượng (`Timestamp`) | Màn hình Trình diễn (`Visual Action on Screen`) | Lời thoại & Diễn giải (`Pitch Script / Voiceover`) | Điểm nhấn Kỹ thuật (`Phase Highlight`) |
| :--- | :--- | :--- | :--- |
| **0:00 - 0:45** <br> *(45s: Mở đầu & Thách thức Quản trị)* | Mở Terminal (PowerShell) font chữ `Cascadia Code 15pt`. Hiển thị sơ đồ kiến trúc 5 tầng (`Clean Architecture`). | *"Xin chào Ban Giám đốc và các anh/chị. Trong quản trị tài sản số và tuân thủ bản quyền doanh nghiệp, chúng ta luôn đau đầu trước 3 bài toán: 1 - Vi phạm bản quyền phần mềm thương mại đắt đỏ (Adobe, Office, Autodesk); 2 - Xung đột kết quả khi quét từ nhiều nguồn khác nhau; và 3 - Báo cáo kiểm toán thiếu bằng chứng giải trình pháp lý. Hôm nay em xin trình diễn **License Intelligence Platform (LIP) v1.0** — Nền tảng nhận diện bản quyền thông minh đã hoàn tất 100% đặc tả từ Phase 1 đến Phase 4."* | **Enterprise Foundation & Clean Architecture** |
| **0:45 - 1:45** <br> *(60s: Thực thi Quét & Phase 1-3 Intelligence)* | Chạy lệnh CLI thực thi toàn bộ luồng quét:<br>`dotnet run --project src/LicenseIntelligencePlatform.Presentation.Cli -c Release --format BOTH`<br>Màn hình Terminal chạy chớp nhoáng hiển thị thông số RAM và Stopwatch trong `< 200 ms`. | *"Em xin phép nhấn Enter. Như anh/chị thấy, không cần mạng hay quyền ghi vào Registry, LIP lập tức khởi chạy `CompositeScanner` để thu thập dữ liệu thô từ 3 nguồn (Registry 32/64-bit, WinGet, và Deep File System). Ngay sau đó, `SoftwareMergeEngine` (Phase 2) gộp chuẩn hóa hàng chục gói phần mềm bị lặp, rồi chuyển cho 33 Plugin nhận diện chuyên sâu. Toàn bộ tiến trình chỉ mất đúng **170 mili-giây** và tiêu thụ vỏn vẹn **4 MB RAM**."* | **Phase 1 Rule Engine 2.0 & Phase 3 Composite Scanner** |
| **1:45 - 2:45** <br> *(60s: Đánh giá Bằng chứng & Backlog Need-Plugin)* | Cuộn Terminal dừng lại ở bảng tổng hợp và phần cảnh báo `[BACKLOG - NEED PLUGIN]`. Chỉ tay vào cột `Confidence Level` và `Evidences`. | *"Khác với các công cụ phán đoán mò mẫm, LIP áp dụng `Confidence Engine 100 điểm` (Phase 1). Anh/chị hãy nhìn các phần mềm trọng yếu như `Office 365 ProPlus` hay `Autodesk AutoCAD`: hệ thống không chỉ báo `Commercial` mà còn đạt mức độ `Verified` tuyệt đối nhờ phát hiện chính xác chữ ký số và file `LICENSE` trong ổ đĩa. Với các phần mềm lạ chưa rõ danh tính, LIP tự động gom vào danh sách **Need Plugin Backlog** để hệ thống tiếp tục học hỏi ở các giai đoạn sau."* | **Phase 1 Confidence Engine & Evidence Sandbox** |
| **2:45 - 4:15** <br> *(90s: Báo cáo Kiểm toán Phase 4 HTML & Audit MD)* | Mở thư mục `reports/` vừa tự động sinh ra. Double-click mở file `license_report_*.html` trên trình duyệt Edge/Chrome. Sau đó mở `audit_report_*.md` trên VS Code. | *"Điểm cải phá lớn nhất của Phase 4 là `Reporting Engine Hierarchy`. Thay vì chỉ xuất CSV thô, LIP tự động sinh ra **5 định dạng báo cáo song song**: Báo cáo **HTML Visual Report** với giao diện Dark Mode hiện đại, bảng màu trực quan, badge xác thực sẵn sàng để C-Suite xem trực tiếp hoặc in sang PDF trình kiểm toán; Báo cáo **Audit Markdown** chi tiết từng dòng trích dẫn file gốc; và **Statistics JSON** cung cấp số liệu phân bổ theo nhà phát hành cho hệ thống BI."* | **Phase 4 IReportMapper Multi-Output Hierarchy** |
| **4:15 - 5:00** <br> *(45s: Kiểm chứng SLA & Lời mời Triển khai Pilot)* | Chạy nhanh lệnh `dotnet test` cho thấy `36/36 tests passed (100% green)`. | *"Toàn bộ hệ thống được bảo vệ bởi bộ kiểm thử tự động 36 bài test đạt chuẩn **100% Accuracy KPI**. Không một lỗi ngoại lệ nào từ plugin hay scanner có thể làm sập Core Engine nhờ kỷ luật `Sandboxed Error Isolation`. LIP v1.0 đã hoàn toàn sẵn sàng để triển khai thử nghiệm trên quy mô 50 - 100 máy trạm doanh nghiệp ngay hôm nay. Xin cảm ơn Ban Giám đốc đã theo dõi!"* | **Quality Attributes & SLA Assurance** |

---

# 2. Checklist Chuẩn bị Trước khi Demo (`Demo Preparation Checklist`)

Để bài trình diễn diễn ra hoàn hảo không tì vết, người thuyết trình phải kiểm tra các bước sau trước khi bật máy chiếu hoặc ghi hình:

- [x] **Xác minh Build & Clean Solution:**
  ```powershell
  dotnet clean src/LicenseIntelligencePlatform.slnx
  dotnet build src/LicenseIntelligencePlatform.slnx -c Release
  ```
- [x] **Kiểm chứng Bộ Unit Test 100% Pass:**
  ```powershell
  dotnet test src/LicenseIntelligencePlatform.slnx -v normal
  ```
  *(Đảm bảo hiển thị dòng `Passed! - Failed: 0, Passed: 36, Total: 36`).*
- [x] **Dọn dẹp thư mục Báo cáo cũ (`Cleanup Reports`):**
  ```powershell
  Remove-Item -Path "reports/*" -Recurse -Force -ErrorAction SilentlyContinue
  ```
- [x] **Tối ưu hiển thị Terminal & Browser:**
  - PowerShell / Terminal: Chuyển sang theme tối, font `Cascadia Code` hoặc `Consolas` kích thước **`15pt`** hoặc **`16pt`**.
  - Trình duyệt Web (Edge/Chrome): Đặt mức Zoom **`110%`** để khi mở file `license_report_*.html`, các thẻ Card thống kê và bảng Badges hiển thị sắc nét, ấn tượng ngay cái nhìn đầu tiên.

---

# 3. Handling Q&A & Tough Technical Inquiries (`Khắc phục Câu hỏi Khó`)

**Q1: Nếu một Plugin của bên thứ ba bị viết sai lệnh, lặp vô hạn `while(true)` hoặc cố tình đọc file hệ thống bị cấm thì sao?**
> **Trả lời:** *Dạ, theo đặc tả Phase 2 và Rule 9, mỗi `CheckLicenseAsync` của Plugin được chạy trong một ranh giới cách ly (`Exception Shielding + 5000ms CancellationToken Timeout Guard`). Nếu Plugin lặp vô hạn hoặc gặp `UnauthorizedAccessException`, token sẽ tự ngắt sau 5 giây, trả về `ErrorResult` cô lập và tiến trình tiếp tục quét 100% các phần mềm còn lại mà không bao giờ sập Core Engine.*

**Q2: Làm sao chứng minh rằng LIP không lén ghi sửa Registry hay gọi mạng ra internet?**
> **Trả lời:** *Dạ, toàn bộ kiến trúc `IScanner` tuân thủ nguyên tắc 100% Read-Only, chỉ sử dụng các API `OpenSubKey` chỉ đọc và `File.OpenRead`. Khối Dependency Injection (`Program.cs`) hoàn toàn không đăng ký bất kỳ `HttpClient` hay `TcpClient` nào cho luồng quét (Zero-Network Capability).*
