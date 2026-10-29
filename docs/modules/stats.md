# Stats

Chỉ số có modifier (Stat), giá trị tiêu và hồi bị chặn bởi một Stat (Resource), và gói modifier có thời hạn (Effect), dùng chung cho nhân vật và NPC.

Tài liệu thiết kế: phạm vi, kiến trúc, mô hình nội bộ và lý do. Luật hành vi (API, Settings, event, save, quy ước cho module dùng nó): `stats.readme.md`, từ giai đoạn Package là `README.md` của package. Thuật ngữ: `GLOSSARY.md` (mục Stats). Trạng thái: `roadmap.md`.

| Mục | Giá trị |
|---|---|
| Package | `unity/Packages/com.guenbankhai.stats` |
| Namespace / asmdef | `GuenBanKhai.Stats`, `GuenBanKhai.Stats.Tests` (lõi); `GuenBanKhai.Stats.Effects`, `GuenBanKhai.Stats.Effects.Tests` (lớp Effects) |
| Phụ thuộc | Không có |
| Quyết định | [ADR 0005](../adr/0005-stats-resource-and-effects-in-stats.md) · [0006](../adr/0006-stats-time-agnostic-core.md) · [0007](../adr/0007-stats-save-base-values-only.md) · [0008](../adr/0008-stats-asset-name-as-id.md) · [0009](../adr/0009-stats-nested-handler-commands.md) |

Không dùng tên `Attribute`: trùng `System.Attribute`, file có `using System;` sẽ lỗi CS0104.

## Phạm vi [T2]

**Trong module:** Stat (base + modifier ba tầng, min/max, gỡ theo source); Resource (current trong `[Min, Max]`, max là một Stat, event khi cạn, khi đầy); Effect ở lớp Effects (gói modifier có thời hạn hoặc vô hạn, luật áp lại, group); snapshot cho cả hai lớp.

**Ngoài module:**

