# Stats

Chỉ số có modifier (Stat), giá trị tiêu và hồi bị chặn bởi một Stat (Resource), và gói modifier có thời hạn (Effect), dùng chung cho nhân vật và NPC.

Tài liệu thiết kế: phạm vi, kiến trúc, mô hình nội bộ và lý do. Luật hành vi (API, Settings, event, save, quy ước cho module dùng nó): `stats.readme.md`, từ giai đoạn Package là `README.md` của package. Thuật ngữ: `GLOSSARY.md` (mục Stats). Trạng thái: `roadmap.md`.

| Mục | Giá trị |
|---|---|
| Package | `unity/Packages/com.guenbankhai.stats` |
| Namespace / asmdef | `GuenBanKhai.Stats`, `GuenBanKhai.Stats.Tests` (lõi); `GuenBanKhai.Stats.Effects`, `GuenBanKhai.Stats.Effects.Tests` (lớp Effects) |
| Phụ thuộc | Không có |
| Quyết định | [ADR 0005](../adr/0005-stats-resource-and-effects-in-stats.md) · [0006](../adr/0006-stats-time-agnostic-core.md) · [0007](../adr/0007-stats-save-base-values-only.md) · [0008](../adr/0008-stats-explicit-definition-id.md) · [0009](../adr/0009-stats-nested-handler-commands.md) |

Không dùng tên `Attribute`: trùng `System.Attribute`, file có `using System;` sẽ lỗi CS0104.

## Phạm vi [T2]

**Trong module:** Stat (base + modifier ba tầng, min/max, gỡ theo source); Resource (current trong `[Min, Max]`, max là một Stat, event khi cạn, khi đầy); Effect ở lớp Effects (gói modifier có thời hạn hoặc vô hạn, luật áp lại, group); snapshot cho cả hai lớp.

**Ngoài module:**

- Luật riêng của Health (sát thương, ngất) và Needs (ngưỡng gây ốm): các module đó dùng `Resource` và tự giữ luật.
- Cấp và XP: thuộc Skills.
- Nối thời gian với Calendar: việc của scenario (xem Ghép với module khác).
- Chưa có ở bản đầu, thêm khi có nhu cầu thật:
  - Derived stat trong lõi: game tự nối bằng `ReplaceModifier`; đưa vào lõi khi giới hạn batch (README, mục Viết module dùng Stats) gặp trong game thật.
  - Override: ép về 0 đã làm được bằng `PercentMult` −1.
  - Resource giữ tỉ lệ khi max đổi: chưa module nào cần.
  - Effect tác động định kỳ hoặc có hook logic: thêm khi Needs hay Health cần.
  - Stack `Independent` (mỗi lần áp một instance): chưa có effect nào cần đồng hồ riêng cho từng stack.
  - Stack kiểu `Repeat` (mỗi stack nhân riêng `PercentMult`, `(1 + Value)^n − 1`): chưa có effect nào cần; bản đầu mọi stack cộng thành `Value × Stacks`.
  - Thêm stat, resource lúc chạy (`AddStat`, `AddResource`): chưa module nào cần; thêm kèm luật dựng lại trước khi restore và cách xử lý save cũ không có stat đó.
  - Nhãn phân loại effect (khác Effect group): thêm khi UI hay luật game cần lọc effect.
  - Icon và mô tả của stat, effect: thêm khi làm UI thật.
  - Batch public: thêm khi Equipment cần đổi trang bị không bị clamp ở giữa.
  - `Stat.RemoveAllFromSource` (gỡ theo source trên một stat): chưa module nào cần; nguồn modifier gỡ qua `StatCollection.RemoveAllFromSource`.

## Kiến trúc [T3]

Hai asmdef; lõi không biết Effects, game không dùng effect thì không reference `GuenBanKhai.Stats.Effects`.

```text
com.guenbankhai.stats/
├── Runtime/            GuenBanKhai.Stats
│   └── Effects/        GuenBanKhai.Stats.Effects (reference GuenBanKhai.Stats)
└── Tests/              GuenBanKhai.Stats.Tests
    └── Effects/        GuenBanKhai.Stats.Effects.Tests
```

