# <Module>

<Một câu: module làm gì.>

Tài liệu này mô tả thiết kế hiện hành của module. Thuật ngữ: `GLOSSARY.md` (mục <Module>). Cách dùng nhanh: `README.md` của package. Trạng thái: `roadmap.md`.

| Mục | Giá trị |
|---|---|
| Package | `unity/Packages/com.guenbankhai.basegame.<module>` |
| Namespace / asmdef | `BaseGame.<Module>`, `BaseGame.<Module>.Tests` |
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

<State, giá trị tính ra, quy tắc.>

## Event

<Danh sách event, thời điểm phát, thứ tự.>

## API và trường hợp biên

```csharp
<API public>
```

| Lệnh | Quy tắc |
|---|---|
| Chung | <loại exception, ràng buộc chung> |
| <…> | <đầu vào sai, gọi lại, gọi trong handler, giới hạn> |

### Settings

| Trường | Ràng buộc |
|---|---|
| <…> | <…> |

## Save

<Gì được lưu, gì không, kiểm tra khi nạp.>

## Quy ước cho module dùng <Module>

<…>

## Kiểm thử

| File | Phủ |
|---|---|
| <…> | <…> |

Thử bằng tay: playground `Assets/Playground/<Experiment>/`, scene `Assets/Scenes/<Experiment>.unity`.
