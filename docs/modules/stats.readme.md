# GuenBanKhai Stats

<!--
Cách dùng:
- Giai đoạn Thiết kế: tạo file này thành `docs/modules/<module>.readme.md`. Giai đoạn Package: di chuyển nguyên file thành `README.md` của package, điền Cài đặt và Thiết lập.
- Tự đứng một mình: không trỏ tới `docs/`, `GLOSSARY.md`, ADR hay file nào ngoài package. Người chỉ có package vẫn dùng đúng.
- Đây là nơi duy nhất giữ luật hành vi của module. Tài liệu thiết kế không viết lại.
- Mục không có gì để viết thì ghi "Không có", không xóa.
- Luật mặc định của `coding-standard.md` không cần viết lại, trừ khi người dùng package cần biết (ví dụ lỗi ném loại gì).
- [Tn] là cổng thiết kế trong review.md mà mục đó phải đạt. Xóa khối comment này khi chuyển vào package.
-->

Chỉ số có modifier (Stat), giá trị tiêu và hồi bị chặn bởi một Stat (Resource), và gói modifier có thời hạn (Effect), dùng chung cho nhân vật và NPC.

| Mục | Giá trị |
|---|---|
| Package | `com.guenbankhai.stats` |
| Version | Chưa phát hành (thay đổi giữa các version: `CHANGELOG.md`) |
| Namespace | `GuenBanKhai.Stats` (lõi), `GuenBanKhai.Stats.Effects` (lớp Effects) |
| Unity | 6000.6 trở lên |
| Phụ thuộc | Không có |

## Cài đặt

Điền ở giai đoạn Package.

## Khái niệm

| Thuật ngữ | Nghĩa |
|---|---|
| **Stat** | Chỉ số có giá trị tính từ base value và các modifier, như tốc độ chạy hay máu tối đa |
| **Base value** | Giá trị gốc của stat trước khi áp modifier |
| **Modifier** | Một thay đổi gắn vào stat, mang source của nó; gỡ modifier thì stat trở về như cũ. Loại: `Flat`, `PercentAdd`, `PercentMult` |
| **Source** | Thứ đã thêm modifier (món đồ, effect, token); dùng để gỡ mọi modifier của nó một lần |
| **Resource** | Giá trị bị tiêu và hồi trực tiếp, luôn nằm giữa một min cố định và một max là stat, như máu hay độ đói |
| **Depleted** / **Filled** | Resource vừa chạm min từ trên xuống / vừa chạm max từ dưới lên |
| **Effect** | Gói modifier áp lên thực thể trong một thời hạn, hoặc vô hạn tới khi bị gỡ, như "Ăn sáng no" hay "Ốm" |
| **Stack**, **Stack rule** | Số lần effect chồng lên chính nó; luật khi áp lại effect đang chạy |
| **Effect group**, **Rank** | Tập effect loại trừ nhau; mỗi lúc chỉ effect có rank cao nhất trong group có tác dụng |
| **Group policy** | Luật cho effect thua trong group: bị gỡ (`Replace`) hay bị suppress (`Suppress`) |
| **Suppressed** | Effect vẫn chạy thời hạn nhưng không có tác dụng vì thua trong group |
| **Derived stat** | Stat phụ thuộc stat khác, như máu tối đa tăng theo thể lực; game tự nối (xem Viết module dùng Stats) |

## Thành phần

Hai assembly: lõi không biết Effects; game không dùng effect thì không reference `GuenBanKhai.Stats.Effects`.

