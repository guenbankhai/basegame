# Changelog

Mọi thay đổi của package `com.guenbankhai.calendar`, mới nhất ở trên. Version theo [Semantic Versioning](https://semver.org/); khi còn `0.x`, đổi API public thì tăng version minor.

## [0.2.1] - 2026-10-31

### Đổi hành vi

- `GameClockDriver` gặp config sai: log bằng `LogError` kèm tên config, thay cho `LogException`. Driver vẫn tự tắt như cũ.
- Nội dung log của `GameClock`, `GameClockDriver`, `CalendarConfig` theo định dạng `<Loại> '<tên>': <nội dung>`. Code đang dò chuỗi log cũ cần đổi theo.

## [0.2.0] - 2026-10-29

### Đổi API (không tương thích)

- Bỏ `GameClock.FastForward(float speed)` (fast-forward không đích). Dùng `FastForwardTo(targetMinutes, speed)`.
- Bỏ kiểu `StepInfo`; `Stepped` đổi thành `Action<CalendarDate>` như các event mốc khác. Độ dài step đọc qua `GameClock.MinutesPerStep` (mới); tốc độ và trạng thái fast-forward đọc từ `Speed`, `IsFastForwarding` trong handler.
- `MaxSkipDays` thành setting: `CalendarSettings.MaxSkipDays` (mặc định 36500, phải `> 0`). Hằng `GameClock.MaxSkipDays` thành property đọc giá trị của settings.
- `GameClock.MaxTotalMinutes` đổi từ `long.MaxValue / 2` thành `700_000_000 × 1440`. `Restore`, `SkipTo` và `FastForwardTo` từ chối giá trị lớn hơn, kể cả save cũ có `TotalMinutes` trong khoảng hai giá trị này.

### Thêm

- `GameClock.MaxCommandsPerDrain` (1000) thành hằng public.
- `GameClock.MaxStepsPerFrame`: đọc lại setting cùng tên.
- `SeasonDefinition.Name` và `SeasonDefinition.LengthInDays` có setter public.

### Đổi hành vi

- Settings kiểm chặt hơn: `MaxSpeed` phải `>= 1` (trước là `> 0`); tên thứ và tên mùa không được null hoặc rỗng. Settings trước đây hợp lệ mà vi phạm các luật này giờ làm constructor `GameClock` ném `ArgumentException`.
- `Tick` không còn ném `InvalidOperationException` khi chạy quá `MaxTotalMinutes`, và `NextOccurrence` không còn ném khi kết quả vượt `MaxTotalMinutes`. `MaxTotalMinutes` chỉ còn dùng để kiểm tra đầu vào của `Restore`, `SkipTo` và `FastForwardTo`.
- Pause source so sánh theo reference (trước là `Equals`): hai source khác object nhưng `Equals` bằng nhau giờ giữ đồng hồ riêng rẽ.
- Bỏ giới hạn `Year == int.MaxValue` khi sang năm (`Tick` và `SkipTo` không còn ném ở đó). Package giả định đồng hồ không chạy tới các giới hạn này.

## [0.1.1] - 2026-10-24

### Đổi tên

- Package `com.guenbankhai.basegame.calendar` thành `com.guenbankhai.calendar`; namespace và asmdef `BaseGame.Calendar` thành `GuenBanKhai.Calendar`; menu tạo asset thành `GuenBanKhai/Calendar Config`.
- Event `Ticked` thành `Stepped`, kiểu `TickInfo` thành `StepInfo`.

### Đổi hành vi

- `SkipTo(Now)` hủy fast-forward đang chạy.
- `SkipTo` và `FastForwardTo` từ chối đích lớn hơn `MaxTotalMinutes`.
- `AdvanceSeason()` gọi trong handler được xếp hàng như các lệnh khác.

## [0.1.0] - 2026-10-24

Bản đầu tiên: đồng hồ theo step, lịch (giờ, ngày, tuần, mùa, năm), tốc độ, tạm dừng, skip, fast-forward, event và snapshot để save.
