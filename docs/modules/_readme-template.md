# GuenBanKhai <Module>

<!--
Cách dùng:
- Giai đoạn Thiết kế: tạo file này thành `docs/modules/<module>.readme.md`. Giai đoạn Package: di chuyển nguyên file thành `README.md` của package, điền Cài đặt và Thiết lập.
- Tự đứng một mình: không trỏ tới `docs/`, `GLOSSARY.md`, ADR hay file nào ngoài package. Người chỉ có package vẫn dùng đúng.
- Đây là nơi duy nhất giữ luật hành vi của module. Tài liệu thiết kế không viết lại.
- Mục không có gì để viết thì ghi "Không có", không xóa.
- Luật mặc định của `coding-standard.md` không cần viết lại, trừ khi người dùng package cần biết (ví dụ lỗi ném loại gì).
- [Tn] là cổng thiết kế trong review.md mà mục đó phải đạt. Xóa khối comment này khi chuyển vào package.
-->

<Một câu: module làm gì.>

| Mục | Giá trị |
|---|---|
| Package | `com.guenbankhai.<module>` |
| Version | <x.y.z> (thay đổi giữa các version: `CHANGELOG.md`) |
| Namespace | `GuenBanKhai.<Module>` |
| Unity | 6000.6 trở lên |
| Phụ thuộc | <package khác, hoặc "Không có"> |

## Cài đặt

<Điền ở giai đoạn Package: embedded, đường dẫn local hoặc Git; `testables` nếu cài ngoài `Packages/`.>

## Khái niệm

| Thuật ngữ | Nghĩa |
|---|---|
| <…> | <…> |

## Thành phần

| Type | Vai trò |
|---|---|
| <…> | <…> |

## Thiết lập

<Điền ở giai đoạn Package: tạo asset, gắn component, lấy tham chiếu, ví dụ code; tạo lõi bằng code không cần asset.>

### Settings

| Trường | Mặc định | Ràng buộc |
|---|---|---|
| <…> | <…> | <…> |

Settings sai: <…>

## API [T4]

```csharp
<mọi type, lệnh, property, event, hằng số public>
```

## Event [T5]

| Event | Khi nào |
|---|---|
| <…> | <…> |

- Thứ tự khi một lệnh gây nhiều event: <…>
- Gọi lệnh trong handler: <được phép | xếp hàng | cấm>; riêng `Restore` và lệnh làm thời gian trôi: <…>
- Handler ném exception: <…>

## Điều khiển [T4]

Mỗi lệnh public một dòng; chỉ ghi điều riêng của lệnh.

| Lệnh | Quy tắc |
|---|---|
| Chung | <lỗi ném loại gì; gọi trong handler; chỗ làm khác mặc định> |
| <lệnh> | <trả về, gọi lại, trạng thái đích, giới hạn; hoặc "Mặc định"> |

<Luật mô hình mà người dùng cần biết: mỗi luật một mục con.>

## Save [T6]

- Lưu: <…>. Không lưu: <…>, vì <…>.
- Dữ liệu hỏng hoặc không khớp settings: <…>
- Dữ liệu vắng mặt trong snapshot: <…>
- Thứ tự restore khi nhiều module góp vào một giá trị: <…, hoặc "Không có">

## Viết module dùng <Module>

<Chỉ điều API bắt buộc nơi dùng phải làm.>

## Giới hạn

| Hằng số / setting | Giá trị | Ý nghĩa |
|---|---|---|
| <…> | <…> | <…> |

Giả định quy mô: <…, hoặc "Không có">