| Type | Vai trò |
|---|---|
| `StatCollection` | Lõi, mỗi thực thể một cái: giữ `Stat` và `Resource`, snapshot, restore |
| `Stat`, `Resource` | Do collection tạo; thay đổi đi qua lệnh của chúng |
| `Modifier` | Class bất biến: `Type`, `Value`, `Source` |
| `StatId`, `ResourceId`, `EffectId` | Struct bọc chuỗi, so sánh ordinal; key khi save |
| `StatCollectionSettings`, `StatSettings`, `ResourceSettings` | Cấu hình C# thuần |
| `StatDefinition`, `ResourceDefinition`, `StatProfile` | ScriptableObject authoring. Profile liệt kê stat và resource của một loại thực thể, có thể override base |
| `StatsBehaviour` | MonoBehaviour tạo `StatCollection` từ `StatProfile` trong `Awake`, chạy trước component khác |
| `EffectCollection` | Lõi của lớp Effects, mỗi thực thể một cái, giữ `StatCollection`: áp, gỡ, `Advance`, snapshot |
| `EffectInstance` | Một effect đang chạy; là `Source` của mọi modifier nó thêm |
| `EffectSettings`, `EffectModifierSettings`, `EffectGroupSettings` | Cấu hình C# thuần của effect và group |
| `EffectDefinition`, `EffectGroupDefinition` | ScriptableObject authoring; effect kéo thả `StatDefinition` cho modifier và `EffectGroupDefinition` cho group |
| `EffectsBehaviour` | MonoBehaviour cần `StatsBehaviour` cùng GameObject: tạo `EffectCollection` trong `Awake`, chạy ngay sau `StatsBehaviour`. Không tự gọi `Advance` |
| `StatsSnapshot`, `EffectsSnapshot` | Snapshot `[Serializable]`, dùng được với `JsonUtility` |

## Thiết lập

Điền ở giai đoạn Package. Điểm chính:

- Lấy tham chiếu: `[SerializeField] StatsBehaviour` (hoặc `EffectsBehaviour`) → `.Collection` từ `Awake`/`OnEnable`, hoặc truyền qua constructor. Không có singleton.
- Profile thiếu hoặc sai: `StatsBehaviour` log lỗi, tự tắt, `Collection` là `null`. `EffectsBehaviour` gặp `StatsBehaviour.Collection` là `null` cũng log lỗi và tự tắt.
- **ID của definition là tên asset**: `MoveSpeed.asset` → `"MoveSpeed"`. Tên PascalCase, không khoảng trắng, không tiền tố loại; phân loại bằng thư mục. Không `Instantiate` definition (tên thành `"MoveSpeed(Clone)"`). Đổi tên asset là đổi ID.
- UI lấy tên hiển thị qua `Stat.DisplayName`, `Resource.DisplayName`, `EffectInstance.DisplayName`, không đọc definition.
- Settings và snapshot có constructor và setter public để dựng không cần asset.

### Settings

| Trường | Mặc định | Ràng buộc |
|---|---|---|
| `StatCollectionSettings.Stats`, `Resources` | Rỗng | Không null, không có phần tử null |
| `StatSettings.Id` | — | ID hợp lệ |
| `StatSettings.BaseValue` | — | Hữu hạn |
| `StatSettings.Min`, `Max` | −∞, +∞ | Không NaN, `Min <= Max` |
| `ResourceSettings.Id` | — | ID hợp lệ |
| `ResourceSettings.MaxStat` | — | Stat có trong cùng `StatCollectionSettings` |
| `ResourceSettings.Min` | 0 | Hữu hạn |
| `ResourceSettings.InitialFraction` | 1 | Trong `[0, 1]` |
| `DisplayName` (stat, resource, effect) | `Id.Value` | Tùy ý; `null`, rỗng hay chỉ khoảng trắng thì lấy `Id.Value` |
| `EffectSettings.Id` | — | ID hợp lệ |
| `Duration` | +∞ (vô hạn) | `> 0` |
| `MaxDuration` | +∞ | `>= Duration`; dùng khi `Extend` |
| `StackRule` | — | Enum hợp lệ |
| `MaxStacks` | 1 | `>= 1`; dùng khi `Stack` |
| `Group` | `null` (chồng tự do) | `null` hoặc hợp lệ |
| `Rank` | 0 | Số nguyên |
| `EffectGroupSettings.Id` | — | Không null, rỗng hay chỉ khoảng trắng |
| `EffectGroupSettings.Policy` | `Replace` | Enum hợp lệ |
| `Modifiers` | — | Không null, có thể rỗng (effect chỉ làm nhãn); mỗi phần tử có `Stat` hợp lệ, `Type` hợp lệ và `Value` theo luật `Modifier`; ràng buộc stack: `Value × MaxStacks >= −1` với mọi `PercentMult`, kiểm bằng đúng phép nhân `float` dùng để tính modifier, nên settings qua kiểm tra thì stack `MaxStacks` không ném |

Settings sai: `ArgumentException`, không tạo gì. `OnValidate` của ScriptableObject log warning khi: tên asset có khoảng trắng, `Min > Max`, profile có ID trùng (hay gặp: hai asset cùng tên ở hai thư mục), vi phạm ràng buộc stack.

