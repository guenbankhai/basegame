# Calendar: skip và fast-forward là hai cơ chế riêng

Skip nhảy một lần và chỉ báo khoảng đã nhảy; fast-forward vẫn đi từng step và phát mọi event. Lý do: phát đủ event cho một lần nhảy nhiều ngày quá tốn. Đổi lại, module dùng Calendar phải tự xử lý khoảng skip theo quy ước trong tài liệu module.

## Considered Options

- Một cơ chế duy nhất, skip cũng phát đủ event từng step: đơn giản cho module dùng Calendar, nhưng mọi handler (UI, âm thanh) chạy hàng nghìn lần trong một frame.
