# GuenBanKhai Calendar

Đồng hồ và lịch cho game: giờ, ngày, thứ, mùa, năm; tốc độ, tạm dừng, skip, fast-forward; event; snapshot để save.

| Mục | Giá trị |
|---|---|
| Package | `com.guenbankhai.calendar` |
| Version | 0.2.0 (thay đổi giữa các version: `CHANGELOG.md`) |
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
| `SeasonDefinition` | Một mùa trong `CalendarSettings.Seasons`: tên và độ dài |
| `CalendarConfig` | ScriptableObject giữ một `CalendarSettings` để chỉnh trong Inspector |
| `GameClockDriver` | MonoBehaviour gọi `Clock.Tick(Time.deltaTime)` mỗi frame |
| `CalendarDate` | Một thời điểm, chỉ đọc, do `GameClock` tạo: `TotalMinutes`, `Day`, `Hour`, `Minute`, `ClockMinute`, `DayOfWeek`, `DayInSeason`, `SeasonIndex`, `Year` |
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
| `MaxStepsPerFrame` | 120 | > 0; đọc lại qua `clock.MaxStepsPerFrame`. Giá trị lớn kèm fast-forward nhanh có thể chạy rất nhiều step và event trong một frame, gây giật |
| `MaxSkipDays` | 36500 | > 0. `SkipTo` xa hơn số ngày này bị từ chối (coi là bug của nơi gọi); đọc lại qua `clock.MaxSkipDays` |

Settings sai: constructor `GameClock` ném `ArgumentException`; trong Editor, asset log warning.

Cấu trúc lịch (step, giờ bắt đầu ngày, số mùa, tên thứ) không đổi lúc đang chạy.

## API

`GameClock`:

```csharp
GameClock(CalendarSettings settings);

CalendarDate Now { get; }  double ExactMinutes { get; }
float Speed { get; }  float MaxSpeed { get; }  bool IsPaused { get; }  bool IsFastForwarding { get; }
int MinutesPerStep { get; }  int MaxSkipDays { get; }  int MaxStepsPerFrame { get; }  int WeekLength { get; }  int SeasonCount { get; }
string GetDayName(int dayOfWeek);  string GetSeasonName(int seasonIndex);
long NextOccurrence(int clockMinute);

void Tick(float realDeltaSeconds);
void SetSpeed(float speed);
void Pause(object source);  void Resume(object source);
void FastForwardTo(long targetMinutes, float speed);  void StopFastForward();
void SkipTo(long targetMinutes);
void AdvanceSeason();
CalendarSnapshot CaptureSnapshot();  void Restore(CalendarSnapshot snapshot);

event Action<CalendarDate> Stepped, HourChanged, DayChanged, WeekChanged, SeasonChanged, YearChanged;
event Action<CalendarDate, CalendarDate> Skipped;   // from, to
event Action<CalendarDate> Restored, FastForwardCompleted;

const long MaxTotalMinutes;  const int MaxCommandsPerDrain;   // giá trị: mục Giới hạn
```

Các type khác:
- `CalendarDate`: `const int MinutesPerDay = 1440`; `TotalMinutes`, `Day`, `Hour`, `Minute`, `ClockMinute`, `DayOfWeek`, `DayInSeason`, `SeasonIndex`, `Year`; so sánh bằng và `ToString` để debug. Constructor `internal`: chỉ `GameClock` tạo.
- `CalendarSnapshot`: constructor public `CalendarSnapshot(totalMinutes, year, seasonIndex, seasonStartDay, seasonAdvancePending)` để hệ thống save dựng lại; các property cùng tên chỉ đọc; so sánh bằng và `ToString`.
- `CalendarSettings`, `SeasonDefinition`: constructor và setter public, để code và test dựng settings không cần asset. `SeasonDefinition(name, lengthInDays)` có `Name`, `LengthInDays`.
- `CalendarConfig.CreateSettings()`, `GameClockDriver.Clock`: xem mục Thiết lập.

## Đọc thời gian

```csharp
CalendarDate now = clock.Now;      // TotalMinutes, Day, Hour, Minute, ClockMinute, DayOfWeek, DayInSeason, SeasonIndex, Year
string day = clock.GetDayName(now.DayOfWeek);
string season = clock.GetSeasonName(now.SeasonIndex);
double smooth = clock.ExactMinutes; // gồm phần thời gian đã tích cho step kế tiếp; dùng để nội suy hình ảnh
long next6am = clock.NextOccurrence(6 * 60);   // không bao giờ trả Now: đúng 6:00 thì trả 6:00 ngày mai
```

