# HƯỚNG DẪN SỬ DỤNG — LICENSE INTELLIGENCE PLATFORM (LIP) v1.0
**Hệ Thống Kiểm Kê Phần Mềm & Rà Soát Bản Quyền Tự Động Chuẩn Doanh Nghiệp (Enterprise Read-Only Audit Engine)**

---

## I. GIỚI THIỆU CHUNG & ĐẶC TÍNH BẢO MẬT (`SECURITY HIGHLIGHTS`)

**License Intelligence Platform (LIP)** là giải pháp kiểm kê toàn bộ phần mềm, phân tích giấy phép bản quyền (Commercial, Open Source, Freeware) và đánh giá mức độ tuân thủ pháp lý tự động cho doanh nghiệp.

### 🛡️ 3 Nguyên Tắc Bảo Mật Cốt Lõi:
1. **100% Read-Only (Chỉ Đọc & Không Xâm Nhệp):** Engine hoạt động hoàn toàn ở chế độ chỉ đọc (`Get-ItemProperty`, `EnumerateFiles`). Tuyệt đối **KHÔNG GHI**, **KHÔNG SỬA ĐỔI**, **KHÔNG XÓA** bất kỳ tệp tin hay key Registry nào của hệ điều hành và ứng dụng.
2. **100% Air-Gapped (Không Kết Nối Mạng):** Toàn bộ quá trình quét, kiểm chứng chữ ký số, khớp tri thức bản quyền và xuất báo cáo diễn ra **hoàn toàn cục bộ (offline) trên máy tính**. Không có bất kỳ kết nối internet hay gửi dữ liệu telemetry nào ra bên ngoài.
3. **Anti-Tamper Signature & Read-Only Lock (Chống Giả Mạo Báo Cáo):** Sau khi xuất báo cáo, hệ thống tự động tính toán mã băm `SHA-256 Checksum Signature` và khóa thuộc tính **Chỉ đọc (`FileAttributes.ReadOnly`)** trên tất cả các file kết quả nhằm ngăn chặn mọi hành vi chỉnh sửa trái phép làm sai lệch kết quả kiểm toán.

---

## II. HƯỚNG DẪN CHẠY NHANH (`QUICK START`)

