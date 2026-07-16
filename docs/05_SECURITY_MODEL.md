# 05_SECURITY_MODEL.md

# License Intelligence Platform (LIP)

## Security Model

Version: 1.0

Status: Stable

Author: DynamiteV

---

# Purpose

Tài liệu **Security Model** định nghĩa toàn bộ triết lý, kiến trúc bảo mật, các nguyên tắc bắt buộc (Security Invariants) và mô hình đe dọa (Threat Model) của **License Intelligence Platform (LIP)**.

Mục tiêu cốt lõi của LIP khi được triển khai trong các môi trường doanh nghiệp (Enterprise), cơ quan chính phủ và hệ thống mạng nội bộ cách ly (Air-Gapped Networks) là:

- **Tuyệt đối không làm thay đổi trạng thái hệ thống máy trạm/máy chủ (Read-Only Enforcement).**
- **Tuyệt đối không rò rỉ dữ liệu doanh nghiệp ra bên ngoài (Zero Network / Privacy-First).**
- **Hoạt động với đặc quyền tối thiểu (Least Privilege Execution), không yêu cầu quyền Administrator/Root để quét các vùng dữ liệu thông thường.**
- **Cách ly hoàn toàn rủi ro từ các Plugin bên thứ ba (Plugin Sandboxing & Fault Tolerance).**
- **Tuân thủ các tiêu chuẩn bảo mật và riêng tư nghiêm ngặt nhất (GDPR, ISO/IEC 27001, SOC 2 Type II Readiness).**

---

# Security Architecture Overview

LIP áp dụng mô hình phòng thủ nhiều lớp (**Defense-in-Depth**) với các đường ranh giới bảo mật (**Security Boundaries**) rõ ràng giữa các tầng kiến trúc theo tiêu chuẩn Clean Architecture:

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Operating System (Windows / Linux)                                               │
│  ├── Registry Hives (HKLM / HKCU) [Read-Only Boundary]                           │
│  ├── File System (/var/lib/dpkg, Program Files) [Metadata Read-Only Boundary]    │
│  └── Running Processes [Memory Query Boundary]                                   │
└────────────────────────────────────────▲─────────────────────────────────────────┘
                                         │ (Strict Read-Only APIs)
┌────────────────────────────────────────┴─────────────────────────────────────────┐
│ Infrastructure Layer (Scanners)                                                  │
│  ├── WindowsRegistryScanner (OpenSubKey Read-Only, No SetValue/CreateSubKey)     │
│  ├── LinuxPackageScanner (File Metadata Parsing Only)                            │
│  └── DeepFileSystemScanner (Process & File Metadata Only, No Content Reading)    │
└────────────────────────────────────────▲─────────────────────────────────────────┘
                                         │ (SoftwareInfo Entities - In-Memory)
┌────────────────────────────────────────┴─────────────────────────────────────────┐
│ Core Engine / Application Layer                                                  │
│  ├── CoreEngine & Scanner Coordinator                                            │
│  ├── Rule Engine & Confidence Engine (Deterministic Business Logic)              │
│  └── PluginCompatibilityValidator (SDK Compatibility Check Before Execution)     │
└────────────────────────────────────────▲─────────────────────────────────────────┘
                                         │ (Isolated I/O / Try-Catch Sandbox)
┌────────────────────────────────────────┴─────────────────────────────────────────┐
│ Plugins Layer (26 Standard Plugins + Third-Party Plugins)                        │
│  ├── License Header Reader (Keyword Match ≤ 2KB Only)                            │
│  ├── CancellationToken Enforcement (No Timeout / Infinite Loop Tolerance)        │
│  └── Zero Network & Zero Registry Write Enforcement                              │
└────────────────────────────────────────┬─────────────────────────────────────────┘
                                         │ (Structured ScanReport)
