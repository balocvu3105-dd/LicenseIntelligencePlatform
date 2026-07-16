# 15_ADR_INDEX.md

# License Intelligence Platform (LIP)

## Architecture Decision Records (ADR) Index & Governance Master Registry

Version: 1.0

Status: Stable (Phase 0 – Phase 4 Completed)

Author: DynamiteV

---

# 1. Purpose & ADR Governance Philosophy

**Architecture Decision Records (ADR)** là "bộ nhớ kỹ thuật" (`Architectural Memory Registry`) của nền tảng **License Intelligence Platform (LIP)**. Mỗi ADR đóng vai trò là một văn bản bất biến ghi nhận lý do tại sao một quyết định kỹ thuật được đưa ra, bối cảnh lúc đưa ra quyết định, các lựa chọn thay thế đã bị loại bỏ, và hệ quả kỹ thuật lâu dài.

> *"Thay vì chỉ nhìn thấy mã nguồn hiện tại được viết **như thế nào (`How`)**, ADR giúp các thế hệ kỹ sư sau và AI Coding Assistants hiểu chính xác **tại sao (`Why`)** hệ thống được thiết kế theo cách đó, ngăn chặn việc tái diễn tranh luận cũ hoặc vô ý phá vỡ ranh giới kiến trúc."*

---

# 2. ADR Lifecycle & Status Transitions

```
[Proposed] ──► [Accepted] ──► [Implemented] ──► (Optional: [Deprecated] ──► [Superseded])
```

- **`Proposed`:** Quyết định đang được thảo luận trong giai đoạn thiết kế/Roadmap.
- **`Accepted`:** Quyết định đã được phê duyệt bởi Core Architects, chuẩn bị đưa vào code.
- **`Implemented`:** Quyết định đã được triển khai đầy đủ vào mã nguồn C#, có bộ Unit Test kiểm chứng thành công và chạy ổn định trên Production.
- **`Deprecated / Superseded`:** Quyết định cũ bị thay thế bởi một ADR mới trong tương lai.

---

# 3. Master ADR Index Table (Đã cập nhật sau Phase 4)

Toàn bộ các ADR từ `ADR-001` đến `ADR-018` hiện đã được hoàn tất triển khai (`Implemented`) theo đúng tiến độ **Phase 0 đến Phase 4**:

