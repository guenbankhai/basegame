# Calendar: đồng hồ đi theo step cố định

Đồng hồ nhảy theo step cố định (ước của 60 phút) thay vì trôi liên tục, để mọi mốc rơi đúng vào một step và chạy thường, fast-forward, skip đều đi qua cùng các mốc. Đổi lại, mọi thời điểm phải là bội của step, và hình ảnh mượt phải nội suy riêng.

## Considered Options

- Thời gian liên tục: mốc rơi giữa hai frame, module phải tự nội suy ngưỡng, và chạy thường với skip dễ ra kết quả lệch nhau.
