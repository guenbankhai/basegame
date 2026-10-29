using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuenBanKhai.Calendar
{
    /// <summary>
    /// Plain C# clock that moves in steps and raises each event at the step it happens. Commands called from a
    /// handler are queued until that step's events are done, so every handler sees the same moment.
    /// </summary>
    public class GameClock
    {
        /// <summary>
        /// About 1.9 million years: far beyond any real game, and small enough that ExactMinutes (a double) stays precise
        /// to well under a thousandth of a minute. A whole number of days, so it is on every valid step.
        /// </summary>
        public const long MaxTotalMinutes = 700_000_000L * CalendarDate.MinutesPerDay;

        /// <summary>More than this means handlers queue commands in a loop; cutting off keeps the game from hanging.</summary>
        public const int MaxCommandsPerDrain = 1000;

        private readonly int _minutesPerStep;
        private readonly int _dayStartMinute;
        private readonly string[] _dayNames;
        private readonly SeasonDefinition[] _seasons;
        private readonly float _secondsPerGameMinute;
        private readonly float _maxSpeed;
        private readonly int _maxStepsPerFrame;
        private readonly int _maxSkipDays;

        private readonly HashSet<object> _pauseSources = new HashSet<object>();
        private readonly Queue<Action> _commands = new Queue<Action>();

        private CalendarSnapshot _state;

        private float _baseSpeed = 1f;
        private bool _isFastForwarding;
        private float _fastForwardSpeed;
        private long _fastForwardTarget;
        /// <summary>Bumped on every fast-forward start or stop, so a step can tell whether its handlers touched it.</summary>
        private int _fastForwardVersion;

        private double _pendingMinutes;
        private int _dispatchDepth;
        private bool _draining;
        private bool _dropRemainder;

        public GameClock(CalendarSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (settings.TryGetError(out string error))
            {
                throw new ArgumentException(error, nameof(settings));
            }

            _minutesPerStep = settings.MinutesPerStep;
            _dayStartMinute = settings.DayStartMinute;
            _dayNames = CalendarSettings.ToArray(settings.DayNames);
            _seasons = CalendarSettings.ToArray(settings.Seasons);
            _secondsPerGameMinute = settings.SecondsPerGameMinute;
            _maxSpeed = settings.MaxSpeed;
            _maxStepsPerFrame = settings.MaxStepsPerFrame;
            _maxSkipDays = settings.MaxSkipDays;

            _state = new CalendarSnapshot(settings.StartMinutesIntoDay, settings.StartYear, settings.StartSeasonIndex, 0, false);
        }

        /// <summary>Every step; each one is MinutesPerStep long. A skip reports its span through Skipped instead.</summary>
        public event Action<CalendarDate> Stepped;
        public event Action<CalendarDate> HourChanged;
        public event Action<CalendarDate> DayChanged;
        public event Action<CalendarDate> WeekChanged;
        public event Action<CalendarDate> SeasonChanged;
        public event Action<CalendarDate> YearChanged;
        /// <summary>No other events are raised for the skipped span.</summary>
        public event Action<CalendarDate, CalendarDate> Skipped;
        /// <summary>Unlike Skipped, no time passed in the world.</summary>
        public event Action<CalendarDate> Restored;
        /// <summary>
        /// Raised after the target step's events and queued commands, and only if none of those commands stopped,
        /// replaced or cancelled the fast-forward.
        /// </summary>
        public event Action<CalendarDate> FastForwardCompleted;

        public CalendarDate Now
        {
            get
            {
                long day = Day;
                return new CalendarDate(
                    _state.TotalMinutes,
                    _state.Year,
                    _state.SeasonIndex,
                    day - _state.SeasonStartDay,
                    (int)((_state.TotalMinutes + _dayStartMinute) % CalendarDate.MinutesPerDay),
                    (int)(day % _dayNames.Length));
            }
        }

        private long Day => _state.TotalMinutes / CalendarDate.MinutesPerDay;

        /// <summary>Includes time gathered toward the next step; for smoothing visuals between steps.</summary>
        public double ExactMinutes => _state.TotalMinutes + _pendingMinutes;

        /// <summary>Running speed: the fast-forward speed while fast-forwarding, otherwise the base speed.</summary>
        public float Speed => _isFastForwarding ? _fastForwardSpeed : _baseSpeed;
        public bool IsPaused => _pauseSources.Count > 0;
        /// <summary>Stays true until FastForwardCompleted, so the target step's events still see it.</summary>
        public bool IsFastForwarding => _isFastForwarding;

        /// <summary>Higher speeds are rejected rather than clamped, so caller bugs surface at once.</summary>
        public float MaxSpeed => _maxSpeed;

        /// <summary>A longer skip is a caller bug; rejecting it keeps the per-day skip loop from hanging the game.</summary>
        public int MaxSkipDays => _maxSkipDays;

        /// <summary>Length of every step: what an accumulating module advances by on Stepped.</summary>
        public int MinutesPerStep => _minutesPerStep;

        public int WeekLength => _dayNames.Length;
        public int SeasonCount => _seasons.Length;

        public string GetDayName(int dayOfWeek)
        {
            if (dayOfWeek < 0 || dayOfWeek >= _dayNames.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, $"Must be in [0, {_dayNames.Length}).");
            }

            return _dayNames[dayOfWeek];
        }

        public string GetSeasonName(int seasonIndex)
        {
            if (seasonIndex < 0 || seasonIndex >= _seasons.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(seasonIndex), seasonIndex, $"Must be in [0, {_seasons.Length}).");
            }

            return _seasons[seasonIndex].Name;
        }

        /// <summary>
        /// Steps one at a time. The rest of the call is dropped once the clock pauses, skips, restores, changes running
        /// speed or completes a fast-forward, so it never runs past the point where it was stopped.
        /// </summary>
        public void Tick(float realDeltaSeconds)
        {
            if (_dispatchDepth > 0)
            {
                throw new InvalidOperationException("Tick cannot be called from a GameClock event handler.");
            }

            if (!float.IsFinite(realDeltaSeconds) || realDeltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(realDeltaSeconds), realDeltaSeconds, "Must be finite and not negative.");
            }

            if (IsPaused)
            {
                return;
            }

            float speed = Speed;
            _pendingMinutes += (double)realDeltaSeconds * speed / _secondsPerGameMinute;
            for (int steps = 0; _pendingMinutes >= _minutesPerStep; steps++)
            {
                if (steps == _maxStepsPerFrame)
                {
                    _pendingMinutes = 0;
                    return;
                }

                _pendingMinutes -= _minutesPerStep;
                _dropRemainder = false;
                Step();
                if (_dropRemainder || IsPaused || Speed != speed)
                {
                    _pendingMinutes = 0;
                    return;
                }
            }
        }

        /// <summary>Sets the base speed; during a fast-forward it takes effect once the fast-forward ends.</summary>
        public void SetSpeed(float speed)
        {
            if (TryEnqueue(() => SetSpeed(speed)))
            {
                return;
            }

            ValidateSpeed(speed, allowZero: true);
            _baseSpeed = speed;
        }

        /// <summary>The clock runs only while no pause source is held; the caller must Resume with the same source.</summary>
        public void Pause(object source)
        {
            if (TryEnqueue(() => Pause(source)))
            {
                return;
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _pauseSources.Add(source);
        }

        /// <summary>Unknown sources are ignored.</summary>
        public void Resume(object source)
        {
            if (TryEnqueue(() => Resume(source)))
            {
                return;
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _pauseSources.Remove(source);
        }

        /// <summary>
        /// Stops exactly at targetMinutes and replaces any running fast-forward. A target equal to now completes at once.
        /// </summary>
        public void FastForwardTo(long targetMinutes, float speed)
        {
            if (TryEnqueue(() => FastForwardTo(targetMinutes, speed)))
            {
                return;
            }

            ValidateTarget(targetMinutes);
            ValidateSpeed(speed, allowZero: false);
            if (targetMinutes == _state.TotalMinutes)
            {
                var now = Now;
                Dispatch(() => CompleteFastForward(now));
                return;
            }

            StartFastForward(speed, targetMinutes);
        }

        /// <summary>Returns to the base speed without raising FastForwardCompleted.</summary>
        public void StopFastForward()
        {
            if (TryEnqueue(StopFastForward))
            {
                return;
            }

            StopFastForwardNow();
        }

        /// <summary>
        /// Raises only Skipped. Fixed-length seasons still change on the way; command-only seasons do not.
        /// Cancels a fast-forward, even when the target is now, and runs even while paused.
        /// </summary>
        public void SkipTo(long targetMinutes)
        {
            if (TryEnqueue(() => SkipTo(targetMinutes)))
            {
                return;
            }

            ValidateTarget(targetMinutes);
            if (targetMinutes - _state.TotalMinutes > (long)_maxSkipDays * CalendarDate.MinutesPerDay)
            {
                throw new ArgumentOutOfRangeException(nameof(targetMinutes), targetMinutes, $"Must be at most MaxSkipDays ({_maxSkipDays}) days after now.");
            }

            StopFastForwardNow();
            if (targetMinutes == _state.TotalMinutes)
            {
                return;
            }

            var from = Now;
            for (long nextBoundary = (Day + 1) * CalendarDate.MinutesPerDay; nextBoundary <= targetMinutes; nextBoundary += CalendarDate.MinutesPerDay)
            {
                _state.TotalMinutes = nextBoundary;
                ProcessDayBoundary(out _, out _);
            }

            _state.TotalMinutes = targetMinutes;
            RestartFromNow();
            var to = Now;
            Dispatch(() => Raise(Skipped, from, to));
        }

        /// <summary>
        /// Takes effect at the next day boundary; repeated calls before then add nothing. Queued inside a handler like
        /// the other commands, so a queued Restore or SkipTo before it cannot erase or move the flag.
        /// </summary>
        public void AdvanceSeason()
        {
            if (TryEnqueue(AdvanceSeason))
            {
                return;
            }

            _state.SeasonAdvancePending = true;
        }

        /// <summary>Never returns now: at exactly that clock time it returns the same time tomorrow.</summary>
        public long NextOccurrence(int clockMinute)
        {
            if (clockMinute < 0 || clockMinute >= CalendarDate.MinutesPerDay)
            {
                throw new ArgumentOutOfRangeException(nameof(clockMinute), clockMinute, $"Must be in [0, {CalendarDate.MinutesPerDay}).");
            }

            ValidateOnStep(clockMinute, nameof(clockMinute));
            int ahead = (clockMinute - Now.ClockMinute + CalendarDate.MinutesPerDay) % CalendarDate.MinutesPerDay;
            return _state.TotalMinutes + (ahead == 0 ? CalendarDate.MinutesPerDay : ahead);
        }

        public CalendarSnapshot CaptureSnapshot() => _state;

        /// <summary>
        /// Raises only Restored and replays no events. Drops sub-step time and any fast-forward; keeps pause sources
        /// and base speed, which are not part of a save.
        /// </summary>
        public void Restore(CalendarSnapshot snapshot)
        {
            if (TryEnqueue(() => Restore(snapshot)))
            {
                return;
            }

            if (snapshot.TotalMinutes < 0 || snapshot.TotalMinutes > MaxTotalMinutes || snapshot.TotalMinutes % _minutesPerStep != 0)
            {
                throw new ArgumentException($"TotalMinutes {snapshot.TotalMinutes} must be in [0, {MaxTotalMinutes}] and a multiple of the step ({_minutesPerStep}).", nameof(snapshot));
            }

            if (snapshot.SeasonIndex < 0 || snapshot.SeasonIndex >= _seasons.Length)
            {
                throw new ArgumentException($"SeasonIndex {snapshot.SeasonIndex} is outside the {_seasons.Length} configured seasons.", nameof(snapshot));
            }

            if (snapshot.Year < 0)
            {
                throw new ArgumentException($"Year {snapshot.Year} must be >= 0.", nameof(snapshot));
            }

            long day = snapshot.TotalMinutes / CalendarDate.MinutesPerDay;
            if (snapshot.SeasonStartDay < 0 || snapshot.SeasonStartDay > day)
            {
                throw new ArgumentException($"SeasonStartDay {snapshot.SeasonStartDay} must be in [0, {day}].", nameof(snapshot));
            }

            StopFastForwardNow();
            _state = snapshot;
            RestartFromNow();
            var now = Now;
            Dispatch(() => Raise(Restored, now));
        }

        /// <summary>
        /// FastForwardCompleted is dispatched separately, after the step's queued commands, so a handler that stops or
        /// retargets the fast-forward at the target step suppresses it.
        /// </summary>
        private void Step()
        {
            _state.TotalMinutes += _minutesPerStep;
            bool dayChanged = _state.TotalMinutes % CalendarDate.MinutesPerDay == 0;
            bool seasonChanged = false;
            bool yearChanged = false;
            if (dayChanged)
            {
                ProcessDayBoundary(out seasonChanged, out yearChanged);
            }

            var now = Now;
            bool hourChanged = now.ClockMinute % CalendarDate.MinutesPerHour == 0;
            bool weekChanged = dayChanged && now.Day % _dayNames.Length == 0;
            bool reachedTarget = _isFastForwarding && _fastForwardTarget == _state.TotalMinutes;
            int fastForwardVersion = _fastForwardVersion;

            Dispatch(() =>
            {
                Raise(Stepped, now);
                if (hourChanged)
                {
                    Raise(HourChanged, now);
                }

                if (dayChanged)
                {
                    Raise(DayChanged, now);
                }

                if (weekChanged)
                {
                    Raise(WeekChanged, now);
                }

                if (seasonChanged)
                {
                    Raise(SeasonChanged, now);
                }

                if (yearChanged)
                {
                    Raise(YearChanged, now);
                }
            });

            if (reachedTarget && _fastForwardVersion == fastForwardVersion)
            {
                Dispatch(() => CompleteFastForward(now));
            }
        }

        /// <summary>
        /// Shared by stepping and skipping so both apply the same season rules. Expects _state.TotalMinutes to be at
        /// the new day.
        /// </summary>
        private void ProcessDayBoundary(out bool seasonChanged, out bool yearChanged)
        {
            seasonChanged = false;
            yearChanged = false;
            long day = Day;
            int length = _seasons[_state.SeasonIndex].LengthInDays;
            if (!_state.SeasonAdvancePending && (length <= 0 || day - _state.SeasonStartDay < length))
            {
                return;
            }

            seasonChanged = true;
            _state.SeasonAdvancePending = false;
            _state.SeasonStartDay = day;
            _state.SeasonIndex++;
            if (_state.SeasonIndex >= _seasons.Length)
            {
                _state.SeasonIndex = 0;
                _state.Year++;
                yearChanged = true;
            }
        }

        private void StartFastForward(float speed, long target)
        {
            _isFastForwarding = true;
            _fastForwardSpeed = speed;
            _fastForwardTarget = target;
            _fastForwardVersion++;
        }

        private void StopFastForwardNow()
        {
            _isFastForwarding = false;
            _fastForwardVersion++;
        }

        /// <summary>Stops before raising, so Completed handlers already see the base speed.</summary>
        private void CompleteFastForward(CalendarDate now)
        {
            StopFastForwardNow();
            _dropRemainder = true;
            Raise(FastForwardCompleted, now);
        }

        /// <summary>After a skip or restore: time gathered before the jump is dropped.</summary>
        private void RestartFromNow()
        {
            _pendingMinutes = 0;
            _dropRemainder = true;
        }

        private void ValidateSpeed(float speed, bool allowZero)
        {
            if (float.IsNaN(speed))
            {
                throw new ArgumentOutOfRangeException(nameof(speed), speed, "Must be a number.");
            }

            if (allowZero ? speed < 0f : speed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(speed), speed, allowZero ? "Must not be negative." : "Must be greater than 0.");
            }

            if (speed > _maxSpeed)
            {
                throw new ArgumentOutOfRangeException(nameof(speed), speed, $"Must be at most MaxSpeed ({_maxSpeed}).");
            }
        }

        private void ValidateTarget(long targetMinutes)
        {
            if (targetMinutes < _state.TotalMinutes)
            {
                throw new ArgumentOutOfRangeException(nameof(targetMinutes), targetMinutes, $"Must not be before now ({_state.TotalMinutes}).");
            }

            // Past this the clock would hold a state that Restore rejects, so its own snapshot could not be restored.
            if (targetMinutes > MaxTotalMinutes)
            {
                throw new ArgumentOutOfRangeException(nameof(targetMinutes), targetMinutes, $"Must be at most MaxTotalMinutes ({MaxTotalMinutes}).");
            }

            ValidateOnStep(targetMinutes, nameof(targetMinutes));
        }

        private void ValidateOnStep(long minutes, string paramName)
        {
            if (minutes % _minutesPerStep != 0)
            {
                throw new ArgumentException($"Must be a multiple of the step ({_minutesPerStep} minutes).", paramName);
            }
        }

        /// <summary>Arguments are checked when the queued command runs, not when it is queued.</summary>
        private bool TryEnqueue(Action command)
        {
            if (_dispatchDepth == 0)
            {
                return false;
            }

            _commands.Enqueue(command);
            return true;
        }

        /// <summary>
        /// Only the outermost call drains the queue, so commands queued while draining go to the back instead of nesting.
        /// </summary>
        private void Dispatch(Action raise)
        {
            _dispatchDepth++;
            try
            {
                raise();
            }
            finally
            {
                _dispatchDepth--;
            }

            if (_dispatchDepth > 0 || _draining)
            {
                return;
            }

            _draining = true;
            try
            {
                for (int run = 0; _commands.Count > 0; run++)
                {
                    if (run == MaxCommandsPerDrain)
                    {
                        Debug.LogError($"{nameof(GameClock)} dropped {_commands.Count} queued commands: event handlers kept queueing commands ({MaxCommandsPerDrain} in a row).");
                        _commands.Clear();
                        break;
                    }

                    var command = _commands.Dequeue();
                    try
                    {
                        command();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            finally
            {
                _draining = false;
            }
        }

        private static void Raise<T>(Action<T> handlers, T arg)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<T> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(arg);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private static void Raise<T1, T2>(Action<T1, T2> handlers, T1 arg1, T2 arg2)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<T1, T2> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(arg1, arg2);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
