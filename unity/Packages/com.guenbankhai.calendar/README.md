# GuenBanKhai Calendar

Đồng hồ và lịch cho game: giờ, ngày, thứ, mùa, năm; tốc độ, tạm dừng, skip, fast-forward; event; snapshot để save.

| Mục | Giá trị |
|---|---|
| Package | `com.guenbankhai.calendar` |
| Namespace | `GuenBanKhai.Calendar` |
| Unity | 6000.6 trở lên |
| Phụ thuộc | Không có |

## Cài đặt

- **Embedded:** chép thư mục package vào `Packages/` của project.
- **Đường dẫn local hoặc Git:** Package Manager → **+** → *Add package from disk* (chọn `package.json`) hoặc *Add package from git URL*.

Test EditMode nằm trong `Tests/`. Package embedded chạy test được ngay. Cài theo cách khác thì thêm `"testables": ["com.guenbankhai.calendar"]` vào `Packages/manifest.json` của project.

## Khái niệm

| Thuật ngữ | Nghĩa |
|---|---|
| **Step** | Khoảng thời gian game nhỏ nhất mà đồng hồ nhảy qua (`MinutesPerStep` phút, ước của 60). Không có thời điểm nằm giữa hai step |
| **Day 0** | Ngày đầu tiên của game mới, gốc để đếm mọi thời điểm (`TotalMinutes = 0`) |
| **Day boundary** | Thời điểm một ngày mới bắt đầu; đồng hồ lúc đó chỉ `DayStartMinute` |
| **Clock time** | Giờ đồng hồ hiển thị (`ClockMinute`), lệch khỏi `TotalMinutes` một khoảng `DayStartMinute` |
| **Fixed-length season** | Mùa có `LengthInDays > 0`, tự kết thúc khi đủ ngày |
| **Command-only season** | Mùa có `LengthInDays = 0`, chỉ kết thúc khi game gọi `AdvanceSeason()` |
| **Skip** | Nhảy thời gian trong một lần; thế giới coi như khoảng đó đã trôi qua. Chỉ phát `Skipped` |
| **Fast-forward** | Thời gian trôi nhanh qua nhiều frame nhưng vẫn đi từng step và phát mọi event |
| **Restore** | Nạp lại snapshot; thời gian trong thế giới không trôi. Chỉ phát `Restored` |
| **Base speed / running speed** | `SetSpeed` đặt base speed. `Speed` là running speed: tốc độ fast-forward khi đang fast-forward, còn lại là base speed |
| **Pause source** | Object giữ đồng hồ dừng; đồng hồ chỉ chạy khi không còn pause source nào |
| **Snapshot** | Trạng thái lịch vừa đủ để lưu và restore |

## Thành phần

| Type | Vai trò |
|---|---|
| `GameClock` | Lõi C# thuần: giữ state, tốc độ, pause source, fast-forward; phát event |
| `CalendarSettings` | Cấu hình ban đầu, C# thuần. `GameClock` kiểm tra và copy một lần lúc tạo |
| `CalendarConfig` | ScriptableObject giữ một `CalendarSettings` để chỉnh trong Inspector |
| `GameClockDriver` | MonoBehaviour gọi `Clock.Tick(Time.deltaTime)` mỗi frame |
| `CalendarDate`, `StepInfo` | Giá trị chỉ đọc, do `GameClock` tạo và truyền qua event |
| `CalendarSnapshot` | Snapshot của state để save, struct `[Serializable]` |

Không có singleton, không có state static.

## Thiết lập

1. **Create → GuenBanKhai → Calendar Config**, chỉnh trong Inspector.
2. Gắn `GameClockDriver` vào một GameObject, kéo asset vào ô **Config**.
3. Component khác giữ tham chiếu tới driver và lấy `Clock` từ `Awake`/`OnEnable` trở đi. Driver có `DefaultExecutionOrder(-1000)` nên `Clock` đã được tạo trước đó.

```csharp
[SerializeField] private GameClockDriver _driver = null;
private GameClock _clock;

private void OnEnable()
{
    _clock = _driver != null ? _driver.Clock : null;
    if (_clock == null)
    {
        enabled = false; // driver has no valid config and already logged why
        return;
    }

    _clock.DayChanged += OnDayChanged;
}

private void OnDisable()
{
    if (_clock == null)
    {
        return;
    }

    _clock.DayChanged -= OnDayChanged;
    _clock = null;
}
```

