# Calendar

Đồng hồ và lịch của game: giờ, ngày, thứ, mùa, năm; tốc độ; tạm dừng; skip; fast-forward; event; snapshot để save.

Tài liệu này mô tả thiết kế hiện hành của module. Thuật ngữ: `GLOSSARY.md` (mục Calendar). Cách dùng nhanh: `README.md` của package. Lịch sử version: `CHANGELOG.md` của package. Trạng thái: `roadmap.md`.

| Mục | Giá trị |
|---|---|
| Package | `unity/Packages/com.guenbankhai.calendar` |
| Namespace / asmdef | `GuenBanKhai.Calendar`, `GuenBanKhai.Calendar.Tests` |
| Phụ thuộc | Không có |
| Quyết định | [ADR 0001](../adr/0001-calendar-step-based-clock.md) · [0002](../adr/0002-calendar-skip-and-fast-forward.md) · [0003](../adr/0003-calendar-plain-csharp-core.md) · [0004](../adr/0004-calendar-command-queue.md) |

Không dùng tên `Time`: nếu tồn tại namespace `GuenBanKhai.Time`, mọi code trong `GuenBanKhai.*` viết `Time.deltaTime` sẽ trỏ vào namespace đó thay vì `UnityEngine.Time` và lỗi compile.

## Phạm vi

**Trong module:** giờ, ngày, thứ, mùa, năm; tốc độ; tạm dừng; skip; fast-forward; event; snapshot để save.

**Ngoài module** (luật game hoặc module khác):
- Sự kiện nào làm chuyển mùa: game gọi `AdvanceSeason()`.
- Luật ép kết thúc ngày kiểu Stardew (2:00 ngất → 6:00): game tự nghe event giờ và gọi `SkipTo`.
- "Ngày của người chơi" (thức qua đêm vẫn tính hôm qua).
- Play time (giây thật đã chơi): Save hoặc meta.
- Ánh sáng ngày đêm, định dạng file save, nâng phiên bản save.
- Cách các scene khác nhau (trường, thị trấn) cùng dùng một đồng hồ: quyết khi làm scenario đầu tiên.

## Kiến trúc

| Thành phần | Vai trò |
|---|---|
| `GameClock` | Lõi C# thuần. Giữ state, tốc độ, pause source, thời gian lẻ giữa hai step, trạng thái fast-forward; phát event. Test bằng EditMode |
| `CalendarSettings` | Class C# thuần chứa toàn bộ config. `GameClock(CalendarSettings)` kiểm tra và copy một lần; sửa settings sau đó không ảnh hưởng đồng hồ đang chạy |
| `CalendarConfig` | ScriptableObject chỉ giữ một `CalendarSettings` để chỉnh trong Inspector. `CreateSettings()` trả bản copy, nên code không bao giờ sửa được asset. Không giữ state (domain reload đang tắt) |
| `GameClockDriver` | MonoBehaviour mỏng: tạo `GameClock` trong `Awake`, `Update` gọi `Clock.Tick(Time.deltaTime)`. Chạy trước component khác (`DefaultExecutionOrder(-1000)`) |
| `CalendarDate` | Giá trị chỉ đọc do `GameClock` tạo, truyền qua event |
| `CalendarSnapshot` | Snapshot; `GameClock` giữ state sống trong đúng một snapshot |

- Lấy tham chiếu: `[SerializeField] GameClockDriver` → `.Clock` từ `Awake`/`OnEnable` trở đi, hoặc truyền `GameClock` qua constructor. Không singleton, không service locator.
- Config thiếu hoặc sai: driver log lỗi, tự tắt, `Clock` là `null`.
- Có thể thay driver (debug, mạng) mà không sửa lõi.
- UI lấy tên thứ và mùa qua `GameClock` (`GetDayName`, `GetSeasonName`), không đọc thẳng config.

## Mô hình thời gian

State (nguồn sự thật duy nhất):

