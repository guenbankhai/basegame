# Quy trình làm module

Cách một cơ chế đi từ ý tưởng tới module ổn định trong BaseGame, kèm checklist cho từng giai đoạn và kinh nghiệm rút ra từ các module đã làm. Danh sách module và thứ tự làm nằm trong `roadmap.md`.

## Tài liệu: mỗi thông tin một chỗ

| Thông tin | Nơi duy nhất |
|---|---|
| Module nào, thứ tự, trạng thái | `roadmap.md` (bảng module) và bảng Modules trong `README.md`; hai bảng luôn khớp nhau |
| Thuật ngữ: từ nào, nghĩa gì, tránh từ nào | `GLOSSARY.md` ở gốc repo, mỗi module một mục con |
| Thiết kế hiện hành: phạm vi, mô hình, API, trường hợp biên, quy ước cho module khác | `docs/modules/<module>.md` (khung: `docs/modules/_template.md`) |
| Quyết định khó đảo ngược: chọn gì, vì sao, đã loại gì | `docs/adr/NNNN-<module>-<slug>.md` |
| Cách dùng package, đủ để dùng mà không cần repo này | `README.md` của package; tự đứng một mình, không tham chiếu ra ngoài package |
| Quy trình và kinh nghiệm chung | File này |
| Cấu trúc thư mục, đặt tên, phụ thuộc | `structure.md` |

Tài liệu thiết kế chỉ mô tả trạng thái hiện tại. Kế hoạch, checklist tiến độ và câu hỏi đang bàn chỉ nằm trong mục `## Kế hoạch (tạm)` ở cuối tài liệu thiết kế trong lúc làm, và bị xóa khi giai đoạn kết thúc. Quyết định chuyển sang ADR; bài học chuyển vào file này.

Format, tiêu chí và cách cập nhật `GLOSSARY.md` và ADR theo skill `domain-modeling`. Tài liệu module và code dùng đúng thuật ngữ trong glossary.

## Các giai đoạn

| # | Giai đoạn | Trạng thái trong roadmap | Kết quả |
|---|---|---|---|
| 1 | Thiết kế | Thử nghiệm | Tài liệu thiết kế đã chốt |
| 2 | Playground | Thử nghiệm | Câu hỏi thiết kế đã có câu trả lời, tham số đã chốt |
| 3 | Package | Đang phát triển | Package có test, usage doc; test pass trong Unity |
| 4 | Playground dùng package | Đang phát triển | Chỗ thử bằng tay cho package |
| 5 | Ổn định | Ổn định | Đã dùng trong ít nhất một scenario |

### 1. Thiết kế

- Tạo `docs/modules/<module>.md` từ `_template.md`. Đưa câu hỏi nghiên cứu của module trong roadmap vào đây.
- Bàn thiết kế bằng `/grill-with-docs`, để thuật ngữ vào `GLOSSARY.md` và quyết định vào ADR ngay lúc chốt.
- Chốt: tên (package, namespace, tránh trùng tên Unity), phạm vi trong và ngoài, thuật ngữ, mô hình dữ liệu, API, event, save, quy ước cho module dùng nó.
- Lập **bảng trường hợp biên cho mọi lệnh** ngay từ đầu: đầu vào sai (âm, NaN, vô hạn, null, ngoài khoảng), gọi lại, gọi trong handler, giới hạn trên.
- Lập danh sách test cần có, mỗi quy tắc ít nhất một test.
- Kết thúc khi user chốt thiết kế.

### 2. Playground

- Code ở `Assets/Playground/<Experiment>/`; scene ở `Assets/Scenes/`, config ở `Assets/Configs/` (xem `structure.md`).
- Mục tiêu là trả lời câu hỏi thiết kế và chốt tham số bằng cách chơi thử, không phải viết code đẹp. Có overlay debug với nút cho mọi lệnh.
- Test EditMode không bắt buộc, nhưng nên viết cho logic khó: chúng chuyển sang package sau.
- Kết thúc: review 3 trục (`/review-my-code`), ghi quyết định vào tài liệu thiết kế, viết ADR nếu đủ điều kiện, đề xuất danh sách thay đổi cho package để user chốt.

### 3. Package

- Tạo `unity/Packages/com.guenbankhai.<module>/`: `package.json`, `Runtime/`, `Tests/`, `README.md` (usage doc). Không đưa phần debug của playground vào.
- `README.md` của package đi theo package sang game khác, nên phải **tự đứng một mình**: không tham chiếu `docs/`, `GLOSSARY.md`, ADR hay bất kỳ file nào ngoài package. Viết đủ để người chỉ có package vẫn dùng đúng: thiết lập, khái niệm, API chính, quy tắc quan trọng, save, quy ước cho module dùng nó. Tài liệu trong `docs/` được phép trỏ vào README, chiều ngược lại thì không.
- **Viết lại sạch**, không copy nguyên playground. Chuyển test sang, thêm test cho mọi quyết định mới.
- Kiểm tra compile bằng `dotnet build` trên `.csproj` do Unity sinh. Nếu Unity chưa sinh csproj cho file mới, build bằng bản sao csproj trong thư mục tạm, chỉ thay danh sách file, và ghi rõ là đã kiểm tra theo cách này.
- Chạy test EditMode trong Unity.
- Cập nhật trạng thái trong `roadmap.md` và `README.md`.
- Kết thúc: test pass trong Unity, review 3 trục lần nữa.

### 4. Playground dùng package

