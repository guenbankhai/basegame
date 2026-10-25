# Quy trình làm module

Cách một cơ chế đi từ ý tưởng tới module ổn định trong BaseGame: các giai đoạn, điều kiện pass, cách review và checklist cho từng giai đoạn. Danh sách module và thứ tự làm nằm trong `roadmap.md`.

## Tài liệu: mỗi thông tin một chỗ

| Thông tin | Nơi duy nhất |
|---|---|
| Module nào, thứ tự, trạng thái | `roadmap.md` (bảng module) và bảng Modules trong `README.md`; hai bảng luôn khớp nhau |
| Thuật ngữ: từ nào, nghĩa gì, tránh từ nào | `GLOSSARY.md` ở gốc repo, mỗi module một mục con |
| Thiết kế hiện hành: phạm vi, mô hình, API, trường hợp biên, quy ước cho module khác | `docs/modules/<module>.md` (khung: `docs/modules/_template.md`) |
| Quyết định khó đảo ngược: chọn gì, vì sao, đã loại gì | `docs/adr/NNNN-<module>-<slug>.md` |
| Cách dùng package, đủ để dùng mà không cần repo này | `README.md` của package; tự đứng một mình, không tham chiếu ra ngoài package |
| Quy trình, cách review, checklist | File này |
| Cấu trúc thư mục, đặt tên, phụ thuộc | `structure.md` |

Tài liệu thiết kế chỉ mô tả trạng thái hiện tại. Kế hoạch, tiến độ, câu hỏi đang bàn và mục "Nên sửa" đã được duyệt chỉ nằm trong mục `## Kế hoạch (tạm)` ở cuối tài liệu thiết kế trong lúc làm, và bị xóa khi giai đoạn kết thúc. Quyết định chuyển sang ADR; bài học chuyển thành dòng checklist trong file này.

Format, tiêu chí và cách cập nhật `GLOSSARY.md` và ADR theo skill `domain-modeling`. Tài liệu module và code dùng đúng thuật ngữ trong glossary.

## Các giai đoạn

| # | Giai đoạn | Trạng thái trong roadmap | Pass khi | Review |
|---|---|---|---|---|
| 1 | Thiết kế | Thử nghiệm | Checklist thiết kế đủ, review không còn mục Chặn, user chốt | `/review-my-design <module>` |
| 2 | Playground | Thử nghiệm | Câu hỏi thiết kế có câu trả lời, test của logic khó pass, review không còn mục Chặn | `/review-my-module <module> playground` |
| 3 | Package | Đang phát triển | Compile sạch, test EditMode pass trong Unity | Không review riêng; gộp vào giai đoạn 4 |
| 4 | Playground dùng package | Đang phát triển | Playground bỏ code lõi, chỉ dùng package; test pass; review phát hành không còn mục Chặn | `/review-my-module <module> release` |
| 5 | Phát hành → Ổn định | Đang phát triển → Ổn định | Phát hành: version `0.1.0` trở lên. Ổn định: đã dùng trong ít nhất một scenario | — |

### 1. Thiết kế