```text
long TotalMinutes        số phút kể từ lúc bắt đầu Day 0; luôn là bội của N; chỉ giảm khi restore
int  Year
int  SeasonIndex
long SeasonStartDay      ngày tuyệt đối mùa hiện tại bắt đầu
bool SeasonAdvancePending
```

Tính ra:

```text
Day         = TotalMinutes / 1440                          luôn ≥ 0
ClockMinute = (TotalMinutes + DayStartMinute) % 1440       clock time
DayOfWeek   = Day % WeekLength
DayInSeason = Day - SeasonStartDay
```

`TotalMinutes = 0` là lúc bắt đầu Day 0, đồng hồ chỉ `DayStartMinute`.

Quy tắc:
- Một ngày luôn là 24h × 60 phút.
- **Step:** N = `MinutesPerStep`, chia hết cho 60 (mặc định 10, kiểu Stardew). Mọi mốc thời gian (`DayStartMinute`, giờ bắt đầu game mới, đích skip và fast-forward, `NextOccurrence`, snapshot khi restore) phải là bội của N.
- **Sang giờ** khi `ClockMinute % 60 == 0`. **Sang ngày** ở day boundary, tức khi `TotalMinutes % 1440 == 0` (game này: 00:00; kiểu Stardew: 06:00). Ngày trôi liền mạch, không bắt buộc ngủ.
- **Tuần** chạy liên tục qua các mùa, không reset.
- **Mùa:** số mùa trong năm = số phần tử `Seasons[]`; `LengthInDays > 0` là fixed-length season, `0` là command-only season.

### Chuyển mùa

Chỉ xảy ra ở day boundary:
- `AdvanceSeason()` luôn gọi được, chỉ bật cờ `SeasonAdvancePending`. Gọi nhiều lần trong một ngày vẫn chỉ chuyển một mùa.
- Ở day boundary, mùa đổi nếu cờ đang bật **hoặc** fixed-length season đã có `DayInSeason >= LengthInDays`. Mỗi day boundary đổi tối đa một mùa. Fixed-length season vẫn có thể bị chuyển sớm bằng lệnh.
- Khi đổi: `SeasonStartDay = Day` của ngày mới, xóa cờ; qua mùa cuối thì `Year + 1` và về mùa 0.
- Toàn bộ logic nằm trong **một** hàm nội bộ `ProcessDayBoundary()`, dùng chung cho chạy thường và skip.

## Tốc độ và tạm dừng

- Lõi nhận giây thật qua `Tick(realDeltaSeconds)`, nhân với running speed và chia cho `SecondsPerGameMinute`.
- Driver dùng `Time.deltaTime`, nên `timeScale = 0` cũng dừng đồng hồ.
- Mặc định `SecondsPerGameMinute = 1200 / 1440` (≈ 0.8333): một ngày dài 20 phút thật, mỗi step 10 phút là 8.33 giây.
- `ExactMinutes` (`double`) = `TotalMinutes` + thời gian đã tích lũy cho step kế tiếp. Dùng để nội suy hình ảnh (mặt trời, kim đồng hồ) giữa hai step.
- Mỗi `Tick` chạy tối đa `MaxStepsPerFrame` step; phần dư bị bỏ. Khi quá tải thì thời gian game chậm lại, giống `Time.maximumDeltaTime`.
- `Speed` là running speed (kể cả trong các event của step chạm đích fast-forward). `SetSpeed` đặt base speed.
- `MaxSpeed` là giới hạn trên của mọi tốc độ. Tốc độ vượt giới hạn bị từ chối chứ không bị kẹp, để lỗi ở nơi gọi (chia cho 0, dữ liệu sai) lộ ra ngay. UI chọn tốc độ nên giới hạn theo `MaxSpeed`.
- Tạm dừng theo tập pause source: `Pause(source)` / `Resume(source)`; đồng hồ chạy khi tập rỗng.

## Step và event

