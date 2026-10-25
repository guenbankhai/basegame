# Stats

Chỉ số có modifier (Stat), giá trị tiêu và hồi bị chặn bởi một Stat (Resource), và gói modifier có thời hạn (Effect), dùng chung cho nhân vật và NPC.

Tài liệu này mô tả thiết kế hiện hành của module. Thuật ngữ: `GLOSSARY.md` (mục Stats). Cách dùng nhanh: `README.md` của package. Trạng thái: `roadmap.md`.

| Mục | Giá trị |
|---|---|
| Package | `unity/Packages/com.guenbankhai.stats` |
| Namespace / asmdef | `GuenBanKhai.Stats`, `GuenBanKhai.Stats.Tests` (lõi); `GuenBanKhai.Stats.Effects`, `GuenBanKhai.Stats.Effects.Tests` (lớp Effects) |
| Phụ thuộc | Không có |
| Quyết định | [ADR 0005](../adr/0005-stats-resource-and-effects-in-stats.md) · [0006](../adr/0006-stats-time-agnostic-core.md) · [0007](../adr/0007-stats-save-base-values-only.md) · [0008](../adr/0008-stats-asset-name-as-id.md) |

Không dùng tên `Attribute`: trùng `System.Attribute`, file có `using System;` sẽ lỗi CS0104.

## Phạm vi

**Trong module:**
- Stat: giá trị gốc + modifier ba tầng, min/max, event khi đổi, gỡ modifier theo source.
- Resource: giá trị hiện tại trong `[Min, Max]`, max là một Stat; event khi cạn, khi đầy.
- Effect (lớp Effects): gói modifier có thời hạn hoặc vô hạn; luật áp lại chính nó và luật với effect khác cùng group.
- Snapshot để save cho cả hai lớp.

**Ngoài module:**
- Luật riêng của Health (sát thương, ngất) và Needs (ngưỡng gây ốm): các module đó dùng `Resource` và tự giữ luật.
- Cấp và XP của Skills.
- Nối thời gian với Calendar: việc của scenario (xem Quy ước).
- Chưa có ở bản đầu, thêm khi có nhu cầu thật: derived stat (game tự nối, xem Quy ước); Override (ép về 0 dùng `PercentMult` −1); Resource giữ tỉ lệ khi max đổi; effect tác động định kỳ hoặc có hook logic; stack `Independent` (mỗi lần áp một instance); nhãn phân loại effect (khác Effect group); icon và mô tả của stat, effect (thêm khi làm UI thật); batch public (game gom nhiều thay đổi modifier thành một lần tính; hiện chỉ `EffectCollection` batch nội bộ).

## Kiến trúc

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
| `StatCollection` | Lõi C# thuần, mỗi thực thể một cái: giữ `Stat` và `Resource`, snapshot, restore |
| `Stat`, `Resource` | Do collection tạo. `Stat`: base, modifier, giá trị cuối đã cache. `Resource`: current, min, Stat làm max |
| `Modifier` | Class bất biến: `Type`, `Value`, `Source` |
| `StatId`, `ResourceId` | Struct bọc chuỗi, so sánh ordinal; key khi save |
| `StatCollectionSettings`, `StatSettings`, `ResourceSettings` | Cấu hình C# thuần; collection kiểm tra và copy một lần |
| `StatDefinition`, `ResourceDefinition`, `StatProfile` | ScriptableObject authoring. Profile liệt kê stat và resource của một loại thực thể, có thể override base. `CreateSettings()` trả bản copy |
| `StatsBehaviour` | MonoBehaviour mỏng: tạo `StatCollection` từ `StatProfile` trong `Awake`; chạy trước (`DefaultExecutionOrder`) |
| `EffectCollection` | Lõi C# thuần của lớp Effects, mỗi thực thể một cái, giữ `StatCollection`: áp, gỡ, `Advance`, snapshot |
| `EffectInstance` | Một effect đang chạy; là `Source` của mọi modifier nó thêm |
| `EffectId`, `EffectSettings`, `EffectModifierSettings`, `EffectGroupSettings` | ID và cấu hình C# thuần của effect và group |
| `EffectDefinition`, `EffectGroupDefinition` | ScriptableObject authoring; effect kéo thả `StatDefinition` cho modifier và `EffectGroupDefinition` cho group |
| `EffectsBehaviour` | MonoBehaviour mỏng, `[RequireComponent(typeof(StatsBehaviour))]`: tạo `EffectCollection` trong `Awake`. Không tự gọi `Advance` (scenario gọi qua `Simulate`, xem Quy ước) |
| `StatsSnapshot`, `EffectsSnapshot` | Snapshot `[Serializable]` |

