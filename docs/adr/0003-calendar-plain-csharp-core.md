# Calendar: lõi C# thuần, không singleton

Lõi đồng hồ là class C# thuần nhận settings thuần; ScriptableObject và MonoBehaviour chỉ là lớp bọc, và không có singleton. Lý do: test không cần asset hay scene, và không có state toàn cục khi domain reload đang tắt.

## Considered Options

- Lõi nhận ScriptableObject (đã thử ở playground): test phải tạo và hủy asset, config phải có API chỉ dành cho test.
- Singleton: tiện, nhưng là state toàn cục, khó test, và đi ngược hướng chuẩn bị cho multiplayer trong roadmap.