Cách tính: `Day = TotalMinutes / 1440`, `ClockMinute = (TotalMinutes + DayStartMinute) % 1440`, `DayOfWeek = Day % WeekLength`, `DayInSeason = Day - ngày bắt đầu mùa`. Tuần chạy liên tục qua các mùa.

`Tick(d)` đổi giây thật ra phút game: `d × Speed / SecondsPerGameMinute`. Driver truyền `Time.deltaTime`, nên `Time.timeScale = 0` cũng dừng đồng hồ. Khi một frame cần quá `MaxStepsPerFrame` step, phần dư bị bỏ: máy quá tải thì thời gian game chậm lại, giống `Time.maximumDeltaTime`.

## Event

| Event | Khi nào |
|---|---|
| `Stepped(date)` | Mỗi step. Mỗi step dài đúng `clock.MinutesPerStep` phút; tốc độ và trạng thái fast-forward đọc từ `clock.Speed`, `clock.IsFastForwarding` |
| `HourChanged`, `DayChanged`, `WeekChanged`, `SeasonChanged`, `YearChanged` | Đúng phút xảy ra |
| `Skipped(from, to)` | Sau skip; không có event nào khác cho khoảng đã skip |
| `Restored(date)` | Sau restore |
| `FastForwardCompleted(date)` | Khi fast-forward chạm đích |

- Đồng hồ đi từng step một: cập nhật state tới step đó, phát event của step, chạy lệnh xếp hàng, rồi mới sang step kế. Event mốc luôn phát đúng phút xảy ra, vì step là ước của 60.
- Cùng một phút, thứ tự phát là: Stepped → Hour → Day → Week → Season → Year → (lệnh xếp hàng) → FastForwardCompleted. State đã cập nhật xong trước khi phát.
- Trong handler, `clock.Now` là thời điểm của chính event đó.
- Handler ném exception: lỗi được log, các handler còn lại vẫn chạy.

### Lệnh gọi trong handler

- Lệnh đổi đồng hồ (`SkipTo`, `FastForwardTo`, `StopFastForward`, `SetSpeed`, `Pause`, `Resume`, `Restore`, `AdvanceSeason`) gọi trong handler được **xếp hàng**, và chạy sau các event của step đó, đúng thứ tự gọi. Vì vậy mọi handler cùng thấy một thời điểm. Hệ quả: ví dụ gọi `Pause` xong thì `IsPaused` vẫn là `false` cho tới khi hết event của step. Lệnh đọc dùng được bất cứ lúc nào.
- Lệnh sinh ra trong lúc đang chạy hàng đợi (ví dụ từ handler của `Skipped`, `Restored`) xếp vào cuối, không lồng nhau.
- Nhờ xếp hàng theo thứ tự, `Restore(save); AdvanceSeason();` trong handler áp cờ vào state sau restore, còn `SkipTo(t); AdvanceSeason();` đổi mùa ở day boundary sau `t`.
- Tham số của lệnh xếp hàng được kiểm tra lúc lệnh chạy; lệnh sai chỉ bị log.
- Không gọi `Tick` trong handler (ném `InvalidOperationException`).
- Một lượt chạy tối đa `GameClock.MaxCommandsPerDrain` lệnh xếp hàng; vượt quá (handler gọi lệnh lặp vô tận) thì log lỗi và bỏ phần còn lại.

Sau mỗi step, phần thời gian còn lại của lần `Tick` đó bị bỏ nếu đồng hồ vừa tạm dừng, skip, restore, fast-forward chạm đích (kể cả `FastForwardTo(Now)`), hoặc đổi running speed. `SkipTo(Now)` không tính là skip vì thời gian không trôi; nó chỉ làm bỏ phần dư khi hủy một fast-forward (vì running speed đổi). Lệnh không đổi running speed (`SetSpeed` trong lúc fast-forward, `SetSpeed` bằng tốc độ cũ) không làm bỏ. Nhờ vậy đồng hồ không chạy quá chỗ bị dừng khi một frame bị giật.

## Điều khiển

```csharp
clock.SetSpeed(2f);                                   // base speed, trong [0, clock.MaxSpeed]
clock.Pause(this); clock.Resume(this);                // theo pause source
clock.FastForwardTo(clock.NextOccurrence(360), 50f);  // ngủ có hiệu ứng: mọi event vẫn phát
clock.SkipTo(clock.NextOccurrence(360));              // ngủ màn hình đen: chỉ phát Skipped
clock.AdvanceSeason();                                // đổi mùa ở day boundary kế tiếp
```