- Lõi luôn **đi từng step một**: cập nhật state tới step đó, phát các event của step đó, chạy hàng đợi lệnh, rồi mới sang step kế tiếp. Trong mọi handler, `Now` là thời điểm của chính event đó.
- Event mốc luôn phát **đúng phút** xảy ra (N chia hết cho 60 nên mốc luôn rơi vào một step).
- `Stepped` phát mỗi step; mỗi step dài đúng `MinutesPerStep` (N), khoảng của skip đi qua `Skipped`. Tốc độ và trạng thái fast-forward của step đọc từ `Speed` và `IsFastForwarding` trong handler.
- Cùng một phút: Stepped → Hour → Day → Week → Season → Year → (hàng đợi lệnh) → FastForwardCompleted. Trước khi phát, toàn bộ state đã cập nhật xong.
- Handler ném exception: lõi bắt theo từng handler, gọi `Debug.LogException`, rồi vẫn gọi các handler còn lại.

```text
Stepped, HourChanged, DayChanged, WeekChanged,
SeasonChanged, YearChanged(CalendarDate)
Skipped(CalendarDate from, CalendarDate to)
Restored(CalendarDate)
FastForwardCompleted(CalendarDate)
```

### Hàng đợi lệnh

- Lệnh đổi đồng hồ (`SkipTo`, `FastForwardTo`, `StopFastForward`, `SetSpeed`, `Pause`, `Resume`, `Restore`, `AdvanceSeason`) gọi trong handler sẽ vào hàng đợi FIFO. Lệnh đọc luôn dùng được.
- `AdvanceSeason()` cũng xếp hàng, để thứ tự với lệnh khác đúng như lúc gọi: handler gọi `Restore(save); AdvanceSeason();` thì cờ áp vào state sau restore (không bị snapshot xóa); gọi `SkipTo(t); AdvanceSeason();` thì mùa đổi ở day boundary sau `t`. Trường hợp thường không khác gì: cờ vẫn áp ở day boundary kế tiếp.
- Hàng đợi chạy ngay sau khi phát xong các event của step hiện tại. Lệnh sinh ra trong lúc chạy hàng đợi (từ handler của `Skipped`, `Restored`…) xếp vào cuối, không lồng nhau.
- Tham số của lệnh trong hàng đợi được kiểm tra lúc lệnh chạy; lệnh sai thì `Debug.LogException` và bỏ qua.
- Một lượt chạy tối đa `MaxCommandsPerDrain` (1000) lệnh; vượt quá (handler gọi lệnh lặp vô tận) thì log lỗi và bỏ phần còn lại.
- Gọi `Tick` trong handler: ném `InvalidOperationException`.

### Bỏ phần thời gian còn dư

Sau mỗi step (gồm event và hàng đợi), phần thời gian còn lại của lần `Tick` đó bị bỏ nếu:
- đồng hồ đang tạm dừng;
- vừa skip hoặc vừa restore (`SkipTo(Now)` không tính là skip vì thời gian không trôi; nó chỉ làm bỏ phần dư nếu hủy một fast-forward, tức là làm đổi running speed);
- fast-forward vừa tới đích (kể cả `FastForwardTo(Now)`);
- running speed đã đổi.

Lệnh không làm đổi running speed (ví dụ `SetSpeed` trong lúc fast-forward, hoặc `SetSpeed` bằng đúng tốc độ cũ) không làm bỏ.

### Step chạm đích fast-forward

Theo thứ tự:
1. Phát các event của step (Stepped → … → Year). `IsFastForwarding` vẫn là `true` và `Speed` vẫn là tốc độ fast-forward.
2. Chạy các lệnh mà handler của step đó xếp hàng. Lúc này vẫn đang fast-forward.
3. Nếu không lệnh nào dừng, thay hoặc hủy fast-forward (`StopFastForward`, `FastForwardTo`, `SkipTo`, `Restore`): tắt fast-forward, phát `FastForwardCompleted`, rồi chạy các lệnh mà handler của `FastForwardCompleted` xếp hàng. Nếu có lệnh như vậy: không phát `Completed` cho đích cũ.

