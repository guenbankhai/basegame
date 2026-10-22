# Calendar (Time & Calendar)

Thiết kế đã thống nhất cho module Time & Calendar trong `roadmap.md`. Trạng thái: **Chưa bắt đầu**, bước tiếp theo là Playground.

## Tên

| Mục | Giá trị |
|---|---|
| Package | `com.guenbankhai.basegame.calendar` |
| Namespace / asmdef | `BaseGame.Calendar` |
| Experiment | `Assets/Playground/Calendar/`, `BaseGame.Playground.Calendar` (+ `.Tests`) |

Không dùng tên `Time`: nếu tồn tại namespace `BaseGame.Time`, mọi code trong `BaseGame.*` viết `Time.deltaTime` sẽ trỏ vào namespace đó thay vì `UnityEngine.Time` và lỗi compile.

## Phạm vi

**Trong module:** giờ, ngày, thứ, mùa, năm; tốc độ; tạm dừng; skip; tua nhanh; event; snapshot để save.

**Ngoài module** (luật game hoặc module khác):
- Sự kiện nào làm chuyển mùa (game gọi `AdvanceSeason()`).
- Luật ép kết thúc ngày kiểu Stardew (2:00 ngất → 6:00). Game tự nghe event giờ và gọi `SkipTo`.
- "Ngày của người chơi" (thức qua đêm vẫn tính hôm qua).
- Play time (giây thật đã chơi): Save hoặc meta.
- Ánh sáng ngày đêm, định dạng file save, nâng phiên bản dữ liệu save.

## Kiến trúc

- `GameClock`: lõi C# thuần. Giữ toàn bộ state, tốc độ, tạm dừng, phần lẻ dưới một phút, trạng thái tua; phát event. Test bằng EditMode.
- `GameClockDriver`: MonoBehaviour mỏng. Tạo `GameClock` trong `Awake`, `Update` gọi `Clock.Tick(Time.deltaTime)`. Có thể thay bằng driver khác (debug, mạng) mà không sửa lõi.
- `CalendarConfig`: ScriptableObject, **chỉ giữ config**, không giữ state (domain reload đang tắt).
- Lấy tham chiếu: `[SerializeField] GameClockDriver` → `.Clock`, hoặc truyền `GameClock` qua constructor. Không singleton, không service locator.

## Mô hình thời gian

State (nguồn sự thật duy nhất):

```text
long TotalMinutes        số phút kể từ lúc bắt đầu Ngày 0; không bao giờ giảm
int  Year
int  SeasonIndex
long SeasonStartDay      ngày tuyệt đối mùa hiện tại bắt đầu
bool SeasonAdvancePending
```

Tính ra:

```text
Day         = TotalMinutes / 1440                          luôn ≥ 0
ClockMinute = (TotalMinutes + DayStartMinute) % 1440       giờ hiển thị
DayOfWeek   = Day % WeekLength
DayInSeason = Day - SeasonStartDay
```

`TotalMinutes = 0` là lúc bắt đầu Ngày 0, đồng hồ chỉ `DayStartMinute`.

Quy tắc:
- Một ngày luôn là 24h × 60 phút. Phút là đơn vị nhỏ nhất của lõi.
- **Sang giờ** khi `ClockMinute % 60 == 0`. **Sang ngày** khi `TotalMinutes % 1440 == 0`, tức đồng hồ chỉ `DayStartMinute` (game này: 00:00; kiểu Stardew: 06:00). Ngày trôi liền mạch, không bắt buộc ngủ.
- **Tuần** chạy liên tục qua các mùa, không reset.
- **Mùa:** số mùa trong năm = số phần tử `Seasons[]`. Mỗi mùa có `LengthInDays`: `> 0` thì tự chuyển khi đủ ngày; `0` thì chỉ chuyển bằng `AdvanceSeason()`.

**Chuyển mùa** (chỉ xảy ra ở mốc sang ngày):
- `AdvanceSeason()` luôn gọi được, chỉ bật cờ `SeasonAdvancePending`. Gọi nhiều lần trong một ngày vẫn chỉ chuyển một mùa.
- Ở mốc sang ngày, mùa đổi nếu cờ đang bật **hoặc** mùa có độ dài cố định đã có `DayInSeason >= LengthInDays`. Mỗi mốc đổi tối đa một mùa. Mùa có độ dài cố định vẫn có thể bị chuyển sớm bằng lệnh.
- Khi đổi: `SeasonStartDay = Day` của ngày mới, xóa cờ, qua mùa cuối thì `Year + 1` và về mùa 0.
- Toàn bộ logic này nằm trong **một** hàm nội bộ `ProcessDayBoundary()`, dùng chung cho tick và skip.

