using GuenBanKhai.Calendar;
using UnityEngine;

namespace BaseGame.Playground.Calendar
{
    /// <summary>Feeds FakeNeed the way an accumulating module must: from both Ticked and Skipped.</summary>
    [RequireComponent(typeof(GameClockDriver))]
    public class FakeNeedListener : MonoBehaviour
    {
        [SerializeField] private int _max = 1000;

        private GameClockDriver _driver;
        private GameClock _clock;

        public FakeNeed Need { get; private set; }

        private void Awake()
        {
            _driver = GetComponent<GameClockDriver>();
            Need = new FakeNeed(_max);
        }

        private void OnEnable()
        {
            if (_driver.Clock == null)
            {
                Debug.LogError($"{nameof(FakeNeedListener)} on '{name}' has no running {nameof(GameClock)}.", this);
                enabled = false;
                return;
            }

            _clock = _driver.Clock;
            _clock.Ticked += OnTicked;
            _clock.Skipped += OnSkipped;
        }

        private void OnDisable()
        {
            if (_clock == null)
            {
                return;
            }

            _clock.Ticked -= OnTicked;
            _clock.Skipped -= OnSkipped;
            _clock = null;
        }

        private void OnTicked(TickInfo info)
        {
            Need.Advance(info.DeltaMinutes, info.Date.TotalMinutes);
        }

        private void OnSkipped(CalendarDate from, CalendarDate to)
        {
            Need.Advance(to.TotalMinutes - from.TotalMinutes, to.TotalMinutes);
        }
    }
}
