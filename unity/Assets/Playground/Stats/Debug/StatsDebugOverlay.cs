using System;
using System.Collections.Generic;
using GuenBanKhai.Calendar;
using UnityEngine;

namespace BaseGame.Playground.Stats
{
    /// <summary>
    /// Manual test panel for one PlaygroundEntity: a button for every public command, live values and an event log.
    /// Time comes from the overlay's own buttons, or from a GameClockDriver when one is assigned (Stepped and Skipped
    /// call Simulate, Restored does not).
    /// </summary>
    public class StatsDebugOverlay : MonoBehaviour
    {
        private const int MaxLogLines = 28;
        private const double StepMinutes = 10;
        private const float PanelWidth = 560f;

        [Tooltip("Optional. When set, the clock drives Simulate; otherwise the overlay's buttons do.")]
        [SerializeField] private GameClockDriver _clockDriver = null;
        [Tooltip("Real seconds between auto steps of 10 game minutes (overlay time only).")]
        [SerializeField] private float _autoStepSeconds = 0.5f;

        private readonly Queue<string> _log = new Queue<string>();
        // Commands run in Update, not inside OnGUI, so IMGUI layout never sees content change mid-frame.
        private readonly Queue<Action> _pending = new Queue<Action>();
        private readonly Dictionary<StatId, Modifier> _lastOverlayModifier = new Dictionary<StatId, Modifier>();
        private readonly object _overlaySource = new object();
        private readonly object _shirtA = new object();
        private readonly object _shirtB = new object();
        private readonly object _vitalArmor = new object();

        private List<EffectSettings> _effects;
        private PlaygroundEntity _entity;
        private GameClock _clock;
        private bool _autoStep;
        private float _autoTimer;
        private string _savedStats;
        private string _savedEffects;
        private Vector2 _leftScroll;
        private Vector2 _rightScroll;
        private GUIStyle _richLabel;

        private void Awake()
        {
            _effects = PlaygroundCatalog.CreateEffects();
            SetEntity(new PlaygroundEntity(_effects));
        }

        private void OnEnable()
        {
            if (_clockDriver == null)
            {
                return;
            }

            if (_clockDriver.Clock == null)
            {
                Debug.LogError($"{nameof(StatsDebugOverlay)} on '{name}': the assigned {nameof(GameClockDriver)} has no running clock.", this);
                enabled = false;
                return;
            }

            _clock = _clockDriver.Clock;
            _clock.Stepped += OnClockStepped;
            _clock.Skipped += OnClockSkipped;
        }

        private void OnDisable()
        {
            if (_clock == null)
            {
                return;
            }

            _clock.Stepped -= OnClockStepped;
            _clock.Skipped -= OnClockSkipped;
            _clock.Resume(this);
            _clock = null;
        }

        private void Update()
        {
            while (_pending.Count > 0)
            {
                Run(_pending.Dequeue());
            }

            if (!_autoStep || _clock != null)
            {
                return;
            }

            _autoTimer += Time.unscaledDeltaTime;
            if (_autoTimer >= _autoStepSeconds)
            {
                _autoTimer = 0f;
                Run(() => _entity.Simulator.Simulate(StepMinutes));
            }
        }