- Lấy tham chiếu: `[SerializeField] StatsBehaviour` → `.Collection` từ `Awake`/`OnEnable`, hoặc truyền qua constructor. Không singleton.
- Profile thiếu hoặc sai: behaviour log lỗi, tự tắt, `Collection` là `null`. `EffectsBehaviour` gặp `StatsBehaviour.Collection` là `null` cũng log lỗi và tự tắt.
- UI lấy tên hiển thị qua `Stat.DisplayName`, `Resource.DisplayName`, `EffectInstance.DisplayName`, không đọc definition.
- `Stats`, `Resources`, `Modifiers`, `Instances` là wrapper chỉ đọc, không phải `List` mà nơi gọi ép kiểu được.
- **ID của definition là tên asset** ([ADR 0008](../adr/0008-stats-asset-name-as-id.md)): `MoveSpeed.asset` → `"MoveSpeed"`. Tên PascalCase, không khoảng trắng, không tiền tố loại; phân loại bằng thư mục. Không `Instantiate` definition (tên thành `"MoveSpeed(Clone)"`).

## Mô hình

### Stat

```text
unclamped = (BaseValue + Σ Flat) × (1 + Σ PercentAdd) × Π (1 + PercentMult)
Value     = clamp(unclamped, Min, Max)    Min, Max mặc định −∞, +∞
```

- Phần trăm ghi dạng tỉ lệ (`0.1` = +10%). Các `PercentAdd` cộng với nhau rồi nhân một lần ("increased"); mỗi `PercentMult` nhân riêng ("more").
- Chỉ clamp giá trị cuối; base và modifier giữ số gốc. Min 0, base 5, `PercentAdd` −1.5 → 0; gỡ modifier → 5.
- Tính lại ngay khi đổi base hoặc modifier, rồi cache. Luôn tính lại từ đầu theo công thức, không cộng trừ dồn, để sai số float không tích lũy làm `!=` của `ValueChanged` sai.
- Base và modifier đều hữu hạn nhưng phép tính vẫn có thể tràn float (ra ∞, hoặc NaN khi ∞ × 0): coi là dữ liệu sai của game, không xử lý.
- Ví dụ: base 5, giày `Flat` +1, ốm `PercentAdd` −0.3, kiệt sức `PercentMult` −0.5 → `6 × 0.7 × 0.5 = 2.1`.

### Modifier và source

- `Source` là `object`, so sánh theo reference: món đồ, instance effect, hoặc token `new object()`.
- Một modifier thêm được vào nhiều stat, nhưng chỉ một lần mỗi stat. Muốn đổi giá trị thì `ReplaceModifier(old, new)`: tính lại và phát event một lần. Gỡ rồi thêm thì stat đổi hai lần, và Resource lấy stat đó làm max bị clamp theo giá trị ở giữa (xem Derived stat trong Quy ước).
- Gỡ: `RemoveModifier(m)`; `RemoveAllFromSource(src)` trên một stat hoặc trên cả collection (tháo một món đồ cộng nhiều stat).

### Resource

- Luôn `Min <= Current <= Max`, với `Min` là hằng số (mặc định 0) và `Max` = `max(MaxStat.Value, Min)`: stat làm max xuống dưới `Min` thì khoảng là `[Min, Min]`, UI không thấy max nhỏ hơn min.
- Max đổi thì current **giữ số tuyệt đối**, chỉ clamp khi vượt: 80/100 mặc áo +20 → 80/120; 110/120 tháo áo → 100/100. Đổi áo A (+20) sang áo B (+30) là tháo rồi mặc: 110/120 → 100/100 → 100/130. Đây là luật, không có batch public để tránh.
- Resource clamp theo max **trước** khi handler `ValueChanged` của Stat làm max chạy.
- `Normalized` = `(Current − Min) / (Max − Min)`, bằng 1 khi `Max == Min`.
- Giá trị ban đầu `Min + InitialFraction × (Max − Min)`, mặc định đầy, tính theo max lúc tạo. Tạo 100/100 rồi mặc áo +20 → 100/120; muốn đầy thì `Set(Max)`.
- Resource không có modifier; hồi và tiêu là việc của module dùng nó.
- Collection tạo mọi stat trong settings trước resource, nên resource trỏ được tới bất kỳ stat nào trong settings. Không có lệnh xóa stat hay resource.

### Effect

- Mỗi `EffectId` có tối đa một `EffectInstance` trong collection. Instance giữ bản copy settings lúc áp lần đầu, `Stacks`, `Remaining` (`double`, +∞ là vô hạn), `IsSuppressed`.
- Mỗi phần tử trong `Modifiers` của settings sinh đúng một `Modifier`, giá trị tính theo `Stacks` và `StackMode`.
- Instance có tác dụng thì có modifier trên Stat; bị suppress thì không. Đổi stack hay trạng thái thì thay modifier của instance trong một batch nội bộ: mỗi stat bị ảnh hưởng chỉ tính lại và phát `ValueChanged` tối đa một lần mỗi lệnh. Effect +20 `MaxHealth` đang 115/120 lên stack 2 → 115/140, không bị clamp về 100 ở giữa.
- Thời gian theo đơn vị do game quy ước (phút, giây, lượt), dùng chung cho `Duration` và `delta`.

**Áp lại chính nó** (cùng `EffectId`) theo `StackRule`:

