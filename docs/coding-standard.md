# Chuẩn code

Áp dụng cho mọi code C# trong Playground, Integration và Packages. Định dạng và cách đặt tên field do `.editorconfig` kiểm tra.

## Đặt tên

- Theo quy ước C# của Microsoft: `PascalCase` cho type, method, property, event và public field; `camelCase` cho biến cục bộ và tham số.
- Private field: `_camelCase` (Inspector của Unity vẫn hiện `_moveSpeed` thành "Move Speed").
- Hằng số và field `static readonly`: `PascalCase`.

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