- Xóa code lõi của playground, dựng lại playground mỏng chỉ dùng package (overlay, đối tượng giả). Đây là chỗ thử bằng tay khi sửa package sau này.
- Giữ hay xóa playground là quyết định của user. Xóa playground thì xóa luôn scene và asset của nó.

### 5. Ổn định

- Module được dùng trong một scenario thật, API không còn đổi thường xuyên.
- Đổi trạng thái thành `Ổn định`, tăng version trong `package.json` và `README.md`.

## Kinh nghiệm

Rút ra từ Calendar và giai đoạn thiết kế Stats.

**Thiết kế**
- Gặp lại vấn đề mà module trước đã giải (lỗi đầu vào, lệnh trong handler, restore, source) thì làm như module trước; làm khác thì ghi lý do trong tài liệu module. Grill thường bỏ sót các quy tắc chung này, nên khi lập bảng trường hợp biên phải rà mục "API của module" trong `coding-standard.md`.
- Tên lõi của module cần kiểm tra trùng với type của `System` và `UnityEngine` trước khi chốt (Calendar: `Time`; Stats: `Attribute`).
- Chốt trường hợp biên trong spec trước khi viết code. Lỗi ở Calendar đều nằm ở chỗ spec chưa nói: NaN và vô hạn, lệnh gọi từ handler đổi tốc độ, đầu vào quá lớn (skip hàng triệu ngày).
- Spec và code phải nói cùng một điều. Khi đổi một quy tắc, sửa spec, code và test trong cùng một lần.
- Viết rõ thứ tự khi nhiều việc xảy ra cùng lúc (event cùng phút, lệnh xếp hàng so với event cuối). Đây là chỗ dễ hiểu sai nhất.

**Kiến trúc**
- Lõi là C# thuần, nhận settings C# thuần. ScriptableObject chỉ bọc settings cho Inspector và trả bản copy (sửa ScriptableObject lúc Play bị giữ lại sau khi Stop); MonoBehaviour chỉ là driver mỏng. Test không cần asset hay scene.
- Settings là cấu hình ban đầu; thay đổi lúc chạy đi qua lệnh của lõi, thêm lệnh khi có nhu cầu thật thay vì một hàm "update settings" chung.
- Gom state cần save vào một struct duy nhất, dùng luôn làm state sống: save và load không thể lệch nhau. Khi state sống còn gồm thứ không lưu (modifier của Stats) hoặc snapshot phải chứa mảng thì dùng class, và `CaptureSnapshot()` luôn trả object, mảng mới.
- ID của dữ liệu authoring (ScriptableObject) là tên asset: chỉ một nguồn sự thật, đổi tên là đổi ID. Đặt tên PascalCase, không tiền tố loại. Save gặp ID không còn thì bỏ qua entry đó và báo lại; nâng phiên bản save là việc của module Save, không phải từng module.
- Nhiều module cùng góp vào một giá trị (modifier từ đồ, effect) thì thứ tự restore giữa chúng là một phần của thiết kế, ghi trong mục "Quy ước cho module dùng …". Thử quy ước đó với một nguồn **phụ thuộc vào chính dữ liệu được restore** (Stats: derived stat phụ thuộc base): nguồn kiểu này không thể restore trước, nên Stats suýt mất máu khi load.
- Cấu hình dùng chung cho nhiều item (policy của effect group) thì khai một chỗ, trên asset riêng được kéo thả, thay vì lặp trên từng item rồi kiểm tra cho khớp: không gõ sai được, và bỏ được cả một loại lỗi.
- Bộ đếm chống vòng lặp phải phủ mọi đường event. Hai lớp có event riêng mà gọi lẫn nhau được (Stats và Effects) thì dùng chung một bộ đếm.
- Không singleton, không state static (domain reload đang tắt).
- Cho lõi trả thông tin mà UI cần (tên, số lượng), để UI không phải đọc config.
- MonoBehaviour lấy component cùng GameObject bằng `GetComponent` trong `Awake` và `[RequireComponent]`, để giảm kéo thả trong Editor.

**Độ bền**
- Từ chối NaN và vô hạn ở mọi đầu vào số. Giá trị vượt giới hạn cũng báo lỗi (không âm thầm kẹp lại), kèm giá trị giới hạn trong thông báo; expose giới hạn để nơi gọi tự chặn trước.
- Đặt giới hạn trên cho mọi vòng lặp phụ thuộc đầu vào (độ dài skip, số lệnh trong hàng đợi).
- Driver gặp config sai thì log lỗi rõ ràng và tự tắt, không ném lỗi mỗi frame.
- Tài nguyên dạng "giữ chỗ" (pause source) phải có quy ước nhả ra, ghi trong usage doc.

**Test**
- Có một test "hai đường cùng kết quả" cho tính chất quan trọng nhất (Calendar: fast-forward và skip cùng một khoảng ra cùng snapshot).
- Mỗi dòng trong bảng trường hợp biên có test tương ứng.
- Test log lỗi có chủ ý thì dùng `LogAssert.Expect`. Log đỏ trong Console lúc chạy test là bình thường nếu test pass.

**Quy trình**
- Review 3 trục sau playground và sau package. Lần review sau package vẫn tìm ra chỗ spec lệch code.
- Đề xuất thay đổi kèm ví dụ gameplay cụ thể để user quyết, rồi mới viết code.

**Môi trường**
- Smart App Control trên máy dev chặn DLL chưa ký: Burst JIT trong Editor bị chặn (tắt Burst compilation trong Editor), và chương trình tự build trong thư mục tạm cũng không chạy được. Chỉ có thể chạy test trong Unity.