| `StackRule` | Hành vi | Event |
|---|---|---|
| `Refresh` | `Remaining = Duration` | `Refreshed` |
| `Extend` | `Remaining = min(Remaining + Duration, MaxDuration)` | `Refreshed` |
| `Ignore` | Không đổi gì | Không có |
| `Stack` | Dưới `MaxStacks`: `Stacks + 1`, `Remaining = Duration`; đủ rồi: chỉ `Remaining = Duration`. Mọi stack chung một đồng hồ | `Stacked` / `Refreshed` |

Effect vô hạn: `Refresh`, `Extend` không đổi thời gian nhưng vẫn phát `Refreshed`.

`StackMode` (khi `StackRule = Stack`) quyết định `n` stack thành modifier thế nào. Chỉ khác nhau với `PercentMult`:

| `StackMode` | Modifier | Kiệt sức `PercentMult −0.2`, 3 stack |
|---|---|---|
| `Sum` (mặc định) | Một modifier `Value × n` | ×0.4 |
| `Repeat` | Như áp `n` lần riêng: `PercentMult` thành `(1 + Value)^n − 1`; loại khác vẫn `Value × n` | ×0.512 |

`Sum` có thể làm hệ số âm, nên settings phải có `Value × MaxStacks >= −1` với mọi `PercentMult`. Kiểm tra bằng đúng phép nhân `float` dùng để tính giá trị modifier, để settings qua kiểm tra thì `new Modifier` ở stack `MaxStacks` cũng không ném.

`MaxStacks` không có giới hạn trên: `Repeat` tính bằng lũy thừa, không lặp theo số stack. Số instance tối đa bằng số `EffectId` khác nhau; mọi vòng lặp của lõi chạy theo số stat, resource, modifier hay instance.

**Với effect khác cùng group:**
- `Group` là `EffectGroupSettings`, so sánh theo `Id`; `null` là chồng tự do.
- Trong một group chỉ effect có `Rank` (số nguyên trong settings) cao nhất có tác dụng. Bằng rank thì effect áp gần nhất thắng; "áp" tính cả `Refresh`/`Extend`/`Stack`, không tính `Ignore`.
- `Policy` khai trên group quyết định effect thua:

| `GroupPolicy` | Effect thua | Đang có Thuốc II (+30%, rank 2, còn 5 phút), uống Thuốc I (+20%, rank 1, 10 phút) |
|---|---|---|
| `Replace` (mặc định) | Áp vào mà thua: bị bỏ qua, `Apply` trả `null`. Đang chạy mà bị thắng: bị gỡ (`Replaced`) | Thuốc I bị bỏ qua; hết 5 phút thì hết tác dụng |
| `Suppress` | Vẫn chạy đồng hồ, không có modifier; có tác dụng lại khi effect thắng hết | Hết 5 phút, Thuốc I có tác dụng nốt 5 phút |

- Cùng rank: "Bánh mì" và "Cơm" cùng group `FoodBuff` rank 1; ăn món nào sau thì món đó có tác dụng.
- Effect bị suppress vẫn theo `StackRule` (thêm stack, làm mới thời hạn).
- Group chỉ có hiệu lực trong một `EffectCollection`; mỗi `StatCollection` chỉ nên có một `EffectCollection` (quy ước, không chặn bằng code).

### Advance

`Advance(delta)`:
1. Trừ `Remaining` của mọi effect có thời hạn, kể cả effect bị suppress.
2. Effect có `Remaining <= 0` hết hạn, xếp theo thời điểm hết hạn (bằng nhau thì theo thứ tự áp lần đầu).
3. Gỡ hết effect hết hạn, chọn lại effect có tác dụng trong các group bị ảnh hưởng.
4. Phát `Removed(…, Expired)` theo thứ tự bước 2, rồi `Resumed` theo thứ tự áp lần đầu.

Event còn chờ của instance mà handler đã gỡ hay đổi trạng thái thì bỏ qua. Effect áp trong handler không bị trừ `delta` của lần này.

`TimeUntilNextExpiry` là `Remaining` nhỏ nhất trong các effect có thời hạn, kể cả effect bị suppress; +∞ nếu không có. Dùng để chia `delta` tại mốc hết hạn (xem Quy ước).

## Event

```text
Stat.ValueChanged(float old, float new)
Resource.ValueChanged(float old, float new)
Resource.Depleted()                         vừa chạm Min từ trên xuống
Resource.Filled()                           vừa chạm Max từ dưới lên
StatCollection.Restored()

EffectCollection.Applied(EffectInstance)    áp lần đầu (có thể vào thẳng trạng thái suppress)
EffectCollection.Stacked / Refreshed(EffectInstance)
EffectCollection.Suppressed / Resumed(EffectInstance)
EffectCollection.Removed(EffectInstance, EffectRemovalReason)   Expired | Removed | Replaced
EffectCollection.Restored()
```