┌────────────────────────────────────────▼─────────────────────────────────────────┐
│ Presentation Layer (CLI / Output Exporter)                                       │
│  └── Output Directory (--output) [Exclusive Write Boundary - User Specified]     │
└──────────────────────────────────────────────────────────────────────────────────┘
```

---

# Core Security Principles & Invariants

Toàn bộ mã nguồn của LIP phải tuân thủ vô điều kiện 7 nguyên tắc bảo mật (**Security Invariants**). Bất kỳ Vi phạm nào đối với các nguyên tắc này trong Pull Request đều bị từ chối ngay lập tức during Code Review.

| Nguyên tắc | Mô tả chi tiết | Hiện thực kỹ thuật trong Codebase |
| :--- | :--- | :--- |
| **1. Read-Only Enforcement** | Hệ thống chỉ đọc dữ liệu để phân tích, không bao giờ ghi, sửa đổi hay xóa bất kỳ tài nguyên nào trên hệ thống điều hành. | - Scanner chỉ sử dụng `RegistryKey.OpenSubKey(..., writable: false)`.<br>- Chỉ gọi `File.GetLastWriteTime()`, `File.Exists()`, `Directory.GetFiles()` để lấy metadata.<br>- Cấm tuyệt đối `RegistryKey.SetValue()`, `RegistryKey.CreateSubKey()`, `RegistryKey.DeleteSubKey()`, `File.WriteAllText()`, `File.Delete()` trên vùng hệ thống. |
| **2. Least Privilege Execution** | Phầm mềm chạy dưới quyền của người dùng hiện tại (Standard User Mode). Không bắt buộc thăng quyền (UAC Elevation / Root). | - Khi truy cập các nhánh Registry hoặc thư mục bị giới hạn quyền, `try/catch (UnauthorizedAccessException)` sẽ bắt lỗi, ghi chú warning vào log và graceful skip sang đối tượng tiếp theo, không crash ứng dụng. |
| **3. Zero Network Connectivity** | LIP là một nền tảng **100% Air-Gapped**. Không có bất kỳ giao tiếp mạng nào được thực hiện trong suốt vòng đời thực thi. | - Codebase hoàn toàn không chứa tham chiếu đến `System.Net.Http.HttpClient`, `System.Net.Sockets.Socket`, `System.Net.WebClient`, hay các thư viện gRPC/REST client.<br>- Không gửi Telemetry, không gọi Phone-Home, không kiểm tra cập nhật qua mạng tự động. |
| **4. No Process Injection & Payload Exec** | Không can thiệp, tiêm nhiễm mã hay chạy ngầm các tiến trình con không an toàn. | - Cấm gọi `Process.Start()` với các command-line payload nhạy cảm.<br>- Không sử dụng `DllImport` để thực hiện DLL Injection, API Hooking hay Memory Scraping của các tiến trình khác. |
| **5. Zero Credential & Secret Access** | Không truy cập, thu thập hay quét qua các vùng lưu trữ thông tin xác thực của người dùng hoặc hệ thống. | - Cấm truy cập Windows Credential Manager, DPAPI (Data Protection API), Linux Keyring/Keychain, SSH private keys (`~/.ssh`), GPG keys, hay file cấu hình chứa mật khẩu (`.env`, `credentials.json`). |
| **6. Isolated Plugin Sandbox** | Mỗi Plugin chạy như một mô-đun độc lập, rủi ro lỗi từ Plugin được cô lập hoàn toàn khỏi Core Engine. | - Mọi lời gọi `plugin.CheckLicenseAsync(software, cancellationToken)` đều được bọc trong khối `try/catch` tại Core Engine.<br>- Nếu Plugin ném ra Exception (Out of Memory, NullReference, I/O Error), Core Engine cô lập lỗi, tạo `LicenseCheckResult` fallback và tiếp tục xử lý các Plugin khác. |
| **7. Explicit Output Boundary** | Mọi tệp tin báo cáo đầu ra chỉ được ghi vào duy nhất thư mục do người dùng chỉ định qua cờ `--output`. | - Không tự ý tạo thư mục tạm trong `C:\Windows\System32`, `C:\Program Files`, hay ghi đè lên các tệp tin hiện có của hệ thống mà không có sự cho phép rõ ràng. |

---

# Privacy Policy & Enterprise Data Protection

LIP được thiết kế theo mô hình **Privacy-First**. Chúng tôi phân định rõ ràng giữa **Dữ liệu được phép xử lý (Allowed Metadata)** và **Dữ liệu bị cấm tuyệt đối (Forbidden User/System Data)**.

## 1. Dữ liệu KHÔNG BAO GIỜ thu thập hay đọc (Forbidden Data)

| Loại dữ liệu | Trạng thái bảo mật | Lý do & Biện pháp kỹ thuật |
| :--- | :---: | :--- |
| **Mật khẩu, PIN, Credentials, Tokens** | ❌ **CẤM TUYỆT ĐỐI** | Không truy cập Credential Store, không đọc header Authorization hay file cấu hình nhạy cảm. |
| **Cookie & Lịch sử Trình duyệt Web** | ❌ **CẤM TUYỆT ĐỐI** | Cấm truy cập thư mục `User Data` của Chrome/Edge/Firefox/Safari để lấy SQLite DB chứa Cookie/History. |
| **Nội dung Tài liệu Văn bản (Word, Excel, PDF...)** | ❌ **CẤM TUYỆT ĐỐI** | Chỉ đọc metadata (tên file, kích thước); cấm gọi `File.ReadAllText()` hay `File.ReadAllBytes()` trên tài liệu người dùng. |
| **Hình ảnh, Video, Âm thanh cá nhân** | ❌ **CẤM TUYỆT ĐỐI** | Bỏ qua hoàn toàn các định dạng `.jpg`, `.png`, `.mp4`, `.mov`, `.mp3` trong quá trình quét hệ thống file. |
| **Thao tác bàn phím (Keylogging) & Mouse** | ❌ **CẤM TUYỆT ĐỐI** | Không đăng ký Global Keyboard/Mouse Hooks (`SetWindowsHookEx`). |
| **Dữ liệu Clipboard** | ❌ **CẤM TUYỆT ĐỐI** | Cấm gọi `Clipboard.GetText()` hoặc các API tương đương. |
| **Email, Tin nhắn, Chat Logs** | ❌ **CẤM TUYỆT ĐỐI** | Cấm truy cập hồ sơ Outlook (.pst, .ost), Teams cache, Slack storage, Zalo/Telegram local DB. |
| **Dữ liệu Nghiệp vụ Bên trong Ứng dụng** | ❌ **CẤM TUYỆT ĐỐI** | Không query vào các cơ sở dữ liệu nghiệp vụ của doanh nghiệp (SQL Server user tables, Oracle DB, PostgreSQL records). |

## 2. Dữ liệu Metadata ĐƯỢC PHÉP thu thập (Allowed Metadata)

LIP chỉ thu thập các thông tin siêu dữ liệu (Metadata) phục vụ định danh phần mềm và xác minh giấy phép:

- **Từ Windows Registry (`HKLM/HKCU...\Uninstall`)**:
  - `DisplayName` (Tên phần mềm)
  - `DisplayVersion` (Phiên bản)
  - `Publisher` (Nhà phát hành)
  - `InstallLocation` (Đường dẫn cài đặt)
  - `InstallDate` (Ngày cài đặt)
  - `UninstallString` (Chuỗi gỡ cài đặt - dùng để phân loại loại bộ cài đặt)
- **Từ File System Metadata**:
  - Đường dẫn file thực thi chính (`.exe`, `.dll`, `.so`) bên trong `InstallLocation`.
  - Version Info (`FileVersionInfo.GetVersionInfo()`).
  - Digital Certificate / Signature Metadata (Tên đơn vị ký số, tình trạng hợp lệ của chữ ký).
- **Từ License File Header (Giới hạn tối đa ≤ 2KB)**:
  - Khi Plugin phát hiện các file có tên đặc trưng (`LICENSE`, `LICENSE.txt`, `COPYING`, `.lic`, `.key`), Plugin **chỉ được phép đọc tối đa 2048 bytes đầu tiên** (Header) để tìm từ khóa phân loại (`GPL-3.0`, `MIT`, `Apache-2.0`, `Proprietary Commercial`).

---

# Threat Model & STRIDE Risk Mitigation Matrix

Để đảm bảo hệ thống an toàn trước mọi kịch bản tấn công hoặc lạm dụng, LIP thực hiện phân tích rủi ro theo mô hình **STRIDE** (Spoofing, Tampering, Repudiation, Information Disclosure, Denial of Service, Elevation of Privilege).

## 1. STRIDE Analysis & Risk Matrix

```
┌───────────────────────────┬────────────────────────────────────────────────────────────────────────┐
│ STRIDE Category           │ Core Threat Vector in LIP Ecosystem                                    │
├───────────────────────────┼────────────────────────────────────────────────────────────────────────┤
│ S - Spoofing              │ Giả mạo danh tính phần mềm (Tên, Publisher) để lách kiểm tra License.  │
│ T - Tampering             │ Sửa đổi Registry hoặc làm giả file License để biến phần mềm lậu thành  │
│                           │ hợp lệ (Freeware/Commercial).                                          │
│ R - Repudiation           │ Phủ nhận kết quả quét hoặc thiếu bằng chứng xác thực (Evidence Audit). │
│ I - Information Disclosure│ Rò rỉ dữ liệu nhạy cảm của máy trạm ra log file hoặc báo cáo xuất ra.  │
│ D - Denial of Service     │ Plugin lặp vô hạn, tiêu tốn cạn kiệt RAM/CPU khiến hệ thống đơ nghẽn. │
│ E - Elevation of Privilege│ Lợi dụng LIP để leo thang đặc quyền từ Standard User lên SYSTEM/Admin.  │
└───────────────────────────┴────────────────────────────────────────────────────────────────────────┘
```

## 2. Risk Mitigation Strategies

| Mối đe dọa (Threat Vector) | Mức rủi ro | Biện pháp kỹ thuật đối phó (Mitigation Architecture) |
| :--- | :---: | :--- |
| **S1: Giả mạo Publisher / Registry Name**<br>Phần mềm độc hại tự đặt tên là `Microsoft Corporation` hoặc `Adobe Systems` trong Registry. | **Trung bình** | - **Multi-Evidence Verification**: Rule Engine không bao giờ tin tưởng duy nhất `Publisher` từ Registry.<br>- Plugin thực hiện đối chiếu chéo với **Digital Signature** (`X509Certificate`) của tệp thực thi `.exe`. Nếu chữ ký số không khớp hoặc không hợp lệ, `Confidence Score` bị trừ mạnh xuống mức `< 0.30` hoặc đánh dấu `Unknown/Suspicious`. |
| **T1: Làm giả file License Header**<br>Người dùng/Malware chèn text `MIT License` vào file `.exe` hoặc thư mục cài đặt của phần mềm thương mại. | **Trung bình** | - **Priority & Pattern Weighting**: Plugin phân loại giấy phép thương mại (Commercial) luôn có **Priority cao hơn** Plugin mã nguồn mở (Freeware/OpenSource).<br>- Nếu phát hiện đồng thời chữ ký thương mại (Commercial Binary Signature) và file `LICENSE` chứa text MIT, Rule Engine áp dụng quy tắc xung đột (`Conflict Resolution`), ưu tiên kết quả Commercial và báo cáo `Evidence Xung Đột`. |
| **I1: Rò rỉ đường dẫn cá nhân vào Báo cáo**<br>Tên người dùng Windows (`C:\Users\JohnDoe\AppData\...`) xuất hiện trong các báo cáo xuất ra công cộng. | **Thấp** | - **Sanitization Option**: Khi xuất báo cáo cho bên thứ ba kiểm toán, Exporter cung cấp cờ `--sanitize-paths` để ẩn tên user (chuyển thành `C:\Users\<USER>\AppData\...`), bảo vệ quyền riêng tư theo tiêu chuẩn GDPR. |
| **D1: Plugin lặp vô hạn (Infinite Loop DOS)**<br>Một Plugin bị lỗi logic hoặc cố tình lặp vô hạn khi đọc một cấu trúc directory phức tạp. | **Cao** | - **CancellationToken & Timeout Guard**: Core Engine truyền một `CancellationToken` với `TimeSpan` timeout giới hạn (mặc định 5000ms cho mỗi Plugin trên mỗi Software) vào `CheckLicenseAsync()`.<br>- Khi vượt quá thời gian, Task bị hủy bỏ ngay lập tức, giải phóng luồng và ghi log `TimeoutException`. |
| **D2: Cạn kiệt bộ nhớ (Memory Exhaustion OOM)**<br>Plugin cố gắng đọc toàn bộ một file nhị phân lớn (ví dụ `iso` 4GB hoặc `exe` 500MB) vào RAM. | **Cao** | - **Stream Buffer Limit**: Mọi thao tác đọc I/O trong các Plugin chuẩn (`Plugins.Standard`) bắt buộc phải dùng `StreamReader` với bộ đệm nhỏ và giới hạn đọc (`MaxReadBytes = 2048`). Cấm sử dụng `File.ReadAllBytes()` trên file không rõ kích thước. |
| **E1: Leo thang đặc quyền qua DLL Hijacking**<br>Kẻ tấn công đặt DLL độc hại (`version.dll`, `userenv.dll`) vào thư mục chạy CLI để LIP load dưới quyền cao. | **Cao** | - **Secure Assembly Loading**: LIP cấu hình `AssemblyLoadContext` nghiêm ngặt, chỉ nạp DLL từ thư mục cài đặt chính thức và kiểm tra chữ ký/hash của các Plugin chuẩn (`Plugins.Standard.dll`) trước khi khởi tạo. |
| **E2: Lỗi Access Denied gây Crash hệ thống**<br>LIP quét trúng vùng Registry nhạy cảm của SYSTEM (`SAM`, `SECURITY`). | **Trung bình** | - **Access Denied Isolation**: `WindowsRegistryScanner` áp dụng `try/catch (SecurityException, UnauthorizedAccessException)` tại từng node. Vùng không có quyền đọc được bỏ qua trong yên lặng mà không làm sập tiến trình. |

---

# Plugin Security & Compatibility Sandbox

Phần mềm cho phép mở rộng qua các Plugin (`ILicensePlugin`). Để ngăn chặn các Plugin bên ngoài gây tổn hại đến sự ổn định và bảo mật của toàn bộ nền tảng, LIP thiết lập quy trình kiểm tra và ranh giới thực thi vô cùng chặt chẽ.

## 1. Plugin Manifest & Compatibility Validation

Trước khi bất kỳ Plugin nào được đưa vào danh sách thực thi (`RuleEngine`), thành phần `PluginCompatibilityValidator` sẽ kiểm tra tính hợp lệ của metadata khai báo trong `Manifest`:

```csharp
public sealed class PluginManifest
{
    public string PluginName { get; init; } = string.Empty;
    public string Version { get; init; } = "1.0.0";
    public string Author { get; init; } = string.Empty;
    public PluginPriority Priority { get; init; } = PluginPriority.Normal;
    