        private void OnGUI()
        {
            if (_entity == null)
            {
                return;
            }

            if (_richLabel == null)
            {
                _richLabel = new GUIStyle(GUI.skin.label) { richText = true };
            }

            GUILayout.BeginArea(new Rect(10f, 10f, PanelWidth, Screen.height - 20f), GUI.skin.box);
            _leftScroll = GUILayout.BeginScrollView(_leftScroll);
            DrawTime();
            DrawStats();
            DrawResources();
            DrawEquipment();
            DrawSave();
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(20f + PanelWidth, 10f, PanelWidth, Screen.height - 20f), GUI.skin.box);
            _rightScroll = GUILayout.BeginScrollView(_rightScroll);
            DrawEffects();
            DrawLog();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawTime()
        {
            string now = _clock != null
                ? $"Calendar {_clock.Now.Hour:00}:{_clock.Now.Minute:00} (total {_clock.Now.TotalMinutes})"
                : "Thời gian: nút của overlay";
            GUILayout.Label($"<b>{now}</b>   Simulate: {_entity.Simulator.TotalMinutes:0.##} phút, lần gọi cuối {_entity.Simulator.LastSpanCount} đoạn", _richLabel);

            GUILayout.BeginHorizontal();
            if (_clock == null)
            {
                Button("Step 10", () => _entity.Simulator.Simulate(StepMinutes));
                Button("Skip 60", () => _entity.Simulator.Simulate(60));
                Button("Skip 80 (ngủ)", () => _entity.Simulator.Simulate(80));
                _autoStep = GUILayout.Toggle(_autoStep, "Auto step");
            }
            else
            {
                Button(_clock.IsPaused ? "Resume" : "Pause", () =>
                {
                    if (_clock.IsPaused)
                    {
                        _clock.Resume(this);
                    }
                    else
                    {
                        _clock.Pause(this);
                    }
                });
                Button("x1", () => _clock.SetSpeed(1f));
                Button($"x{Mathf.Min(10f, _clock.MaxSpeed):0}", () => _clock.SetSpeed(Mathf.Min(10f, _clock.MaxSpeed)));
                Button("Skip 60", () => _clock.SkipTo(_clock.Now.TotalMinutes + 60));
                Button("Skip 8h", () => _clock.SkipTo(_clock.Now.TotalMinutes + 8 * 60));
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Effects.Advance trực tiếp (bỏ qua Simulate):");
            Button("Advance(1)", () => _entity.Effects.Advance(1));
            Button("Advance(10)", () => _entity.Effects.Advance(10));
            GUILayout.EndHorizontal();
        }

        private void DrawStats()
        {
            Header("Stat");
            foreach (Stat stat in _entity.Stats.Stats)
            {
                GUILayout.Label($"{stat.DisplayName}: <b>{stat.Value:0.##}</b>  (base {stat.BaseValue:0.##}, {Describe(stat.Modifiers)})", _richLabel);
                GUILayout.BeginHorizontal();
                Button("Base -1", () => stat.SetBase(stat.BaseValue - 1f));
                Button("Base +1", () => stat.SetBase(stat.BaseValue + 1f));
                Button("+1 Flat", () => AddOverlayModifier(stat, new Modifier(ModifierType.Flat, 1f, _overlaySource)));
                Button("+10%", () => AddOverlayModifier(stat, new Modifier(ModifierType.PercentAdd, 0.1f, _overlaySource)));
                Button("Remove", () => RemoveOverlayModifier(stat));
                Button("Replace +1", () => ReplaceOverlayModifier(stat));
                GUILayout.EndHorizontal();
            }

            Button("RemoveAllFromSource(overlay)", () =>
            {
                _lastOverlayModifier.Clear();
                Log($"RemoveAllFromSource(overlay) → {_entity.Stats.RemoveAllFromSource(_overlaySource)}");
            });
        }

        private void DrawResources()
        {
            Header("Resource");
            foreach (Resource resource in _entity.Stats.Resources)
            {
                GUILayout.Label($"{resource.DisplayName}: <b>{resource.Current:0.##}/{resource.Max:0.##}</b>  (min {resource.Min:0.##}, max = {resource.MaxStat.Id})", _richLabel);
                DrawBar(resource.Normalized);
                GUILayout.BeginHorizontal();
                Button("Add -10", () => Log($"{resource.Id}.Add(-10) → {resource.Add(-10f):0.##}"));
                Button("Add +10", () => Log($"{resource.Id}.Add(+10) → {resource.Add(10f):0.##}"));
                Button("Set Min", () => resource.Set(resource.Min));
                Button("Set Max", () => resource.Set(resource.Max));
                GUILayout.EndHorizontal();
            }

            GUILayout.Label($"Đói: tới ngưỡng kế tiếp {_entity.Hunger.TimeToNextThreshold:0.##} phút");
        }

        private void DrawEquipment()
        {
            Header("Trang bị (gỡ rồi thêm, không có batch)");
            Stat maxHealth = _entity.Stats.GetStat(PlaygroundCatalog.MaxHealth);
            Stat vitality = _entity.Stats.GetStat(PlaygroundCatalog.Vitality);
            GUILayout.BeginHorizontal();
            Button("Mặc áo A (+20 máu)", () => maxHealth.AddModifier(new Modifier(ModifierType.Flat, 20f, _shirtA)));
            Button("Đổi A → B (+30)", () =>
            {
                _entity.Stats.RemoveAllFromSource(_shirtA);
                maxHealth.AddModifier(new Modifier(ModifierType.Flat, 30f, _shirtB));
            });
            Button("Tháo áo", () =>
            {
                _entity.Stats.RemoveAllFromSource(_shirtA);
                _entity.Stats.RemoveAllFromSource(_shirtB);
            });
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            Button("Giáp +4 thể lực +10 máu (vi phạm)", () =>
            {
                vitality.AddModifier(new Modifier(ModifierType.Flat, 4f, _vitalArmor));
                maxHealth.AddModifier(new Modifier(ModifierType.Flat, 10f, _vitalArmor));
            });
            Button("Tháo giáp", () => Log($"Tháo giáp → gỡ {_entity.Stats.RemoveAllFromSource(_vitalArmor)} modifier"));
            GUILayout.EndHorizontal();
        }

        private void DrawSave()
        {
            Header("Save");
            GUILayout.BeginHorizontal();
            Button("Capture", () =>
            {
                _savedStats = JsonUtility.ToJson(_entity.Stats.CaptureSnapshot());
                _savedEffects = JsonUtility.ToJson(_entity.Effects.CaptureSnapshot());
                Log($"Capture: stats {_savedStats.Length} ký tự, effects {_savedEffects.Length} ký tự");
            });

            GUI.enabled = _savedStats != null;
            Button("Restore", () => _entity.Restore(LoadStats(), LoadEffects()));
            Button("Restore vào thực thể mới", () =>
            {
                SetEntity(new PlaygroundEntity(_effects));
                _entity.Restore(LoadStats(), LoadEffects());
            });
            Button("Restore sai thứ tự", () =>
            {
                _entity.Stats.Restore(LoadStats());
                _entity.Effects.Restore(LoadEffects(), _entity.ResolveEffect);
            });
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            if (_savedStats != null)
            {
                GUILayout.Label(_savedStats);
                GUILayout.Label(_savedEffects);
            }
        }

        private void DrawEffects()
        {
            Header($"Effect   (TimeUntilNextExpiry {_entity.Effects.TimeUntilNextExpiry:0.##})");
            foreach (EffectSettings settings in _effects)
            {
                string state = _entity.Effects.TryGet(settings.Id, out EffectInstance instance)
                    ? $"<b>x{instance.Stacks}  {(instance.IsInfinite ? "∞" : instance.Remaining.ToString("0.##"))}{(instance.IsSuppressed ? "  suppressed" : "")}</b>"
                    : "—";
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{settings.DisplayName}: {state}", _richLabel, GUILayout.Width(PanelWidth - 150f));
                Button("Apply", () =>
                {
                    if (_entity.Effects.Apply(settings) == null)
                    {
                        Log($"Apply {settings.Id} → null (thua trong group Replace)");
                    }
                });
                Button("Remove", () => Log($"Remove {settings.Id} → {_entity.Effects.Remove(settings.Id)}"));
                GUILayout.EndHorizontal();
            }
        }

        private void DrawLog()
        {
            Header("Event");
            Button("Xóa log", () => _log.Clear());
            foreach (string line in _log)
            {
                GUILayout.Label(line);
            }
        }

        private void SetEntity(PlaygroundEntity entity)
        {
            _entity = entity;
            _lastOverlayModifier.Clear();
            foreach (Stat stat in entity.Stats.Stats)
            {
                stat.ValueChanged += (oldValue, newValue) => Log($"Stat {stat.Id}: {oldValue:0.##} → {newValue:0.##}");
            }

            foreach (Resource resource in entity.Stats.Resources)
            {
                resource.ValueChanged += (oldValue, newValue) => Log($"Resource {resource.Id}: {oldValue:0.##} → {newValue:0.##}");
                resource.Depleted += () => Log($"Resource {resource.Id}: Depleted");
                resource.Filled += () => Log($"Resource {resource.Id}: Filled");
            }

            entity.Stats.Restored += () => Log("StatCollection.Restored");
            EffectCollection effects = entity.Effects;
            effects.Applied += e => Log($"Effect Applied {e.Id}");
            effects.Stacked += e => Log($"Effect Stacked {e.Id} x{e.Stacks}");
            effects.Refreshed += e => Log($"Effect Refreshed {e.Id}");
            effects.Suppressed += e => Log($"Effect Suppressed {e.Id}");
            effects.Resumed += e => Log($"Effect Resumed {e.Id}");
            effects.Removed += (e, reason) => Log($"Effect Removed {e.Id} ({reason})");
            effects.Restored += () => Log("EffectCollection.Restored");
            Log("Thực thể mới");
        }

        private void AddOverlayModifier(Stat stat, Modifier modifier)
        {
            stat.AddModifier(modifier);
            _lastOverlayModifier[stat.Id] = modifier;
        }

        private void RemoveOverlayModifier(Stat stat)
        {
            if (_lastOverlayModifier.TryGetValue(stat.Id, out Modifier modifier))
            {
                Log($"{stat.Id}.RemoveModifier → {stat.RemoveModifier(modifier)}");
                _lastOverlayModifier.Remove(stat.Id);
            }
        }

        private void ReplaceOverlayModifier(Stat stat)
        {
            if (_lastOverlayModifier.TryGetValue(stat.Id, out Modifier modifier))
            {
                var next = new Modifier(modifier.Type, modifier.Value + (modifier.Type == ModifierType.Flat ? 1f : 0.1f), _overlaySource);
                Log($"{stat.Id}.ReplaceModifier → {stat.ReplaceModifier(modifier, next)}");
                _lastOverlayModifier[stat.Id] = next;
            }
        }

        private StatsSnapshot LoadStats() => JsonUtility.FromJson<StatsSnapshot>(_savedStats);

        private EffectsSnapshot LoadEffects() => JsonUtility.FromJson<EffectsSnapshot>(_savedEffects);

        private void OnClockStepped(CalendarDate date) => Run(() => _entity.Simulator.Simulate(_clock.MinutesPerStep));

        private void OnClockSkipped(CalendarDate from, CalendarDate to) =>
            Run(() => _entity.Simulator.Simulate(to.TotalMinutes - from.TotalMinutes));

        private void Button(string label, Action action)
        {
            if (GUILayout.Button(label))
            {
                _pending.Enqueue(action);
            }
        }

        private void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Log($"<color=red>{e.GetType().Name}: {e.Message}</color>");
                Debug.LogException(e, this);
            }
        }

        private void Log(string message)
        {
            _log.Enqueue($"[{_entity?.Simulator.TotalMinutes ?? 0:0.##}] {message}");
            while (_log.Count > MaxLogLines)
            {
                _log.Dequeue();
            }
        }

        private void Header(string title)
        {
            GUILayout.Space(6f);
            GUILayout.Label($"<b>{title}</b>", _richLabel);
        }

        private static string Describe(IReadOnlyList<Modifier> modifiers)
        {
            if (modifiers.Count == 0)
            {
                return "không modifier";
            }

            var parts = new string[modifiers.Count];
            for (int i = 0; i < modifiers.Count; i++)
            {
                parts[i] = modifiers[i].ToString();
            }

            return string.Join(", ", parts);
        }

        private static void DrawBar(float fraction)
        {
            Rect rect = GUILayoutUtility.GetRect(1f, 8f, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, new Color(0f, 0f, 0f, 0.5f), 0f, 0f);
            rect.width *= Mathf.Clamp01(fraction);
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, new Color(0.4f, 0.9f, 0.4f, 1f), 0f, 0f);
        }
    }
}
