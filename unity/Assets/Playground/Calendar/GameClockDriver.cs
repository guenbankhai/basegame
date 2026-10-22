using UnityEngine;

namespace BaseGame.Playground.Calendar
{
    /// <summary>
    /// Thin MonoBehaviour that owns a GameClock and feeds it Time.deltaTime, so timeScale 0 also stops the clock.
    /// Runs early so other components can read Clock in their own Awake/OnEnable.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameClockDriver : MonoBehaviour
    {
        [SerializeField] private CalendarConfig _config;

        public CalendarConfig Config => _config;
        public GameClock Clock { get; private set; }

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogError($"{nameof(GameClockDriver)} on '{name}' has no {nameof(CalendarConfig)}.", this);
                enabled = false;
                return;
            }

            Clock = new GameClock(_config);
        }

        private void Update()
        {
            Clock.Tick(Time.deltaTime);
        }
    }
}