    // Yêu cầu tương thích SDK tối thiểu (Ví dụ: "1.0")
    public string MinSdkVersion { get; init; } = "1.0";
}
```

**Quy tắc xác thực (`PluginCompatibilityValidator`):**
- Nếu `plugin.Manifest` là `null` $\to$ **Loại bỏ ngay lập tức** (Throw `InvalidPluginManifestException` hoặc Log Warning và Ignore).
- Nếu `plugin.Manifest.MinSdkVersion` lớn hơn phiên bản SDK hiện tại của Core Engine $\to$ **Từ chối nạp Plugin** để tránh lỗi tương thích nhị phân (Binary Incompatibility).
- Nếu `plugin.Priority` được thiết lập không hợp lệ $\to$ Đưa về mức mặc định `PluginPriority.Low`.

## 2. Runtime Isolation & Exception Boundary

Mỗi Plugin là một thành phần **untrusted** đối với Core Engine. Dưới đây là mô hình xử lý lỗi chuẩn trong `CoreEngine` khi điều phối các Plugin:

```csharp
// Minh họa cơ chế Sandbox Isolation trong CoreEngine
public async Task<LicenseCheckResult> ExecutePluginSafeAsync(
    ILicensePlugin plugin, 
    SoftwareInfo software, 
    CancellationToken cancellationToken)
{
    try
    {
        // 1. Kiểm tra nhanh CanCheck trước khi gọi I/O nặng
        if (!plugin.CanCheck(software))
        {
            return LicenseCheckResult.NotApplicable(plugin.Manifest.PluginName);
        }

        // 2. Thực thi với Timeout CancellationToken
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(5000)); // 5 giây tối đa cho 1 plugin

        var result = await plugin.CheckLicenseAsync(software, timeoutCts.Token);
        
        // 3. Đảm bảo Plugin không trả về null trái phép
        return result ?? LicenseCheckResult.CreateFallback(
            plugin.Manifest.PluginName, 
            LicenseType.Unknown, 
            0.0, 
            "Plugin returned null result.");
    }
    catch (OperationCanceledException)
    {
        _logger.LogWarning("Plugin {PluginName} timed out after 5000ms on software {SoftwareName}.", 
            plugin.Manifest.PluginName, software.Name);
            
        return LicenseCheckResult.CreateErrorResult(
            plugin.Manifest.PluginName, 
            "Plugin execution timed out (Timeout Guard triggered).");
    }
    catch (Exception ex)
    {
        // Cô lập hoàn toàn Exception, KHÔNG để sập Core Engine
        _logger.LogError(ex, "Plugin {PluginName} threw unhandled exception while checking {SoftwareName}.", 
            plugin.Manifest.PluginName, software.Name);

        return LicenseCheckResult.CreateErrorResult(
            plugin.Manifest.PluginName, 
            $"Isolated Plugin Error: {ex.Message}");
    }
}
```

---

# Data Flow Security & Storage Boundaries

Luồng dữ liệu trong LIP tuân theo nguyên tắc **One-Way Read-Only Pipeline** từ hệ thống đích đến thư mục xuất báo cáo:

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Step 1: Read-Only Collection                                                     │
│ Windows Registry / Linux dpkg / File System Metadata                             │
└────────────────────────────────────────┬─────────────────────────────────────────┘
                                         │ (Raw Metadata Stream)
┌────────────────────────────────────────▼─────────────────────────────────────────┐
│ Step 2: In-Memory Domain Processing                                              │
│ SoftwareInfo & Evidence Objects created in secure heap memory (RAM)              │
│ *No temporary files created on disk during scan*                                │
└────────────────────────────────────────┬─────────────────────────────────────────┘
                                         │ (Analyzed Domain Results)
┌────────────────────────────────────────▼─────────────────────────────────────────┐
│ Step 3: Secure Report Generation                                                 │
│ Exporter writes JSON / CSV / TXT reports directly to user-defined --output path  │
└──────────────────────────────────────────────────────────────────────────────────┘
```

