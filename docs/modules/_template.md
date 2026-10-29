# <Module>

<Một câu: module làm gì.>

Tài liệu thiết kế: phạm vi, kiến trúc, mô hình nội bộ và lý do. Luật hành vi (API, Settings, event, save, quy ước cho module dùng nó): `<module>.readme.md`, từ giai đoạn Package là `README.md` của package. Thuật ngữ: `GLOSSARY.md` (mục <Module>). Trạng thái: `roadmap.md`.

| Mục | Giá trị |
|---|---|
| Package | `unity/Packages/com.guenbankhai.<module>` |
| Namespace / asmdef | `GuenBanKhai.<Module>`, `GuenBanKhai.<Module>.Tests` |
| Phụ thuộc | <module khác, hoặc "Không có"> |
| Quyết định | <link ADR, hoặc "Chưa có"> |

<!--
Cách dùng:
- Luật hành vi không viết ở đây mà ở README (khung: _readme-template.md).
- Mục không có gì để viết thì ghi "Không có", không xóa, để review biết đã xét.
- Luật mặc định (lỗi, kẹp, event, source, restore, save) đã có ở coding-standard.md: chỉ ghi chỗ làm khác và lý do.
- [Tn] là cổng thiết kế trong review.md mà mục đó phải đạt.
-->

## Phạm vi [T2]

**Trong module:** <…>

**Ngoài module:**

- <việc>: <thuộc ai, hoặc lý do / điều kiện để làm sau>

## Kiến trúc [T3]

| Thành phần | Vai trò |
|---|---|
| <Lõi C# thuần> | <…> |
| <Settings C# thuần> | <…> |
| <ScriptableObject config> | <…> |
| <MonoBehaviour driver, nếu có> | <…> |

- Lấy tham chiếu: <…>
- Config thiếu hoặc sai: <…>

## Mô hình nội bộ

State (nguồn sự thật duy nhất):

```text
<field>   <ý nghĩa>
```

- Giá trị tính ra từ state: <…>
- Cách lõi xử lý bên trong và lý do: <…>
- Giả định quy mô: <…, hoặc "Không có">

## Quyết định đi kèm

<Lựa chọn nhỏ chưa đáng thành ADR, mỗi dòng kèm lý do: giới hạn, hằng số, chỗ làm khác `coding-standard.md`. Hoặc "Không có".>

## Ghép với module khác

<Quy ước khi dùng chung với một module cụ thể đã thiết kế. Hoặc "Không có".>

## Kiểm thử [R4]

| File test | Phủ |
|---|---|
| <…> | <…> |

Kiểm bằng tay (luật chạy qua callback của Unity):

| Luật | Bước kiểm |
|---|---|
| <…, hoặc "Không có"> | <…> |

Playground: `Assets/Playground/<Experiment>/`, scene `Assets/Scenes/<Experiment>.unity`.

## Kế hoạch (tạm) [T9]

Giai đoạn: **<Thiết kế | Playground | Package>**. Xóa mục này khi phát hành.

- Câu hỏi cho playground: <…, hoặc "Không cần">
- <Nên sửa đã duyệt, việc còn lại>
