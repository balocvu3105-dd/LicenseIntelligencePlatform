# ADR-006: Rule Engine 2.0 & PluginPriority Conflict Resolution

**Status:** Implemented (Phase 1)  
**Date:** 2026-07-14  
**Author:** DynamiteV  

---

## 1. Context & Problem Statement

Trong Phase 0 Pilot, `CoreEngine` chọn Plugin chạy đầu tiên thỏa mãn `CanCheck() == true`. Khi hệ sinh thái Plugin mở rộng lên 33+ gói phần mềm (`Plugins.Standard`), có nhiều trường hợp hai hoặc nhiều Plugin cùng có khả năng quét một phần mềm phức tạp như `Visual Studio Enterprise` hay `SQL Server` (ví dụ: `MicrosoftOfficeCommercialPlugin`, `JetBrainsIdePlugin`, và `HeuristicPlugin`).

Nếu không có cơ chế điều phối ưu tiên và phân xử xung đột (`Conflict Resolution`), Plugin chạy trước (có thể là Plugin đoán mò `Heuristic`) sẽ đưa ra kết luận sai lệch, lấn át Plugin chuyên biệt (`Specific Plugin`) có bằng chứng chữ ký số hoặc file `.lic` chuẩn xác.

## 2. Considered Options

- **Option A:** Hardcode thứ tự `foreach` của danh sách Plugin trong `CoreEngine`.
  - *Nhược điểm:* Vi phạm nguyên tắc OCP (`Open/Closed Principle`). Mỗi khi thêm Plugin mới, phải vào sửa code của `CoreEngine`.
- **Option B:** Khai báo thuộc tính `PluginPriority` trong `PluginManifest` và xây dựng `Rule Engine 2.0` (`CoreEngine`) tự động sắp xếp theo độ ưu tiên và so sánh điểm số `ConfidenceLevel`.
  - *Ưu điểm:* Tách biệt hoàn toàn logic điều phối và giải quyết xung đột ra khỏi logic nhận diện của Plugin. Tuân thủ tuyệt đối OCP.

## 3. Decision

Chúng tôi quyết định áp dụng **Option B — Rule Engine 2.0 & PluginPriority Conflict Resolution** ngay trong Phase 1:
1. **Phân cấp Priority quy chuẩn:**
   - `PluginPriority.CommercialSpecific (100)`: Plugin chuyên cho phần mềm thương mại đắt đỏ (Adobe, Office, Autodesk, VMware).
   - `PluginPriority.Ecosystem (75)`: Plugin cho hệ sinh thái platform (Docker, JetBrains, Steam).
   - `PluginPriority.Heuristic (50)`: Plugin phán đoán theo pattern/từ khóa chung.
   - `PluginPriority.Generic (25)`: Plugin chẩn đoán dự phòng cơ bản.
2. **Thuật toán Phân xử (`Resolution Algorithm`):**
   `CoreEngine` sắp xếp danh sách Plugin theo `Priority DESC`. Nếu có nhiều Plugin cùng trả về kết quả hợp lệ, kết quả được chọn là bản ghi có `ConfidenceLevel` cao nhất (`Verified > High > Medium > Low`), sau đó so sánh tổng trọng số `WeightScore` của các `Evidences`.

## 4. Consequences

### Positive
- **Độ chính xác tuyệt đối (`100% Accuracy`):** Plugin chuyên biệt luôn được ưu tiên xử lý trước, triệt tiêu hiện tượng `False Positives` do Plugin heuristic gây ra.
- **Tuân thủ OCP & Clean Architecture:** Bên thứ ba có thể thêm bao nhiêu Plugin tùy ý với `Priority = 80` hay `90` mà không làm đảo lộn luồng điều phối của Core.

### Negative / Trade-offs
- Cần duy trì kỷ luật gán `Priority` nghiêm ngặt khi review Pull Request để tránh việc Plugin heuristic cố tình đặt Priority quá cao (`> 90`).