## Quy tắc bảo mật lưu trữ (Storage Security Rules):

1. **Không tạo File tạm không cần thiết (Zero Temp File Proliferation)**:
   - Trong suốt quá trình quét và phân tích, tất cả đối tượng `SoftwareInfo`, `Evidence`, và `LicenseCheckResult` được lưu giữ hoàn toàn trong bộ nhớ RAM (In-Memory).
   - Không tạo các file trung gian (`.tmp`, `.cache`, `.dump`) trong `C:\Windows\Temp` hay `/tmp` trừ trường hợp ghi log chẩn đoán khi được yêu cầu rõ ràng qua cờ `--log-level Debug`.
2. **Quản lý quyền truy cập thư mục Output (`--output`)**:
   - Khi CLI xuất báo cáo ra thư mục đích, nếu thư mục chưa tồn tại, LIP sẽ tạo thư mục với quyền mặc định của người dùng hiện tại (`Standard Directory Security`).
   - Các tệp tin báo cáo (`license_report.json`, `executive_summary.txt`) là **nhạy cảm đối với doanh nghiệp** vì chứa danh sách toàn bộ phần mềm cài đặt. Người dùng CLI có trách nhiệm bảo vệ thư mục `--output` bằng quyền truy cập hệ thống file (NTLM / POSIX permissions).

