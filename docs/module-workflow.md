# Quy trình làm module

Cách một cơ chế đi từ ý tưởng tới module ổn định trong BaseGame: các giai đoạn, việc cần làm và điều kiện pass của từng giai đoạn. Luật review và cổng của từng giai đoạn nằm trong `review.md`. Danh sách module và thứ tự làm nằm trong `roadmap.md`.

## Tài liệu: mỗi thông tin một chỗ

| Thông tin | Nơi duy nhất |
|---|---|
| Module nào, thứ tự, trạng thái | `roadmap.md` (bảng module) và bảng Modules trong `README.md`; hai bảng luôn khớp nhau |
| Thuật ngữ: từ nào, nghĩa gì, tránh từ nào | `GLOSSARY.md` ở gốc repo, mỗi module một mục con |
| Thiết kế hiện hành: phạm vi, mô hình, API, trường hợp biên, quy ước cho module khác | `docs/modules/<module>.md` (khung: `docs/modules/_template.md`) |
| Quyết định khó đảo ngược: chọn gì, vì sao, đã loại gì | `docs/adr/NNNN-<module>-<slug>.md` |
| Cách dùng package, đủ để dùng mà không cần repo này | `README.md` của package; tự đứng một mình, không tham chiếu ra ngoài package |
| Quy trình: giai đoạn, việc cần làm | File này |
| Luật review, cổng của từng giai đoạn, gợi ý soi | `review.md` |
| Luật code và mặc định của API module | `coding-standard.md` |
| Cấu trúc thư mục, đặt tên, phụ thuộc | `structure.md` |

Tài liệu thiết kế chỉ mô tả trạng thái hiện tại. Kế hoạch, tiến độ, câu hỏi cho playground và mục "Nên sửa" đã được duyệt chỉ nằm trong mục `## Kế hoạch (tạm)` ở cuối tài liệu thiết kế trong lúc làm, và bị xóa khi phát hành. Quyết định chuyển sang ADR. Bài học chỉ được đề xuất trong báo cáo review; user thêm vào "Gợi ý soi" của `review.md` giữa hai module.

Luật mặc định (lỗi, kẹp, event, source, restore, save lệch settings) viết một lần trong `coding-standard.md`; tài liệu module chỉ ghi chỗ làm khác. Chi tiết của trường hợp biên theo mặc định nằm trong test của package.

Format, tiêu chí và cách cập nhật `GLOSSARY.md` và ADR theo skill `domain-modeling`. Tài liệu module và code dùng đúng thuật ngữ trong glossary.

## Các giai đoạn

| # | Giai đoạn | Trạng thái trong roadmap | Pass khi | Review |
|---|---|---|---|---|
| 1 | Thiết kế | Thử nghiệm | Không còn Chặn theo cổng thiết kế, user chốt | `/review-my-design <module>` |
| 2 | Playground | Thử nghiệm | Không còn Chặn theo cổng playground | `/review-my-module <module> playground` |
| 3 | Package | Đang phát triển | Không còn Chặn theo cổng phát hành | `/review-my-module <module> release` |
| 4 | Phát hành → Ổn định | Đang phát triển → Ổn định | Phát hành: version `0.1.0` trở lên. Ổn định: đã dùng trong ít nhất một scenario | — |

Mỗi review chạy trong một conversation mới, độc lập với các vòng trước (xem `review.md`). Sửa xong các mục Chặn thì review lại từ đầu.

### 1. Thiết kế

- Tạo `docs/modules/<module>.md` từ `_template.md`. Đưa câu hỏi nghiên cứu của module trong roadmap vào đây.
- Bàn thiết kế bằng `/grill-with-docs`, để thuật ngữ vào `GLOSSARY.md` và quyết định vào ADR ngay lúc chốt. `/grill-me` chỉ dùng để bàn ý tưởng chưa thành tài liệu, không dùng để review.
- Tài liệu giữ quyết định, mô hình, API, event, save và quy ước mà API bắt buộc. Câu hỏi chơi thử mới trả lời được thì ghi vào `## Kế hoạch (tạm)` cho playground, không cố chốt trên giấy. Câu hỏi thuộc module chưa thiết kế thì ghi vào `roadmap.md`.
- Tự kiểm theo [cổng thiết kế](review.md#cổng-thiết-kế), rồi review bằng `/review-my-design <module>`.
- Kết thúc khi review không còn Chặn và user chốt.

### 2. Playground

- Code ở `Assets/Playground/<Experiment>/`; scene ở `Assets/Scenes/`, config ở `Assets/Configs/` (xem `structure.md`).
- Mục tiêu là trả lời câu hỏi thiết kế và chốt tham số bằng cách chơi thử, không phải viết code đẹp. Có overlay debug với nút cho mọi lệnh.
- Test EditMode không bắt buộc, nhưng nên viết cho logic khó: chúng chuyển sang package sau.
- Review không xét chất lượng code. Đầu ra: câu trả lời cho từng câu hỏi, thay đổi đề xuất cho tài liệu thiết kế. User chốt rồi mới ghi vào tài liệu.

### 3. Package

- Tạo `unity/Packages/com.guenbankhai.<module>/`: `package.json`, `Runtime/`, `Tests/`, `README.md` (usage doc). Không đưa phần debug của playground vào.
- **Viết lại sạch**, không copy nguyên playground. Chuyển test sang; mỗi dòng bảng trường hợp biên và mỗi luật của mô hình có test.
- `README.md` của package đi theo package sang game khác, nên phải **tự đứng một mình**: không tham chiếu `docs/`, `GLOSSARY.md`, ADR hay bất kỳ file nào ngoài package. Viết đủ để người chỉ có package vẫn dùng đúng: thiết lập, khái niệm, API chính, quy tắc quan trọng, save, quy ước cho module dùng nó. Tài liệu trong `docs/` được phép trỏ vào README, chiều ngược lại thì không.
- Xóa code lõi của playground, dựng lại playground mỏng chỉ dùng package (overlay, đối tượng giả). Đây là chỗ thử bằng tay khi sửa package sau này.
- Kiểm tra compile bằng `dotnet build` trên `.csproj` do Unity sinh. Nếu Unity chưa sinh csproj cho file mới, build bằng bản sao csproj trong thư mục tạm, chỉ thay danh sách file, và ghi rõ là đã kiểm tra theo cách này.
- Chạy test EditMode trong Unity, rồi review bằng `/review-my-module <module> release`. Cập nhật trạng thái trong `roadmap.md` và `README.md`.

### 4. Phát hành → Ổn định

- Phát hành: tăng version trong `package.json` và `README.md` (bản đầu `0.1.0`), xóa mục Kế hoạch (tạm), trạng thái vẫn là `Đang phát triển`.
- Ổn định: module được dùng trong một scenario thật, API không còn đổi thường xuyên. Đổi trạng thái thành `Ổn định`, tăng version.
- Sửa module đã phát hành: review thay đổi theo mục "Review code ngoài giai đoạn" của `review.md`. Đổi API public thì tăng version minor (`0.1.x` → `0.2.0`).
- Giữ hay xóa playground sau khi ổn định là quyết định của user. Xóa playground thì xóa luôn scene và asset của nó.

## Môi trường

- Smart App Control trên máy dev chặn DLL chưa ký: Burst JIT trong Editor bị chặn (tắt Burst compilation trong Editor), và chương trình tự build trong thư mục tạm cũng không chạy được. Chỉ có thể chạy test trong Unity.