## API [T4]

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
    public Stat GetStat(StatId id);  public bool TryGetStat(StatId id, out Stat stat);
    public Resource GetResource(ResourceId id);  public bool TryGetResource(ResourceId id, out Resource resource);
    public int RemoveAllFromSource(object source);
    public event Action Restored;
    public StatsSnapshot CaptureSnapshot();
    public void Restore(StatsSnapshot snapshot);
}

// GuenBanKhai.Stats.Effects
public readonly struct EffectId : IEquatable<EffectId> { public EffectId(string value); public string Value { get; } }
public enum StackRule { Refresh, Extend, Ignore, Stack }
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
    public void Restore(EffectsSnapshot snapshot, Func<EffectId, EffectSettings> resolve);
}
```

ID có so sánh bằng và `ToString`. `Stats`, `Resources`, `Modifiers`, `Instances` là wrapper chỉ đọc, không ép kiểu về `List` được.

Các type khác (constructor và setter public, để code và test dựng không cần asset; field và ràng buộc: mục Settings):
- `StatCollectionSettings`: `List<StatSettings> Stats`, `List<ResourceSettings> Resources`.
- `StatSettings(StatId id, float baseValue)`: `Id`, `DisplayName`, `BaseValue`, `Min`, `Max`.
- `ResourceSettings(ResourceId id, StatId maxStat)`: `Id`, `DisplayName`, `MaxStat`, `Min`, `InitialFraction`.
- `EffectSettings(EffectId id, StackRule stackRule)`: `Id`, `DisplayName`, `double Duration`, `double MaxDuration`, `StackRule`, `int MaxStacks`, `EffectGroupSettings Group`, `int Rank`, `List<EffectModifierSettings> Modifiers`.
- `EffectModifierSettings(StatId stat, ModifierType type, float value)`: `Stat`, `Type`, `Value`.
- `EffectGroupSettings(string id, GroupPolicy policy)`: `Id`, `Policy`.
- `StatDefinition.CreateSettings()` → `StatSettings`; `ResourceDefinition.CreateSettings()` → `ResourceSettings`; `StatProfile.CreateSettings()` → `StatCollectionSettings` (đã gồm override base); `EffectDefinition.CreateSettings()` → `EffectSettings`; `EffectGroupDefinition.CreateSettings()` → `EffectGroupSettings`.
- `StatsBehaviour.Collection` (`StatCollection`), `EffectsBehaviour.Collection` (`EffectCollection`): `null` khi component tự tắt vì config sai.
- `StatsSnapshot`: `StatEntry[] Stats` (`string Id`, `float BaseValue`), `ResourceEntry[] Resources` (`string Id`, `float Current`).
- `EffectsSnapshot`: `EffectEntry[] Effects` (`string Id`, `int Stacks`, `double Remaining`, `bool IsInfinite`, `int LastApplySequence`).

## Event [T5]

| Event | Khi nào |
|---|---|
| `Stat.ValueChanged(old, new)` | Giá trị cuối đổi (`!=`). Đang có `PercentMult` −1, thêm `Flat` +1 vẫn là 0: không phát |
| `Resource.ValueChanged(old, new)` | Current đổi, kể cả do max giảm làm clamp |
| `Resource.Depleted()` | Current vừa chạm `Min` từ trên xuống |
| `Resource.Filled()` | Current tăng lên chạm `Max`. Max giảm xuống bằng hay dưới current (current đứng yên hoặc bị kéo xuống) không phát |
| `StatCollection.Restored()` | Cuối `Restore` |
| `EffectCollection.Applied(e)` | Áp lần đầu (có thể vào thẳng trạng thái suppress) |
| `EffectCollection.Stacked(e)` / `Refreshed(e)` | Áp lại theo `StackRule` |
| `EffectCollection.Suppressed(e)` / `Resumed(e)` | Mất / có lại tác dụng do group |
| `EffectCollection.Removed(e, reason)` | `Expired`, `Removed` (gọi `Remove`), `Replaced` (thua trong group `Replace`) |
| `EffectCollection.Restored()` | Cuối `Restore` |

- Mọi lệnh cập nhật xong modifier, giá trị mọi stat và current mọi resource rồi mới phát, theo thứ tự: `Stat.ValueChanged` của từng stat đổi (thứ tự đăng ký trong collection), event của Resource bị đổi, rồi event của effect. Tháo giáp +20 `MaxHealth` và +20 `MaxStamina`: handler của `MaxHealth` đã thấy `MaxStamina` mới.
- Resource: `ValueChanged` rồi `Depleted`/`Filled`; ở sẵn ngưỡng thì không phát lại. Stat làm max xuống dưới `Min` kéo current về `Min`: phát `ValueChanged` và `Depleted`.
- Nhiều event của effect trong một lệnh: effect thua trước, effect thắng sau. Ví dụ `Removed(old, Replaced)` hoặc `Suppressed(old)` rồi `Applied(new)`; `Removed(old)` rồi `Resumed(next)`. Group `Suppress`, đang ăn Cơm (Bánh mì bị suppress), ăn lại Bánh mì cùng rank: `Suppressed(Cơm)`, `Refreshed(Bánh mì)` (hoặc `Stacked`), rồi `Resumed(Bánh mì)`.
- Handler ném exception: lỗi được log, các handler còn lại vẫn chạy.

**Gọi lệnh trong handler:**
- Được phép, chạy lồng ngay (không xếp hàng), để handler derived stat đọc ngay giá trị mới. Handler derived stat ở bước `Stat.ValueChanged` chạy lồng ngay, nên handler của effect thấy giá trị đã cập nhật.
- Event còn chờ (Stat, Resource) của giá trị mà handler lồng đã đổi tiếp thì bỏ qua; event lồng mang giá trị mới nhất, và `old` là giá trị listener nhận gần nhất (không phải giá trị tạm của event bị bỏ); sau khi gộp mà `old == new` thì không phát. Ví dụ ở mục Giới hạn của derived stat.
- `StatCollection` đếm độ sâu event lồng, `EffectCollection` dùng chung bộ đếm đó. Lệnh đổi state ở độ sâu `MaxEventDepth` ném `InvalidOperationException`, không đổi gì; handler bên ngoài bắt và log, chuỗi dừng ở đó.
- `Advance` trong handler của Stats hay Effects (cùng bộ đếm độ sâu): ném `InvalidOperationException`. Ví dụ: effect hết hạn làm `Stat.ValueChanged`, handler gọi `Advance` lần nữa trong khi `Removed`/`Resumed` của lần ngoài chưa phát.
- `Restore` (cả hai collection) trong handler của Stats hay Effects: ném `InvalidOperationException`, vì restore thay toàn bộ state trong khi event bên ngoài vẫn đang phát. Muốn restore từ handler thì hoãn sang frame sau.

## Điều khiển [T4]

| Lệnh | Quy tắc |
|---|---|
| Chung | Đầu vào sai (null, NaN, vô hạn, ngoài khoảng, ID không có, ID trùng) ném `ArgumentException`; gọi trong handler bị cấm hoặc ở độ sâu `MaxEventDepth` ném `InvalidOperationException`. Lệnh bị từ chối không đổi state |
| `new StatId`/`ResourceId`/`EffectId` | Chuỗi null, rỗng, chỉ khoảng trắng: lỗi. ID `default` truyền vào lệnh: lỗi |
| `new Modifier(t, v, src)` | `PercentMult` với `v < −1`: lỗi |
| `SetBase(v)` | Đặt tự do, kể cả ngoài `[Min, Max]` |
| `AddModifier(m)`, `RemoveModifier(m)` | Đã có / không có thì trả `false` |
| `ReplaceModifier(old, new)` | `old` không có trên stat, hoặc `new` đã có (kể cả `old == new`): trả `false`. Còn lại: thay đúng vị trí của `old`, tính lại một lần. `new` không bắt buộc cùng `Type` hay `Source` |
| `RemoveAllFromSource(src)` | Trả số modifier đã gỡ. Mỗi stat phát event tối đa một lần |
| `Resource.Add(d)` | Clamp, trả lượng thực sự áp dụng (đang 5, `Add(−20)` trả −5); `d == 0` không làm gì |
| `Resource.Set(v)` | Clamp rồi đặt |
| `GetStat`, `GetResource` | ID chưa đăng ký: lỗi. `TryGet*` trả `false` |
| `StatCollection(settings)` | Một phần tử sai (kể cả resource trỏ stat không có): lỗi, không tạo collection. Không thêm hay xóa stat, resource sau khi tạo |
| `CreateSettings()` (mọi definition) | Luôn trả bản copy mới; không kiểm tra, collection kiểm tra khi nhận |
| `CaptureSnapshot()` (cả hai) | Luôn trả object và mảng mới |
| `EffectCollection(stats)` | Mặc định |
| `Apply(s)` | Modifier trỏ stat lạ: lỗi, không áp phần nào. Group cùng `Id` khác `Policy` với effect đang có (chỉ xảy ra khi dựng bằng code): lỗi. Đang chạy: theo `StackRule`, settings mới khác nội dung bị bỏ qua. Thua trong group `Replace`: trả `null` |
| `Remove(id)` | Gỡ, phát `Removed(…, Removed)`, trả `true`; effect kế tiếp trong group `Suppress` có tác dụng lại. Không có thì trả `false` |
| `Has(id)`, `TryGet(id, out e)` | Không có thì `false` |
| `Advance(d)` | `d < 0`: lỗi; `d == 0` không làm gì. Gọi trong handler của Stats hay Effects: `InvalidOperationException` |
| `Restore` (cả hai) | Theo mục Save |

### Stat

```text
unclamped = (BaseValue + Σ Flat) × (1 + Σ PercentAdd) × Π (1 + PercentMult)
Value     = clamp(unclamped, Min, Max)
```

- Phần trăm ghi dạng tỉ lệ (`0.1` = +10%). Các `PercentAdd` cộng với nhau rồi nhân một lần; mỗi `PercentMult` nhân riêng.
- Chỉ clamp giá trị cuối; base và modifier giữ số gốc. Min 0, base 5, `PercentAdd` −1.5 → 0; gỡ modifier → 5.
- Ví dụ: base 5, giày `Flat` +1, ốm `PercentAdd` −0.3, kiệt sức `PercentMult` −0.5 → `6 × 0.7 × 0.5 = 2.1`.
- Phép tính tràn `float` (ra ∞, hoặc NaN khi ∞ × 0) là dữ liệu sai của game, không xử lý.

### Modifier và source

- `Source` là `object`, so sánh theo reference: món đồ, instance effect, hoặc token `new object()`.
- Một modifier thêm được vào nhiều stat, nhưng chỉ một lần mỗi stat. Muốn đổi giá trị thì `ReplaceModifier`: tính lại và phát event một lần. Gỡ rồi thêm thì stat đổi hai lần, và Resource lấy stat đó làm max bị clamp theo giá trị ở giữa.

### Resource

- Luôn `Min <= Current <= Max`, với `Min` là hằng số và `Max` = `max(MaxStat.Value, Min)`: stat làm max xuống dưới `Min` thì khoảng là `[Min, Min]`.
- Max đổi thì current **giữ số tuyệt đối**, chỉ clamp khi vượt: 80/100 mặc áo +20 → 80/120; 110/120 tháo áo → 100/100.
- Resource clamp theo max **trước** khi handler `ValueChanged` của stat làm max chạy.
- `Normalized` = `(Current − Min) / (Max − Min)`, bằng 1 khi `Max == Min`.
- Current lúc tạo là `Min + InitialFraction × (Max − Min)`, tính theo max lúc tạo. Tạo 100/100 rồi mặc áo +20 → 100/120; muốn đầy thì `Set(Max)`.
- Resource không có modifier; hồi và tiêu là việc của module dùng nó.

### Effect

- Mỗi `EffectId` có tối đa một `EffectInstance`. Instance giữ bản copy settings lúc áp lần đầu, `Stacks`, `Remaining` (+∞ là vô hạn), `IsSuppressed`.
- Mỗi phần tử trong `Modifiers` của settings sinh đúng một `Modifier` có giá trị `Value × Stacks`, kể cả `PercentMult`: Kiệt sức `PercentMult −0.2`, 3 stack → `PercentMult −0.6` (×0.4).
- Instance có tác dụng thì có modifier trên stat; bị suppress thì không. Mỗi lệnh của `EffectCollection` đổi modifier trong một batch: mỗi stat tính lại một lần, Resource clamp theo max cuối của batch. Effect +20 `MaxHealth` đang 115/120 lên stack 2 → 115/140, không bị clamp về 100 ở giữa. Batch không phủ derived stat nối bằng handler (xem Viết module dùng Stats).
- Thời gian theo đơn vị do game quy ước (phút, giây, lượt), dùng chung cho `Duration` và `delta`.

**Áp lại chính nó** (cùng `EffectId`) theo `StackRule`:

| `StackRule` | Hành vi | Event |
|---|---|---|
| `Refresh` | `Remaining = Duration` | `Refreshed` |
| `Extend` | `Remaining = min(Remaining + Duration, MaxDuration)` | `Refreshed` |
| `Ignore` | Không đổi gì | Không có |
| `Stack` | Dưới `MaxStacks`: `Stacks + 1`, `Remaining = Duration`; đủ rồi: chỉ `Remaining = Duration`. Mọi stack chung một đồng hồ | `Stacked` / `Refreshed` |

Effect vô hạn: `Refresh`, `Extend` không đổi thời gian nhưng vẫn phát `Refreshed`.

**Với effect khác cùng group:**
- `Group` so sánh theo `Id`; `null` là chồng tự do. Group chỉ có hiệu lực trong một `EffectCollection`; mỗi `StatCollection` chỉ nên có một `EffectCollection`.
- Trong group chỉ effect có `Rank` cao nhất có tác dụng. Bằng rank thì effect áp gần nhất thắng; "áp" tính cả `Refresh`/`Extend`/`Stack`, không tính `Ignore`. Ví dụ: "Bánh mì" và "Cơm" cùng group `Food` rank 1; ăn món nào sau thì món đó có tác dụng.
- Effect bị suppress vẫn theo `StackRule` (thêm stack, làm mới thời hạn).

| `GroupPolicy` | Effect thua | Đang có Thuốc II (+30%, rank 2, còn 5 phút), uống Thuốc I (+20%, rank 1, 10 phút) |
|---|---|---|
| `Replace` | Áp vào mà thua: bị bỏ qua, `Apply` trả `null`. Đang chạy mà bị thắng: bị gỡ (`Replaced`) | Thuốc I bị bỏ qua; hết 5 phút thì hết tác dụng |
| `Suppress` | Vẫn chạy đồng hồ, không có modifier; có tác dụng lại khi effect thắng hết | Hết 5 phút, Thuốc I có tác dụng nốt 5 phút |

### Advance

`Advance(delta)`:
1. Trừ `Remaining` của mọi effect có thời hạn, kể cả effect bị suppress.
2. Effect có `Remaining <= 0` hết hạn, xếp theo thời điểm hết hạn (bằng nhau thì theo thứ tự áp lần đầu).
3. Gỡ hết effect hết hạn, chọn lại effect có tác dụng trong các group bị ảnh hưởng.
4. Phát `Removed(…, Expired)` theo thứ tự bước 2, rồi `Resumed` theo thứ tự áp lần đầu.

- Event còn chờ của instance mà handler đã gỡ hay đổi trạng thái thì bỏ qua. Effect áp trong handler không bị trừ `delta` của lần này.
- `TimeUntilNextExpiry` là `Remaining` nhỏ nhất trong các effect có thời hạn, kể cả effect bị suppress; +∞ nếu không có.
- `Advance(10)` một lần và `Advance(1)` mười lần cho cùng state khi `delta` biểu diễn chính xác trong `double` (đơn vị nguyên như phút, lượt). Với giây thật, sai số làm tròn có thể làm effect hết hạn trễ một lần gọi. Event có thể khác: effect có tác dụng lại rồi hết hạn trong cùng một lần gọi thì không phát `Resumed` (Thuốc II còn 5, Thuốc I bị suppress còn 10, `Advance(10)`: chỉ có hai `Removed`). Muốn đủ event thì cắt `delta` theo `TimeUntilNextExpiry`, như `Simulate` ở mục Viết module dùng Stats.

## Save [T6]

Snapshot là class `[Serializable]` (mảng entry). `Restore` kiểm tra toàn bộ snapshot trước; bị từ chối thì ném `ArgumentException` và không đổi gì. Lõi không tự sửa save cho khớp settings: đổi settings (xóa hay đổi tên asset, giảm `MaxStacks`…) sau khi đã có save thì game tự nâng cấp save trước khi gọi `Restore`, hoặc chấp nhận save cũ bị từ chối.

**`StatsSnapshot`**
- Lưu: base của stat nào khác base trong settings, current của mọi resource. Không lưu modifier: mỗi nguồn modifier tự áp lại khi restore. Không lưu base trùng settings, để chỉnh base trong profile có tác dụng với save cũ; người chơi đổi base đúng bằng mặc định cũ thì khi restore nhận mặc định mới. "Base trong settings" là `BaseValue` trong `StatCollectionSettings` (đã gồm override của profile); so sánh bằng `!=`.
- Từ chối: mảng hay entry `null`; ID sai hoặc trùng; base hay current là NaN hoặc vô hạn; ID không có trong collection.
- Vắng mặt: stat → base trong settings; resource (save cũ trước khi thêm Thirst) → giá trị theo `InitialFraction`, tính theo max sau bước 1, để restore vào thực thể mới hay đang chơi cũng ra cùng kết quả.
- `Restore` chạy ba bước:
  1. Đặt mọi base, phát `Stat.ValueChanged` cho stat đổi giá trị, để handler derived stat cập nhật max.
  2. Đặt mọi current (resource vắng mặt về giá trị theo `InitialFraction`), clamp vào `[Min, Max]` theo max cuối.
  3. Phát `StatCollection.Restored`.
- Trong suốt `Restore`, Resource không phát event nào. Clamp ở bước 1 chỉ là trạng thái tạm, bước 2 ghi đè. Bước 1 phát event vì derived stat phụ thuộc chính base được restore: không phát thì với MaxHealth = 50 + Vitality × 5 và Vitality mặc định 10, save có Vitality 20, máu 140/150 sẽ ra 100/150.

**`EffectsSnapshot`**
- Lưu: mọi effect (kể cả bị suppress) theo thứ tự áp lần đầu: `EffectId`, `Stacks`, `Remaining` (vô hạn lưu bằng cờ `IsInfinite`), số thứ tự lần áp gần nhất. Không lưu settings (tra qua `resolve`) và `IsSuppressed` (tính lại từ group).
- Từ chối: mảng hay entry `null`; `resolve == null`; `EffectId` sai hoặc trùng; số thứ tự âm hoặc trùng giữa hai effect; `Stacks < 1`; `Remaining <= 0`, NaN hay +∞ với effect có thời hạn; `resolve` trả `null`, settings sai (theo mục Settings) hoặc có `Id` khác ID của entry; modifier trỏ stat không có trong collection; `Stacks` vượt mức effect đạt được khi chơi (`MaxStacks` nếu `Stack`, còn lại 1); `Remaining` vượt mức effect đạt được khi chơi (`MaxDuration` nếu `Extend`, còn lại `Duration`); cờ `IsInfinite` khác settings; hai effect cùng group `Replace`; hai effect có group cùng `Id` khác `Policy`.
- Gỡ effect đang chạy, áp lại theo snapshot trong một batch, phát `Restored`. Stat vẫn phát `ValueChanged` do modifier đổi, mỗi stat tối đa một lần, để handler derived stat cập nhật.
- Bộ đếm lần áp đi tiếp từ số lớn nhất trong snapshot, để effect áp sau khi restore luôn được coi là áp gần nhất.

**Thứ tự restore** (bắt buộc):
1. Nguồn modifier: `EffectCollection.Restore`, trang bị…
2. `StatCollection.Restore` sau cùng.

Trong lúc restore, event của Stat chỉ dùng để dựng lại derived stat; event của Resource mang giá trị tạm (ở bước 1, modifier đổi làm Resource clamp, hiếm hơn là `Depleted` khi max ≤ min; bước 2 đặt lại current). Luật game (ngất khi máu cạn, áp effect khi stat qua ngưỡng) bỏ qua cả hai cho tới khi nhận `StatCollection.Restored`. Không thì effect đã có trong save bị áp lại: Poison −6 Vitality đưa Vitality 10 → 4, handler "dưới 5 thì áp Weak" làm Weak còn 3 bị `Refresh` lên 10.

## Viết module dùng Stats

- **Giá trị tiêu và hồi** (máu, đói): mỗi cái là một `Resource`; nghe `Depleted` cho chạm đáy.
- **Tham số là Stat:** tham số mà effect hay trang bị có thể đổi (tốc độ giảm đói, tốc độ hồi máu) là Stat, không phải hằng số. "Ốm: đói nhanh gấp đôi" là effect `PercentAdd +1` lên `HungerRate`.
- **Source:** mỗi nguồn dùng object riêng làm source và tự `RemoveAllFromSource` khi hết hiệu lực, kể cả trong `OnDisable`/`OnDestroy`.
- **Đổi trang bị:** gỡ rồi thêm nên current bị clamp ở giữa: áo A (+20) sang áo B (+30), 110/120 → 100/100 → 100/130; không có batch public để tránh. Effect thay effect trong group `Replace` thì không (110/120 → 110/130) nhờ batch.
- **Thời gian:** game có **một** hàm `Simulate(delta)` cho cả chạy thường lẫn skip; không gọi `Advance` của Effects hay của module dùng tham số thẳng từ nhiều event. `Simulate` lặp tới hết `delta`, mỗi đoạn `d = min(phần còn lại, TimeUntilNextExpiry, thời gian tới ngưỡng kế tiếp của module dùng tham số)`, kể cả khi chạy thường (mốc hết hạn không rơi vào bước của đồng hồ):
  1. Module dùng tham số `Advance(d)`; tham số không đổi trong cả đoạn.
  2. `EffectCollection.Advance(d)`; effect hết hạn đúng ở cuối đoạn.
  3. Áp các effect mà ngưỡng ở bước 1 gây ra (đã hoãn), đủ thời hạn, vì chúng bắt đầu ở cuối đoạn.
- **Module dùng tham số** cho biết thời gian tới ngưỡng kế tiếp, luôn `> 0` (đang ở ngưỡng thì tính ngưỡng sau, không có thì +∞), để mỗi đoạn `d > 0` và vòng lặp dừng; không tự `Apply` effect trong lúc `Advance`, mà báo ra để `Simulate` áp ở bước 3.
- Ví dụ, bước 10 phút, đói giảm 1/phút, Ốm (`HungerRate` ×2, Duration 45):
  - Chạy thường, Ốm áp lúc 0: bước 40→50 bị cắt thành 5 + 5, nên ×2 đúng 45 phút. Skip 0→60: đoạn 45 (×2), đoạn 15 (×1). Hai đường cùng tiêu 105 đói.
  - Còn 40 đói, ngủ skip 0→80, Ốm Duration 60: đoạn 40 (tới ngưỡng) → đói cạn, `Advance(40)`, rồi áp Ốm còn 60; đoạn 40 → Ốm còn 20 lúc thức dậy, giống chạy thường. Áp Ốm ngay trong handler `Depleted` thì `Advance(40)` của đoạn đầu trừ cả 40, và người chơi dậy không ốm.
- **Derived stat:** nối bằng một modifier `Flat` có source riêng, đổi bằng `ReplaceModifier`. Vẫn đúng khi restore nhờ bước 1 của `StatCollection.Restore`. Vitality 20 → 21 với máu 140/150 → 140/155; gỡ rồi thêm sẽ clamp máu về 50 ở giữa.
- **Giới hạn của derived stat:** handler derived chạy sau khi lệnh đã clamp Resource, nên batch của effect không phủ nó. Một lệnh vừa đổi stat nguồn vừa đổi stat làm max (effect +4 Vitality thay effect +20 `MaxHealth` trong group `Replace`) có thể clamp current theo max tạm (170/170 → 150/170), và stat làm max có thể phát `ValueChanged` hai lần. Khi lỡ vi phạm luật ở cuối đoạn này, listener vẫn nhận giá trị cuối nhờ luật bỏ event còn chờ: Vitality (đăng ký trước `MaxHealth`) base 20, giáp +4 Vitality và +10 `MaxHealth`, đang 180/180; tháo giáp → batch ra 170/170 (derived chưa cập nhật), handler Vitality lồng đưa về 150/150 và phát `MaxHealth(180,150)`, `Resource(180,150)`; event còn chờ `MaxHealth(180,170)`, `Resource(180,170)` bị bỏ. Không để một lệnh vừa đổi stat nguồn vừa đổi stat làm max của cùng resource: không đặt hai effect như vậy chung một group (`Replace` hay `Suppress`; với `Suppress`, effect +20 `MaxHealth` hết hạn và effect +4 Vitality có tác dụng lại cũng làm 170/170 → 150/170), và không đặt cả hai loại modifier trong một effect.

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

## Giới hạn

| Hằng số / setting | Giá trị | Ý nghĩa |
|---|---|---|
| `StatCollection.MaxEventDepth` | 32 | Độ sâu event lồng tối đa; chống handler gọi lệnh lặp vô tận |
| `MaxStacks` (settings) | ≥ 1, không giới hạn trên | Modifier tính bằng một phép nhân, không lặp theo số stack |

Giả định quy mô: mỗi thực thể tới vài chục stat và resource, vài modifier mỗi stat, tới vài chục effect. Mọi lệnh duyệt tuyến tính theo số stat, resource, modifier hay instance.