- Phát sau khi state đã cập nhật xong. Handler lỗi: bắt từng handler, `Debug.LogException`, gọi tiếp handler còn lại.
- `Stat.ValueChanged` chỉ phát khi giá trị cuối khác (`!=`): đang có `PercentMult` −1, thêm giày +1 vẫn là 0, không phát.
- Resource: `ValueChanged` rồi `Depleted`/`Filled`; ở sẵn ngưỡng thì không phát lại. Max giảm làm clamp cũng phát. Stat làm max xuống dưới `Min` kéo current về `Min`: chỉ phát `Depleted`, không phát `Filled` (current không đi lên).
- Mọi lệnh (`Stat.AddModifier`/`RemoveModifier`/`ReplaceModifier`/`SetBase`, `RemoveAllFromSource` của collection, mọi lệnh của `EffectCollection`) cập nhật xong modifier, giá trị mọi stat và current mọi resource rồi mới phát, theo thứ tự: `Stat.ValueChanged` của từng stat đổi (thứ tự đăng ký trong collection), event của Resource bị clamp, rồi event của effect. Tháo giáp +20 `MaxHealth` và +20 `MaxStamina`: handler của `MaxHealth` đã thấy `MaxStamina` mới. Handler derived stat ở bước đầu chạy lồng ngay, nên handler của effect thấy giá trị đã cập nhật.
- Một lệnh gây nhiều event của effect: effect thua trước, effect thắng sau. Ví dụ `Removed(old, Replaced)` hoặc `Suppressed(old)` rồi `Applied(new)`; `Removed(old)` rồi `Resumed(next)`. Effect đang bị suppress được áp lại và thành effect thắng (đang ăn Cơm, ăn lại Bánh mì cùng rank): `Suppressed(Cơm)`, `Refreshed(Bánh mì)` (hoặc `Stacked`), rồi `Resumed(Bánh mì)`.

**Lệnh trong handler:**
- Được phép, chạy lồng ngay (không xếp hàng như Calendar, vì Stats không có step và handler derived stat cần đọc ngay giá trị mới).
- `StatCollection` đếm độ sâu event lồng, `EffectCollection` dùng chung bộ đếm đó. Lệnh đổi state ở độ sâu `MaxEventDepth` (32) ném `InvalidOperationException`, không đổi gì; handler bên ngoài bắt và log, chuỗi dừng ở đó.
- `Advance` trong bất kỳ handler nào (độ sâu event > 0): ném `InvalidOperationException`. Ví dụ: effect hết hạn làm `Stat.ValueChanged`, handler gọi `Advance` lần nữa trong khi `Removed`/`Resumed` của lần ngoài chưa phát.
- `StatCollection.Restore` hay `EffectCollection.Restore` trong bất kỳ handler nào (độ sâu event > 0): ném `InvalidOperationException`, vì restore thay toàn bộ state trong khi event bên ngoài vẫn đang phát. Muốn load từ handler thì game hoãn sang frame sau.

## API và trường hợp biên

```csharp
// GuenBanKhai.Stats
public readonly struct StatId : IEquatable<StatId> { public StatId(string value); public string Value { get; } }
public readonly struct ResourceId : IEquatable<ResourceId> { public ResourceId(string value); public string Value { get; } }
public enum ModifierType { Flat, PercentAdd, PercentMult }

public sealed class Modifier
{
    public Modifier(ModifierType type, float value, object source);
    public ModifierType Type { get; }  public float Value { get; }  public object Source { get; }
}

public sealed class Stat
{
    public StatId Id { get; }  public string DisplayName { get; }  public float BaseValue { get; }  public float Value { get; }
    public float Min { get; }  public float Max { get; }  public IReadOnlyList<Modifier> Modifiers { get; }
    public event Action<float, float> ValueChanged;
    public void SetBase(float value);
    public bool AddModifier(Modifier modifier);   // false if already added
    public bool RemoveModifier(Modifier modifier);
    public bool ReplaceModifier(Modifier oldModifier, Modifier newModifier);   // one recalculation, one event
    public int RemoveAllFromSource(object source);
}

public sealed class Resource
{
    public ResourceId Id { get; }  public string DisplayName { get; }  public float Current { get; }  public float Min { get; }  public float Max { get; }
    public float Normalized { get; }  public Stat MaxStat { get; }
    public event Action<float, float> ValueChanged;  public event Action Depleted;  public event Action Filled;
    public float Add(float delta);   // returns the amount actually applied
    public void Set(float value);
}

public sealed class StatCollection
{
    public const int MaxEventDepth = 32;
    public StatCollection(StatCollectionSettings settings);
    public IReadOnlyList<Stat> Stats { get; }  public IReadOnlyList<Resource> Resources { get; }
    public Stat AddStat(StatSettings settings);  public Resource AddResource(ResourceSettings settings);
    public Stat GetStat(StatId id);  public bool TryGetStat(StatId id, out Stat stat);
    public Resource GetResource(ResourceId id);  public bool TryGetResource(ResourceId id, out Resource resource);
    public int RemoveAllFromSource(object source);
    public event Action Restored;
    public StatsSnapshot CaptureSnapshot();
    public IReadOnlyList<string> Restore(StatsSnapshot snapshot);   // returns skipped ids
}

// GuenBanKhai.Stats.Effects
public readonly struct EffectId : IEquatable<EffectId> { public EffectId(string value); public string Value { get; } }
public enum StackRule { Refresh, Extend, Ignore, Stack }
public enum StackMode { Sum, Repeat }
public enum GroupPolicy { Replace, Suppress }
public enum EffectRemovalReason { Expired, Removed, Replaced }

public sealed class EffectInstance
{
    public EffectId Id { get; }  public string DisplayName { get; }  public EffectSettings Settings { get; }   // a new copy on every get
    public int Stacks { get; }  public double Remaining { get; }  public bool IsInfinite { get; }  public bool IsSuppressed { get; }
}

public sealed class EffectCollection
{
    public EffectCollection(StatCollection stats);
    public IReadOnlyList<EffectInstance> Instances { get; }   // suppressed included, first-apply order
    public double TimeUntilNextExpiry { get; }   // +∞ if no timed effect
    public event Action<EffectInstance> Applied, Stacked, Refreshed, Suppressed, Resumed;
    public event Action<EffectInstance, EffectRemovalReason> Removed;
    public event Action Restored;
    public EffectInstance Apply(EffectSettings settings);   // null if rejected by a Replace group
    public bool Remove(EffectId id);
    public bool Has(EffectId id);  public bool TryGet(EffectId id, out EffectInstance instance);
    public void Advance(double delta);
    public EffectsSnapshot CaptureSnapshot();
    public IReadOnlyList<EffectId> Restore(EffectsSnapshot snapshot, Func<EffectId, EffectSettings> resolve);   // returns skipped ids
}
```

