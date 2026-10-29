# Stats

Chỉ số có modifier (Stat), giá trị tiêu và hồi bị chặn bởi một Stat (Resource), và gói modifier có thời hạn (Effect), dùng chung cho nhân vật và NPC.

Tài liệu thiết kế: phạm vi, kiến trúc, mô hình nội bộ và lý do. Luật hành vi (API, Settings, event, save, quy ước cho module dùng nó): `stats.readme.md`, từ giai đoạn Package là `README.md` của package. Thuật ngữ: `GLOSSARY.md` (mục Stats). Trạng thái: `roadmap.md`.

| Mục | Giá trị |
|---|---|
| Package | `unity/Packages/com.guenbankhai.stats` |
| Namespace / asmdef | `GuenBanKhai.Stats`, `GuenBanKhai.Stats.Tests` (lõi); `GuenBanKhai.Stats.Effects`, `GuenBanKhai.Stats.Effects.Tests` (lớp Effects) |
| Phụ thuộc | Không có |
| Quyết định | [ADR 0005](../adr/0005-stats-resource-and-effects-in-stats.md) · [0006](../adr/0006-stats-time-agnostic-core.md) · [0007](../adr/0007-stats-save-base-values-only.md) · [0008](../adr/0008-stats-asset-name-as-id.md) |

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
  - Nhãn phân loại effect (khác Effect group): thêm khi UI hay luật game cần lọc effect.
  - Icon và mô tả của stat, effect: thêm khi làm UI thật.
  - Batch public: thêm khi Equipment cần đổi trang bị không bị clamp ở giữa.

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
| `StatCollection`, `EffectCollection` | Lõi C# thuần, mỗi thực thể một cái. `EffectCollection` giữ `StatCollection` và thêm modifier qua lệnh public của nó |
| `Stat`, `Resource`, `EffectInstance` | Do collection tạo; mọi thay đổi đi qua lệnh của lõi. `Modifier` bất biến |
| `*Settings` | Cấu hình C# thuần; collection kiểm tra và copy một lần |
| `StatDefinition`, `ResourceDefinition`, `StatProfile`, `EffectDefinition`, `EffectGroupDefinition` | ScriptableObject chỉ bọc settings; `CreateSettings()` trả bản copy. ID là tên asset ([ADR 0008](../adr/0008-stats-asset-name-as-id.md)). Group khai `Policy` một chỗ trên asset riêng |
| `StatsBehaviour` | Driver mỏng: tạo `StatCollection` từ `StatProfile` trong `Awake`; `DefaultExecutionOrder` sớm để component khác đọc `Collection` trong `Awake` |
| `EffectsBehaviour` | Driver mỏng, `[RequireComponent(typeof(StatsBehaviour))]`, lấy `StatsBehaviour` bằng `GetComponent` trong `Awake`. Không tự gọi `Advance` |

- Lấy tham chiếu: `[SerializeField] StatsBehaviour` → `.Collection`, hoặc truyền qua constructor. Không singleton, không state static.
- Config thiếu hoặc sai: behaviour log lỗi, tự tắt, `Collection` là `null`.
- Lõi trả thông tin UI cần (`DisplayName`); `Stats`, `Modifiers`, `Instances` là wrapper chỉ đọc; `EffectInstance.Settings` trả bản copy mới.

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
- Giả định quy mô: mỗi thực thể tới vài chục stat và resource, vài modifier mỗi stat, tới vài chục effect; mọi lệnh duyệt tuyến tính và cấp phát theo số phần tử. Số instance tối đa bằng số `EffectId` khác nhau; `MaxStacks` không giới hạn trên vì `Repeat` tính bằng lũy thừa, không lặp theo số stack.

## Quyết định đi kèm

- **Lệnh trong handler chạy lồng ngay, không xếp hàng như Calendar:** Stats không có step, và handler derived stat cần đọc ngay giá trị mới. Chống lặp bằng `MaxEventDepth` = 32 (hằng số, vì chỉ chặn lỗi của code bên ngoài); `Advance` và `Restore` trong handler bị cấm.
- **Restore phát event ngoài `Restored`** (khác mặc định của `coding-standard.md`): bước 1 của `StatCollection.Restore` phát `Stat.ValueChanged`, và `EffectCollection.Restore` để stat phát `ValueChanged`, vì derived stat phụ thuộc chính base và modifier được restore nên phải cập nhật trước khi đặt current ([ADR 0007](../adr/0007-stats-save-base-values-only.md)).
- **Current ngoài `[Min, Max]` khi restore thì clamp, không từ chối:** max phụ thuộc modifier của nguồn khác mà snapshot Stats không giữ; clamp là luật của Resource.
- **Ngoại lệ B3 cho derived stat:** derived stat nối bằng handler nằm ngoài batch của effect, nên một lệnh đổi cả stat nguồn lẫn stat làm max có thể clamp current theo max tạm và phát `ValueChanged` hai lần cho stat làm max. Chấp nhận vì derived stat chưa thuộc lõi; README ghi giới hạn và cách tránh. Đưa derived stat vào lõi khi gặp trong game thật.
- **`Stat.ValueChanged` so sánh bằng `!=`, không epsilon:** giá trị luôn tính lại từ đầu nên cùng đầu vào cho cùng kết quả.
- **Ràng buộc `Sum` kiểm bằng đúng phép nhân `float` của lõi:** settings qua kiểm tra thì `new Modifier` ở stack `MaxStacks` không ném.
- **Snapshot là class, không phải struct như Calendar:** struct chứa mảng khi copy vẫn dùng chung mảng.

## Ghép với module khác