Config thiếu hoặc sai: driver log lỗi, tự tắt, và `Clock` là `null`, nên component dùng đồng hồ phải kiểm tra `Clock` trước khi đăng ký event như ví dụ trên. Giữ `GameClock` trong field riêng để `OnDisable` hủy đăng ký đúng object đã đăng ký.

Tạo đồng hồ bằng code (test, tool) không cần asset:

```csharp
var clock = new GameClock(new CalendarSettings { MinutesPerStep = 10, DayStartMinute = 360 });
clock.Tick(deltaSeconds);
```

Lấy settings từ asset bằng `config.CreateSettings()`. Hàm này trả bản copy: trong Editor, sửa ScriptableObject lúc Play sẽ bị giữ lại sau khi Stop, nên code không bao giờ được sửa thẳng asset. Settings chỉ là cấu hình ban đầu; đổi đồng hồ đang chạy thì dùng lệnh của `GameClock`.

### Settings

| Trường | Mặc định | Ràng buộc |
|---|---|---|
| `SecondsPerGameMinute` | `1200 / 1440` (ngày 20 phút thật) | Số hữu hạn, > 0 |
| `MinutesPerStep` | 10 | Ước dương của 60 |
| `DayStartMinute` | 0 (00:00) | Trong `[0, 1440)`, bội của step |
| `DayNames` | Mon … Sun | Ít nhất một tên, không tên nào null hoặc rỗng; số tên là độ dài tuần |
| `Seasons` | 4 mùa × 28 ngày | Ít nhất một mùa; `Name` không null hoặc rỗng; `LengthInDays >= 0` |
| `StartYear`, `StartSeasonIndex` | 1, 0 | `StartYear >= 0`; index nằm trong `Seasons` |
| `StartMinutesIntoDay` | 360 | Trong `[0, 1440)`, bội của step |
| `MaxSpeed` | 100 | Số hữu hạn, `>= 1` (đồng hồ bắt đầu ở base speed 1) |
| `MaxStepsPerFrame` | 120 | > 0. Giá trị lớn kèm fast-forward nhanh có thể chạy rất nhiều step và event trong một frame, gây giật |

Settings sai: constructor `GameClock` ném `ArgumentException`; trong Editor, asset log warning.

## Đọc thời gian

```csharp
CalendarDate now = clock.Now;      // TotalMinutes, Day, Hour, Minute, ClockMinute, DayOfWeek, DayInSeason, SeasonIndex, Year
string day = clock.GetDayName(now.DayOfWeek);
string season = clock.GetSeasonName(now.SeasonIndex);
double smooth = clock.ExactMinutes; // gồm phần thời gian đã tích cho step kế tiếp; dùng để nội suy hình ảnh
long next6am = clock.NextOccurrence(6 * 60);   // không bao giờ trả Now: đúng 6:00 thì trả 6:00 ngày mai; quá MaxTotalMinutes thì ném InvalidOperationException
```

Cách tính: `Day = TotalMinutes / 1440`, `ClockMinute = (TotalMinutes + DayStartMinute) % 1440`, `DayOfWeek = Day % WeekLength`. Tuần chạy liên tục qua các mùa.

## Event

| Event | Khi nào |
|---|---|
| `Stepped(StepInfo)` | Mỗi step. `DeltaMinutes` luôn bằng một step |
| `HourChanged`, `DayChanged`, `WeekChanged`, `SeasonChanged`, `YearChanged` | Đúng phút xảy ra |
| `Skipped(from, to)` | Sau skip; không có event nào khác cho khoảng đã skip |
| `Restored(date)` | Sau restore |
| `FastForwardCompleted(date)` | Khi fast-forward chạm đích |

- Cùng một phút, thứ tự phát là: Stepped → Hour → Day → Week → Season → Year → (lệnh xếp hàng) → FastForwardCompleted. State đã cập nhật xong trước khi phát.
- Trong handler, `clock.Now` là thời điểm của chính event đó.
- Handler ném exception: lỗi được log, các handler còn lại vẫn chạy.

### Lệnh gọi trong handler