ID có so sánh bằng và `ToString`. Settings và snapshot có constructor và setter public để dựng không cần asset.

Lỗi theo mục "API của module" trong `coding-standard.md`: "báo lỗi" là `ArgumentException`; NaN và vô hạn ở mọi đầu vào số đều báo lỗi, trừ khi ghi khác; lệnh bị từ chối không đổi state.

| Lệnh | Quy tắc |
|---|---|
| ID | Chuỗi null, rỗng, chỉ khoảng trắng, hoặc `default` báo lỗi |
| `new Modifier(t, v, src)` | `src == null` báo lỗi; `PercentMult` với `v < −1` báo lỗi |
| `SetBase(v)` | Đặt tự do, kể cả ngoài `[Min, Max]` |
| `AddModifier(m)`, `RemoveModifier(m)` | `m == null` báo lỗi; đã có / không có thì trả `false` |
| `ReplaceModifier(old, new)` | `old` hoặc `new` là `null` báo lỗi; `old` không có trên stat, hoặc `new` đã có (kể cả `old == new`): trả `false`, không đổi gì. Còn lại: thay đúng vị trí của `old`, tính lại một lần, phát `ValueChanged` nếu giá trị cuối khác. `new` không bắt buộc cùng `Type` hay `Source` |
| `RemoveAllFromSource(src)` | `src == null` báo lỗi; trả số modifier đã gỡ. Bản collection: theo thứ tự đăng ký, mỗi stat phát event một lần |
| `Resource.Add(d)` | Clamp, trả lượng thực sự áp dụng (đang 5, `Add(−20)` trả −5); `d == 0` không làm gì |
| `Resource.Set(v)` | Clamp rồi đặt |
| `GetStat`, `GetResource` | ID chưa đăng ký báo lỗi; `TryGet*` trả `false` |
| `StatCollection(settings)` | `settings`, `Stats` hay `Resources` là `null`, hoặc có phần tử `null`: báo lỗi. Từng phần tử theo luật của `AddStat`/`AddResource` (stat tạo trước resource); một phần tử sai thì không tạo collection |
| `AddStat`, `AddResource` | Settings sai, trùng ID, hoặc resource trỏ stat chưa có: báo lỗi |
| `EffectCollection(stats)` | `stats == null` báo lỗi |
| `Apply(s)` | `s == null`, settings sai, hoặc modifier trỏ stat lạ: báo lỗi, không áp phần nào. Group cùng `Id` khác `Policy` với effect đang có (chỉ xảy ra khi dựng bằng code): báo lỗi. Đang chạy: theo `StackRule`, settings mới khác nội dung bị bỏ qua. Thua trong group `Replace`: trả `null` |
| `Remove(id)` | Gỡ, phát `Removed(…, Removed)`, trả `true`; effect kế tiếp trong group `Suppress` có tác dụng lại. Không có thì trả `false` |
| `Advance(d)` | `d < 0` báo lỗi; `d == 0` không làm gì |
| `Advance` trong handler | Ném `InvalidOperationException`, không đổi gì |
| `StatCollection.Restore(s)` | Dữ liệu hỏng, lệch settings, vắng mặt: theo mục Save |
| `EffectCollection.Restore(s, resolve)` | Dữ liệu hỏng, lệch settings: theo mục Save |
| `Restore` (cả hai collection) | Gọi trong handler: ném `InvalidOperationException`, không đổi gì |

