using System;
using UnityEngine;

namespace GuenBanKhai.Calendar
{
    /// <summary>
    /// Feeds the clock Time.deltaTime, so timeScale 0 also stops it. Runs early so other components can read Clock in
    /// their own Awake/OnEnable.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameClockDriver : MonoBehaviour
    {
        [SerializeField] private CalendarConfig _config = null;

        /// <summary>Null if the config is missing or invalid; the driver then disables itself.</summary>
        public GameClock Clock { get; private set; }

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogError($"{nameof(GameClockDriver)} '{name}': no {nameof(CalendarConfig)} assigned.", this);
                enabled = false;
                return;
            }

            try
            {
                Clock = new GameClock(_config.CreateSettings());
            }
            catch (ArgumentException e)
            {
                Debug.LogError($"{nameof(GameClockDriver)} '{name}': config '{_config.name}' is invalid. {e.Message}", this);
                enabled = false;
            }
        }

        private void Update()
        {
            // Re-enabled after a failed Awake.
            if (Clock == null)
            {
                enabled = false;
                return;
            }

            Clock.Tick(Time.deltaTime);
        }
    }
}
