# 16_FAQ.md

# License Intelligence Platform (LIP)

## Frequently Asked Questions (FAQ) & C-Suite Technical Brief

Version: 1.0

Status: Stable (Phase 0 – Phase 4 Completed)

Author: DynamiteV

---

# 1. Executive & Business Questions

### Q1.1: LIP là gì và giải quyết bài toán gì cho doanh nghiệp?
**License Intelligence Platform (LIP)** là nền tảng chẩn đoán và kiểm kê bản quyền phần mềm tự động chạy trên .NET 8 LTS. LIP giúp các Giám đốc CNTT (`CIO`), Quản trị viên tài sản (`IT Asset Managers`), và bộ phận Tuân thủ (`Compliance`) biết chính xác 100% các phần mềm đang cài đặt trên máy trạm, phân loại giấy phép (`Commercial`, `OpenSource`, `Freeware`) kèm theo các bằng chứng vật lý (`Physical Evidences`) để phục vụ giải trình kiểm toán.

### Q1.2: LIP có phải là công cụ bẻ khóa (crack/bypass) hay công cụ quét vi phạm của các hãng phần mềm không?
**Hoàn toàn không.** LIP được định vị là **Nền tảng Hỗ trợ Quản trị Tài sản & Tuân thủ Bản quyền Nội bộ (`Enterprise ITAM & Compliance Enablement`)**. LIP tuyệt đối không có chức năng bẻ khóa, không tạo keygen, không can thiệp hay sửa đổi bất kỳ file nào trên đĩa (`Rule 3 & Rule 6 Read-Only Invariants`), và cũng không bao giờ tự ý gửi dữ liệu hay báo cáo cho các vendor bên ngoài (như Microsoft, Adobe hay BSA).

### Q1.3: LIP có gửi dữ liệu máy tính hay danh sách phần mềm lên Cloud của bên thứ ba không?
**Không.** LIP tuân thủ kỷ luật **Local-First & Air-Gapped**. Toàn bộ tiến trình quét, gộp dữ liệu, chẩn đoán bằng chứng qua 33 Plugins và xuất bộ 5 báo cáo đều diễn ra 100% trên RAM của máy trạm cục bộ mà không cần bất kỳ kết nối Internet hay mạng nội bộ nào.

---

# 2. Technical & Security Questions

### Q2.1: LIP có cần quyền Administrator / root hoặc cài đặt Windows Service chạy ngầm không?
- **Quyền hạn:** LIP có thể chạy ngay với quyền **Standard User**. Nếu chạy dưới quyền Administrator, `WindowsRegistryScanner` sẽ đọc thêm được các nhánh HKLM chuyên sâu của hệ thống. Nếu gặp nhánh bị khóa quyền (`UnauthorizedAccessException`), khối `try/catch (Rule 9 Sandbox)` sẽ tự động bỏ qua và tiếp tục quét các vùng còn lại mà không bao giờ sập chương trình.
- **Không cài Agent:** LIP là một công cụ **Portable CLI (`dotnet run / self-contained exe`)**. Khi gọi lệnh, LIP bật lên, hoàn tất quét trong khoảng `170 ms`, xuất báo cáo ra ổ cứng rồi giải phóng toàn bộ RAM và tiến trình.

### Q2.2: LIP quét những gì và KHÔNG quét những gì trên hệ thống của tôi?
- **LIP chỉ thu thập:** Tên phần mềm, phiên bản, nhà xuất bản từ Registry 32/64-bit và WinGet; metadata file thực thi (`LastModifiedDate`, `InstallPath`); và sự tồn tại của file văn bản license (`LICENSE`, `COPYING`, `.lic`) bên trong thư mục cài đặt.
- **LIP KHÔNG BAO GIỜ chạm vào:** Mật khẩu, cookie, lịch sử duyệt web, email, tài liệu cá nhân (Word, Excel, PDF, hình ảnh, video), hoặc mã nguồn kinh doanh của người dùng.

### Q2.3: Thang điểm 100 của `Confidence Engine` hoạt động như thế nào?
Thay vì phán đoán cảm tính, LIP áp dụng thang điểm `Confidence Engine` định lượng chuẩn xác:
- **`Verified` ($\ge 70\text{ pts}$):** Tìm thấy file license header rõ ràng (+40-50 pts) hoặc chữ ký số Authenticode hợp lệ (+25-30 pts).
- **`High` ($50 - 69\text{ pts}$):** Khớp GUID `ProductCode` hoặc nhánh Registry Enterprise chuẩn (+15-20 pts).
- **`Medium / Low` ($10 - 49\text{ pts}$):** Chỉ nhận diện qua từ khóa tên nhà phát hành (`Heuristic Match`).
- **`None` ($0\text{ pts}$ / `Unknown`):** Phần mềm mới lạ chưa rõ danh tính, sẽ tự động ghi nhận vào `backlog_need_plugins.json`.

---

# 3. Usage & Extension Questions (Phase 1 – Phase 4)

### Q3.1: Khi có nhiều plugin cùng nhận diện 1 phần mềm (ví dụ Visual Studio), hệ thống xử lý xung đột ra sao?
Nhờ **Rule Engine 2.0 (Phase 1)**, mỗi Plugin được gán một mức `PluginPriority` (`CommercialSpecific = 100`, `Ecosystem = 75`, `Heuristic = 50`, `Generic = 25`). Trong trường hợp xung đột, `CoreEngine` tự động ưu tiên Plugin có Priority cao hơn, sau đó so sánh `ConfidenceLevel` và tổng điểm trọng số bằng chứng để đưa ra kết luận chuẩn xác nhất.

### Q3.2: Bộ 5 báo cáo của Phase 4 phục vụ cho những ai?
Khi chạy lệnh với `--format BOTH` hoặc các Exporter, LIP xuất đồng thời 5 định dạng báo cáo chuyên biệt:
1. **`license_report_*.html` (Visual HTML Report):** Giao diện Dark Mode đẹp mắt, các thẻ Card tổng kết dành cho Ban Giám đốc (`C-Suite / CIO`) xem hoặc in trực tiếp sang PDF.
2. **`audit_report_*.md` (Legal Audit Report):** Báo cáo Markdown chi tiết từng dòng bằng chứng (`SourceLocation`, `Snippet`) dành cho Kiểm toán viên pháp lý.
3. **`statistics_report_*.json` (BI Analytics Telemetry):** File JSON cấu trúc chuyên sâu về tỷ lệ nhà phát hành và thời gian mili-giây dành cho Kỹ sư Dữ liệu nạp vào PowerBI / Tableau.
4. **`license_report_*.csv` & `license_report_*.json`:** Bảng biểu phẳng cho Excel và cấu trúc JSON chuẩn cho các công cụ tự động hóa IT.

### Q3.3: Làm thế nào để thêm một Plugin nhận diện cho phần mềm nội bộ của công ty tôi?
Bạn chỉ cần tạo một class C# mới kế thừa `ILicensePlugin` trong tầng `Plugins.Standard` (hoặc nạp động qua `--plugins path/to/dll`), khai báo `PluginManifest`, và đặt từ khóa nhận diện trong `CanCheck()`. Nhờ kiến trúc Clean Architecture mở (`OCP`), plugin của bạn lập tức hòa vào luồng điều phối của `CoreEngine` mà không cần thay đổi bất kỳ dòng code nền tảng nào.