## Tốc độ và tạm dừng

- Lõi nhận giây thật qua `Tick(realDeltaSeconds)`, nhân với tốc độ riêng và `SecondsPerGameMinute`.
- Driver dùng `Time.deltaTime`, nên `timeScale = 0` cũng dừng đồng hồ.
- Tạm dừng riêng theo tập lý do: `Pause(source)` / `Resume(source)`. Đồng hồ chạy khi tập rỗng.
- Mỗi frame trôi tối đa `MaxMinutesPerFrame` phút; phần dư bị bỏ. Khi quá tải thì thời gian game chậm lại, giống `Time.maximumDeltaTime`.

## Bước thời gian và event

- Lõi luôn **đi từng bước một phút**: cập nhật state tới phút đó, phát các event của phút đó, rồi mới sang phút kế tiếp. Trong mọi handler, `Now` là thời điểm của chính event đó.
- Event mốc (`HourChanged`, `DayChanged`, `WeekChanged`, `SeasonChanged`, `YearChanged`) luôn phát **đúng phút** xảy ra.
- `Ticked` là **nhịp cập nhật gameplay**, phát mỗi `TickMinutes` phút, thẳng hàng với mốc đầu ngày. `TickMinutes` phải chia hết cho 60. Độ chính xác của lịch không phụ thuộc vào `TickMinutes`.
- `TickInfo.DeltaMinutes` = số phút kể từ lần cập nhật gần nhất (`Ticked`, `Skipped` hoặc `Restored`). Ví dụ: N = 10, skip tới 6:05 thì tick lúc 6:10 có delta 5.
- Cùng một phút: phát theo thứ tự Tick → Hour → Day → Week → Season → Year. Trước khi phát, toàn bộ state đã cập nhật xong.

```text
Ticked(TickInfo)                         Date, DeltaMinutes, Speed, IsFastForwarding
HourChanged, DayChanged, WeekChanged,
SeasonChanged, YearChanged(CalendarDate)
Skipped(CalendarDate from, CalendarDate to)
Restored(CalendarDate)
FastForwardCompleted(CalendarDate)
```

**Hàng đợi lệnh và exception:**
- Lệnh làm thay đổi đồng hồ (`SkipTo`, `FastForward*`, `StopFastForward`, `SetSpeed`, `Pause`, `Resume`, `Restore`) được gọi trong handler sẽ vào hàng đợi FIFO. `AdvanceSeason()` chỉ bật cờ nên chạy ngay. Lệnh đọc luôn dùng được.
- Hàng đợi chạy ngay sau khi phát xong các event của phút hiện tại. Sau đó, nếu đồng hồ đang tạm dừng, vừa skip hoặc vừa restore, thì bỏ phần phút còn lại của lần `Tick` này.
- Tham số của lệnh trong hàng đợi được kiểm tra lúc lệnh chạy.
- Handler ném exception: lõi bắt lỗi theo từng handler, gọi `Debug.LogException`, rồi vẫn gọi các handler còn lại.

## Skip và tua nhanh: hai cơ chế riêng

| | **Skip** (`SkipTo`) | **Tua nhanh** (`FastForward`, `FastForwardTo`) |
|---|---|---|
| Cách chạy | Nhảy thẳng trong một lệnh | Thời gian trôi nhanh qua từng frame, vẫn đi từng phút |
| Event | Chỉ một `Skipped(from, to)` | Mọi event bình thường, kèm tốc độ hiện tại |
| Lịch | Lặp qua từng mốc sang ngày, gọi `ProcessDayBoundary()` mà không phát event | Như chạy thường |
| Dùng cho | Ngủ có màn hình đen, chuyển cảnh, debug | Ngủ có hiệu ứng, ngồi hết giờ học, chờ tới giờ hẹn |

