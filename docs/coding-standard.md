# Chuẩn code

Áp dụng cho mọi code C# trong Playground, Integration và Packages. Định dạng và cách đặt tên field do `.editorconfig` kiểm tra.

## Đặt tên

- Theo quy ước C# của Microsoft: `PascalCase` cho type, method, property, event và public field; `camelCase` cho biến cục bộ và tham số.
- Private field: `_camelCase` (Inspector của Unity vẫn hiện `_moveSpeed` thành "Move Speed").
- Hằng số và field `static readonly`: `PascalCase`.
- Không đặt type hay namespace trùng tên type hay dùng của `System` và `UnityEngine` (`Time`, `Attribute`, `Object`, `Random`, `Debug`…). Namespace `GuenBanKhai.Time` làm mọi `Time.deltaTime` trong `GuenBanKhai.*` trỏ sai; class `Attribute` làm file có `using System;` lỗi CS0104.

## API của module

Áp dụng cho Packages; Playground nên theo để code chuyển sang package dễ hơn. Đây là mặc định của mọi module: tài liệu module không viết lại, chỉ ghi chỗ làm khác và lý do.

- **Kiểm tra ở biên:** kiểm tra đầy đủ đầu vào từ bên ngoài: tham số của lệnh public, settings, snapshot. Bên trong lõi không thêm code cho trường hợp chỉ xảy ra khi tràn kiểu dữ liệu.
- **Lỗi:** đầu vào sai (null, NaN, vô hạn, ngoài khoảng, ID không có, ID trùng) ném `ArgumentException` hoặc lớp con. Lệnh không hợp với trạng thái hiện tại (gọi trong handler bị cấm, chạm giới hạn của lõi) ném `InvalidOperationException`. Lệnh bị từ chối không làm đổi state.
- **Giá trị ngoài khoảng:** báo lỗi, không âm thầm kẹp lại. Chỉ kẹp khi kẹp chính là luật của mô hình (giá trị hiện tại của Resource, min/max của Stat).
- **Giới hạn:** giới hạn mà game thật có thể chạm tới (độ dài skip, số stack) nằm trong settings, có giá trị mặc định, để mỗi game tự đặt. Hằng số chỉ dùng cho giới hạn chống treo khi code bên ngoài có lỗi (handler gọi lệnh lặp vô tận). Lỗi do vượt giới hạn ghi giới hạn đó trong thông báo; giới hạn đọc được qua property.
- **Giả định quy mô:** thiết kế dựa vào quy mô (duyệt tuyến tính, cấp phát theo số phần tử) thì tài liệu module ghi giả định đó.
- **Event:** cập nhật xong toàn bộ state rồi mới phát. Lõi gọi từng handler trong `try/catch`, lỗi thì `Debug.LogException` rồi gọi tiếp handler còn lại.
- **Source** (thứ giữ chỗ hay đóng góp vào state, như pause source, source của modifier): kiểu `object`, so sánh theo reference, `null` báo lỗi. Thêm trùng thì không làm gì, gỡ thứ không có cũng không làm gì; trả `bool` nếu nơi gọi cần biết.
- **Restore:** kiểm tra toàn bộ snapshot trước, bị từ chối thì không đổi gì. Không phát lại event của từng thay đổi, chỉ phát một `Restored` ở cuối. Ngoại lệ: phần state mà code bên ngoài phải phản ứng để dựng lại state phụ thuộc trước khi restore tiếp (derived stat của Stats) thì vẫn phát event; ghi lý do trong tài liệu module.
- **Dữ liệu save lệch settings:** snapshot hỏng (NaN, âm, trùng) hoặc không khớp settings hiện tại (ID đã bị xóa, index ngoài khoảng, không còn trên lưới mới) thì từ chối cả snapshot. Lõi không tự sửa save cho khớp và không có cơ chế chuyển đổi save. Product đổi luật gốc của module sau khi đã có save thì tự chọn: chấp nhận mất save không còn hợp, hoặc tự nâng cấp save trước khi gọi `Restore`. Muốn chỉnh cân bằng mà không hỏng save cũ thì không lưu giá trị trùng settings.

### Bảng trường hợp biên

Module có một bảng, mỗi lệnh public một dòng: trong tài liệu thiết kế khi chưa có package, trong `README.md` của package từ giai đoạn Package (xem `module-workflow.md`). Mỗi dòng chỉ ghi điều riêng của lệnh: giá trị trả về, gọi lại hay gọi khi đã ở trạng thái đích, chỗ làm khác mặc định ở trên. Lệnh không có gì riêng thì ghi "Mặc định". Luật chung cho cả module (gọi trong handler, constructor) có dòng riêng.

## File

- Mỗi file một type chính; tên file trùng tên type.
- Namespace trùng tên asmdef (xem `docs/structure.md`).

## Unity

- Dùng `[SerializeField] private` thay cho public field; cho đọc qua property.
- Field `[SerializeField]` luôn có giá trị khởi tạo rõ ràng: `= null` cho tham chiếu tới object Unity, giá trị mặc định cho kiểu khác (`= 0`, `= false`, `= new …`). Lý do: Unity gán field qua serialization nên compiler không thấy, và sẽ báo nhầm CS0649 ("never assigned"); khởi tạo rõ ràng cũng cho biết giá trị mặc định trong Inspector. Ngoại lệ: field trong `struct`, vì C# 9 không cho khởi tạo field trong struct.
- Không dùng `Find*` hay `SendMessage`. Cache component trong `Awake`, không gọi `GetComponent` mỗi frame.
- Giữa các phần code, dùng `event Action<T>` của C#. Chỉ dùng `UnityEvent` khi nối trong Inspector.

## Null

- Nullable reference types để tắt.
- Không dùng `?.` hay `??` với `UnityEngine.Object`: Unity override `== null`, còn hai toán tử này bỏ qua override đó.

## State static

- Không giữ state gameplay trong field `static`. State thuộc về instance: component, ScriptableObject hoặc class C# thường.
- `const`, giá trị `static readonly` và static method thuần thì được phép.
- Project này tắt domain reload, nên field static giữ nguyên giá trị giữa các lần Play. Khi buộc phải có state static, reset nó:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void ResetStatics()
{
    _cache = null;
}
```

## Phiên bản ngôn ngữ

- C# 9 (`LangVersion` trong `.csproj` do Unity sinh): không dùng file-scoped namespace, global using hay tính năng C# 10 trở lên.

## Test và tài liệu

- Package: bắt buộc có test cho hành vi quan trọng và một usage doc ngắn.
- Playground: test và tài liệu không bắt buộc.