- Tạo `docs/modules/<module>.md` từ `_template.md`. Đưa câu hỏi nghiên cứu của module trong roadmap vào đây.
- Bàn thiết kế bằng `/grill-with-docs`, để thuật ngữ vào `GLOSSARY.md` và quyết định vào ADR ngay lúc chốt. `/grill-me` chỉ dùng để bàn ý tưởng chưa thành tài liệu, không dùng để review.
- Tự kiểm theo [checklist thiết kế](#checklist-thiết-kế), rồi review bằng `/review-my-design <module>`.
- Kết thúc khi review không còn mục Chặn và user chốt.

### 2. Playground

- Code ở `Assets/Playground/<Experiment>/`; scene ở `Assets/Scenes/`, config ở `Assets/Configs/` (xem `structure.md`).
- Mục tiêu là trả lời câu hỏi thiết kế và chốt tham số bằng cách chơi thử, không phải viết code đẹp. Có overlay debug với nút cho mọi lệnh.
- Test EditMode không bắt buộc, nhưng nên viết cho logic khó: chúng chuyển sang package sau.
- Code playground sẽ bị bỏ, nên review không xét chất lượng code và không sửa playground trừ khi lỗi làm sai câu trả lời cho câu hỏi thiết kế.
- Đầu ra của review: danh sách thay đổi cho tài liệu thiết kế và package, ADR mới nếu có, bài học cho checklist. User chốt danh sách này rồi mới ghi vào tài liệu.

### 3. Package

- Tạo `unity/Packages/com.guenbankhai.<module>/`: `package.json`, `Runtime/`, `Tests/`, `README.md` (usage doc). Không đưa phần debug của playground vào.
- `README.md` của package đi theo package sang game khác, nên phải **tự đứng một mình**: không tham chiếu `docs/`, `GLOSSARY.md`, ADR hay bất kỳ file nào ngoài package. Viết đủ để người chỉ có package vẫn dùng đúng: thiết lập, khái niệm, API chính, quy tắc quan trọng, save, quy ước cho module dùng nó. Tài liệu trong `docs/` được phép trỏ vào README, chiều ngược lại thì không.
- **Viết lại sạch**, không copy nguyên playground. Chuyển test sang, thêm test cho mọi quyết định mới.
- Kiểm tra compile bằng `dotnet build` trên `.csproj` do Unity sinh. Nếu Unity chưa sinh csproj cho file mới, build bằng bản sao csproj trong thư mục tạm, chỉ thay danh sách file, và ghi rõ là đã kiểm tra theo cách này.
- Chạy test EditMode trong Unity. Cập nhật trạng thái trong `roadmap.md` và `README.md`.

### 4. Playground dùng package

- Xóa code lõi của playground, dựng lại playground mỏng chỉ dùng package (overlay, đối tượng giả). Đây là chỗ thử bằng tay khi sửa package sau này.
- Test pass, rồi review phát hành bằng `/review-my-module <module> release` theo [checklist phát hành](#checklist-phát-hành).
- Giữ hay xóa playground sau khi ổn định là quyết định của user. Xóa playground thì xóa luôn scene và asset của nó.

### 5. Phát hành → Ổn định

- Phát hành: tăng version trong `package.json` và `README.md` (bản đầu `0.1.0`), xóa mục Kế hoạch (tạm), trạng thái vẫn là `Đang phát triển`.
- Ổn định: module được dùng trong một scenario thật, API không còn đổi thường xuyên. Đổi trạng thái thành `Ổn định`, tăng version.

## Review

### Mức của phát hiện

| Mức | Nghĩa | Xử lý |
|---|---|---|
| **Chặn** | Thỏa định nghĩa Chặn của giai đoạn (bảng dưới) | Phải sửa trước khi pass |
| **Nên sửa** | Đúng, nhưng không chặn | User duyệt; nếu đồng ý thì sửa luôn hoặc ghi vào Kế hoạch (tạm) cho giai đoạn sau |
| **Ghi chú** | Câu chữ, ý thích, cách làm khác | Sửa tiện tay hoặc bỏ qua |

| Giai đoạn | Định nghĩa **Chặn** |
|---|---|
| Thiết kế | (a) Làm đúng spec mà gameplay sai hoặc mất dữ liệu; (b) spec tự mâu thuẫn, hoặc mâu thuẫn với ADR / `coding-standard.md`; (c) thiếu quyết định làm đổi public API, format save hay phụ thuộc giữa module; (d) lệnh public chưa có trong bảng trường hợp biên |
| Playground | Lỗi làm sai câu trả lời cho câu hỏi thiết kế |
| Phát hành | Bug; spec khác code; dòng trong bảng trường hợp biên thiếu test; vi phạm `coding-standard.md` ở public API; README của package sai hoặc thiếu so với API |

### Quy tắc

- **Pass = không còn mục Chặn.** "Nên sửa" và "Ghi chú" không bao giờ chặn.
- Review đối chiếu với checklist của giai đoạn. Phát hiện nằm ngoài checklist chỉ là Chặn nếu thỏa định nghĩa Chặn.
- **Review lại sau khi sửa chỉ xem phần vừa sửa** và ảnh hưởng của nó, không review lại toàn bộ.
- **Review chỉ báo cáo.** Không sửa file, không ghi vào Kế hoạch (tạm), checklist, ADR hay `GLOSSARY.md`. Mọi đề xuất được báo về; user xác nhận rồi mới áp dụng.
- Mục Chặn mà checklist chưa có: review đề xuất thêm một dòng checklist, để lần sau bắt được ngay khi tự kiểm.
- Mỗi phát hiện kèm ví dụ gameplay hoặc kịch bản cụ thể (đầu vào, state → kết quả sai) và đề xuất cách sửa.

## Checklist

Mỗi dòng là một bài học từ module trước. Ghi chú in nghiêng là lý do, giữ lại khi dòng đó không tự giải thích.

### Checklist thiết kế

**Phạm vi và tên**
- [ ] Tên lõi, namespace không trùng type của `System` và `UnityEngine`. _Calendar: `Time`; Stats: `Attribute`._
- [ ] Phạm vi trong và ngoài; thứ "chưa làm" có lý do hoặc điều kiện để làm.
- [ ] Thuật ngữ có trong `GLOSSARY.md`; tài liệu và ADR không dùng từ trong mục _Avoid_.
- [ ] Mỗi quyết định khó đảo ngược có ADR.

**Kiến trúc**
- [ ] Lõi C# thuần, nhận settings C# thuần, kiểm tra và copy một lần. ScriptableObject chỉ bọc settings và trả bản copy; MonoBehaviour chỉ là driver mỏng; driver gặp config sai thì log lỗi và tự tắt. _Sửa ScriptableObject lúc Play bị giữ lại sau khi Stop._
- [ ] Thay đổi lúc chạy đi qua lệnh của lõi; không có hàm "update settings" chung.
- [ ] Không singleton, không state static (domain reload đang tắt).
- [ ] Lõi trả thông tin UI cần (tên, số lượng), để UI không đọc config.
- [ ] Không trả ra ngoài object mutable mà lõi đang giữ (settings, mảng); trả bản copy hoặc property chỉ đọc.
- [ ] ID của dữ liệu authoring là tên asset, PascalCase, không tiền tố loại.
- [ ] Cấu hình dùng chung cho nhiều item khai một chỗ, trên asset riêng được kéo thả, thay vì lặp trên từng item.
- [ ] MonoBehaviour lấy component cùng GameObject bằng `GetComponent` trong `Awake` và `[RequireComponent]`.

**Bảng trường hợp biên** (mỗi lệnh public một dòng)
- [ ] Đầu vào sai: null, NaN, vô hạn, âm, ngoài khoảng, ID không có, ID trùng.
- [ ] Gọi lại, gọi khi đã ở trạng thái đích.
- [ ] Gọi trong handler: được phép, xếp hàng hay bị cấm; riêng `Restore` và lệnh "chạy thời gian".
- [ ] Constructor của lõi và dữ liệu vắng mặt trong snapshot, mỗi thứ một dòng. _Stats: resource thêm sau khi có save giữ giá trị của ván đang chơi khi load._
- [ ] Giới hạn trên của mọi vòng lặp và cấp phát phụ thuộc đầu vào (độ dài skip, số lệnh, số stack).
- [ ] Đối chiếu từng mục "API của module" trong `coding-standard.md`: lỗi, kẹp, event, source, restore, save lệch settings. Làm khác module trước thì ghi lý do.

**Event**
- [ ] Thứ tự khi một lệnh gây nhiều event, kể cả event của các lớp khác nhau (giá trị trước, ý nghĩa gameplay sau).
- [ ] State cập nhật xong rồi mới phát; một lệnh đổi một giá trị nhiều lần thì chỉ phát một lần với giá trị cuối. _Stats: gỡ rồi thêm modifier làm Resource bị clamp theo max ở giữa._
- [ ] Bộ đếm chống vòng lặp phủ mọi đường event; hai lớp gọi lẫn nhau được thì dùng chung một bộ đếm.
- [ ] Tài nguyên dạng "giữ chỗ" (pause source, source của modifier) có quy ước nhả ra.

**Save**
- [ ] State cần save gom vào một snapshot, dùng luôn làm state sống nếu được, để save và load không lệch nhau. Snapshot chứa mảng thì là class, và `CaptureSnapshot()` luôn trả object, mảng mới.
- [ ] Lưu gì, không lưu gì, vì sao. Không lưu giá trị trùng settings nếu muốn chỉnh cân bằng có tác dụng với save cũ.
- [ ] Dữ liệu hỏng (từ chối cả snapshot) và dữ liệu lệch settings (sửa cho khớp hoặc bỏ entry và báo lại), mỗi trường hợp một dòng.
- [ ] Nhiều module cùng góp vào một giá trị thì thứ tự restore là một phần của thiết kế. Thử với một nguồn phụ thuộc vào chính dữ liệu được restore. _Stats: derived stat suýt làm mất máu khi load._
- [ ] Event phát trong lúc load (giá trị tạm) và cách module khác xử lý.

**Chạy tay**
- [ ] Chạy từng bước mọi ví dụ trong tài liệu, nhất là trong mục "Quy ước cho module dùng …": ghi state, event và clamp sau mỗi bước. _Đây là cách bắt lỗi tương tác giữa hai quy tắc mà mỗi quy tắc riêng lẻ đều đúng._
- [ ] Chạy thường và skip/restore ra cùng kết quả, kể cả khi ghép với module có liên quan. _Stats: effect hết hạn giữa lần skip làm lệch Needs._
- [ ] Chạy thường đi qua cùng vòng chia đoạn với skip; mốc không rơi vào step thì chạy thường cũng cắt tại đó. _Stats: Ốm 45 phút với step 10 làm Needs tính ×2 suốt 50 phút._
- [ ] Effect hay trạng thái sinh ra giữa một khoảng thời gian chỉ chịu phần thời gian sau lúc nó sinh ra. _Stats: Ốm áp khi đói cạn giữa lần skip bị trừ cả khoảng._

**Test**
- [ ] Mỗi quy tắc ít nhất một test; mỗi dòng bảng trường hợp biên có test.
- [ ] Có một test "hai đường cùng kết quả" cho tính chất quan trọng nhất. _Calendar: fast-forward và skip cùng một khoảng ra cùng snapshot._

### Checklist playground

- [ ] Mỗi câu hỏi thiết kế trong Kế hoạch (tạm) có câu trả lời.
- [ ] Overlay có nút cho mọi lệnh.
- [ ] Logic khó có test EditMode.
- [ ] Danh sách thay đổi cho tài liệu thiết kế và package đã được user chốt; bài học đã thành dòng checklist.

### Checklist phát hành

- [ ] Compile sạch bằng `dotnet build`; test EditMode pass trong Unity. _Test log lỗi có chủ ý dùng `LogAssert.Expect`; log đỏ trong Console là bình thường nếu test pass._
- [ ] Spec, code và test nói cùng một điều. _Khi đổi một quy tắc, sửa cả ba trong cùng một lần._
- [ ] Mỗi dòng bảng trường hợp biên có test; có test "hai đường cùng kết quả".
- [ ] Code theo `coding-standard.md` (đặt tên, `[SerializeField] private` có giá trị khởi tạo, không state static, không `?.`/`??` với `UnityEngine.Object`, C# 9).
- [ ] Từ chối NaN và vô hạn; giá trị vượt giới hạn báo lỗi kèm giới hạn trong thông báo; giới hạn được expose.
- [ ] README của package tự đứng một mình và khớp API.
- [ ] Playground chỉ dùng package, không còn code lõi.
- [ ] `roadmap.md`, `README.md` và version trong `package.json` khớp nhau; mục Kế hoạch (tạm) đã xóa.

## Môi trường

- Smart App Control trên máy dev chặn DLL chưa ký: Burst JIT trong Editor bị chặn (tắt Burst compilation trong Editor), và chương trình tự build trong thư mục tạm cũng không chạy được. Chỉ có thể chạy test trong Unity.