- Phân biệt **skip** (thời gian có trôi trong thế giới) và **load** (không trôi): `Skipped` và `Restored` là hai event khác nhau.
- **Giới hạn của skip:** trong khoảng nhảy, mùa có độ dài cố định tự chuyển bao nhiêu lần cũng được; cờ chờ đã bật thì áp ở mốc sang ngày đầu tiên; mùa chuyển bằng lệnh thì đứng yên. Module nào quét `(from, to]` rồi gọi `AdvanceSeason()` thì mùa đổi ở mốc sang ngày **sau** `to`. Muốn đúng tuyệt đối thì dùng tua nhanh.

Tương tác:
- `SkipTo` trong lúc tua: hủy tua (không phát `Completed`) rồi nhảy.
- Tạm dừng khi đang tua: tua dừng theo, resume thì chạy tiếp.
- `SkipTo` vẫn chạy khi đang tạm dừng.

## API và trường hợp biên

```csharp
CalendarDate Now { get; }  float Speed { get; }  bool IsPaused { get; }  bool IsFastForwarding { get; }

void Tick(float realDeltaSeconds);
void SetSpeed(float speed);
void Pause(object source);  void Resume(object source);
void FastForward(float speed);  void FastForwardTo(long targetMinutes, float speed);  void StopFastForward();
void SkipTo(long targetMinutes);
void AdvanceSeason();
long NextOccurrence(int clockMinute);
CalendarSnapshot CaptureSnapshot();  void Restore(CalendarSnapshot snapshot);
```

| Lệnh | Quy tắc |
|---|---|
| `SetSpeed(s)` | `s < 0` báo lỗi; kẹp trong `[0, MaxSpeed]`. Là tốc độ cơ bản; đổi trong lúc tua thì áp dụng sau khi tua xong |
| `FastForward(s)`, `FastForwardTo(t, s)` | `s` kẹp ở `MaxSpeed`. Gọi lại khi đang tua thì thay mục tiêu cũ, không phát `Completed` cho mục tiêu cũ. `t == Now` thì hoàn thành ngay và phát `Completed`; `t < Now` báo lỗi. Dừng đúng tại `t` |
| `StopFastForward()` | Quay về tốc độ cơ bản, không phát `Completed` |
| `SkipTo(t)` | `t == Now` thì không làm gì, không phát event; `t < Now` báo lỗi |
| `NextOccurrence(m)` | Thời điểm **sau** `Now` mà `ClockMinute == m` (đang đúng 6:00 thì trả 6:00 ngày mai); `m` ngoài `[0, 1440)` báo lỗi |
| `Pause(src)`, `Resume(src)` | `src == null` báo lỗi; pause hai lần cùng nguồn vẫn tính là một; resume nguồn không có thì không làm gì |

## Save

- `CaptureSnapshot()` / `Restore(snapshot)`. `CalendarSnapshot` là struct serializable chứa đúng 5 trường state ở trên.
- Không lưu: lý do tạm dừng, tốc độ, phần lẻ dưới một phút, trạng thái tua.
- `Restore` không phát lại event, chỉ phát `Restored`.
- `Restore` báo lỗi nếu: `TotalMinutes < 0`, `SeasonIndex` ngoài số mùa hiện có, `Year < 0`, hoặc `SeasonStartDay > Day`.
- `DayInSeason` vượt độ dài mới của mùa: không xử lý riêng, mùa tự chuyển ở mốc sang ngày kế tiếp.
- Đổi `DayStartMinute` sau khi đã có save thì giờ hiển thị của save cũ bị lệch.

## Quy ước cho module dùng Calendar

Các module sau phải tuân theo quy ước này để chạy thường và skip luôn ra cùng một kết quả.

- **Module tích lũy** (Needs, Status Effects, Crafting, cây lớn): logic mô phỏng viết một lần thành `Advance(deltaMinutes)`, gọi từ cả `Ticked` lẫn `Skipped`. Tự xử lý đúng các ngưỡng xảy ra giữa khoảng thời gian (ví dụ hết đói lúc 3h thì bị ốm từ 3h).
- **Module theo mốc** (Opportunities, lớp học): ở `Skipped`, tự quét khoảng `(from, to]` để không sót mốc.
- **Module theo trạng thái** (NPC Routines, cửa hàng mở hay đóng): trạng thái là hàm của thời gian, `f(lịch, thời điểm)`. Tính lại ở `Skipped` và `Restored`; khi tốc độ cao thì không đi bộ bằng NavMesh mà đặt vị trí theo thời gian. Chi tiết quyết khi làm Schedule và NPC Routines.
- Muốn giảm chi phí thì giảm tần suất cập nhật gameplay (`TickMinutes`), không giảm độ chính xác của lịch.