| Thành phần | Vai trò |
|---|---|
| `StatCollection`, `EffectCollection` | Lõi C# thuần, mỗi thực thể một cái. `EffectCollection` giữ `StatCollection`, đổi modifier qua batch `internal` của lõi và dùng chung bộ đếm độ sâu event; lõi mở cho lớp Effects bằng `InternalsVisibleTo("GuenBanKhai.Stats.Effects")` |
| `Stat`, `Resource`, `EffectInstance` | Do collection tạo; mọi thay đổi đi qua lệnh của lõi. `Modifier` bất biến |
| `*Settings` | Cấu hình C# thuần; collection kiểm tra và copy một lần |
| `StatDefinition`, `ResourceDefinition`, `StatProfile`, `EffectDefinition`, `EffectGroupDefinition` | ScriptableObject chỉ bọc settings; `CreateSettings()` trả bản copy. ID là field `_id` ([ADR 0008](../adr/0008-stats-explicit-definition-id.md)). Group khai `Policy` một chỗ trên asset riêng |
| `DefinitionEditor` | Lớp trừu tượng cho Inspector của definition: tự điền `_id` trống từ tên asset nếu đúng chuẩn, báo lỗi ID trống, sai chuẩn hoặc trùng. Mỗi loại definition có một Editor con (`CustomEditor` không gắn nhiều lần được); `EffectDefinitionEditor` ẩn thêm trường theo lựa chọn |
| `StatsBehaviour` | Driver mỏng: tạo `StatCollection` từ `StatProfile` trong `Awake`; `DefaultExecutionOrder` sớm để component khác đọc `Collection` trong `Awake` |
| `EffectsBehaviour` | Driver mỏng, `[RequireComponent(typeof(StatsBehaviour))]`, lấy `StatsBehaviour` bằng `GetComponent` trong `Awake`. `DefaultExecutionOrder` ngay sau `StatsBehaviour` để component khác đọc `Collection` trong `Awake`. Không tự gọi `Advance` |

- Lấy tham chiếu: `[SerializeField] StatsBehaviour` → `.Collection`, hoặc truyền qua constructor. Không singleton, không state static.
- Config thiếu hoặc sai: behaviour log lỗi, tự tắt, `Collection` là `null`.
- Lõi trả thông tin UI cần (`DisplayName`) và không trả ra danh sách hay settings mutable mà lõi giữ (README, mục API).

## Mô hình nội bộ

State (nguồn sự thật duy nhất):

```text
StatCollection
  stats[]        Id, BaseValue, Min, Max, modifiers[] (thứ tự thêm), Value (cache)
  resources[]    Id, Min, MaxStat, Current
  eventDepth     độ sâu event lồng, dùng chung với EffectCollection
EffectCollection
  instances[]    Id, settings (copy), Stacks, Remaining, IsSuppressed, lastApplySeq; thứ tự áp lần đầu
  applySeq       bộ đếm lần áp
```

- `Stat.Value` tính lại từ đầu theo công thức mỗi khi base hay modifier đổi rồi cache; không cộng trừ dồn, để sai số float không tích lũy làm `!=` của `ValueChanged` sai.
- `Resource.Max` tính từ `MaxStat.Value` và `Min`; không lưu riêng.
- `IsSuppressed` và modifier của instance suy ra từ group, rank và `lastApplySeq`. Đổi stack hay trạng thái thì thay modifier của instance trong một batch nội bộ: gom thay đổi modifier, tính lại mỗi stat bị ảnh hưởng một lần, clamp Resource theo max cuối, rồi mới phát event.
- `TimeUntilNextExpiry` duyệt instance có thời hạn, kể cả bị suppress.
- Số instance tối đa bằng số `EffectId` khác nhau. Giả định quy mô: README, mục Giới hạn.

## Quyết định đi kèm