---

# Security Verification & Code Review Checklist

Toàn bộ đội ngũ kỹ sư phát triển (Core Contributors), nhà phát triển Plugin (Plugin Developers) và AI Coding Assistants bắt buộc phải sử dụng các Checklist sau đây trước khi Commit, Pull Request hoặc Deploy hệ thống.

## 1. Core Codebase Security Checklist (Dành cho Core Engineers & AI)

- [ ] **No Network APIs**: Kiểm tra toàn bộ Solution bằng `grep` / Search, đảm bảo **KHÔNG CÓ** bất kỳ tham chiếu nào tới `HttpClient`, `WebClient`, `Socket`, `TcpClient`, `UdpClient`, `HttpWebRequest`.
- [ ] **No Registry Write APIs**: Đảm bảo tuyệt đối **KHÔNG CÓ** phương thức `SetValue()`, `CreateSubKey()`, `DeleteSubKey()`, hay `DeleteValue()` trong `RegistryKey`.
- [ ] **No File Modification APIs**: Đảm bảo `Scanner` và `Plugin` không gọi `File.WriteAllText()`, `File.Delete()`, `Directory.Delete()`, hoặc `File.Move()`.
- [ ] **Safe File Reading Limit**: Mọi lời gọi đọc file License (như `File.OpenRead()`, `StreamReader`) đều phải có giới hạn kích thước đọc (`MaxReadBytes ≤ 2048`), không đọc toàn bộ file vào RAM bằng `File.ReadAllText()` trên các file không xác định kích thước.
- [ ] **CancellationToken Propagation**: Mọi phương thức bất đồng bộ (`async Task`) trong `Application`, `Infrastructure`, và `Plugins` đều nhận vào `CancellationToken` và truyền tiếp xuống các API hệ thống (I/O, Task.Delay).
- [ ] **No Hardcoded Secrets**: Không chứa bất kỳ API key, token, mật khẩu, hay connection string hardcoded nào trong mã nguồn (`.cs`, `.json`, `.yaml`).
- [ ] **Graceful Exception Handling**: Mọi vòng lặp duyệt Registry hoặc thư mục đều có `try/catch (UnauthorizedAccessException, SecurityException, IOException)` để đảm bảo khả năng chịu lỗi và tiếp tục quét.