Ví dụ: fast-forward lúc ngủ tới 6:00, handler `HourChanged` lúc 6:00 gọi `FastForwardTo(7:00)` thì chỉ có `Completed(7:00)`, không có `Completed(6:00)`. Lệnh khác (`SetSpeed`, `Pause`) không ngăn `Completed`.

## Skip và fast-forward

| | **Skip** (`SkipTo`) | **Fast-forward** (`FastForwardTo`) |
|---|---|---|
| Cách chạy | Nhảy thẳng trong một lệnh | Thời gian trôi nhanh qua từng frame, vẫn đi từng step |
| Event | Chỉ một `Skipped(from, to)` | Mọi event bình thường, kèm tốc độ hiện tại |
| Lịch | Lặp qua từng day boundary, gọi `ProcessDayBoundary()` mà không phát event | Như chạy thường |
| Dùng cho | Ngủ có màn hình đen, chuyển cảnh, debug | Ngủ có hiệu ứng, ngồi hết giờ học, chờ tới giờ hẹn |

- Skip (thời gian có trôi trong thế giới) khác restore (không trôi): `Skipped` và `Restored` là hai event khác nhau.
- **Giới hạn của skip:** trong khoảng nhảy, fixed-length season tự chuyển bao nhiêu lần cũng được; cờ chờ đã bật thì áp ở day boundary đầu tiên; command-only season thì đứng yên. Module nào quét `(from, to]` rồi gọi `AdvanceSeason()` thì mùa đổi ở day boundary **sau** `to`. Muốn đúng tuyệt đối thì dùng fast-forward.
- `SkipTo` trong lúc fast-forward: hủy fast-forward (không phát `Completed`) rồi nhảy. `SkipTo(Now)` cũng hủy fast-forward, nhưng không phát `Skipped` vì thời gian không trôi.
- Tạm dừng khi đang fast-forward: fast-forward dừng theo, resume thì chạy tiếp.
- `SkipTo` vẫn chạy khi đang tạm dừng.

## API và trường hợp biên

Khối API, bảng quy tắc từng lệnh và bảng Settings nằm ở `README.md` của package (mục API, Điều khiển, Settings). Từ khi có package, README là nơi duy nhất giữ chúng.

Quyết định thiết kế đi kèm:
- `MaxTotalMinutes` là bội của 1440, nên nằm trên mọi step hợp lệ. Nó đủ nhỏ để `ExactMinutes` (`double`) vẫn chính xác dưới một phần nghìn phút. Chỉ dùng để kiểm tra đầu vào (`Restore`, đích của `SkipTo` và `FastForwardTo`).
- **Giả định quy mô:** đồng hồ không chạy tới `MaxTotalMinutes` và `Year` không chạm `int.MaxValue`. Lõi không kiểm tra hai giới hạn này khi `Tick` hay khi sang năm, theo luật không thêm code cho trường hợp chỉ xảy ra khi tràn kiểu dữ liệu.
- `MaxSkipDays` là setting: skip xa hơn coi là bug của nơi gọi, chặn lại để vòng lặp từng ngày của skip không treo game.
- Settings chỉ là cấu hình ban đầu. Thay đổi lúc đang chạy (kể cả menu cheat) dùng lệnh của `GameClock`. Cần đổi thứ chưa có lệnh (ví dụ `MaxSpeed`, độ dài ngày) thì thêm lệnh riêng khi có nhu cầu. Cấu trúc lịch (step, giờ bắt đầu ngày, số mùa, tên thứ) không đổi lúc đang chạy.

## Save

