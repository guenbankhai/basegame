# <Module>

<Một câu: module làm gì.>

Tài liệu này mô tả thiết kế hiện hành của module. Thuật ngữ: `GLOSSARY.md` (mục <Module>). Cách dùng nhanh: `README.md` của package. Trạng thái: `roadmap.md`.

| Mục | Giá trị |
|---|---|
| Package | `unity/Packages/com.guenbankhai.<module>` |
| Namespace / asmdef | `GuenBanKhai.<Module>`, `GuenBanKhai.<Module>.Tests` |
| Phụ thuộc | <module khác, hoặc "Không có"> |
| Quyết định | <link ADR> |

## Phạm vi

**Trong module:** <…>

**Ngoài module** (luật game hoặc module khác): <…>

## Kiến trúc

| Thành phần | Vai trò |
|---|---|
| <Lõi C# thuần> | <…> |
| <Settings> | <…> |
| <ScriptableObject config> | <…> |
| <MonoBehaviour driver, nếu có> | <…> |

- Lấy tham chiếu: <…>

## Mô hình

<State, giá trị tính ra, quy tắc. Giả định quy mô nếu thiết kế dựa vào nó.>

## Event

<Danh sách event, thời điểm phát, thứ tự.>

## API và trường hợp biên

Từ giai đoạn Package: khối API, bảng dưới và bảng Settings chuyển sang `README.md` của package; mục này chỉ còn một câu trỏ sang README và các quyết định thiết kế đi kèm (giới hạn, giả định quy mô, lý do).

```csharp
<API public>
```

Mặc định theo mục "API của module" trong `coding-standard.md`; bảng chỉ ghi điều riêng của từng lệnh.

| Lệnh | Quy tắc |
|---|---|
| Chung | <gọi trong handler; chỗ làm khác mặc định và lý do> |
| <…> | <giá trị trả về, gọi lại, trạng thái đích, giới hạn; hoặc "Mặc định"> |

### Settings

| Trường | Ràng buộc |
|---|---|
| <…> | <…> |

## Save

<Gì được lưu, gì không, kiểm tra khi nạp.>

## Quy ước cho module dùng <Module>

<Chỉ điều mà API bắt buộc nơi dùng phải làm. Câu hỏi thuộc module chưa thiết kế ghi vào `roadmap.md`.>

## Kiểm thử

| File | Phủ |
|---|---|
| <…> | <…> |

Thử bằng tay: playground `Assets/Playground/<Experiment>/`, scene `Assets/Scenes/<Experiment>.unity`.

## Kế hoạch (tạm)

Giai đoạn: **<Thiết kế | Playground | Package>**. Xóa mục này khi phát hành.

- Câu hỏi cho playground: <câu hỏi chơi thử mới trả lời được, hoặc "Không cần">
- <Nên sửa đã duyệt, việc còn lại>