- Lệnh đổi đồng hồ (`SkipTo`, `FastForward*`, `StopFastForward`, `SetSpeed`, `Pause`, `Resume`, `Restore`, `AdvanceSeason`) gọi trong handler được **xếp hàng**, và chạy sau các event của step đó, đúng thứ tự gọi. Vì vậy mọi handler cùng thấy một thời điểm. Hệ quả: ví dụ gọi `Pause` xong thì `IsPaused` vẫn là `false` cho tới khi hết event của step.
- Nhờ xếp hàng theo thứ tự, `Restore(save); AdvanceSeason();` trong handler áp cờ vào state sau restore, còn `SkipTo(t); AdvanceSeason();` đổi mùa ở day boundary sau `t`.
- Tham số của lệnh xếp hàng được kiểm tra lúc lệnh chạy; lệnh sai chỉ bị log.
- Không gọi `Tick` trong handler (ném `InvalidOperationException`).
- Một lượt chạy tối đa 1000 lệnh xếp hàng; vượt quá (handler gọi lệnh lặp vô tận) thì log lỗi và bỏ phần còn lại.

Sau mỗi step, phần thời gian còn lại của lần `Tick` đó bị bỏ nếu đồng hồ vừa tạm dừng, skip, restore, fast-forward chạm đích, hoặc đổi running speed. `SkipTo(Now)` không tính là skip vì thời gian không trôi. Nhờ vậy đồng hồ không chạy quá chỗ bị dừng khi một frame bị giật.

## Điều khiển

```csharp
clock.SetSpeed(2f);                                   // base speed, trong [0, clock.MaxSpeed]
clock.Pause(this); clock.Resume(this);                // theo pause source
clock.FastForward(10f);                               // fast-forward không đích, tới khi StopFastForward
clock.FastForwardTo(clock.NextOccurrence(360), 50f);  // ngủ có hiệu ứng: mọi event vẫn phát
clock.SkipTo(clock.NextOccurrence(360));              // ngủ màn hình đen: chỉ phát Skipped
clock.AdvanceSeason();                                // đổi mùa ở day boundary kế tiếp
```

| Lệnh | Quy tắc |
|---|---|
| Chung | Mọi mốc thời gian phải là bội của step. Sai thì ném `ArgumentException` (hoặc lớp con) |
| `Tick(d)` | `d` âm, NaN hoặc vô hạn: lỗi. Mỗi lần chạy tối đa `MaxStepsPerFrame` step, phần dư bị bỏ. Chạy quá `GameClock.MaxTotalMinutes`, hoặc cần sang năm khi `Year` đã là `int.MaxValue`: ném `InvalidOperationException`, đồng hồ đứng tại chỗ |
| `SetSpeed(s)` | NaN, `s < 0` hoặc `s > MaxSpeed`: lỗi (thông báo ghi rõ `MaxSpeed`). Đổi trong lúc fast-forward thì áp dụng sau khi fast-forward xong |
| `FastForward(s)`, `FastForwardTo(t, s)` | NaN, `s <= 0` hoặc `s > MaxSpeed`: lỗi. Gọi lại khi đang fast-forward thì thay đích cũ, không phát `Completed` cho đích cũ. `t == Now`: hoàn thành ngay. `t < Now` hoặc `t > GameClock.MaxTotalMinutes`: lỗi |
| `StopFastForward()` | Về base speed, không phát `Completed` |
| `SkipTo(t)` | Hủy fast-forward đang chạy, kể cả khi `t == Now`. `t == Now`: không phát `Skipped`. `t < Now`, `t > GameClock.MaxTotalMinutes` hoặc xa hơn `GameClock.MaxSkipDays` ngày: lỗi. Cần sang năm khi `Year` đã là `int.MaxValue`: ném `InvalidOperationException`, không đổi gì. Chạy được cả khi đang tạm dừng |
| `Pause(src)`, `Resume(src)` | `src == null`: lỗi. Pause hai lần cùng source tính là một; resume source không có thì bỏ qua |
| `NextOccurrence(m)` | Thời điểm **sau** `Now` mà `ClockMinute == m` (đang đúng 6:00 thì trả 6:00 ngày mai). `m` ngoài `[0, 1440)` hoặc không phải bội của step: lỗi. Kết quả vượt `GameClock.MaxTotalMinutes`: ném `InvalidOperationException` |
| `GetDayName(i)`, `GetSeasonName(i)` | `i` ngoài `[0, WeekLength)` hoặc `[0, SeasonCount)`: lỗi |

Tốc độ vượt `MaxSpeed` bị từ chối chứ không bị kẹp, để lỗi ở nơi gọi lộ ra ngay. UI chọn tốc độ nên giới hạn theo `clock.MaxSpeed`.

**Ai gọi `Pause(this)` thì tự `Resume(this)`, kể cả trong `OnDisable`.** Pause source bị hủy mà chưa resume sẽ giữ đồng hồ dừng mãi.