- **Lệnh trong handler chạy lồng ngay, không xếp hàng như Calendar; `MaxEventDepth` là hằng số** vì chỉ chặn lỗi của code bên ngoài ([ADR 0009](../adr/0009-stats-nested-handler-commands.md)).
- **Restore phát event ngoài `Restored`** (khác mặc định của `coding-standard.md`) để derived stat cập nhật trước khi đặt current ([ADR 0007](../adr/0007-stats-save-base-values-only.md)).
- **Current ngoài `[Min, Max]` khi restore thì clamp, không từ chối:** max phụ thuộc modifier của nguồn khác mà snapshot Stats không giữ; clamp là luật của Resource.
- **Ngoại lệ B3 cho derived stat** nối bằng handler, chấp nhận tới khi derived stat vào lõi ([ADR 0009](../adr/0009-stats-nested-handler-commands.md)); giới hạn và cách tránh: README, mục Viết module dùng Stats.
- **`Stat.ValueChanged` so sánh bằng `!=`, không epsilon:** giá trị luôn tính lại từ đầu nên cùng đầu vào cho cùng kết quả.
- **Snapshot là class, không phải struct như Calendar:** struct chứa mảng khi copy vẫn dùng chung mảng.

## Ghép với module khác

**Calendar** (scenario nối, Stats không phụ thuộc): `Simulate(delta)` theo mục "Viết module dùng Stats" của README, gọi từ `Stepped` (`clock.MinutesPerStep`) và `Skipped` (`to.TotalMinutes − from.TotalMinutes`); không gọi từ `Restored`. Đơn vị là phút nguyên, nên chạy thường và skip cho cùng kết quả (B1). Một hàm cho cả hai đường thì thứ tự gọi không phụ thuộc thứ tự đăng ký handler.

## Kiểm thử [R4]

| File test | Phủ |
|---|---|
| `StatIdTests` | ID đúng và sai chuẩn PascalCase, ordinal, `default`, ID của group và snapshot sai chuẩn |
| `StatFormulaTests` | Ba tầng, ví dụ trong README, clamp giá trị cuối, base ngoài khoảng |
| `StatModifierTests` | Thêm, gỡ, thêm trùng, gỡ theo source, `ReplaceModifier` (vị trí, một event, các trường hợp trả `false`), event chỉ khi giá trị đổi, `RemoveAllFromSource` của collection cập nhật mọi stat rồi mới phát |
| `ResourceTests` | `Add`/`Set`, clamp, max đổi giữ số tuyệt đối, đổi áo A sang B, stat làm max dưới `Min`, `Normalized`, `InitialFraction`, thứ tự event, `Filled` không phát khi max giảm xuống current; derived stat qua `ReplaceModifier`: Vitality đổi, máu giữ 140 |
| `StatCollectionTests` | Settings (null, phần tử null, trùng ID, resource trỏ stat không có), copy, `DisplayName` (mặc định là ID), `Get`/`TryGet`, wrapper chỉ đọc, lệnh và exception trong handler, event còn chờ bị bỏ khi handler lồng đổi tiếp (tháo giáp +Vitality +`MaxHealth`: listener nhận `(180,150)`), `MaxEventDepth` |
| `StatsSnapshotTests` | `JsonUtility`, `CaptureSnapshot` trả mảng mới, chỉ lưu base khác settings, stat vắng mặt về base trong settings, resource vắng mặt về giá trị theo `InitialFraction` (thực thể mới và đang chơi cùng kết quả), ba bước restore, Resource im lặng khi restore, derived stat giữ đúng máu 140/150, từ chối từng loại dữ liệu hỏng và ID không có trong collection (state không đổi), `Restore` trong handler |
| `StatDefinitionTests` | Definition và profile: mặc định, ID lấy từ `_id` chứ không từ tên asset, `_id` trống hoặc sai chuẩn bị từ chối, bản copy, override base, `MaxStat` tự thêm (không liệt kê thì lấy base của definition, liệt kê có override thì dòng liệt kê thắng, để trống thì bị từ chối), `DisplayName` |
| `Effects/EffectApplyTests` | Bốn `StackRule`, `MaxStacks`, `MaxDuration`, rule khác bỏ qua `MaxStacks`/`MaxDuration`, `Value × Stacks` (mỗi modifier settings đúng một `Modifier`), ràng buộc stack, vô hạn, stat lạ, stack lên `MaxHealth` không mất current, `Settings` trả bản copy mới, `Has`/`TryGet` |
| `Effects/EffectGroupTests` | `Replace`/`Suppress` (thuốc tốc độ), bằng rank (bánh mì, cơm; áp lại effect bị suppress), group trùng `Id` khác `Policy`, suppress vẫn stack, thứ tự event (Stat → Resource → Effect; thua trước thắng sau) |
| `Effects/EffectAdvanceTests` | Hết hạn và thứ tự, resume, `TimeUntilNextExpiry` (kể cả suppress, +∞), `delta` sai, handler áp/gỡ (event chờ bị bỏ), `Advance` trong handler (của effect và của Stat), vòng lặp chỉ gồm event effect; `Advance(10)` một lần và `Advance(1)` mười lần ra cùng snapshot và giá trị stat; resume rồi hết hạn trong một lần gọi không phát `Resumed` |
| `Effects/EffectsSnapshotTests` | `JsonUtility` với vô hạn, `CaptureSnapshot` trả mảng mới, suppress tính lại, từ chối từng loại dữ liệu hỏng và lệch settings (`resolve` trả `null`, settings sai hoặc khác `Id`, stat lạ, số thứ tự âm hoặc trùng, `Stacks` vượt mức theo `StackRule`, `Remaining` vượt mức, cờ vô hạn khác settings, hai effect cùng group `Replace`, group cùng `Id` khác `Policy`; state không đổi), `Restore` trong handler, bộ đếm lần áp đi tiếp sau restore; restore effects rồi stats giữ đúng current |
| `Effects/EffectDefinitionTests` | Effect và group definition: mặc định, ID lấy từ `_id`, bản copy, `DisplayName`, tên field mà Inspector ẩn theo điều kiện vẫn tồn tại |
| `Effects/EffectsAssemblyTests` | `EffectCollection` nằm trong assembly `GuenBanKhai.Stats.Effects` (bắt lỗi lỡ xóa asmdef con) |