- `CaptureSnapshot()` trả bản copy của state; `Restore(snapshot)` nạp lại. `CalendarSnapshot` là struct `[Serializable]`, dùng được với `JsonUtility`.
- Không lưu: pause source, tốc độ, thời gian lẻ giữa hai step, trạng thái fast-forward.
- `Restore` không phát lại event, chỉ phát `Restored`; hủy fast-forward đang chạy (không phát `Completed`), bỏ thời gian lẻ; giữ nguyên pause source và base speed.
- `Restore` báo lỗi nếu: `TotalMinutes` ngoài `[0, MaxTotalMinutes]` hoặc không phải bội của N, `SeasonIndex` ngoài số mùa hiện có, `Year < 0`, hoặc `SeasonStartDay` ngoài `[0, Day]`. Snapshot bị từ chối không làm đổi gì.
- Đổi `DayStartMinute` sau khi đã có save thì `Restore` không phát hiện được: clock time của save cũ bị lệch.
- `DayInSeason` vượt độ dài mới của mùa: không xử lý riêng, mùa tự chuyển ở day boundary kế tiếp.

## Quy ước cho module dùng Calendar

Để chạy thường và skip luôn ra cùng một kết quả:

- **Accumulating module** (Needs, Effects của Stats, Crafting, cây lớn): viết logic mô phỏng một lần thành `Advance(deltaMinutes)`, gọi từ cả `Stepped` lẫn `Skipped`. Tự xử lý đúng các ngưỡng xảy ra giữa khoảng thời gian (hết đói lúc 3h thì bị ốm từ 3h). Module đọc tham số mà Effects đổi (Needs) không được gọi riêng lẻ: scenario gọi chung qua một hàm `Simulate` chia đoạn theo mục "Quy ước cho module dùng Stats" trong `stats.md`.
- **Milestone module** (Opportunities, lớp học): ở `Skipped`, tự quét khoảng `(from, to]` để không sót mốc.
- **State-based module** (NPC Routines, cửa hàng mở hay đóng): trạng thái là hàm của thời gian, `f(lịch, thời điểm)`. Tính lại ở `Skipped` và `Restored`.

Quy ước chung:
- Ai gọi `Pause(this)` thì tự `Resume(this)`, kể cả trong `OnDisable`. Pause source bị hủy mà chưa resume sẽ giữ đồng hồ dừng mãi.
- Muốn giảm chi phí thì tăng `MinutesPerStep` (step thô hơn cho cả lịch); hình ảnh nội suy bằng `ExactMinutes`.

## Kiểm thử

Test EditMode trong `Tests/` của package, mỗi file một nhóm quy tắc ở trên:

| File | Phủ |
|---|---|
| `GameClockSettingsTests` | Settings hợp lệ và sai, giá trị bắt đầu, tên thứ và mùa, settings được copy |
| `CalendarConfigTests` | Giá trị mặc định của asset, `CreateSettings()` trả bản copy |
| `GameClockTimeTests` | Step, `ExactMinutes`, `DayStartMinute`, thứ tự event, tốc độ, tạm dừng, `NextOccurrence`, `MaxStepsPerFrame` |
| `GameClockSeasonTests` | Fixed-length season, command-only season, chuyển sớm, sang năm, tuần qua các mùa |
| `GameClockSkipFastForwardTests` | Skip, fast-forward, tương tác giữa hai cơ chế, `SkipTo(Now)`, `MaxSkipDays`, đích quá `MaxTotalMinutes`, lệnh trong handler của `FastForwardCompleted`; **fast-forward và skip cùng một khoảng ra cùng kết quả** |
| `GameClockCommandQueueTests` | Hàng đợi lệnh (kể cả `AdvanceSeason`), bỏ phần dư, exception trong handler, lệnh sai trong hàng đợi |
| `GameClockSnapshotTests` | Lưu và restore, đi qua `JsonUtility`, dữ liệu sai (không đổi gì kể cả fast-forward và thời gian lẻ), giữ pause source và base speed |

Thử bằng tay: experiment `Assets/Playground/Calendar/` (overlay IMGUI có nút cho mọi lệnh), scene `Assets/Scenes/Calendar.unity`.
