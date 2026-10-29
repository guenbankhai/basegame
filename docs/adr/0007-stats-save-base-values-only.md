# Stats: save không lưu modifier, nguồn modifier restore trước

Snapshot chỉ lưu base value của stat (chỉ những base khác base trong settings, để chỉnh cân bằng có tác dụng với save cũ) và giá trị hiện tại của resource; mỗi nguồn modifier (Effects, Equipment…) tự áp lại modifier của mình khi restore, nên không bao giờ còn modifier "mồ côi". Đổi lại, thứ tự restore là bắt buộc: các nguồn modifier restore trước, Stats restore sau cùng, nếu không giá trị hiện tại bị clamp theo max chưa có modifier. Nguồn phụ thuộc vào base (derived stat) không thể restore trước, nên `Restore` phát `ValueChanged` cho base trước khi đặt giá trị hiện tại.

## Considered Options

- Lưu cả modifier: restore xong là đủ, nhưng source là object nên phải biến thành ID ổn định, và modifier của nguồn đã bị xóa khỏi game sẽ còn mãi.
- Lưu mọi base: đơn giản, nhưng đổi base trong profile không bao giờ tới được save cũ.
- Tạm tắt clamp tới khi gọi `EndRestore()`: thứ tự restore tự do, nhưng thêm trạng thái "đang restore", quên gọi thì resource vượt max mà không ai biết.
