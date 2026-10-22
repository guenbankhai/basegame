using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaseGame.Playground.Calendar
{
    /// <summary>
    /// Pure C# game clock. Moves in steps of MinutesPerStep and raises every event at the exact step it happens.
    /// Commands that change the clock, called from an event handler, are queued and run once the current
    /// step's events are done.
    /// </summary>
    public class GameClock
    {
        private readonly int _minutesPerStep;
        private readonly int _dayStartMinute;
        private readonly int _weekLength;
        private readonly SeasonDefinition[] _seasons;
        private readonly float _secondsPerGameMinute;
        private readonly float _maxSpeed;
        private readonly int _maxStepsPerFrame;

        private readonly HashSet<object> _pauseSources = new HashSet<object>();
        private readonly Queue<Action> _commands = new Queue<Action>();

        private long _totalMinutes;
        private int _year;
        private int _seasonIndex;
        private long _seasonStartDay;
        private bool _seasonAdvancePending;

        private float _baseSpeed = 1f;
        private bool _isFastForwarding;
        private float _fastForwardSpeed;
        private long? _fastForwardTarget;

        private double _pendingMinutes;
        private long _lastUpdateMinutes;
        private int _dispatchDepth;
        private bool _jumped;

        public GameClock(CalendarConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (config.TryGetError(out string error))
            {
                throw new ArgumentException(error, nameof(config));
            }

            _minutesPerStep = config.MinutesPerStep;
            _dayStartMinute = config.DayStartMinute;
            _weekLength = config.DayNames.Count;
            _seasons = new List<SeasonDefinition>(config.Seasons).ToArray();
            _secondsPerGameMinute = config.SecondsPerGameMinute;
            _maxSpeed = config.MaxSpeed;
            _maxStepsPerFrame = config.MaxStepsPerFrame;

            _totalMinutes = config.StartMinutesIntoDay;
            _year = config.StartYear;
            _seasonIndex = config.StartSeasonIndex;
            _lastUpdateMinutes = _totalMinutes;
        }

        public event Action<TickInfo> Ticked;
        public event Action<CalendarDate> HourChanged;
        public event Action<CalendarDate> DayChanged;
        public event Action<CalendarDate> WeekChanged;
        public event Action<CalendarDate> SeasonChanged;
        public event Action<CalendarDate> YearChanged;
        /// <summary>Time jumped from the first date to the second. No other events were raised in between.</summary>
        public event Action<CalendarDate, CalendarDate> Skipped;
        /// <summary>State was loaded from a snapshot. No time passed in the world.</summary>
        public event Action<CalendarDate> Restored;
        /// <summary>A fast forward reached its target. Not raised when it is stopped, replaced or cancelled.</summary>
        public event Action<CalendarDate> FastForwardCompleted;

        public CalendarDate Now
        {
            get
            {
                long day = _totalMinutes / CalendarDate.MinutesPerDay;
                return new CalendarDate(
                    _totalMinutes,
                    _year,
                    _seasonIndex,
                    day - _seasonStartDay,
                    (int)((_totalMinutes + _dayStartMinute) % CalendarDate.MinutesPerDay),
                    (int)(day % _weekLength));
            }
        }

        /// <summary>TotalMinutes plus the time gathered toward the next step. Use it to smooth visuals.</summary>
        public double ExactMinutes => _totalMinutes + _pendingMinutes;

        /// <summary>The speed time runs at now: the fast-forward speed while fast-forwarding, otherwise the base speed.</summary>
        public float Speed => _isFastForwarding ? _fastForwardSpeed : _baseSpeed;
        public bool IsPaused => _pauseSources.Count > 0;
        public bool IsFastForwarding => _isFastForwarding;

        public void Tick(float realDeltaSeconds)
        {
            if (_dispatchDepth > 0)
            {
                throw new InvalidOperationException("Tick cannot be called from a GameClock event handler.");
            }

            if (realDeltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(realDeltaSeconds), realDeltaSeconds, "Must not be negative.");
            }

            if (IsPaused)
            {
                return;
            }

            _pendingMinutes += realDeltaSeconds * Speed / _secondsPerGameMinute;
            for (int steps = 0; _pendingMinutes >= _minutesPerStep; steps++)
            {
                if (steps == _maxStepsPerFrame)
                {
                    _pendingMinutes = 0;
                    return;
                }

                _pendingMinutes -= _minutesPerStep;
                _jumped = false;
                bool completed = Step();
                if (completed || _jumped || IsPaused)
                {
                    _pendingMinutes = 0;
                    return;
                }
            }
        }

        /// <summary>Sets the base speed, clamped to MaxSpeed. While fast-forwarding it applies once the fast forward ends.</summary>
        public void SetSpeed(float speed)
        {
            if (TryEnqueue(() => SetSpeed(speed)))
            {
                return;
            }

            if (speed < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(speed), speed, "Must not be negative.");
            }

            _baseSpeed = Math.Min(speed, _maxSpeed);
        }

        /// <summary>Adds a pause source. The clock runs only while no source is held.</summary>
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

        /// <summary>Removes a pause source. Unknown sources are ignored.</summary>
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

        /// <summary>Runs time at the given speed (clamped to MaxSpeed) until StopFastForward.</summary>
        public void FastForward(float speed)
        {
            if (TryEnqueue(() => FastForward(speed)))
            {
                return;
            }

            ValidateFastForwardSpeed(speed);
            StartFastForward(speed, null);
        }

        /// <summary>
        /// Runs time at the given speed (clamped to MaxSpeed) and stops exactly at targetMinutes.
        /// Replaces any running fast forward.
        /// </summary>
        public void FastForwardTo(long targetMinutes, float speed)
        {
            if (TryEnqueue(() => FastForwardTo(targetMinutes, speed)))
            {
                return;
            }

            ValidateTarget(targetMinutes, nameof(targetMinutes));
            ValidateFastForwardSpeed(speed);
            if (targetMinutes == _totalMinutes)
            {
                StopFastForwardNow();
                Dispatch(() => Raise(FastForwardCompleted, Now));
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
        /// Jumps to targetMinutes in one go and raises only Skipped. Fixed-length seasons still change on the way;
        /// command-only seasons do not. Cancels a fast forward. Runs while paused.
        /// </summary>
        public void SkipTo(long targetMinutes)
        {
            if (TryEnqueue(() => SkipTo(targetMinutes)))
            {
                return;
            }

            ValidateTarget(targetMinutes, nameof(targetMinutes));
            if (targetMinutes == _totalMinutes)
            {
                return;
            }

            StopFastForwardNow();
            var from = Now;
            long nextBoundary = (_totalMinutes / CalendarDate.MinutesPerDay + 1) * CalendarDate.MinutesPerDay;
            for (; nextBoundary <= targetMinutes; nextBoundary += CalendarDate.MinutesPerDay)
            {
                _totalMinutes = nextBoundary;
                ProcessDayBoundary(out _, out _);
            }

            _totalMinutes = targetMinutes;
            MarkJumped();
            var to = Now;
            Dispatch(() => Raise(Skipped, from, to));
        }

        /// <summary>
        /// Requests a season change at the next day boundary. Calling it again before then has no extra effect.
        /// Runs immediately even inside a handler.
        /// </summary>
        public void AdvanceSeason()
        {
            _seasonAdvancePending = true;
        }

        /// <summary>The next TotalMinutes after now at which the clock shows clockMinute.</summary>
        public long NextOccurrence(int clockMinute)
        {
            if (clockMinute < 0 || clockMinute >= CalendarDate.MinutesPerDay)
            {
                throw new ArgumentOutOfRangeException(nameof(clockMinute), clockMinute, "Must be in [0, 1440).");
            }

            if (clockMinute % _minutesPerStep != 0)
            {
                throw new ArgumentException($"Must be a multiple of the step ({_minutesPerStep} minutes).", nameof(clockMinute));
            }

            int ahead = (clockMinute - Now.ClockMinute + CalendarDate.MinutesPerDay) % CalendarDate.MinutesPerDay;
            return _totalMinutes + (ahead == 0 ? CalendarDate.MinutesPerDay : ahead);
        }

        public CalendarSnapshot CaptureSnapshot() =>
            new CalendarSnapshot(_totalMinutes, _year, _seasonIndex, _seasonStartDay, _seasonAdvancePending);

        /// <summary>
        /// Loads state without replaying events; raises only Restored. Drops sub-step time and any fast forward.
        /// Pause sources and base speed are kept.
        /// </summary>
        public void Restore(CalendarSnapshot snapshot)
        {
            if (TryEnqueue(() => Restore(snapshot)))
            {
                return;
            }

            if (snapshot.TotalMinutes < 0 || snapshot.TotalMinutes % _minutesPerStep != 0)
            {
                throw new ArgumentException($"TotalMinutes {snapshot.TotalMinutes} must be >= 0 and a multiple of the step ({_minutesPerStep}).", nameof(snapshot));
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
            _totalMinutes = snapshot.TotalMinutes;
            _year = snapshot.Year;
            _seasonIndex = snapshot.SeasonIndex;
            _seasonStartDay = snapshot.SeasonStartDay;
            _seasonAdvancePending = snapshot.SeasonAdvancePending;
            MarkJumped();
            var now = Now;
            Dispatch(() => Raise(Restored, now));
        }

        /// <summary>Moves one step and raises its events. Returns true if a fast forward reached its target.</summary>
        private bool Step()
        {
            _totalMinutes += _minutesPerStep;
            bool dayChanged = _totalMinutes % CalendarDate.MinutesPerDay == 0;
            bool seasonChanged = false;
            bool yearChanged = false;
            if (dayChanged)
            {
                ProcessDayBoundary(out seasonChanged, out yearChanged);
            }

            var now = Now;
            bool hourChanged = now.ClockMinute % 60 == 0;
            bool weekChanged = dayChanged && now.Day % _weekLength == 0;
            var tick = new TickInfo(now, _totalMinutes - _lastUpdateMinutes, Speed, _isFastForwarding);
            _lastUpdateMinutes = _totalMinutes;

            bool completed = _isFastForwarding && _fastForwardTarget == _totalMinutes;
            if (completed)
            {
                StopFastForwardNow();
            }

            Dispatch(() =>
            {
                Raise(Ticked, tick);
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

                if (completed)
                {
                    Raise(FastForwardCompleted, now);
                }
            });
            return completed;
        }

        /// <summary>
        /// Applies the season rules at a day boundary. _totalMinutes must already be at the new day.
        /// Shared by stepping and skipping.
        /// </summary>
        private void ProcessDayBoundary(out bool seasonChanged, out bool yearChanged)
        {
            seasonChanged = false;
            yearChanged = false;
            long day = _totalMinutes / CalendarDate.MinutesPerDay;
            int length = _seasons[_seasonIndex].LengthInDays;
            if (!_seasonAdvancePending && (length <= 0 || day - _seasonStartDay < length))
            {
                return;
            }

            seasonChanged = true;
            _seasonAdvancePending = false;
            _seasonStartDay = day;
            _seasonIndex++;
            if (_seasonIndex >= _seasons.Length)
            {
                _seasonIndex = 0;
                _year++;
                yearChanged = true;
            }
        }

        private void StartFastForward(float speed, long? target)
        {
            _isFastForwarding = true;
            _fastForwardSpeed = Math.Min(speed, _maxSpeed);
            _fastForwardTarget = target;
        }

        private void StopFastForwardNow()
        {
            _isFastForwarding = false;
            _fastForwardTarget = null;
        }

        /// <summary>Time jumped (skip or restore): the next update counts from here and gathered time is dropped.</summary>
        private void MarkJumped()
        {
            _lastUpdateMinutes = _totalMinutes;
            _pendingMinutes = 0;
            _jumped = true;
        }

        private static void ValidateFastForwardSpeed(float speed)
        {
            if (speed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(speed), speed, "Must be greater than 0.");
            }
        }

        private void ValidateTarget(long targetMinutes, string paramName)
        {
            if (targetMinutes < _totalMinutes)
            {
                throw new ArgumentOutOfRangeException(paramName, targetMinutes, $"Must not be before now ({_totalMinutes}).");
            }

            if (targetMinutes % _minutesPerStep != 0)
            {
                throw new ArgumentException($"Must be a multiple of the step ({_minutesPerStep} minutes).", paramName);
            }
        }

        /// <summary>Queues the command if an event is being raised. Arguments are checked when it runs.</summary>
        private bool TryEnqueue(Action command)
        {
            if (_dispatchDepth == 0)
            {
                return false;
            }

            _commands.Enqueue(command);
            return true;
        }

        /// <summary>Raises events, then runs the commands handlers queued (including ones queued by those commands).</summary>
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

            if (_dispatchDepth > 0)
            {
                return;
            }

            while (_commands.Count > 0)
            {
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

        private static void Raise<T>(Action<T> handlers, T arg)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (var handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<T>)handler)(arg);
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

            foreach (var handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<T1, T2>)handler)(arg1, arg2);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
