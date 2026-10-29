# Stats: lệnh trong handler chạy lồng ngay

Lệnh gọi trong handler của Stats và Effects chạy lồng ngay, không xếp hàng như Calendar, vì Stats không có step để xả hàng đợi và handler derived stat cần đọc ngay giá trị mới. Đổi lại, event của lệnh ngoài có thể đã cũ khi tới lượt phát, nên event còn chờ của giá trị mà handler lồng đã đổi tiếp bị bỏ để listener luôn nhận giá trị cuối. Derived stat nối bằng handler vẫn nằm ngoài batch, nên một lệnh đổi cả stat nguồn lẫn stat làm max có thể clamp current theo max tạm. Chống lặp vô tận bằng `MaxEventDepth`; `Advance` và `Restore` trong handler bị cấm.

## Considered Options

- Xếp hàng lệnh trong handler như Calendar: event luôn theo thứ tự và không bao giờ cũ, nhưng handler derived stat đọc giá trị chưa cập nhật, và lệnh xếp hàng không có mốc tự nhiên để chạy.
- Đưa derived stat vào lõi: batch phủ cả derived, không clamp ở giữa, nhưng lõi phải có công thức hoặc đồ thị phụ thuộc giữa các stat trước khi game thật cần.