| Mã ADR (`ADR Code`) | Tiêu đề Quyết định Kiến trúc (`Title`) | Giai đoạn (`Phase`) | Trạng thái (`Status`) | Nhóm Kiến trúc (`Category`) |
| :--- | :--- | :---: | :---: | :--- |
| **ADR-001** | [Plugin Architecture via ILicensePlugin & AssemblyLoadContext](file:///d:/source%20code/License-Intelligence-Platform-Docs/docs/adr/ADR-001-Plugin-Architecture.md) | Phase 0 | ✅ **Implemented** | Extensibility & Core |
| **ADR-002** | [Mapping Layer Separation (Report Mappers vs Domain Entities)](file:///d:/source%20code/License-Intelligence-Platform-Docs/docs/adr/ADR-002-Mapping-Layer.md) | Phase 0 & 4 | ✅ **Implemented** | Presentation & Exporters |
| **ADR-003** | [100% Read-Only Scanner Boundaries (Rule 3 & Rule 6)](file:///d:/source%20code/License-Intelligence-Platform-Docs/docs/adr/ADR-003-ReadOnly-Scanner.md) | Phase 0 & 3 | ✅ **Implemented** | Infrastructure Security |
| **ADR-004** | [Deterministic 100-Point Confidence Scoring Engine](file:///d:/source%20code/License-Intelligence-Platform-Docs/docs/adr/ADR-004-Confidence-Score.md) | Phase 1 | ✅ **Implemented** | Detection & Scoring |
| **ADR-005** | [Generic / Heuristic Plugin Fallback Mechanics](file:///d:/source%20code/License-Intelligence-Platform-Docs/docs/adr/ADR-005-Generic-Plugin.md) | Phase 0 & 1 | ✅ **Implemented** | Detection & Scoring |
| **ADR-006** | [Rule Engine 2.0 & PluginPriority Conflict Resolution](file:///d:/source%20code/License-Intelligence-Platform-Docs/docs/adr/ADR-006-Rule-Engine.md) | Phase 1 | ✅ **Implemented** | Core Coordination |
| **ADR-007** | [Immutable Evidence Engine Domain Entity Record](file:///d:/source%20code/License-Intelligence-Platform-Docs/docs/adr/ADR-007-Evidence-Engine.md) | Phase 1 | ✅ **Implemented** | Domain Forensics |
| **ADR-008** | [SoftwareMergeEngine & Multi-Source Deduplication](file:///d:/source%20code/License-Intelligence-Platform-Docs/docs/adr/ADR-008-Data-Merge-Engine.md) | Phase 1 & 2 | ✅ **Implemented** | Application Processing |
| **ADR-009** | [Software Identity & Publisher Sanitization Invariants](file:///d:/source%20code/License-Intelligence-Platform-Docs/docs/adr/ADR-009-Software-Identity.md) | Phase 1 | ✅ **Implemented** | Domain Forensics |
| **ADR-010** | [Structured Logging Standardization with Parameterized Templates](file:///d:/source%20code/License-Intelligence-Platform-Docs/docs/adr/ADR-010-Structured-Logging.md) | Phase 0 | ✅ **Implemented** | Infrastructure Diagnostics |
| **ADR-011** | Diagnostic Package & Forensic Evidence Export | Phase 0 & 4 | ✅ **Implemented** | Exporters / Diagnostics |
| **ADR-012** | Plugin SDK v1.0 & Compatibility Validator Architecture | Phase 2 | ✅ **Implemented** | Extensibility Governance |
| **ADR-013** | Historical Scan Repository & Change Tracking | Phase 5 | ⏳ *Proposed* | Future Storage |
| **ADR-014** | Unknown Software Backlog Learning Loop (`backlog_need_plugins.json`) | Phase 1 & 4 | ✅ **Implemented** | Exporters / Harvesting |
| **ADR-015** | Local-First & Air-Gapped Execution Boundary | Phase 0 | ✅ **Implemented** | Core Security |
| **ADR-016** | Zero-Database Runtime Architecture (In-Memory Pipeline) | Phase 0 – 4 | ✅ **Implemented** | Core Performance |
| **ADR-017** | Product Positioning: Read-Only ITAM vs No-Crack Policy | Phase 0 | ✅ **Implemented** | Legal & Compliance |
| **ADR-018** | Multi-Output Reporting Hierarchy (`CSV`, `JSON`, `AUDIT`, `HTML`, `STATS`) | Phase 4 | ✅ **Implemented** | Presentation & Exporters |

---

# 4. Detailed Summary of Key Phase 1–4 ADRs

## ADR-006: Rule Engine 2.0 & PluginPriority Conflict Resolution
- **Bối cảnh:** Khi quét hệ thống, một ứng dụng như `Visual Studio Enterprise` có thể được cả `MicrosoftOfficeCommercialPlugin`, `JetBrainsIdePlugin`, và `HeuristicPlugin` cùng chẩn đoán, gây ra xung đột kết quả (`Conflict Resolution`).
- **Quyết định:** Áp dụng `PluginPriority` (`CommercialSpecific = 100`, `Ecosystem = 75`, `Heuristic = 50`, `Generic = 25`). Trong trường hợp có nhiều kết quả khác nhau, `CoreEngine` chọn bản ghi có `ConfidenceLevel` cao nhất và tổng điểm trọng số bằng chứng lớn nhất.

## ADR-007 & ADR-004: Evidence Engine & 100-Point Confidence Scoring
- **Bối cảnh:** Các hệ thống kiểm toán pháp lý không chấp nhận phán đoán cảm tính.
- **Quyết định:** Tách `Evidence` thành `sealed record` bất biến. Thiết lập thang điểm 100:
  - File Header license: +40 – +50 pts
  - Chữ ký số Authenticode: +25 – +30 pts
  - Khớp GUID Registry: +15 – +20 pts
  Chỉ những kết quả đạt $\ge 70\text{ pts}$ mới được thăng hạng lên `ConfidenceLevel.Verified`.

## ADR-008: SoftwareMergeEngine & Multi-Source Deduplication
- **Bối cảnh:** `CompositeScanner` quét từ 3 nguồn (Registry 32-bit, 64-bit, WinGet) dẫn đến việc 1 phần mềm xuất hiện tới 3-4 lần trong danh sách thô.
- **Quyết định:** Viết `SoftwareMergeEngine` trong tầng `Application` thực hiện gộp GUID `ProductCode`, sau đó gộp cặp `Name + Version` và chuẩn hóa tên nhà phát hành (`Sanitize Publisher`) để danh sách đầu ra là duy nhất (`Unique list`).

## ADR-012: Plugin SDK v1.0 & Compatibility Validator Architecture
- **Bối cảnh:** Khi cập nhật Core Engine, các Plugin cũ do bên thứ ba phát triển có thể bị lỗi không tương thích.
- **Quyết định:** Buộc mỗi Plugin khai báo `PluginManifest` chứa `MinSdkVersion`. Khi `CoreEngine` khởi tạo, `PluginCompatibilityValidator` sẽ kiểm tra (`Manifest.MinSdkVersion <= CurrentSdkVersion`). Nếu không đạt, Plugin bị loại khỏi RAM ngay từ bước khởi động để bảo đảm tính ổn định (`Zero Crash`).

## ADR-018: Multi-Output Reporting Hierarchy (Phase 4)
- **Bối cảnh:** Người dùng ở các phòng ban khác nhau cần các định dạng báo cáo khác nhau (C-Suite cần HTML trực quan, IT Asset Manager cần CSV, Auditor cần Markdown trích dẫn bằng chứng, BI Engineer cần JSON).
- **Quyết định:** Thiết kế interface `IReportMapper` với thuộc tính `FormatName`. Đăng ký Singleton cả 5 Mappers (`CSV`, `JSON`, `AUDIT`, `HTML`, `STATS`) vào `Program.cs`. Khi chạy lệnh CLI `--format BOTH`, hệ thống tự động sinh đồng thời 5 báo cáo và file `backlog_need_plugins.json` trong vòng `~40 ms`.

---

# 5. ADR Creation & Review Protocol

Mọi kiến trúc sư khi đề xuất một thay đổi có tác động đến ranh giới tầng hoặc cấu trúc dữ liệu chính phải tạo file `ADR-019-<Topic>.md` trong `docs/adr/` theo mẫu:

```markdown
# ADR-019: [Architecture Decision Title]
**Status:** Proposed | Accepted | Implemented | Deprecated
**Date:** YYYY-MM-DD
**Author:** [Name]

## 1. Context & Problem Statement
[Mô tả vấn đề kỹ thuật và lý do cần thay đổi]

## 2. Considered Options
- **Option A:** [Mô tả] — Ưu/Nhược điểm
- **Option B:** [Mô tả] — Ưu/Nhược điểm

## 3. Decision
[Quyết định được chọn và giải thích lý do kỹ thuật dựa trên SOLID / Clean Architecture]

## 4. Consequences
- **Positive:** [Hệ quả tốt]
- **Negative / Trade-offs:** [Sự đánh đổi hoặc chi phí bảo trì]
```