Kiểm bằng tay (luật chạy qua callback của Unity):

| Luật | Bước kiểm |
|---|---|
| `OnValidate` log warning | Lần lượt tạo: `StatDefinition` có `_id` sai chuẩn (`move speed`); `StatDefinition` có `Min > Max`; hai `StatDefinition` cùng `_id`, đưa vào một `StatProfile`; resource để trống `MaxStat` trong profile; `EffectDefinition` `Extend` có `MaxDuration < Duration`. Mỗi trường hợp có warning trong Console, và trừ trường hợp `_id` sai chuẩn, nội dung warning đúng câu lỗi của lõi |
| Inspector của definition: ID | Tạo asset mới tên `MoveSpeed`: ID tự điền `MoveSpeed`, không dính tên tạm `NewStat`. Tạo asset tên có khoảng trắng: ID trống, có lỗi đỏ. Xóa trống ID của asset tên đúng chuẩn: nhấn Enter thì ID điền lại theo tên, đang gõ thì không bị điền. Đổi tên file: ID giữ nguyên. Duplicate asset: có lỗi đỏ báo trùng kèm đường dẫn. Áp dụng cho cả bốn loại definition |
| `StatsBehaviour` gặp profile thiếu hoặc sai | Để trống profile (rồi thử profile có ID trùng), Play: có log lỗi, component bị tắt, `Collection` là `null` |
| `EffectsBehaviour` khi `Collection` là `null` | Cùng GameObject với `StatsBehaviour` profile trống, Play: có log lỗi, `EffectsBehaviour` bị tắt |
| Inspector của `EffectDefinition` | Đổi lần lượt bốn `StackRule`: `Max Duration` chỉ hiện khi `Extend`, `Max Stacks` chỉ hiện khi `Stack`, dòng mô tả dưới `Stack Rule` đổi theo rule. Bật `Infinite`: ẩn `Duration`, `Max Duration`, dòng mô tả có thêm câu về vô hạn. Bỏ `Group`: ẩn `Rank`. Chọn hai effect khác rule cùng lúc: hiện đủ mọi trường, không có dòng mô tả. Đổi rule rồi đổi lại: giá trị cũ vẫn còn |
| `DefaultExecutionOrder` của `StatsBehaviour`, `EffectsBehaviour` | Component trên GameObject khác đọc `StatsBehaviour.Collection` và `EffectsBehaviour.Collection` trong `Awake`, Play: không `null` |