- Luật riêng của Health (sát thương, ngất) và Needs (ngưỡng gây ốm): các module đó dùng `Resource` và tự giữ luật.
- Cấp và XP: thuộc Skills.
- Nối thời gian với Calendar: việc của scenario (xem Ghép với module khác).
- Chưa có ở bản đầu, thêm khi có nhu cầu thật:
  - Derived stat trong lõi: game tự nối bằng `ReplaceModifier`; đưa vào lõi khi giới hạn batch (xem Quyết định đi kèm) gặp trong game thật.
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
| `StatDefinition`, `ResourceDefinition`, `StatProfile`, `EffectDefinition`, `EffectGroupDefinition` | ScriptableObject chỉ bọc settings; `CreateSettings()` trả bản copy. ID là tên asset ([ADR 0008](../adr/0008-stats-asset-name-as-id.md)). Group khai `Policy` một chỗ trên asset riêng |
| `StatsBehaviour` | Driver mỏng: tạo `StatCollection` từ `StatProfile` trong `Awake`; `DefaultExecutionOrder` sớm để component khác đọc `Collection` trong `Awake` |
| `EffectsBehaviour` | Driver mỏng, `[RequireComponent(typeof(StatsBehaviour))]`, lấy `StatsBehaviour` bằng `GetComponent` trong `Awake`. Không tự gọi `Advance` |

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
| `StatIdTests` | ID hợp lệ và sai, ordinal, `default` |
| `StatFormulaTests` | Ba tầng, ví dụ trong README, clamp giá trị cuối, base ngoài khoảng |
| `StatModifierTests` | Thêm, gỡ, thêm trùng, gỡ theo source, `ReplaceModifier` (vị trí, một event, các trường hợp trả `false`), event chỉ khi giá trị đổi, `RemoveAllFromSource` của collection cập nhật mọi stat rồi mới phát |
| `ResourceTests` | `Add`/`Set`, clamp, max đổi giữ số tuyệt đối, đổi áo A sang B, stat làm max dưới `Min`, `Normalized`, `InitialFraction`, thứ tự event, `Filled` không phát khi max giảm xuống current; derived stat qua `ReplaceModifier`: Vitality đổi, máu giữ 140 |
| `StatCollectionTests` | Settings (null, phần tử null, trùng ID, resource trỏ stat không có), copy, `DisplayName` (mặc định là ID), `Get`/`TryGet`, wrapper chỉ đọc, lệnh và exception trong handler, event còn chờ bị bỏ khi handler lồng đổi tiếp (tháo giáp +Vitality +`MaxHealth`: listener nhận `(180,150)`), `MaxEventDepth` |
| `StatsSnapshotTests` | `JsonUtility`, `CaptureSnapshot` trả mảng mới, chỉ lưu base khác settings, stat vắng mặt về base trong settings, resource vắng mặt về giá trị theo `InitialFraction` (thực thể mới và đang chơi cùng kết quả), ba bước restore, Resource im lặng khi restore, derived stat giữ đúng máu 140/150, từ chối từng loại dữ liệu hỏng và ID không có trong collection (state không đổi), `Restore` trong handler |
| `StatDefinitionTests` | Definition và profile: mặc định, ID là tên asset, bản copy, override base, `DisplayName` |
| `Effects/EffectApplyTests` | Bốn `StackRule`, `MaxStacks`, `MaxDuration`, `Value × Stacks` (mỗi modifier settings đúng một `Modifier`), ràng buộc stack, vô hạn, stat lạ, stack lên `MaxHealth` không mất current, `Settings` trả bản copy mới, `Has`/`TryGet` |
| `Effects/EffectGroupTests` | `Replace`/`Suppress` (thuốc tốc độ), bằng rank (bánh mì, cơm; áp lại effect bị suppress), group trùng `Id` khác `Policy`, suppress vẫn stack, thứ tự event (Stat → Resource → Effect; thua trước thắng sau) |
| `Effects/EffectAdvanceTests` | Hết hạn và thứ tự, resume, `TimeUntilNextExpiry` (kể cả suppress, +∞), `delta` sai, handler áp/gỡ (event chờ bị bỏ), `Advance` trong handler (của effect và của Stat), vòng lặp chỉ gồm event effect; `Advance(10)` một lần và `Advance(1)` mười lần ra cùng snapshot và giá trị stat; resume rồi hết hạn trong một lần gọi không phát `Resumed` |
| `Effects/EffectsSnapshotTests` | `JsonUtility` với vô hạn, `CaptureSnapshot` trả mảng mới, suppress tính lại, từ chối từng loại dữ liệu hỏng và lệch settings (`resolve` trả `null`, settings sai hoặc khác `Id`, stat lạ, số thứ tự âm hoặc trùng, `Stacks` vượt mức theo `StackRule`, `Remaining` vượt mức, cờ vô hạn khác settings, hai effect cùng group `Replace`, group cùng `Id` khác `Policy`; state không đổi), `Restore` trong handler, bộ đếm lần áp đi tiếp sau restore; restore effects rồi stats giữ đúng current |
| `Effects/EffectDefinitionTests` | Effect và group definition: mặc định, ID là tên asset, bản copy, `DisplayName` |
| `Effects/EffectsAssemblyTests` | `EffectCollection` nằm trong assembly `GuenBanKhai.Stats.Effects` (bắt lỗi lỡ xóa asmdef con) |

Kiểm bằng tay (luật chạy qua callback của Unity):

| Luật | Bước kiểm |
|---|---|
| `OnValidate` log warning | Tạo `StatDefinition` tên có khoảng trắng, đặt `Min > Max`; tạo hai `StatDefinition` cùng tên ở hai thư mục và đưa vào một `StatProfile`; tạo `EffectDefinition` `PercentMult` −0.5, `MaxStacks` 3. Mỗi trường hợp có warning trong Console |
| `StatsBehaviour` gặp profile thiếu hoặc sai | Để trống profile (rồi thử profile có ID trùng), Play: có log lỗi, component bị tắt, `Collection` là `null` |
| `EffectsBehaviour` khi `Collection` là `null` | Cùng GameObject với `StatsBehaviour` profile trống, Play: có log lỗi, `EffectsBehaviour` bị tắt |
| `DefaultExecutionOrder` của `StatsBehaviour` | Component trên GameObject khác đọc `StatsBehaviour.Collection` trong `Awake`, Play: không `null` |

Playground: `Assets/Playground/Stats/`, scene `Assets/Scenes/Stats.unity` (tạo ở giai đoạn Playground).

## Kế hoạch (tạm) [T9]

Giai đoạn: **Thiết kế**, kết thúc khi user chốt tài liệu này. Xóa mục này khi phát hành.

- [ ] User chốt tài liệu.
- Câu hỏi cho playground: authoring `StatProfile`/`EffectDefinition` trong Inspector có tiện không; overlay debug có nút cho mọi lệnh.