### Settings

| Trường | Ràng buộc |
|---|---|
| `StatSettings` | `Id` hợp lệ; `BaseValue` hữu hạn; `Min`, `Max` không NaN, mặc định −∞ / +∞, `Min <= Max` |
| `ResourceSettings` | `Id` hợp lệ; `MaxStat` là stat đã có; `Min` hữu hạn, mặc định 0; `InitialFraction` trong `[0, 1]`, mặc định 1 |
| `DisplayName` (stat, resource, effect) | Tùy ý; `null`, rỗng hay chỉ khoảng trắng thì lấy `Id.Value` |
| `EffectSettings.Id` | ID hợp lệ |
| `Duration` | `> 0`; +∞ (mặc định) là vô hạn |
| `MaxDuration` | `>= Duration`, mặc định +∞; dùng khi `Extend` |
| `StackRule`, `StackMode` | Enum hợp lệ; `StackMode` mặc định `Sum` |
| `MaxStacks` | `>= 1`; dùng khi `Stack` |
| `Group`, `Rank` | `Group` `null` hoặc hợp lệ; `Rank` số nguyên, mặc định 0 |
| `EffectGroupSettings` | `Id` không null, rỗng hay chỉ khoảng trắng; `Policy` enum hợp lệ, mặc định `Replace` |
| `Modifiers` | Không null, có thể rỗng (effect chỉ làm nhãn); mỗi phần tử có `Stat` hợp lệ và `Value` theo luật `Modifier`; ràng buộc `Sum` ở trên |

`OnValidate` của ScriptableObject log warning: tên asset có khoảng trắng, `Min > Max`, profile có ID trùng (hay gặp: hai asset cùng tên ở hai thư mục), ràng buộc `Sum`.

## Save

Snapshot là class `[Serializable]` (mảng entry, dùng được với `JsonUtility`); `CaptureSnapshot()` luôn trả object và mảng mới. Là class chứ không phải struct như Calendar, vì struct chứa mảng khi copy vẫn dùng chung mảng.

Theo `coding-standard.md`: dữ liệu hỏng thì từ chối cả snapshot; dữ liệu chỉ lệch settings hiện tại thì sửa cho khớp hoặc bỏ entry và trả về trong danh sách bị bỏ.

**`StatsSnapshot`**: base của stat nào khác base trong settings, current của mọi resource. Không lưu modifier ([ADR 0007](../adr/0007-stats-save-base-values-only.md)).
- "Base trong settings" là giá trị sau override của profile, hoặc `StatSettings` truyền vào `AddStat`; so sánh bằng `!=`. Không lưu base trùng settings, để chỉnh cân bằng trong profile có tác dụng với save cũ. Người chơi đổi base đúng bằng mặc định cũ thì khi load nhận mặc định mới.
- Dữ liệu hỏng, từ chối: mảng `null`, entry `null`, ID sai (null, rỗng, chỉ khoảng trắng), ID trùng, base hay current là NaN hoặc vô hạn.
- ID lạ: bỏ qua, trả về. Stat có trong collection mà không có trong snapshot: đặt về base trong settings. Resource có trong collection mà không có trong snapshot (save cũ trước khi thêm Thirst): đặt về giá trị ban đầu (`InitialFraction`) theo max sau bước 1, để load vào thực thể mới hay đang chơi cũng ra cùng kết quả. Current ngoài `[Min, Max]`: clamp ở bước 2.
- `Restore` chạy ba bước:
  1. Đặt mọi base (stat không có trong snapshot về base trong settings), phát `Stat.ValueChanged` cho stat đổi giá trị, để handler derived stat cập nhật max.
  2. Đặt mọi current (resource vắng mặt về giá trị ban đầu), clamp theo max cuối.
  3. Phát `StatCollection.Restored`.
- Trong suốt `Restore`, Resource không phát event nào. Clamp ở bước 1 chỉ là trạng thái tạm, bước 2 ghi đè.
- Bước 1 khác quy tắc "chỉ phát `Restored`", vì derived stat phụ thuộc chính base được restore nên không restore trước được. Không phát thì save có Vitality 20, máu 140/150 sẽ load ra 100/150.

**`EffectsSnapshot`**: mọi effect (kể cả bị suppress) theo thứ tự áp lần đầu: `EffectId`, `Stacks`, `Remaining` (vô hạn lưu bằng cờ `IsInfinite`), số thứ tự lần áp gần nhất. Không lưu settings (tra qua `resolve`) và `IsSuppressed` (tính lại từ group).
- Dữ liệu hỏng, từ chối: `resolve == null`, `Stacks < 1`, `Remaining <= 0`, NaN hay +∞ với effect có thời hạn (+∞ không bị kẹp khi `MaxDuration` = +∞, effect sẽ không bao giờ hết hạn), `EffectId` trùng, số thứ tự không hợp lệ.
- Bỏ effect, trả về: `resolve` trả `null`; modifier trỏ tới stat không có trong collection.
- Sửa cho khớp: `Stacks` kẹp về `MaxStacks`; `Remaining` kẹp về `MaxDuration` nếu `StackRule = Extend`, còn lại về `Duration` (mức tối đa effect đạt được khi chơi); nhiều effect cùng group `Replace` thì giữ effect thắng; settings đổi từ có thời hạn sang vô hạn (hoặc ngược lại) thì theo settings mới (`Remaining = Duration`).
- Gỡ effect đang chạy, áp lại theo snapshot trong một batch, chỉ phát `Restored`. Stat vẫn phát `ValueChanged` do modifier đổi, mỗi stat tối đa một lần.
- Bộ đếm lần áp đi tiếp từ số lớn nhất trong snapshot, để effect áp sau khi load luôn được coi là áp gần nhất.