### Skip hay fast-forward

| | Skip | Fast-forward |
|---|---|---|
| Cách chạy | Nhảy thẳng trong một lệnh | Trôi nhanh qua từng frame, vẫn đi từng step |
| Event | Chỉ `Skipped(from, to)` | Mọi event, kèm tốc độ hiện tại |
| Dùng cho | Ngủ màn hình đen, chuyển cảnh, debug | Ngủ có hiệu ứng, ngồi hết giờ học, chờ tới giờ hẹn |

Trong lúc skip, fixed-length season vẫn tự chuyển; cờ `AdvanceSeason()` đã bật thì áp ở day boundary đầu tiên; command-only season đứng yên. Muốn mọi event đúng từng phút thì dùng fast-forward.

### Step chạm đích fast-forward

1. Phát các event của step. `IsFastForwarding` vẫn là `true`.
2. Chạy các lệnh mà handler xếp hàng.
3. Nếu không lệnh nào dừng, thay hoặc hủy fast-forward thì về base speed rồi phát `FastForwardCompleted`. Có lệnh như vậy thì không phát cho đích cũ.

Ví dụ: fast-forward tới 6:00, handler `HourChanged` lúc 6:00 gọi `FastForwardTo(7:00)` → chỉ có `Completed(7:00)`.

## Mùa

Mùa chỉ đổi ở day boundary. Ở mỗi day boundary, mùa đổi khi cờ `AdvanceSeason()` đang bật **hoặc** fixed-length season đã đủ ngày; mỗi day boundary đổi tối đa một mùa. Qua mùa cuối thì sang năm mới và về mùa 0. Gọi `AdvanceSeason()` nhiều lần trong một ngày vẫn chỉ đổi một mùa.

## Save

```csharp
CalendarSnapshot data = clock.CaptureSnapshot();   // bản copy; dùng được với JsonUtility
clock.Restore(data);
```

- Snapshot gồm `TotalMinutes`, `Year`, `SeasonIndex`, `SeasonStartDay`, `SeasonAdvancePending`.
- Không lưu tốc độ, pause source, trạng thái fast-forward. `Restore` giữ nguyên base speed và pause source hiện có, hủy fast-forward đang chạy.
- `Restore` ném `ArgumentException` nếu `TotalMinutes` âm, quá `GameClock.MaxTotalMinutes` hoặc không phải bội của step, `SeasonIndex` ngoài số mùa, `Year < 0`, hoặc `SeasonStartDay` ngoài `[0, Day]`. Snapshot bị từ chối không làm đổi gì.
- Đổi `MinutesPerStep` hoặc `DayStartMinute` sau khi đã có save: save cũ có thể bị từ chối hoặc lệch giờ.

## Viết module dùng Calendar

Để chạy thường và skip ra cùng kết quả:

- **Accumulating module** (đói, hiệu ứng có thời hạn, chế tạo, cây lớn): viết logic một lần thành `Advance(deltaMinutes)`, gọi từ `Stepped` (`info.DeltaMinutes`) và `Skipped` (`to.TotalMinutes - from.TotalMinutes`). Tự xử lý ngưỡng rơi giữa khoảng (hết đói lúc 3h thì bị ốm từ 3h).
- **Milestone module** (lớp học, sự kiện giờ cố định): ở `Skipped`, tự quét khoảng `(from, to]` để không sót mốc.
- **State-based module** (NPC đang ở đâu, cửa hàng mở chưa): tính trạng thái từ thời điểm hiện tại; tính lại ở `Skipped` và `Restored`.

Muốn giảm tải thì tăng `MinutesPerStep` cho cả lịch, rồi nội suy hình ảnh bằng `ExactMinutes`.

## Giới hạn

| Hằng số | Giá trị | Ý nghĩa |
|---|---|---|
| `GameClock.MaxSkipDays` | 36500 | Skip xa hơn coi là bug của nơi gọi |
| `GameClock.MaxTotalMinutes` | `700_000_000 × 1440` (khoảng 1,9 triệu năm) | `Restore`, `SkipTo`, `FastForwardTo` từ chối giá trị lớn hơn, `Tick` không chạy qua, nên snapshot của đồng hồ luôn restore được. Đủ nhỏ để `ExactMinutes` vẫn chính xác |
| `CalendarDate.MinutesPerDay` | 1440 | Một ngày luôn là 24 × 60 phút |
