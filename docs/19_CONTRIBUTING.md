# 19_CONTRIBUTING.md

# License Intelligence Platform (LIP)

## Enterprise Contributing Guidelines & Plugin Development Protocol

Version: 1.0

Status: Stable (Phase 0 – Phase 4 Completed)

Author: DynamiteV

---

# 1. Purpose & Contribution Philosophy

Cảm ơn bạn đã quan tâm đóng góp cho **License Intelligence Platform (LIP)**! Là một dự án theo đuổi tiêu chuẩn **Clean Architecture 5 tầng** và **SOLID**, chúng tôi chào đón mọi đóng góp từ cộng đồng Lập trình viên, Kỹ sư Hệ thống, và **AI Coding Assistants**.

Quy tắc đóng góp của LIP dựa trên 3 nguyên tắc vàng:
1. **Không bao giờ phá vỡ ranh giới tầng:** Không thêm thư viện bên thứ ba vào `Domain`. Các `IScanner` trong `Infrastructure` phải 100% `Read-Only`.
2. **Luôn đi kèm Unit Test:** Mọi Plugin hoặc tính năng mới phải có Unit Test kiểm chứng đi kèm và đảm bảo `dotnet test` đạt **100% passed (`36/36+ tests green`)**.
3. **Thang điểm 100 bằng chứng minh bạch:** Không trả về kết quả phán đoán mò mẫm. `ConfidenceLevel.Verified` chỉ dành cho phần mềm có `Evidence` rõ ràng ($\ge 70\text{ pts}$).

---

# 2. How to Contribute a New License Plugin (SDK v1.0 Workflow)

Cách đóng góp giá trị nhất cho hệ sinh thái LIP là viết thêm Plugin nhận diện cho các phần mềm đang nằm trong danh sách **Need Plugin Backlog (`backlog_need_plugins.json`)**.

### Bước 1: Thu hoạch từ Backlog (`Harvesting from Backlog`)
Mở file `reports/backlog_need_plugins.json` (được sinh ra tự động từ Phase 4) để chọn 1 phần mềm chưa có plugin (Ví dụ: `Postman`, `Discord`, hay `TeamViewer`).

### Bước 2: Tạo Class Plugin trong `Plugins.Standard`
Trong thư mục `src/LicenseIntelligencePlatform.Plugins.Standard/Plugins/`, tạo một class C# mới kế thừa từ interface `ILicensePlugin`.

### Bước 3: Implement Mã nguồn Mẫu chuẩn (`Production Template`)
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Enums;
using LicenseIntelligencePlatform.Domain.Interfaces;

namespace LicenseIntelligencePlatform.Plugins.Standard.Plugins;

public sealed class MyEnterpriseAppPlugin : ILicensePlugin
{
    // 1. Khai báo Manifest tuân thủ SDK v1.0
    public PluginManifest Manifest => new PluginManifest(
        pluginId: "standard.myenterpriseapp",
        pluginName: "My Enterprise App License Detector",
        pluginVersion: "1.0.0",
        author: "Your Name / AI Assistant",
        description: "Detects commercial subscription for My Enterprise App",
        priority: PluginPriority.CommercialSpecific, // 100
        minSdkVersion: "1.0.0",
        maxSdkVersion: "",
        supportedOs: "Windows, Linux"
    );

    // 2. CanCheck phải siêu tốc < 1ms, cấm I/O
    public bool CanCheck(SoftwareInfo software)
    {
        if (string.IsNullOrWhiteSpace(software.Name))
            return false;

        return software.Name.Contains("My Enterprise App", StringComparison.OrdinalIgnoreCase) ||
               software.Publisher.Contains("MyCompany Inc", StringComparison.OrdinalIgnoreCase);
    }

    // 3. CheckLicenseAsync bọc trong Sandboxed try/catch
    public async Task<LicenseCheckResult> CheckLicenseAsync(SoftwareInfo software, CancellationToken cancellationToken = default)
    {
        var evidences = new List<Evidence>();
        int totalScore = 0;

        try
        {
            // Kiểm tra GUID trong Registry (+20 pts)
            if (software.Name.Contains("My Enterprise App", StringComparison.OrdinalIgnoreCase))
            {
                totalScore += 20;
                evidences.Add(new Evidence(
                    evidenceId: Guid.NewGuid().ToString("N"),
                    evidenceType: EvidenceType.RegistryKey,
                    description: "Detected enterprise application name match in OS Registry.",
                    sourceLocation: software.InstallLocation ?? "OS Registry",
                    weightScore: 20,
                    rawDataSnippet: $"Name: {software.Name} | Publisher: {software.Publisher}"
                ));
            }

            // Kiểm tra file license header (+50 pts)
            if (!string.IsNullOrWhiteSpace(software.InstallLocation) && Directory.Exists(software.InstallLocation))
            {
                var licPath = Path.Combine(software.InstallLocation, "enterprise.lic");
                if (File.Exists(licPath))
                {
                    totalScore += 50;
                    evidences.Add(new Evidence(
                        evidenceId: Guid.NewGuid().ToString("N"),
                        evidenceType: EvidenceType.LicenseFileHeader,
                        description: "Found enterprise license subscription file.",
                        sourceLocation: licPath,
                        weightScore: 50,
                        rawDataSnippet: "ENTERPRISE-SUBSCRIPTION-VALID"
                    ));
                }
            }

            // Phán đoán theo thang điểm 100
            var confidence = totalScore >= 70 ? ConfidenceLevel.Verified :
                             totalScore >= 50 ? ConfidenceLevel.High : ConfidenceLevel.Medium;

            return await Task.FromResult(new LicenseCheckResult(
                pluginId: Manifest.PluginId,
                pluginName: Manifest.PluginName,
                software: software,
                licenseType: LicenseType.Commercial,
                confidenceLevel: confidence,
                evidences: evidences,
                errorMessage: null
            ));
        }
        catch (Exception ex)
        {
            return LicenseCheckResult.CreateErrorResult(Manifest.PluginId, Manifest.PluginName, software, ex.Message);
        }
    }
}
```

### Bước 4: Viết Unit Test & Kiểm chứng
Thêm bài test kiểm chứng cho plugin mới của bạn trong `src/LicenseIntelligencePlatform.Tests/` và chạy lệnh:
```powershell
dotnet test src/LicenseIntelligencePlatform.slnx -v normal
```
Đảm bảo toàn bộ test case (`36 + 1 = 37 tests`) đều xanh (`Passed`).

---

# 3. Pull Request (PR) & Code Review Checklist

Khi mở Pull Request, hãy điền đầy đủ bảng thông tin sau vào mô tả PR:

- [ ] **Kiểm chứng Build:** `dotnet build -c Release` đạt `0 Errors, 0 Warnings`.
- [ ] **Kiểm chứng Test:** Toàn bộ Unit Test (`dotnet test`) passed 100%.
- [ ] **Quy tắc Read-Only:** Đảm bảo không sử dụng bất kỳ thao tác `File.WriteAllText`, `File.Delete` hay `Registry.SetValue` nào.
- [ ] **Quy tắc String & Zero-Allocation:** Đã dùng `StringComparison.OrdinalIgnoreCase` khi so sánh chuỗi.
- [ ] **Đăng ký Dependency Injection:** Nếu tạo Mapper hoặc Scanner mới, đã đăng ký Singleton vào `Program.cs`.