**Thứ tự khi load** (bắt buộc):
1. Dựng lại stat thêm lúc chạy (`AddStat`), do module đã thêm chúng làm.
2. Nguồn modifier restore: `EffectCollection.Restore`, Equipment…
3. `StatCollection.Restore` sau cùng.

Ở bước 2, modifier đổi làm Resource clamp và phát event với giá trị tạm (hiếm hơn: `Depleted` khi max ≤ min); bước 3 đặt lại current. Module phản ứng với event của Resource mà nhạy cảm (Health: ngất) thì bỏ qua các event này cho tới khi nhận `StatCollection.Restored`.

## Quy ước cho module dùng Stats

- **Health, Needs:** mỗi giá trị tiêu và hồi là một `Resource`; nghe `Depleted` cho chạm đáy (ngất, ốm).
- **Tham số là Stat:** tham số mà effect hay trang bị có thể đổi (tốc độ giảm đói, tốc độ hồi máu, kháng) là Stat, không phải hằng số trong module. "Ốm: đói nhanh gấp đôi" chỉ là effect `PercentAdd +1` lên `HungerRate`.
- **Thời gian:** scenario có **một** hàm `Simulate(delta)`, gọi từ cả `Stepped` lẫn `Skipped` của Calendar; không gọi `Advance` của Effects hay Needs thẳng từ event. Một hàm cho cả hai đường thì thứ tự gọi không phụ thuộc thứ tự đăng ký handler.
- **Chia đoạn:** effect đổi tham số của module khác (ốm làm `HungerRate` ×2) hết hạn, hay ngưỡng của module đó gây ra effect mới, ở giữa một khoảng thì gọi một lần cho cả khoảng sẽ sai. `Simulate` lặp tới hết `delta`, mỗi đoạn `d = min(phần còn lại, TimeUntilNextExpiry, thời gian tới ngưỡng kế tiếp của module dùng tham số)`, kể cả khi chạy thường (mốc hết hạn không rơi vào step):
  1. Module dùng tham số (Needs) `Advance(d)`; tham số không đổi trong cả đoạn.
  2. `EffectCollection.Advance(d)`; effect hết hạn đúng ở cuối đoạn.
  3. Áp các effect mà ngưỡng ở bước 1 gây ra (đã hoãn), đủ thời hạn, vì chúng bắt đầu ở cuối đoạn.
- **Yêu cầu cho module dùng tham số:** có thời gian tới ngưỡng kế tiếp, luôn `> 0` (đang ở ngưỡng thì tính ngưỡng sau, không có thì +∞), để mỗi đoạn `d > 0` và vòng lặp dừng; không tự `Apply` effect trong lúc `Advance`, mà báo ra để `Simulate` áp ở bước 3. Code cụ thể quyết khi làm Needs và scenario.
- Ví dụ, step 10, đói giảm 1/phút, Ốm (`HungerRate` ×2, Duration 45):
  - Chạy thường, Ốm áp lúc 0: step 40→50 bị cắt thành 5 + 5, nên ×2 đúng 45 phút. Skip 0→60: đoạn 45 (×2), đoạn 15 (×1). Hai đường cùng tiêu 105 đói.
  - Còn 40 đói, ngủ skip 0→80, Ốm Duration 60: đoạn 40 (tới ngưỡng) → đói cạn, `Advance(40)`, rồi áp Ốm còn 60; đoạn 40 → Ốm còn 20 lúc thức dậy, giống chạy thường. Áp Ốm ngay trong handler `Depleted` thì `Advance(80)` trừ cả 80 và người chơi dậy không ốm.
- **Source:** mỗi nguồn dùng object riêng làm source và tự `RemoveAllFromSource(this)` khi hết hiệu lực, kể cả trong `OnDisable`/`OnDestroy`.
- **Stat thêm lúc chạy:** module nào `AddStat` thì tự dựng lại trước khi restore (xem Save).
- **Đổi trang bị:** gỡ rồi thêm nên current bị clamp ở giữa (110/120 → 100/130), còn effect thay effect trong group `Replace` thì không (110/120 → 110/130) vì có batch nội bộ. Module Equipment cần đổi không mất máu là lý do để thêm batch public.
- **Derived stat:** nối bằng một modifier flat có source riêng, đổi bằng `ReplaceModifier`. Vẫn đúng khi load nhờ bước 1 của `Restore`. Vitality 20 → 21 với máu 140/150 → 140/155; gỡ rồi thêm sẽ clamp máu về 50 ở giữa.

