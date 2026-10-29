# Review

Luật cho mọi review trong BaseGame: review thiết kế, review module, review code (kể cả sau khi phát hành), dùng skill nào cũng vậy. Đọc mục [Luật chung](#luật-chung) và đúng mục cổng của việc đang review.

## Luật chung

### Nguyên tắc

- **Review độc lập.** Chỉ dựa vào tài liệu và code đang có trong repo; không cần biết các vòng review trước. Vòng 1 tìm lỗi, sửa xong thì vòng 2 chạy lại từ đầu theo cùng luật.
- **Review chỉ báo cáo.** Không sửa code, tài liệu, `## Kế hoạch (tạm)`, ADR, `GLOSSARY.md` hay file này. User xác nhận rồi mới áp dụng, như một bước riêng.
- **Luật đứng yên trong một module.** Cổng, bất biến lõi và gợi ý soi chỉ đổi giữa hai module, do user quyết.
- **Mỗi phát hiện có bằng chứng:** vị trí, câu hoặc code được trích, kịch bản cụ thể (đầu vào, state → kết quả sai), đề xuất sửa. Thiếu một trong số đó thì bỏ.
- **Sửa theo hướng thu nhỏ.** Đề xuất sửa ưu tiên bỏ bớt hoặc thêm một câu luật; không thêm tính năng.

### Xếp mức

Với mỗi phát hiện, hỏi lần lượt, dừng ở câu đầu tiên đúng:

| # | Câu hỏi | Mức |
|---|---|---|
| 1 | Trượt một cổng của việc đang review; vi phạm một câu đã viết (tài liệu module, ADR, `coding-standard.md`); hoặc vi phạm một [bất biến lõi](#bất-biến-lõi)? Phải trích được cổng, câu hoặc bất biến đó | **Chặn**: phải sửa mới pass |
| 2 | Xảy ra được khi dùng đúng cách, nhưng không thuộc câu 1? | **Nên sửa**: liệt kê đủ, kèm đề xuất; user chọn |
| 3 | Chỉ là câu chữ, đặt tên, ý thích? | **Ghi chú** |
| 4 | Còn lại | **Bỏ**: không báo |

**Dùng đúng cách** là điều kiện cho kịch bản ở câu 1 và 2:
- Chỉ đi qua API public, theo cách tài liệu hướng dẫn. Đầu vào từ bên ngoài (tham số của lệnh public, settings, snapshot) thì có thể sai theo bất kỳ kiểu nào.
- Nằm trong giả định quy mô mà tài liệu module đã ghi; không có giả định thì trong khoảng một game có thể đặt qua settings. Kịch bản chỉ xảy ra khi tràn kiểu dữ liệu (`int`, `long`, `float`) thì bỏ.
- Không phụ thuộc module chưa thiết kế. Kịch bản như vậy tối đa là Nên sửa, đề xuất ghi một dòng vào câu hỏi của module đó trong `roadmap.md`.

### Bất biến lõi

Áp cho mọi module, kể cả khi tài liệu không nhắc tới:

| # | Bất biến |
|---|---|
| B1 | Chạy thường và skip/restore cho cùng kết quả (với module có thời gian hoặc save), kể cả khi ghép với module đã có mà tài liệu nêu tên |
| B2 | Save rồi load không làm mất hay đổi state của người chơi, kể cả khi load vào thực thể đang chơi |
| B3 | Một lệnh cập nhật xong state rồi mới phát event; listener nhận giá trị cuối, mỗi giá trị đổi phát một lần; không giá trị nào bị clamp hay mất ở trạng thái giữa của một lệnh |
| B4 | Lệnh bị từ chối không làm đổi state |

### Thu gọn

Mục riêng trong mọi báo cáo, mức Nên sửa, liệt kê đủ. Dấu hiệu:
- Code, luật hay test chỉ phục vụ kịch bản thuộc mức Bỏ.
- Field hay tham số luôn bằng một hằng.
- Một luật viết ở nhiều chỗ: giữ một chỗ theo bảng "Tài liệu: mỗi thông tin một chỗ" trong `module-workflow.md`.
- Tính năng hay tham số chưa có người dùng trong module đã có hoặc trong roadmap gần: đề xuất chuyển sang "Chưa có ở bản đầu".
- Quy ước cho module chưa thiết kế dài hơn điều mà API bắt buộc.
- Giới hạn cố định mà game khác có thể cần giá trị khác: đề xuất chuyển vào settings.

Đề xuất thu gọn làm đổi API public của module đã phát hành thì ghi rõ cần nâng version.

### Pass

Pass khi không còn Chặn. Nên sửa, Thu gọn và Ghi chú không bao giờ chặn.

### Báo cáo

Viết tiếng Việt, theo thứ tự:
1. **Kết luận**: `Pass` hoặc `Chưa pass (n Chặn)`.
2. **Cổng**: mỗi cổng của việc đang review một dòng, `Đạt` / `Chưa đạt` / `Chưa kiểm được`, kèm bằng chứng ngắn. Đi hết mọi cổng, không dừng khi đã thấy lỗi.
3. **Chặn**: mỗi mục ghi cổng, câu hoặc bất biến bị vi phạm. Mục cần user quyết thì viết thành câu hỏi đánh số, kèm câu trả lời đề xuất, sao cho trả lời "có" là chấp nhận đề xuất.
4. **Nên sửa**: mỗi mục ghi nên sửa ngay hay ghi vào `## Kế hoạch (tạm)`.
5. **Thu gọn**.
6. **Ghi chú**.
7. **Gợi ý cho quy trình** (không bắt buộc): bài học muốn thêm vào [Gợi ý soi](#gợi-ý-soi) hay vào cổng, áp dụng từ module sau.

## Cổng thiết kế

Đối tượng: `docs/modules/<module>.md`, các ADR nó trỏ tới, mục của module trong `GLOSSARY.md`.

| # | Cổng |
|---|---|
| T1 | Tên lõi, namespace không trùng type của `System` và `UnityEngine` |
| T2 | Có phạm vi trong và ngoài module; mỗi mục "chưa làm" có lý do hoặc điều kiện để làm; bảng đầu tài liệu ghi phụ thuộc vào module khác |
| T3 | Kiến trúc theo mẫu: lõi C# thuần nhận settings C# thuần, kiểm tra và copy một lần; ScriptableObject chỉ bọc settings và trả bản copy; MonoBehaviour là driver mỏng, gặp config sai thì log lỗi và tự tắt, lấy component cùng GameObject bằng `GetComponent` trong `Awake` và `[RequireComponent]`; thay đổi lúc chạy đi qua lệnh của lõi; không singleton, không state static; không trả ra object mutable mà lõi giữ; lõi trả thông tin UI cần; ID của dữ liệu authoring là tên asset; cấu hình dùng chung khai một chỗ trên asset riêng |
| T4 | Khối API liệt kê mọi type và lệnh public; mỗi lệnh public có một dòng trong bảng trường hợp biên theo mục "Bảng trường hợp biên" của `coding-standard.md` |
| T5 | Có danh sách event; có thứ tự khi một lệnh gây nhiều event; có luật gọi lệnh trong handler (được phép, xếp hàng hay bị cấm), riêng cho `Restore` và lệnh làm thời gian trôi |
| T6 | Save: lưu gì, không lưu gì và vì sao; với từng loại dữ liệu, ghi khi hỏng thì làm gì và khi lệch settings thì làm gì; dữ liệu vắng mặt trong snapshot; thứ tự restore khi nhiều module cùng góp vào một giá trị |
| T7 | Mọi ví dụ có sẵn trong tài liệu, chạy tay từng bước (state, event, clamp sau mỗi bước), ra đúng kết quả tài liệu ghi |
| T8 | Không có hai câu mâu thuẫn nhau, trong tài liệu, với ADR hay với `coding-standard.md`; tài liệu và ADR không dùng từ trong mục _Avoid_ của `GLOSSARY.md` |
| T9 | `## Kế hoạch (tạm)` có câu hỏi cho playground, hoặc ghi rõ là không cần |

## Cổng playground

Đối tượng: experiment trong `Assets/Playground/<Experiment>/`. Không xét chất lượng code: code playground sẽ bị bỏ.

| # | Cổng |
|---|---|
| P1 | Mỗi câu hỏi trong `## Kế hoạch (tạm)` có câu trả lời, kèm bằng chứng (chơi thử, test) |
| P2 | Không có lỗi playground làm sai một câu trả lời |
| P3 | Overlay debug có nút cho mọi lệnh public của lõi |

Báo cáo thêm: câu trả lời của từng câu hỏi; thay đổi đề xuất cho tài liệu thiết kế, kèm ví dụ gameplay.

## Cổng phát hành

Đối tượng: package, playground dùng package, tài liệu của module (tài liệu thiết kế, ADR, `README.md` và `CHANGELOG.md` của package).

| # | Cổng |
|---|---|
| R1 | `dotnet build` trên `.csproj` do Unity sinh không lỗi; user báo test EditMode pass. Chưa có kết quả test thì `Chưa kiểm được`, chưa pass |
| R2 | API public trong code khớp khối API trong `README.md` của package; tài liệu thiết kế không còn giữ bản API riêng |
| R3 | Code làm đúng từng câu của tài liệu thiết kế và của bảng quy tắc, bảng Settings trong `README.md` của package |
| R4 | Mỗi dòng bảng trường hợp biên (trong `README.md` của package) và mỗi luật của mô hình có ít nhất một test; module có thời gian có test "hai đường cùng kết quả" cho B1 |
| R5 | Code theo `coding-standard.md` |
| R6 | `README.md` của package tự đứng một mình, có mọi type public và khớp API |
| R7 | Playground chỉ dùng package, không còn code lõi |
| R8 | Version trong `package.json`, `README.md` và mục mới nhất của `CHANGELOG.md` khớp nhau; `CHANGELOG.md` ghi đủ thay đổi API public và hành vi so với version trước; trạng thái trong `roadmap.md` đúng giai đoạn; `## Kế hoạch (tạm)` đã xóa (chỉ xét khi phát hành) |

## Review code ngoài giai đoạn

Dùng khi review một thay đổi bất kỳ (`/review-my-code` hay skill review khác), kể cả trên module đã phát hành.

- Code trong package: cổng R1, R3, R5; thêm R2, R6 khi thay đổi đụng API public; R4 cho luật mới hay luật bị sửa; R8 (trừ phần Kế hoạch) khi tăng version.
- Code trong playground: chỉ lỗi làm sai câu trả lời cho câu hỏi thiết kế.
- Code trong scenario: R1, R5 và bất biến lõi.
- Thu gọn chỉ xét phần code mà thay đổi đụng tới.

## Gợi ý soi

Bài học từ các module trước: chỗ nên nhìn kỹ khi review. Không phải cổng: thấy lỗi ở đây thì xếp mức theo [Xếp mức](#xếp-mức).

- Tên trùng type của engine. _Calendar: `Time`; Stats: `Attribute`._
- Quyết định khó đảo ngược, gây bất ngờ và là kết quả của một trade-off thật thì nên có ADR.
- Snapshot chứa mảng thì là class, và `CaptureSnapshot()` luôn trả object, mảng mới; dùng snapshot làm state sống nếu được, để save và load không lệch nhau.
- Không lưu giá trị trùng settings nếu muốn chỉnh cân bằng có tác dụng với save cũ.
- Event phát trong lúc load mang giá trị tạm: module khác xử lý thế nào.
- Sửa ScriptableObject lúc Play bị giữ lại sau khi Stop, nên lõi chỉ dùng bản copy.
- Dữ liệu vắng mặt trong snapshot. _Stats: resource thêm sau khi có save phải giữ giá trị của ván đang chơi khi load._
- Gỡ rồi thêm trong một lệnh làm giá trị phụ thuộc bị clamp ở giữa. _Stats: Resource lấy stat làm max._
- Nguồn phụ thuộc vào chính dữ liệu được restore. _Stats: derived stat suýt làm mất máu khi load._
- Mốc không rơi vào step: chạy thường cũng phải cắt tại đó. _Stats: Ốm 45 phút với step 10 làm Needs tính ×2 suốt 50 phút._
- Effect hay trạng thái sinh ra giữa một khoảng thời gian chỉ chịu phần thời gian sau lúc nó sinh ra. _Stats: Ốm áp khi đói cạn giữa lần skip bị trừ cả khoảng._
- Effect hết hạn giữa lần skip làm lệch module dùng tham số. _Stats: Needs._
- Bộ đếm chống vòng lặp phải phủ mọi đường event; hai lớp gọi lẫn nhau được thì dùng chung một bộ đếm.
- Tài nguyên dạng "giữ chỗ" (pause source, source của modifier) cần quy ước nhả ra.
- Test log lỗi có chủ ý dùng `LogAssert.Expect`; log đỏ trong Console là bình thường nếu test pass.
