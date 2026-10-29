# Calendar

Đồng hồ và lịch của game: giờ, ngày, thứ, mùa, năm; tốc độ; tạm dừng; skip; fast-forward; event; snapshot để save.

Tài liệu này giữ phạm vi, kiến trúc, mô hình nội bộ và lý do thiết kế. Cách dùng và mọi luật hành vi (API, Settings, event, hàng đợi lệnh, skip, fast-forward, mùa, save, quy ước cho module dùng Calendar) nằm duy nhất ở `README.md` của package. Thuật ngữ: `GLOSSARY.md` (mục Calendar). Lịch sử version: `CHANGELOG.md` của package. Trạng thái: `roadmap.md`.

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
| `GameClock` | Lõi C# thuần: giữ state, tốc độ, pause source, thời gian lẻ giữa hai step, trạng thái fast-forward; phát event. Test bằng EditMode |
| `CalendarSettings` | Class C# thuần chứa toàn bộ config; `GameClock` kiểm tra và copy một lần, sửa settings sau đó không ảnh hưởng đồng hồ đang chạy |
| `CalendarConfig` | ScriptableObject chỉ bọc một `CalendarSettings`; `CreateSettings()` trả bản copy nên code không sửa được asset. Không giữ state (domain reload đang tắt) |
| `GameClockDriver` | MonoBehaviour mỏng: tạo `GameClock` trong `Awake`, `Update` gọi `Tick(Time.deltaTime)`; chạy trước component khác |
| `CalendarDate` | Giá trị chỉ đọc do `GameClock` tạo, nên các giá trị tính ra luôn khớp nhau |
| `CalendarSnapshot` | `GameClock` giữ state sống trong đúng một snapshot, nên save luôn có đủ mọi field state |

- Không singleton, không service locator: nơi dùng lấy `GameClock` qua driver hoặc constructor.
- Có thể thay driver (debug, mạng) mà không sửa lõi.
- UI lấy tên thứ và mùa qua `GameClock`, không đọc thẳng config.

## Mô hình nội bộ

State (nguồn sự thật duy nhất, cũng là snapshot):

```text
long TotalMinutes        số phút kể từ lúc bắt đầu Day 0; luôn là bội của step; chỉ giảm khi restore
int  Year
int  SeasonIndex
long SeasonStartDay      ngày tuyệt đối mùa hiện tại bắt đầu
bool SeasonAdvancePending
```

Mọi giá trị khác (`Day`, `ClockMinute`, `DayOfWeek`, `DayInSeason`) tính ra từ state và settings, không lưu.

- Thời gian đi theo step cố định, ước của 60, để mọi mốc giờ rơi đúng vào một step và chạy thường, fast-forward, skip ra cùng kết quả (ADR 0001).
- Toàn bộ luật chuyển mùa nằm trong một hàm nội bộ `ProcessDayBoundary()`, dùng chung cho step thường và cho vòng lặp từng ngày của skip. Khi đổi mùa: `SeasonStartDay` = ngày mới, xóa cờ chờ; qua mùa cuối thì `Year + 1`.
- Skip và fast-forward là hai cơ chế riêng (ADR 0002); lệnh gọi trong handler được xếp hàng để mọi handler cùng thấy một thời điểm (ADR 0004).

## Quyết định đi kèm

- `MaxTotalMinutes` là bội của 1440, nên nằm trên mọi step hợp lệ, và đủ nhỏ để `ExactMinutes` (`double`) chính xác dưới một phần nghìn phút. Chỉ dùng để kiểm tra đầu vào (`Restore`, đích của `SkipTo` và `FastForwardTo`).
- Giả định quy mô (mục Giới hạn trong `README.md`) không được kiểm tra khi `Tick` hay khi sang năm, theo luật không thêm code cho trường hợp chỉ xảy ra khi tràn kiểu dữ liệu.
- `MaxSkipDays` là setting: skip xa hơn coi là bug của nơi gọi, chặn lại để vòng lặp từng ngày của skip không treo game.
- Settings chỉ là cấu hình ban đầu. Cần đổi lúc chạy thứ chưa có lệnh (ví dụ `MaxSpeed`, độ dài ngày) thì thêm lệnh riêng khi có nhu cầu.
- `AdvanceSeason()` cũng xếp hàng trong handler, để `Restore(save); AdvanceSeason();` không bị snapshot xóa cờ.

## Ghép với module khác

- Stats: module đọc tham số mà Effects đổi (Needs) không gọi `Advance` riêng lẻ ở `Stepped`/`Skipped`; scenario gọi chung qua một hàm `Simulate` chia đoạn theo mục "Quy ước cho module dùng Stats" trong `stats.md`.

## Kiểm thử

Test EditMode trong `Tests/` của package, mỗi file một nhóm quy tắc ở trên:

| File | Phủ |
|---|---|
| `GameClockSettingsTests` | Settings hợp lệ và sai, giá trị bắt đầu, tên thứ và mùa, settings được copy |
| `CalendarConfigTests` | Giá trị mặc định của asset, `CreateSettings()` trả bản copy |
| `GameClockTimeTests` | Step, `ExactMinutes`, `DayStartMinute`, thứ tự event, tốc độ, tạm dừng, `NextOccurrence`, `MaxStepsPerFrame` |
| `GameClockSeasonTests` | Fixed-length season, command-only season, chuyển sớm, sang năm, tuần qua các mùa |
| `GameClockSkipFastForwardTests` | Skip, fast-forward, tương tác giữa hai cơ chế, `SkipTo(Now)`, `MaxSkipDays`, đích quá `MaxTotalMinutes`, lệnh bị từ chối khi đang fast-forward, lệnh trong handler của `FastForwardCompleted`; **fast-forward và skip cùng một khoảng ra cùng kết quả** |
| `GameClockCommandQueueTests` | Hàng đợi lệnh (kể cả `AdvanceSeason`), bỏ phần dư, exception trong handler, lệnh sai trong hàng đợi |
| `GameClockSnapshotTests` | Lưu và restore, đi qua `JsonUtility`, dữ liệu sai (không đổi gì kể cả fast-forward và thời gian lẻ), giữ pause source và base speed |

Thử bằng tay: experiment `Assets/Playground/Calendar/` (overlay IMGUI có nút cho mọi lệnh), scene `Assets/Scenes/Calendar.unity`.

Kiểm bằng tay:

| Luật | Bước kiểm |
|---|---|
| Config sai: asset log warning | Đặt `Minutes Per Step` = 7 trong `CalendarConfig.asset` → Console có warning |
| Config thiếu: driver log lỗi, tự tắt | Bỏ trống ô Config của driver, vào Play → có lỗi, driver tắt |
| Config sai: driver log lỗi, tự tắt | `Minutes Per Step` = 7, vào Play → có `ArgumentException`, driver tắt |
| Driver bị bật lại khi không có `Clock` thì tự tắt | Bỏ trống ô Config, vào Play (có lỗi "has no CalendarConfig", ô enabled bị bỏ tick); trong lúc Play, tick lại ô enabled của driver → ô enabled bị bỏ tick lại ngay, Console không có `NullReferenceException` |
| `Time.timeScale = 0` dừng đồng hồ | Vào Play, đặt `Time Scale` = 0 trong **Project Settings → Time** → giờ trên overlay đứng yên; đặt lại 1 → chạy tiếp |
| Driver chạy trước: `Clock` có sẵn từ `Awake`/`OnEnable` | Mở scene `Calendar.unity`, vào Play → không có lỗi "has no running GameClock" từ `CalendarDebugOverlay` hay `FakeNeedListener` (hai component này đọc `Clock` trong `OnEnable`) |