```csharp
// MaxHealth = 50 + Vitality * 5; other MaxHealth modifiers keep stacking normally
var fromVitality = new object();
var current = new Modifier(ModifierType.Flat, vitality.Value * 5f, fromVitality);
maxHealth.AddModifier(current);
vitality.ValueChanged += (_, value) =>
{
    // Remove-then-add would clamp health against the max without this modifier
    var next = new Modifier(ModifierType.Flat, value * 5f, fromVitality);
    maxHealth.ReplaceModifier(current, next);
    current = next;
};
```

## Kiểm thử

Test EditMode, mỗi file một nhóm quy tắc; mỗi dòng của bảng trường hợp biên có test.

| File | Phủ |
|---|---|
| `StatIdTests` | ID hợp lệ và sai, ordinal, `default` |
| `StatFormulaTests` | Ba tầng, ví dụ trong tài liệu, clamp giá trị cuối, base ngoài khoảng |
| `StatModifierTests` | Thêm, gỡ, thêm trùng, gỡ theo source (stat và collection), `ReplaceModifier` (vị trí, một event, các trường hợp trả `false`), event chỉ khi giá trị đổi, `RemoveAllFromSource` của collection cập nhật mọi stat rồi mới phát |
| `ResourceTests` | `Add`/`Set`, clamp, max đổi giữ số tuyệt đối, đổi áo A sang B, stat làm max dưới `Min` (`Max` bằng `Min`, chỉ `Depleted`), `Normalized`, `InitialFraction`, thứ tự event; **derived stat qua `ReplaceModifier`: Vitality đổi, máu giữ 140** |
| `StatCollectionTests` | Settings (null, phần tử null, trùng ID), copy, `DisplayName` (mặc định là ID), `Get`/`TryGet`, wrapper chỉ đọc, lệnh và exception trong handler, `MaxEventDepth` |
| `StatsSnapshotTests` | `JsonUtility`, chỉ lưu base khác settings, stat vắng mặt về base trong settings, resource vắng mặt về giá trị ban đầu (thực thể mới và đang chơi cùng kết quả), ba bước restore, Resource im lặng khi restore, **derived stat giữ đúng máu 140/150**, ID lạ, dữ liệu hỏng (từng loại), `Restore` trong handler |
| `StatDefinitionTests` | Definition và profile: mặc định, ID là tên asset, bản copy, override base, `DisplayName` |
| `Effects/EffectApplyTests` | Bốn `StackRule`, `MaxStacks`, `MaxDuration`, `Sum`/`Repeat` (mỗi modifier settings đúng một `Modifier`), ràng buộc `Sum`, vô hạn, stat lạ, stack lên `MaxHealth` không mất current, `Settings` trả bản copy mới |
| `Effects/EffectGroupTests` | `Replace`/`Suppress` (thuốc tốc độ), bằng rank (bánh mì, cơm; áp lại effect bị suppress), group trùng `Id` khác `Policy`, suppress vẫn stack, thứ tự event (Stat → Resource → Effect; thua trước thắng sau) |
| `Effects/EffectAdvanceTests` | Hết hạn và thứ tự, resume, `TimeUntilNextExpiry` (kể cả suppress, +∞), `delta` sai, handler áp/gỡ (event chờ bị bỏ), `Advance` trong handler (của effect và của Stat), vòng lặp chỉ gồm event effect; **`Advance(10)` một lần và `Advance(1)` mười lần ra cùng snapshot và giá trị stat** |
| `Effects/EffectsSnapshotTests` | `JsonUtility` với vô hạn, suppress tính lại, dữ liệu hỏng (kể cả +∞ với effect có thời hạn), bỏ effect (resolve `null`, stat lạ), kẹp theo settings (`Remaining` theo `StackRule`), `Restore` trong handler, bộ đếm lần áp đi tiếp sau load (effect cùng group, cùng rank áp sau khi load thắng effect có trong save); **restore effects rồi stats giữ đúng current** |
| `Effects/EffectDefinitionTests` | Effect và group definition: mặc định, ID là tên asset, bản copy, `DisplayName` |
| `Effects/EffectsAssemblyTests` | `EffectCollection` nằm trong assembly `GuenBanKhai.Stats.Effects` (bắt lỗi lỡ xóa asmdef con) |

Thử bằng tay: experiment `Assets/Playground/Stats/`, scene `Assets/Scenes/Stats.unity` (tạo ở giai đoạn Playground).

## Kế hoạch (tạm)

Giai đoạn: **Thiết kế**, kết thúc khi user chốt tài liệu này.

- [ ] User chốt tài liệu.
- [ ] Playground trả lời: authoring `StatProfile`/`EffectDefinition` trong Inspector có tiện không; overlay debug có nút cho mọi lệnh.
