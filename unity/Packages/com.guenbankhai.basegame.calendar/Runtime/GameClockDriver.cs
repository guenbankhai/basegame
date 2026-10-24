using System;
using UnityEngine;

namespace BaseGame.Calendar
{
    /// <summary>
    /// Feeds the clock Time.deltaTime, so timeScale 0 also stops it. Runs early so other components can read Clock in
    /// their own Awake/OnEnable.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameClockDriver : MonoBehaviour
    {
        [SerializeField] private CalendarConfig _config;

        /// <summary>Null if the config is missing or invalid; the driver then disables itself.</summary>
        public GameClock Clock { get; private set; }

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogError($"{nameof(GameClockDriver)} on '{name}' has no {nameof(CalendarConfig)}.", this);
                enabled = false;
                return;
            }

            try
            {
                Clock = new GameClock(_config.CreateSettings());
            }
            catch (ArgumentException e)
            {
                Debug.LogException(e, this);
                enabled = false;
            }
        }

        private void Update()
        {
            Clock.Tick(Time.deltaTime);
        }
    }
}
