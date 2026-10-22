using System.Collections.Generic;
using UnityEngine;

namespace BaseGame.Playground.Calendar
{
    /// <summary>
    /// IMGUI panel for trying the clock: shows the date, speed and FakeNeed, logs recent events,
    /// and has buttons for every clock command.
    /// </summary>
    public class CalendarDebugOverlay : MonoBehaviour
    {
        private const int MaxLogLines = 20;
        private const int MorningMinute = 6 * 60;
        private const float FastForwardSpeed = 50f;
        private static readonly float[] SpeedOptions = { 1f, 2f, 5f, 10f };

        [SerializeField] private GameClockDriver _driver;
        [SerializeField] private FakeNeedListener _needListener;
        [SerializeField] private bool _logTicked;

        private readonly Queue<string> _log = new Queue<string>();
        private GameClock _clock;
        private CalendarSnapshot? _savedSnapshot;
        private int _eventsThisWindow;
        private float _windowStart;
        private float _eventsPerSecond;

        private void OnEnable()
        {
            if (_driver == null || _driver.Clock == null)
            {
                Debug.LogError($"{nameof(CalendarDebugOverlay)} on '{name}' has no running {nameof(GameClockDriver)}.", this);
                enabled = false;
                return;
            }

            _clock = _driver.Clock;
            _clock.Ticked += OnTicked;
            _clock.HourChanged += OnHourChanged;
            _clock.DayChanged += OnDayChanged;
            _clock.WeekChanged += OnWeekChanged;
            _clock.SeasonChanged += OnSeasonChanged;
            _clock.YearChanged += OnYearChanged;
            _clock.Skipped += OnSkipped;
            _clock.Restored += OnRestored;
            _clock.FastForwardCompleted += OnFastForwardCompleted;
            _windowStart = Time.unscaledTime;
        }

        private void OnDisable()
        {
            if (_clock == null)
            {
                return;
            }

            _clock.Ticked -= OnTicked;
            _clock.HourChanged -= OnHourChanged;
            _clock.DayChanged -= OnDayChanged;
            _clock.WeekChanged -= OnWeekChanged;
            _clock.SeasonChanged -= OnSeasonChanged;
            _clock.YearChanged -= OnYearChanged;
            _clock.Skipped -= OnSkipped;
            _clock.Restored -= OnRestored;
            _clock.FastForwardCompleted -= OnFastForwardCompleted;
            _clock = null;
        }

        private void Update()
        {
            float elapsed = Time.unscaledTime - _windowStart;
            if (elapsed >= 1f)
            {
                _eventsPerSecond = _eventsThisWindow / elapsed;
                _eventsThisWindow = 0;
                _windowStart = Time.unscaledTime;
            }
        }

        private void OnGUI()
        {
            if (_clock == null)
            {
                return;
            }

            var config = _driver.Config;
            var now = _clock.Now;

            GUILayout.BeginArea(new Rect(10f, 10f, 440f, Screen.height - 20f), GUI.skin.box);

            GUILayout.Label($"<b>{FormatDate(now)}</b>  ({config.DayNames[now.DayOfWeek]})", RichLabel());
            GUILayout.Label($"Total minutes {now.TotalMinutes}   Day {now.Day}   Step {config.MinutesPerStep} min");
            GUILayout.Label($"Speed x{_clock.Speed:0.##}   {(_clock.IsPaused ? "PAUSED" : "running")}   {(_clock.IsFastForwarding ? "FAST-FORWARD" : "")}");

            var need = _needListener != null ? _needListener.Need : null;
            if (need != null)
            {
                string zero = need.ZeroSinceMinute.HasValue ? $"   zero since {need.ZeroSinceMinute.Value}" : "";
                GUILayout.Label($"FakeNeed {need.Value}/{need.Max}{zero}");
            }

            GUILayout.Space(4f);
            GUILayout.Label("Day progress: stepped / smooth (ExactMinutes)");
            DrawBar(now.ClockMinute / (float)CalendarDate.MinutesPerDay);
            double smoothMinute = (_clock.ExactMinutes + config.DayStartMinute) % CalendarDate.MinutesPerDay;
            DrawBar((float)(smoothMinute / CalendarDate.MinutesPerDay));

            GUILayout.Space(4f);
            DrawButtons(need);

            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Events / real second: {_eventsPerSecond:0.0}");
            _logTicked = GUILayout.Toggle(_logTicked, "Log Ticked");
            GUILayout.EndHorizontal();
            foreach (string line in _log)
            {
                GUILayout.Label(line);
            }

            GUILayout.EndArea();
        }