**Calendar** (scenario nối, Stats không phụ thuộc): `Simulate(delta)` theo mục "Viết module dùng Stats" của README, gọi từ `Stepped` (`clock.MinutesPerStep`) và `Skipped` (`to.TotalMinutes − from.TotalMinutes`); không gọi từ `Restored`. Đơn vị là phút nguyên, nên chạy thường và skip cho cùng kết quả (B1). Một hàm cho cả hai đường thì thứ tự gọi không phụ thuộc thứ tự đăng ký handler.

## Kiểm thử [R4]

| File test | Phủ |
|---|---|
| `StatIdTests` | ID hợp lệ và sai, ordinal, `default` |
| `StatFormulaTests` | Ba tầng, ví dụ trong README, clamp giá trị cuối, base ngoài khoảng |
| `StatModifierTests` | Thêm, gỡ, thêm trùng, gỡ theo source (stat và collection), `ReplaceModifier` (vị trí, một event, các trường hợp trả `false`), event chỉ khi giá trị đổi, `RemoveAllFromSource` của collection cập nhật mọi stat rồi mới phát |
| `ResourceTests` | `Add`/`Set`, clamp, max đổi giữ số tuyệt đối, đổi áo A sang B, stat làm max dưới `Min`, `Normalized`, `InitialFraction`, thứ tự event, `Filled` không phát khi max giảm xuống current; derived stat qua `ReplaceModifier`: Vitality đổi, máu giữ 140 |
| `StatCollectionTests` | Settings (null, phần tử null, trùng ID), copy, `DisplayName` (mặc định là ID), `Get`/`TryGet`, wrapper chỉ đọc, lệnh và exception trong handler, `MaxEventDepth` |
| `StatsSnapshotTests` | `JsonUtility`, `CaptureSnapshot` trả mảng mới, chỉ lưu base khác settings, stat vắng mặt về base trong settings, resource vắng mặt về giá trị ban đầu (thực thể mới và đang chơi cùng kết quả), ba bước restore, Resource im lặng khi restore, derived stat giữ đúng máu 140/150, từ chối từng loại dữ liệu hỏng và ID không có trong collection (state không đổi), `Restore` trong handler |
| `StatDefinitionTests` | Definition và profile: mặc định, ID là tên asset, bản copy, override base, `DisplayName` |
| `Effects/EffectApplyTests` | Bốn `StackRule`, `MaxStacks`, `MaxDuration`, `Sum`/`Repeat` (mỗi modifier settings đúng một `Modifier`), ràng buộc `Sum`, vô hạn, stat lạ, stack lên `MaxHealth` không mất current, `Settings` trả bản copy mới, `Has`/`TryGet` |
| `Effects/EffectGroupTests` | `Replace`/`Suppress` (thuốc tốc độ), bằng rank (bánh mì, cơm; áp lại effect bị suppress), group trùng `Id` khác `Policy`, suppress vẫn stack, thứ tự event (Stat → Resource → Effect; thua trước thắng sau) |
| `Effects/EffectAdvanceTests` | Hết hạn và thứ tự, resume, `TimeUntilNextExpiry` (kể cả suppress, +∞), `delta` sai, handler áp/gỡ (event chờ bị bỏ), `Advance` trong handler (của effect và của Stat), vòng lặp chỉ gồm event effect; `Advance(10)` một lần và `Advance(1)` mười lần ra cùng snapshot và giá trị stat |
| `Effects/EffectsSnapshotTests` | `JsonUtility` với vô hạn, `CaptureSnapshot` trả mảng mới, suppress tính lại, từ chối từng loại dữ liệu hỏng và lệch settings (`resolve` trả `null`, stat lạ, `Stacks > MaxStacks`, `Remaining` vượt mức, cờ vô hạn khác settings, hai effect cùng group `Replace`; state không đổi), `Restore` trong handler, bộ đếm lần áp đi tiếp sau restore; restore effects rồi stats giữ đúng current |
| `Effects/EffectDefinitionTests` | Effect và group definition: mặc định, ID là tên asset, bản copy, `DisplayName` |
| `Effects/EffectsAssemblyTests` | `EffectCollection` nằm trong assembly `GuenBanKhai.Stats.Effects` (bắt lỗi lỡ xóa asmdef con) |

Kiểm bằng tay (luật chạy qua callback của Unity):

| Luật | Bước kiểm |
|---|---|
| `OnValidate` log warning | Tạo `StatDefinition` tên có khoảng trắng, đặt `Min > Max`; tạo hai `StatDefinition` cùng tên ở hai thư mục và đưa vào một `StatProfile`; tạo `EffectDefinition` `PercentMult` −0.5, `MaxStacks` 3, `Sum`. Mỗi trường hợp có warning trong Console |
| `StatsBehaviour` gặp profile thiếu hoặc sai | Để trống profile (rồi thử profile có ID trùng), Play: có log lỗi, component bị tắt, `Collection` là `null` |
| `EffectsBehaviour` khi `Collection` là `null` | Cùng GameObject với `StatsBehaviour` profile trống, Play: có log lỗi, `EffectsBehaviour` bị tắt |
| `DefaultExecutionOrder` của `StatsBehaviour` | Component trên GameObject khác đọc `StatsBehaviour.Collection` trong `Awake`, Play: không `null` |

Playground: `Assets/Playground/Stats/`, scene `Assets/Scenes/Stats.unity` (tạo ở giai đoạn Playground).

## Kế hoạch (tạm) [T9]

Giai đoạn: **Thiết kế**, kết thúc khi user chốt tài liệu này. Xóa mục này khi phát hành.

- [ ] User chốt tài liệu.
- Câu hỏi cho playground: authoring `StatProfile`/`EffectDefinition` trong Inspector có tiện không; overlay debug có nút cho mọi lệnh.