Playground: `Assets/Playground/Stats/`, scene `Assets/Scenes/Stats.unity`, asset trong `Assets/Configs/Stats/` (`Stats/`, `Pools/`, `Effects/`, `Groups/`, profile `TestEntity`). Asset sinh từ `PlaygroundCatalog` bằng menu `GuenBanKhai/Stats/Playground/Generate Assets` (chỉ tạo asset chưa có). Generator ghi asset giống cách designer tạo tay: có `_id`; profile không liệt kê `MaxStat` của resource; effect chỉ có các trường mà rule và group của nó dùng.

## Kế hoạch (tạm) [T9]

Giai đoạn: **Playground**. Xóa mục này khi phát hành.

- [x] User chốt tài liệu.
- [x] Playground bước 1: lõi C# (settings dựng bằng code), overlay có nút cho mọi lệnh, thời gian chạy bằng nút của overlay hoặc bằng `GameClockDriver` của Calendar, test EditMode cho logic khó.
- [x] Playground bước 2: ScriptableObject (`StatDefinition`, `ResourceDefinition`, `StatProfile`, `EffectDefinition`, `EffectGroupDefinition`) và `StatsBehaviour`, `EffectsBehaviour`; overlay lấy thực thể từ hai behaviour, effect từ `EffectDefinition`. User đã chơi thử: happy case đúng.
- [x] Kiểm bằng tay các trường hợp lỗi (bảng "Kiểm bằng tay" ở mục Kiểm thử): warning của `OnValidate`, profile trống hoặc sai, `EffectsBehaviour` khi `Collection` là `null`, `DefaultExecutionOrder`. User đã kiểm: đúng.
- [x] Trả lời câu hỏi authoring. User đã chơi thử:
  - Nhập `Infinity` cho `Min`/`Max` dùng được và rõ ràng, giữ nguyên.
  - Toggle `Infinite`: bật thì Inspector ẩn `Duration`, `MaxDuration`; giá trị cũ vẫn giữ trong asset, lõi bỏ qua.
  - Đã sửa ba chỗ: rule bỏ qua hẳn trường nó không dùng (`MaxDuration` chỉ cho `Extend`, `MaxStacks` chỉ cho `Stack`); `OnValidate` chạy đúng phép kiểm của lõi; profile tự thêm `MaxStat` của resource.
  - Chống hiểu lầm: Inspector của `EffectDefinition` chỉ hiện trường mà lựa chọn hiện tại dùng tới, kèm dòng mô tả rule; thêm tooltip cho đơn vị thời gian, `Value` của modifier, `Rank`, `Policy`, và `Min` của stat so với `Min` của resource; enum lưu trong asset có số cố định.
- [x] Kiểm bằng tay dòng "Inspector của `EffectDefinition`" và "Inspector của definition: ID" trong bảng "Kiểm bằng tay". User đã kiểm: đúng.
- [x] Xóa asset cũ trong `Assets/Configs/Stats/`, tạo lại bằng generator, gán lại vào scene `Stats.unity` (ID đổi sang `_id`, [ADR 0008](../adr/0008-stats-explicit-definition-id.md)).
- [ ] Review: `/review-my-module stats playground`.
- Để lúc lên Package:
  - Sửa lỗi restore: `EffectCollection` nhận `Remaining = +∞` khi `IsInfinite = false` nếu effect `Extend` có `MaxDuration` là +∞, trái README.
  - `DefinitionEditor` vào asmdef `GuenBanKhai.Stats.Editor`, `EffectDefinitionEditor` vào asmdef riêng hay chung thì quyết khi đó; asmdef Editor cần truy cập `IdFormat` internal.
- Câu hỏi cho playground: authoring `StatProfile`/`EffectDefinition` trong Inspector có tiện không (đã trả lời ở trên); overlay debug có nút cho mọi lệnh (đã có, gồm cả bước 2).