        private void DrawButtons(FakeNeed need)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_clock.IsPaused ? "Resume" : "Pause"))
            {
                if (_clock.IsPaused)
                {
                    _clock.Resume(this);
                }
                else
                {
                    _clock.Pause(this);
                }
            }

            foreach (float speed in SpeedOptions)
            {
                if (GUILayout.Button($"x{speed:0}"))
                {
                    _clock.SetSpeed(speed);
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"Fast-forward to 06:00 (x{FastForwardSpeed:0})"))
            {
                _clock.FastForwardTo(_clock.NextOccurrence(MorningMinute), FastForwardSpeed);
            }

            if (_clock.IsFastForwarding && GUILayout.Button("Stop"))
            {
                _clock.StopFastForward();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Skip to 06:00"))
            {
                _clock.SkipTo(_clock.NextOccurrence(MorningMinute));
            }

            if (GUILayout.Button("Skip 3 days"))
            {
                _clock.SkipTo(_clock.Now.TotalMinutes + 3L * CalendarDate.MinutesPerDay);
            }

            if (GUILayout.Button("Advance season"))
            {
                _clock.AdvanceSeason();
                AddLog("AdvanceSeason requested");
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save snapshot"))
            {
                _savedSnapshot = _clock.CaptureSnapshot();
                AddLog($"Saved: {_savedSnapshot.Value}");
            }

            GUI.enabled = _savedSnapshot.HasValue;
            if (GUILayout.Button("Load snapshot") && _savedSnapshot.HasValue)
            {
                _clock.Restore(_savedSnapshot.Value);
            }

            GUI.enabled = need != null;
            if (GUILayout.Button("Refill need") && need != null)
            {
                need.Refill();
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        private static void DrawBar(float fraction)
        {
            Rect rect = GUILayoutUtility.GetRect(1f, 12f, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, new Color(0f, 0f, 0f, 0.5f), 0f, 0f);
            rect.width *= Mathf.Clamp01(fraction);
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, new Color(1f, 0.8f, 0.3f, 1f), 0f, 0f);
        }

        private static GUIStyle RichLabel() => new GUIStyle(GUI.skin.label) { richText = true };

        private string FormatDate(CalendarDate date)
        {
            string season = _driver.Config.Seasons[date.SeasonIndex].Name;
            return $"Year {date.Year}, {season} day {date.DayInSeason + 1}, {date.Hour:00}:{date.Minute:00}";
        }

        private void AddLog(string message)
        {
            _log.Enqueue($"[{_clock.Now.Hour:00}:{_clock.Now.Minute:00}] {message}");
            while (_log.Count > MaxLogLines)
            {
                _log.Dequeue();
            }
        }

        private void CountEvent()
        {
            _eventsThisWindow++;
        }

        private void OnTicked(TickInfo info)
        {
            CountEvent();
            if (_logTicked)
            {
                AddLog($"Ticked +{info.DeltaMinutes} min (x{info.Speed:0.##}{(info.IsFastForwarding ? ", FF" : "")})");
            }
        }

        private void OnHourChanged(CalendarDate date)
        {
            CountEvent();
            AddLog("HourChanged");
        }

        private void OnDayChanged(CalendarDate date)
        {
            CountEvent();
            AddLog($"DayChanged: {FormatDate(date)}");
        }

        private void OnWeekChanged(CalendarDate date)
        {
            CountEvent();
            AddLog("WeekChanged");
        }

        private void OnSeasonChanged(CalendarDate date)
        {
            CountEvent();
            AddLog($"SeasonChanged: {_driver.Config.Seasons[date.SeasonIndex].Name}");
        }

        private void OnYearChanged(CalendarDate date)
        {
            CountEvent();
            AddLog($"YearChanged: {date.Year}");
        }

        private void OnSkipped(CalendarDate from, CalendarDate to)
        {
            CountEvent();
            AddLog($"Skipped {FormatDate(from)} -> {FormatDate(to)}");
        }

        private void OnRestored(CalendarDate date)
        {
            CountEvent();
            AddLog($"Restored: {FormatDate(date)}");
        }

        private void OnFastForwardCompleted(CalendarDate date)
        {
            CountEvent();
            AddLog("FastForwardCompleted");
        }
    }
}
