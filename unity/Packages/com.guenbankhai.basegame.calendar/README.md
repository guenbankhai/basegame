# BaseGame Calendar

Đồng hồ và lịch cho game: giờ, ngày, thứ, mùa, năm; tốc độ, tạm dừng, skip, fast-forward; event; snapshot để save. Thiết kế đầy đủ: `docs/modules/calendar.md`. Thuật ngữ: `GLOSSARY.md` (mục Calendar).

Namespace: `BaseGame.Calendar`. Không phụ thuộc module nào khác.

## Thiết lập

1. Tạo asset: **Create → BaseGame → Calendar Config**, chỉnh trong Inspector (step, giờ bắt đầu ngày, tên thứ, mùa, mốc game mới, giới hạn).
2. Gắn `GameClockDriver` vào một GameObject, kéo asset vào ô Config.
3. Component khác giữ `[SerializeField] private GameClockDriver _driver;` và lấy `_driver.Clock` từ `Awake`/`OnEnable` trở đi (driver chạy trước nhờ `DefaultExecutionOrder(-1000)`). Config thiếu hoặc sai thì driver log lỗi, tự tắt và `Clock` là `null`.

Không có singleton. Code thuần (test, tool) tạo trực tiếp:

```csharp
var clock = new GameClock(new CalendarSettings { MinutesPerStep = 10, DayStartMinute = 360 });
clock.Tick(deltaSeconds);
```

Settings là cấu hình ban đầu: được kiểm tra và copy một lần trong constructor, sửa sau đó không ảnh hưởng đồng hồ đang chạy. Lấy settings từ asset bằng `config.CreateSettings()` (bản copy, sửa không đụng tới asset). Muốn đổi đồng hồ đang chạy thì dùng lệnh bên dưới.

## Đọc thời gian

```csharp
CalendarDate now = clock.Now;          // TotalMinutes, Day, Hour, Minute, DayOfWeek, DayInSeason, SeasonIndex, Year
string day = clock.GetDayName(now.DayOfWeek);
string season = clock.GetSeasonName(now.SeasonIndex);
double smooth = clock.ExactMinutes;    // nội suy hình ảnh giữa hai step
long next6am = clock.NextOccurrence(6 * 60);
```

## Event

`Ticked(TickInfo)` mỗi step; `HourChanged`, `DayChanged`, `WeekChanged`, `SeasonChanged`, `YearChanged(CalendarDate)` đúng phút xảy ra, theo thứ tự đó; `Skipped(from, to)`; `Restored(date)`; `FastForwardCompleted(date)` khi fast-forward tới đích (không phát nếu handler ở step đó dừng hoặc đổi đích fast-forward).

Trong handler, `clock.Now` là thời điểm của chính event. Lệnh đổi đồng hồ gọi trong handler (`SkipTo`, `FastForward*`, `StopFastForward`, `SetSpeed`, `Pause`, `Resume`, `Restore`) được xếp hàng và chạy sau các event của step đó. Sau đó, nếu đồng hồ tạm dừng, vừa skip hoặc restore, fast-forward vừa tới đích, hoặc running speed (`clock.Speed`) đổi, phần thời gian còn lại của lần `Tick` đó bị bỏ. Không gọi `Tick` trong handler.

## Điều khiển

```csharp
clock.SetSpeed(2f);                         // base speed, trong [0, clock.MaxSpeed]
clock.Pause(menu); clock.Resume(menu);      // theo pause source; chạy khi không còn pause source nào
clock.FastForwardTo(clock.NextOccurrence(360), 50f);   // ngủ có hiệu ứng: mọi event vẫn phát
clock.SkipTo(clock.NextOccurrence(360));    // ngủ màn hình đen: chỉ phát Skipped
clock.AdvanceSeason();                      // mùa đổi ở day boundary kế tiếp
```

Mọi mốc thời gian phải là bội của `MinutesPerStep`. `SkipTo` nhảy tối đa `GameClock.MaxSkipDays` ngày. Tốc độ NaN, âm hoặc lớn hơn `clock.MaxSpeed` (kể cả vô hạn) bị từ chối bằng `ArgumentOutOfRangeException`; thông báo lỗi ghi rõ `MaxSpeed`. UI chọn tốc độ nên giới hạn theo `clock.MaxSpeed`.

Ai gọi `Pause(this)` thì tự `Resume(this)`, kể cả trong `OnDisable`: pause source bị hủy mà chưa resume sẽ giữ đồng hồ dừng mãi.

## Viết module dùng Calendar

Để chạy thường và skip ra cùng kết quả:

- **Accumulating module** (nhu cầu, hiệu ứng, chế tạo): viết một hàm `Advance(deltaMinutes)`, gọi từ `Ticked` (`info.DeltaMinutes`) và `Skipped` (`to.TotalMinutes - from.TotalMinutes`). Tự xử lý ngưỡng rơi giữa khoảng.
- **Milestone module** (lớp học, cơ hội): ở `Skipped`, tự quét khoảng `(from, to]`.
- **State-based module** (NPC ở đâu, cửa hàng mở chưa): tính trạng thái từ thời gian; tính lại ở `Skipped` và `Restored`.

## Save

```csharp
CalendarSnapshot data = clock.CaptureSnapshot();   // struct [Serializable], dùng được với JsonUtility
clock.Restore(data);                               // chỉ phát Restored; dữ liệu sai thì ném ArgumentException
```

Không lưu tốc độ, pause source, trạng thái fast-forward. Đổi `MinutesPerStep` hoặc `DayStartMinute` sau khi đã có save có thể làm save cũ bị từ chối hoặc lệch clock time.