| Lệnh | Quy tắc |
|---|---|
| Chung | Mọi mốc thời gian phải là bội của step. Sai thì ném `ArgumentException` (hoặc lớp con) |
| Gọi trong handler | Lệnh đổi đồng hồ được xếp hàng; xem mục Lệnh gọi trong handler |
| `GameClock(settings)` | `settings == null` hoặc settings sai (bảng Settings): ném `ArgumentException` (hoặc lớp con). Settings được copy, sửa sau đó không ảnh hưởng đồng hồ |
| `Tick(d)` | `d` âm, NaN hoặc vô hạn: lỗi. Mỗi lần chạy tối đa `MaxStepsPerFrame` step, phần dư bị bỏ |
| `SetSpeed(s)` | NaN, `s < 0` hoặc `s > MaxSpeed` (kể cả +∞): lỗi (thông báo ghi rõ `MaxSpeed`). Đặt base speed; đổi trong lúc fast-forward thì áp dụng sau khi fast-forward xong |
| `FastForwardTo(t, s)` | NaN, `s <= 0` hoặc `s > MaxSpeed` (kể cả +∞): lỗi. Gọi lại khi đang fast-forward thì thay đích cũ, không phát `Completed` cho đích cũ, kể cả khi gọi trong handler ở step chạm đích cũ. `t == Now`: hoàn thành ngay và phát `Completed`. `t < Now` hoặc `t > GameClock.MaxTotalMinutes`: lỗi. Dừng đúng tại `t` |
| `StopFastForward()` | Về base speed, không phát `Completed` |
| `SkipTo(t)` | Hủy fast-forward đang chạy (không phát `Completed`), kể cả khi `t == Now`. `t == Now`: không phát `Skipped`. `t < Now`, `t > GameClock.MaxTotalMinutes` hoặc xa hơn `MaxSkipDays` ngày: lỗi (thông báo ghi rõ `MaxSkipDays`). Chạy được cả khi đang tạm dừng |
| `Pause(src)`, `Resume(src)` | `src == null`: lỗi. Source so sánh theo reference: hai object khác nhau là hai source, kể cả khi `Equals` bằng nhau. Pause hai lần cùng source tính là một; resume source không có thì bỏ qua. Tạm dừng khi đang fast-forward thì fast-forward dừng theo; resume thì chạy tiếp |
| `NextOccurrence(m)` | Thời điểm **sau** `Now` mà `ClockMinute == m` (đang đúng 6:00 thì trả 6:00 ngày mai). `m` ngoài `[0, 1440)` hoặc không phải bội của step: lỗi |
| `GetDayName(i)`, `GetSeasonName(i)` | `i` ngoài `[0, WeekLength)` hoặc `[0, SeasonCount)`: lỗi |
| `AdvanceSeason()` | Gọi lại trước day boundary kế tiếp thì không thêm gì. Luật đổi mùa: mục Mùa |
| `CaptureSnapshot()` | Trả bản copy; đồng hồ chạy tiếp không đổi snapshot đã lấy |
| `Restore(s)` | `TotalMinutes` âm, quá `GameClock.MaxTotalMinutes` hoặc không phải bội của step, `SeasonIndex` ngoài số mùa, `Year < 0`, hoặc `SeasonStartDay` ngoài `[0, Day]`: lỗi. Hủy fast-forward đang chạy (không phát `Completed`), bỏ thời gian lẻ giữa hai step, giữ nguyên base speed và pause source |
| `CalendarConfig.CreateSettings()` | Trả bản copy mới mỗi lần gọi; sửa bản copy không đổi asset |

Tốc độ vượt `MaxSpeed` bị từ chối chứ không bị kẹp, để lỗi ở nơi gọi lộ ra ngay. UI chọn tốc độ nên giới hạn theo `clock.MaxSpeed`.

**Ai gọi `Pause(this)` thì tự `Resume(this)`, kể cả trong `OnDisable`.** Pause source bị hủy mà chưa resume sẽ giữ đồng hồ dừng mãi.

### Skip hay fast-forward

| | Skip | Fast-forward |
|---|---|---|
| Cách chạy | Nhảy thẳng trong một lệnh | Trôi nhanh qua từng frame, vẫn đi từng step |
| Event | Chỉ `Skipped(from, to)` | Mọi event, kèm tốc độ hiện tại |
| Dùng cho | Ngủ màn hình đen, chuyển cảnh, debug | Ngủ có hiệu ứng, ngồi hết giờ học, chờ tới giờ hẹn |

Trong lúc skip, fixed-length season vẫn tự chuyển; cờ `AdvanceSeason()` đã bật thì áp ở day boundary đầu tiên; command-only season đứng yên. Module nào ở `Skipped` quét `(from, to]` rồi gọi `AdvanceSeason()` thì mùa đổi ở day boundary **sau** `to`. Muốn mọi event đúng từng phút thì dùng fast-forward.