### Cách 1: Chạy trực tiếp (Dành cho Người dùng phổ thông / Quản lý IT)
1. Mở thư mục sản phẩm `LicenseIntelligencePlatform-v1.0.0-win-x64`.
2. Nhấp đúp chuột vào file **`LicenseIntelligencePlatform.Presentation.Cli.exe`**.
3. Chương trình sẽ hiển thị **Màn hình Chào mừng (Welcome Banner)** ghi nhận các cam kết bảo mật (100% Read-Only & Offline). Nhấn phím **[ENTER]** để bắt đầu quét.
4. Quá trình kiểm kê toàn bộ hệ thống diễn ra siêu tốc trong vòng chưa đến **1 giây (`~500ms`)**.
5. Toàn bộ kết quả kiểm toán và nhật ký hệ thống sẽ được tự động đóng gói gọn gàng vào duy nhất **1 thư mục gốc mang tên theo ngày giờ** (Ví dụ: **`License_Audit_Results_2026-07-16_16h35m\`**) nằm ngay tại nơi bạn chạy file `.exe`. Bạn sẽ không bao giờ bị rác file hay lẫn lộn dữ liệu giữa các lần quét.
6. Tại bước cuối cùng, bạn có thể nhấn phím **[ENTER / Y]** để mở ngay Báo cáo Web HTML trực quan trên trình duyệt của máy!

### Cách 2: Chạy qua Command Line (Dành cho System Admin / DevOps / SIEM)
Mở `PowerShell` hoặc `Command Prompt` tại thư mục chứa file `.exe` và chạy các lệnh tùy chọn:

```powershell
# Chạy ở chế độ tự động hóa batch (không dừng chờ phím Enter ở màn hình chào mừng hay khi kết thúc)
.\LicenseIntelligencePlatform.Presentation.Cli.exe --no-pause

# Chỉ xuất báo cáo ra định dạng cụ thể (Ví dụ: XLSX và HTML) ra thư mục tùy chọn
.\LicenseIntelligencePlatform.Presentation.Cli.exe --format XLSX,HTML --output "D:\AuditReports\2026_Q3" --no-pause

# Xem toàn bộ các tham số cấu hình nâng cao
.\LicenseIntelligencePlatform.Presentation.Cli.exe --help
```

---

## III. CẤU TRÚC & CHI TIẾT CÁC ĐỊNH DẠNG BÁO CÁO TRONG THƯ MỤC TỔNG HỢP

Sau khi hoàn tất quét, bên trong thư mục theo ngày giờ (Ví dụ: `License_Audit_Results_2026-07-16_16h35m\`) sẽ có cấu trúc ngăn nắp như sau:

- **2 File Báo Cáo Chính (Nằm ngay ngoài thư mục gốc để người dùng dễ tìm nhất):**
  - `Bao_Cao_Tong_Hop_Ban_Quen_<Ngày-Giờ>.xlsx`
  - `Bao_Cao_Truc_Quan_Web_<Ngày-Giờ>.html`
- **Thư mục `raw_data/`:** Chứa dữ liệu thô (`CSV`, `JSON`, `Audit Markdown`, file backlog, gói chẩn đoán `.zip` và file mã băm `sha256_manifest.json`).
- **Thư mục `system_logs/`:** Chứa nhật ký hệ thống (`application.log`, `audit.log`).

### 1. Bảng Tính Excel Chuyên Nghiệp (`Bao_Cao_Tong_Hop_Ban_Quen_...xlsx`) — *Khuyên Dùng*
Được xây dựng trên chuẩn OpenXML gồm **4 Trang tính (Tabs)** chuyên biệt:
- **`Executive Dashboard`**: Bảng điều khiển KPI tổng hợp, tỷ lệ phần trăm các nhóm bản quyền, thời gian quét chuẩn **Giờ Việt Nam (`VN Time - UTC+7`)**.
- **`Full Inventory & Audit`**: Danh sách toàn diện 12 cột thông tin chuyên sâu cho mỗi phần mềm (`Software Package`, `Version`, `Publisher`, `Install Path`, `Install Date`, `Last Updated - VN Time`, `Last Used / Active - VN Time`, `Scan Source`, `License Type`, `Confidence`, `Plugin Detector`, `Verification Evidence`), có sẵn bộ lọc (`Auto-Filter`), cố định dòng tiêu đề (`Sticky Header`) và tô nền màu cảnh báo:
  - 🔴 **Màu hồng nhạt (`#FEE2E2`)**: Phần mềm thương mại (`Commercial`) cần kiểm tra giấy phép mua sắm.
  - 🔵 **Màu xanh dương nhạt (`#E0F2FE`)**: Phần mềm mã nguồn mở (`OpenSource`).
  - 🟢 **Màu xanh lá (`#DCFCE7`)**: Các phần mềm đã được xác minh chính xác bằng Plugin tri thức chuyên sâu (`Verified`).
- **`Commercial Licenses (Action)`**: Tab lọc riêng danh sách các phần mềm thương mại (`Docker Desktop`, `SQL Server`, `Unity`, `TablePlus`, `Figma`...) để bộ phận tài chính/pháp chế kiểm tra số lượng license đã mua.
- **`Open Source Compliance`**: Tab lọc riêng các phần mềm mã nguồn mở và bằng chứng tuân thủ (MIT, Apache 2.0, GPL...).

### 2. Báo Cáo Trực Quan Web HTML (`Bao_Cao_Truc_Quan_Web_...html`)
- Giao diện Dark Mode sang trọng, hiển thị trọn vẹn **12 cột thông tin kiểm kê chi tiết** (đường dẫn cài đặt, ngày giờ cài đặt, thời điểm cập nhật lần cuối, trạng thái hoạt động/lần dùng gần nhất).
- Bảng dữ liệu tự động co giãn (`table-layout: auto`), thiết lập độ rộng tối thiểu thoáng đãng, chống trôi header (`Sticky Header`) và bảo đảm **không bao giờ bị dính chữ hay đè chữ vào nhau**.

### 3. Hồ Sơ Kiểm Toán Pháp Lý (`audit_report_<ScanId>.md`)
- Định dạng Markdown chuẩn dành cho bộ phận kiểm toán nội bộ và luật sư sở hữu trí tuệ. Liệt kê chi tiết trọng số bằng chứng (`Evidence Weights`), độ tin cậy (`Confidence Level`) và phân tích rủi ro.

### 4. Dữ Liệu Tích Hợp Hệ Thống (`.CSV` & `.JSON`)
- `license_report_<ScanId>.csv`: Chuẩn hóa dữ liệu bảng để nạp vào hệ thống ERP / Asset Management.
- `license_report_<ScanId>.json` & `statistics_report_<ScanId>.json`: Cấu trúc JSON đầy đủ để tích hợp vào các pipeline CI/CD hoặc SIEM (Security Information and Event Management).

### 5. Danh Sách Tồn Đọng Cần Bổ Sung (`backlog_need_plugins.json`)
- Liệt kê các phần mềm mới chưa có Plugin tri thức chuyên sâu, giúp đội ngũ kỹ thuật có cơ sở mở rộng bộ Plugin SDK trong tương lai.

---

## IV. CÁC HÀNH VI BẢO MẬT & THỰC THI HỢP LỆ (`FAQ & TROUBLESHOOTING`)

**Q: Tại sao tôi không thể xóa hoặc chỉnh sửa nội dung file báo cáo `.xlsx` hay `.html` sau khi quét?**
> **A:** Vì lý do bảo mật và tính toàn vẹn pháp lý, hệ thống tự động khóa thuộc tính **Chỉ đọc (`FileAttributes.ReadOnly`)** cho tất cả file xuất ra. Nếu bạn thực sự cần chỉnh sửa hoặc xóa, hãy nhấp chuột phải vào file -> chọn `Properties` -> bỏ tích ô `Read-only` -> chọn `Apply` (Hoặc chạy lệnh PowerShell: `Get-ChildItem -Path .\reports\* | ForEach-Object { $_.Attributes = 'Normal' }`).

**Q: Hệ thống có bỏ sót ứng dụng Portable hay ứng dụng chạy không cần cài đặt không?**
> **A:** Không. Bên cạnh việc quét Registry tiêu chuẩn, LIP tích hợp `DeepFileSystemScanner` quét quét sâu các tệp thực thi (`.exe`, `.dll`, `.jar`) trong các thư mục người dùng (`AppData\Local`, `AppData\Roaming`), kiểm tra cấu trúc PE Header, chữ ký số Authenticode và VersionInfo để nhận diện chính xác.

---
*Bản quyền phần mềm thuộc về Bá Lộc Vũ (DynamiteV) — Hỗ trợ kỹ thuật 24/7.*