## Câu hỏi mở

- **Giá trị mặc định của `TickMinutes`** (1 hay ví dụ 10). Chốt sau Playground. Mỗi phút: mượt, nhưng 1440 lần `Ticked` mỗi ngày. Mỗi N phút: rời rạc, dễ đoán, rẻ; hình ảnh cần nội suy riêng.
- Tham chiếu tới đồng hồ qua nhiều scene (trường, thị trấn): quyết ở Integration scenario.

## Kế hoạch Playground

```text
Assets/Playground/Calendar/
├── BaseGame.Playground.Calendar.asmdef
├── Core/
│   ├── CalendarConfig.cs        ScriptableObject
│   ├── SeasonDefinition.cs      [Serializable] struct: Name, LengthInDays
│   ├── CalendarSnapshot.cs      [Serializable] struct
│   ├── CalendarDate.cs          readonly struct: giá trị tính ra
│   ├── TickInfo.cs              readonly struct
│   └── GameClock.cs
├── GameClockDriver.cs
├── Debug/
│   ├── CalendarDebugOverlay.cs  IMGUI
│   ├── FakeNeed.cs              C# thuần: Advance(deltaMinutes), có ngưỡng
│   └── FakeNeedListener.cs      nối FakeNeed với Ticked + Skipped
└── Tests/
    └── BaseGame.Playground.Calendar.Tests.asmdef   Editor only
```

`CalendarConfig`: `SecondsPerGameMinute`, `DayStartMinute`, `DayNames[]` (độ dài tuần), `Seasons[]`, mốc bắt đầu game mới (năm, mùa, phút trong Ngày 0), `TickMinutes` (chia hết cho 60), `MaxSpeed`, `MaxMinutesPerFrame`.

Debug overlay:
- Hiển thị: giờ, thứ, ngày trong mùa, mùa, năm; tốc độ và trạng thái; giá trị FakeNeed.
- Log 20 event gần nhất; số event mỗi giây thật (để chọn `TickMinutes`).
- Nút: tạm dừng/tiếp tục, tốc độ, tua tới 6:00, skip tới 6:00, skip 3 ngày, `AdvanceSeason`, lưu/nạp snapshot.

Test EditMode cần phủ:
- **Tua và skip cùng một khoảng ra cùng snapshot và cùng giá trị FakeNeed** (test quan trọng nhất).
- `Day`, `ClockMinute`, mốc sang giờ và sang ngày với `DayStartMinute` khác 0.
- Chuyển mùa theo độ dài và theo lệnh; gọi `AdvanceSeason()` nhiều lần; chuyển sớm; sang năm.
- Skip qua nhiều mùa và nhiều năm; mùa chuyển bằng lệnh đứng yên khi skip.
- Một `Tick` nhiều phút: `Now` đúng tại từng event; thứ tự event.
- `DeltaMinutes` sau skip và restore.
- Hàng đợi lệnh trong handler; bỏ phần phút còn lại sau skip hoặc pause; exception trong handler.
- Tua dừng đúng mốc; `MaxSpeed`; `MaxMinutesPerFrame`.
- Các trường hợp biên trong bảng API.
- Snapshot: lưu rồi nạp ra cùng state; `Restore` báo lỗi với dữ liệu sai.

Setup Editor: tạo asset `CalendarConfig` và scene `Calendar.unity`; một GameObject gắn `GameClockDriver`, `CalendarDebugOverlay`, `FakeNeedListener`.

## Sang package

Sau khi chạy thử và chốt `TickMinutes`:
1. Tạo `com.guenbankhai.basegame.calendar`: `Runtime/` (lõi, config, driver) và `Tests/`. Không đưa phần Debug vào.
2. Viết lại sạch, không copy nguyên bản thử; chuyển test sang.
3. Viết usage doc ngắn; cập nhật trạng thái trong `roadmap.md` và `README.md`.
