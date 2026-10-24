# Calendar: lệnh gọi trong handler vào hàng đợi

Lệnh đổi đồng hồ gọi trong event handler được xếp hàng và chạy sau các event của step đó, để mọi handler cùng thấy một thời điểm. Đổi lại, lệnh gọi trong handler có tác dụng trễ.

## Considered Options

- Chạy lệnh ngay: state đổi giữa lúc đang phát event, và kết quả phụ thuộc thứ tự đăng ký handler.
- Cấm gọi lệnh trong handler: an toàn nhưng không viết được luật game kiểu "ngất lúc 2:00 thì skip tới 6:00".
