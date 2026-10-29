# BaseGame

**Môi trường cá nhân để học, thử nghiệm và phát triển các cơ chế gameplay trên Unity.**

BaseGame là dự án dài hạn. Nơi đây dùng để hiểu sâu Unity, thử nhiều cách xây dựng cơ chế gameplay, và hoàn thiện những cơ chế đã được kiểm chứng thành các module đủ chuẩn để dùng trong các sản phẩm game thực tế sau này.

> BaseGame không phải một game, không phải game engine, và không phải framework cho mọi thể loại game.

---

## Tầm nhìn

Theo thời gian, BaseGame trở thành hai thứ:

- **Một kho module gameplay đáng tin cậy.** Khi bắt đầu một game mới, chỉ cần chọn những module phù hợp và tập trung vào phần gameplay riêng của game đó, thay vì viết lại từ đầu những cơ chế đã từng làm.
- **Một nguồn kiến thức tích lũy.** Hiểu Unity hoạt động thế nào, phương án nào đã thử, vì sao chọn và vì sao bỏ.

## Mục đích

Mỗi game thường phải xây lại những cơ chế quen thuộc: chỉ số nhân vật, túi đồ, tương tác, chiến đấu, lưu game... Làm vội trong một sản phẩm cụ thể thì code dễ gắn chặt vào game đó và khó dùng lại.

BaseGame tách việc **tìm hiểu và hoàn thiện cơ chế** ra khỏi áp lực của một sản phẩm. Ở đây có thời gian để thử, so sánh, làm sai và làm lại, cho đến khi một cơ chế đủ rõ ràng và ổn định để tin dùng.

## Mục tiêu

| Mục tiêu | Ý nghĩa |
|---|---|
| **Học và nghiên cứu** | Hiểu kiến trúc, vòng đời và các hệ thống của Unity; tìm hiểu cách thiết kế gameplay systems và các đánh đổi. |
| **Thử nghiệm** | Xây prototype cho từng cơ chế, thử nhiều cách triển khai, quyết định dựa trên kết quả thực tế thay vì giả định. |
| **Tái sử dụng** | Những cơ chế đã ổn định được hoàn thiện thành module có ranh giới rõ ràng, có thể đưa vào các sản phẩm khác nhau. |

Không phải thử nghiệm nào cũng phải trở thành module. Một thử nghiệm thất bại nhưng giúp loại bỏ một hướng đi sai vẫn có giá trị.

### Phạm vi quan tâm

RPG (attributes, progression, abilities, equipment) · Action/Combat · Adventure (exploration, interaction, quests) · Survival (gathering, crafting, building) · Simulation · Minigames.

Đây là hướng quan tâm, không phải cam kết triển khai.

### Không hướng tới

- Xây dựng game engine hoặc thay thế hệ thống có sẵn của Unity.
- Framework độc lập với engine hoặc hỗ trợ nhiều engine.
- Một kiến trúc cố định cho mọi thể loại game.
- Abstraction hay tối ưu hóa khi chưa có nhu cầu thực tế.
- Bắt buộc mọi thứ phải tái sử dụng được.

## Quan điểm phát triển

- **Unity-first.** Dùng tối đa những gì Unity đã làm tốt; chỉ tự xây khi giải pháp có sẵn thực sự không đáp ứng.
- **Thử trước, làm thật sau.** Thử nghiệm được tự do thay đổi; chỉ những gì đã được kiểm chứng mới được hoàn thiện thành module.
- **Vừa đủ.** Một hệ thống tốt giải quyết bài toán rõ ràng và đáng tin cậy, không đo bằng số lớp hay mức trừu tượng.
- **Module tự đứng được.** Mang một module sang game khác không kéo theo những thứ game đó không có.

## Nền tảng

Unity `6000.6.5f1` (Unity 6.6) · C# · Universal Render Pipeline · PC · Git (một repo duy nhất).

---

## Modules

Các module đã có thiết kế. Trạng thái và thứ tự làm của mọi module: [`docs/roadmap.md`](docs/roadmap.md). Version của package: `README.md` và `CHANGELOG.md` trong package.

| Module | Mô tả |
|---|---|
| Calendar (`com.guenbankhai.calendar`) | Đồng hồ và lịch theo step: giờ, ngày, tuần, mùa, năm; tốc độ, tạm dừng, skip, fast-forward; event; snapshot để save |
| Stats (`com.guenbankhai.stats`) | Stat có modifier ba tầng, Resource bị chặn bởi một Stat làm max, Effect có thời hạn và luật stack; snapshot để save |

## Tài liệu

| Muốn biết | Đọc |
|---|---|
| Làm module nào, theo thứ tự nào, đang ở trạng thái nào | [`docs/roadmap.md`](docs/roadmap.md) |
| Một module đi từ ý tưởng tới phát hành ra sao; thông tin nào nằm ở đâu | [`docs/module-workflow.md`](docs/module-workflow.md) |
| Thuật ngữ | [`GLOSSARY.md`](GLOSSARY.md) |
| Thiết kế và lý do của từng module | `docs/modules/<module>.md`, quyết định trong [`docs/adr/`](docs/adr/) |
| Cách dùng, API và luật hành vi của module; lịch sử version | `README.md` và `CHANGELOG.md` trong `unity/Packages/com.guenbankhai.<module>/` (khi chưa có package: `docs/modules/<module>.readme.md`) |
| Luật code, cấu trúc thư mục, luật review | [`docs/coding-standard.md`](docs/coding-standard.md), [`docs/structure.md`](docs/structure.md), [`docs/review.md`](docs/review.md) |