## 2. Plugin Development Security Checklist (Dành cho Plugin Authors)

- [ ] **CanCheck() Safety**: Phương thức `CanCheck(SoftwareInfo software)` thực hiện kiểm tra nhanh trong RAM (`string.Equals`, `string.Contains`), **KHÔNG** thực hiện I/O ổ cứng nặng hoặc ném ra Exception.
- [ ] **Try-Catch Around I/O**: Bên trong `CheckLicenseAsync()`, mọi thao tác kiểm tra file tồn tại (`File.Exists`) hoặc đọc header (`StreamReader`) đều được bọc trong `try/catch`.
- [ ] **Path Traversal Protection**: Khi xử lý `software.InstallLocation` hoặc `software.ExecutablePath`, kiểm tra kỹ đường dẫn, không cho phép truy cập ngược ra ngoài bằng các chuỗi nguy hiểm (`../../`).
- [ ] **Valid Manifest Declaration**: Plugin khai báo đầy đủ `PluginName`, `Version`, `Author`, và `MinSdkVersion = "1.0"`.
- [ ] **No Process Execution**: Plugin tuyệt đối **KHÔNG ĐƯỢC** gọi `Process.Start()` để chạy các tiện ích dòng lệnh bên ngoài (`wmic.exe`, `powershell.exe`, `cmd.exe`).