### Step chạm đích fast-forward

1. Phát các event của step. `IsFastForwarding` vẫn là `true`, `Speed` vẫn là tốc độ fast-forward.
2. Chạy các lệnh mà handler xếp hàng; lúc này vẫn đang fast-forward.
3. Nếu không lệnh nào dừng, thay hoặc hủy fast-forward (`StopFastForward`, `FastForwardTo`, `SkipTo`, `Restore`) thì về base speed, phát `FastForwardCompleted`, rồi chạy các lệnh mà handler của `FastForwardCompleted` xếp hàng. Có lệnh như vậy thì không phát cho đích cũ. Lệnh khác (`SetSpeed`, `Pause`) không ngăn `Completed`.

Ví dụ: fast-forward tới 6:00, handler `HourChanged` lúc 6:00 gọi `FastForwardTo(7:00)` → chỉ có `Completed(7:00)`.

## Mùa

Mùa chỉ đổi ở day boundary. Ở mỗi day boundary, mùa đổi khi cờ `AdvanceSeason()` đang bật **hoặc** fixed-length season đã đủ ngày; mỗi day boundary đổi tối đa một mùa. Qua mùa cuối thì sang năm mới và về mùa 0. Gọi `AdvanceSeason()` nhiều lần trong một ngày vẫn chỉ đổi một mùa. `AdvanceSeason()` cũng kết thúc sớm được một fixed-length season; mùa mới đếm ngày từ day boundary đó.

## Save

```csharp
CalendarSnapshot data = clock.CaptureSnapshot();   // bản copy; dùng được với JsonUtility
clock.Restore(data);
```

- Snapshot gồm `TotalMinutes`, `Year`, `SeasonIndex`, `SeasonStartDay`, `SeasonAdvancePending`.
- Không lưu tốc độ, pause source, trạng thái fast-forward. Lỗi và tác dụng của `Restore` lên các phần này: dòng `Restore(s)` trong bảng ở mục Điều khiển. Snapshot bị từ chối không làm đổi gì.
- `Restore` không tự sửa save cho khớp settings hiện tại. Đổi luật chung của lịch (số mùa, `MinutesPerStep`, `DayStartMinute`) sau khi đã có save thì game phải tự chuyển đổi save cũ trước khi gọi `Restore`; nếu không, save cũ có thể bị từ chối hoặc lệch giờ.
- Rút ngắn một mùa sau khi đã có save: save có `DayInSeason` vượt độ dài mới vẫn nạp được, mùa tự chuyển ở day boundary kế tiếp.

## Viết module dùng Calendar

Để chạy thường và skip ra cùng kết quả:

- **Accumulating module** (đói, hiệu ứng có thời hạn, chế tạo, cây lớn): viết logic một lần thành `Advance(deltaMinutes)`, gọi từ `Stepped` (`clock.MinutesPerStep`) và `Skipped` (`to.TotalMinutes - from.TotalMinutes`). Tự xử lý ngưỡng rơi giữa khoảng (hết đói lúc 3h thì bị ốm từ 3h).
- **Milestone module** (lớp học, sự kiện giờ cố định): ở `Skipped`, tự quét khoảng `(from, to]` để không sót mốc.
- **State-based module** (NPC đang ở đâu, cửa hàng mở chưa): tính trạng thái từ thời điểm hiện tại; tính lại ở `Skipped` và `Restored`.

Muốn giảm tải thì tăng `MinutesPerStep` cho cả lịch, rồi nội suy hình ảnh bằng `ExactMinutes`.

## Giới hạn

| Hằng số | Giá trị | Ý nghĩa |
|---|---|---|
| `GameClock.MaxTotalMinutes` | `700_000_000 × 1440` (khoảng 1,9 triệu năm) | `Restore`, `SkipTo`, `FastForwardTo` từ chối giá trị lớn hơn. Đủ nhỏ để `ExactMinutes` vẫn chính xác |
| `GameClock.MaxCommandsPerDrain` | 1000 | Số lệnh xếp hàng tối đa trong một lượt, chống handler gọi lệnh lặp vô tận |
| `CalendarDate.MinutesPerDay` | 1440 | Một ngày luôn là 24 × 60 phút |

Giới hạn skip là setting `MaxSkipDays` (xem Settings).

Package giả định đồng hồ không chạy tới `MaxTotalMinutes` và `Year` không chạm `int.MaxValue`; hai giới hạn này không được kiểm tra khi `Tick` hay khi sang năm.