## 3. Deployment & Operational Security Checklist (Dành cho DevOps / System Admins)

- [ ] **Run as Standard User**: Khuyến nghị thực thi LIP CLI dưới tài khoản người dùng thông thường (Non-Admin / Standard User) trong các lịch trình quét định kỳ.
- [ ] **Verify Output Directory Permissions**: Đảm bảo thư mục được chỉ định bởi `--output` (ví dụ `D:\Reports`) chỉ cho phép các Quản trị viên IT hoặc hệ thống thu thập log (SIEM) truy cập.
- [ ] **Audit Trail Log Verification**: Khi chạy với `--log-level Info` hoặc `Debug`, kiểm tra file log (`application.log`, `audit.log`) để xác minh không có lỗi lạ hoặc hành vi truy cập trái phép bị chặn.
- [ ] **Binary Integrity Verification**: Kiểm tra chữ ký số SHA-256 / Authenticode của tệp thực thi `LicenseIntelligencePlatform.Presentation.Cli.exe` trước khi phân phối xuống hàng loạt máy trạm.

---

# Summary & Architectural Compliance

**Security Model** của License Intelligence Platform không phải là một tính năng bổ sung, mà là **nền tảng kiến trúc sống còn** giúp LIP tự tin triển khai trong bất kỳ môi trường an ninh nhạy cảm nào.

Bằng cách duy trì kiên định 3 trụ cột:
1. **Zero-Trust Read-Only Scanner** (Chỉ đọc metadata, không chỉnh sửa hệ thống).
2. **Air-Gapped Zero-Network Execution** (Không kết nối mạng, không rò rỉ dữ liệu).
3. **Sandbox Isolated Plugins** (Cô lập rủi ro lỗi bên thứ ba).

LIP mang lại giải pháp phân tích bản quyền phần mềm minh bạch, chính xác tuyệt đối và an toàn tối đa cho mọi tổ chức và doanh nghiệp.