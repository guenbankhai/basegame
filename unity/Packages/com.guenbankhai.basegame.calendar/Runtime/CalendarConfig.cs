using UnityEngine;

namespace BaseGame.Calendar
{
    /// <summary>Inspector wrapper for CalendarSettings. Holds no runtime state, since domain reload is disabled.</summary>
    [CreateAssetMenu(fileName = "CalendarConfig", menuName = "BaseGame/Calendar Config")]
    public class CalendarConfig : ScriptableObject
    {
        [SerializeField] private CalendarSettings _settings = new CalendarSettings();

        /// <summary>A copy, because ScriptableObject changes made in Play mode persist in the Editor.</summary>
        public CalendarSettings CreateSettings() => JsonUtility.FromJson<CalendarSettings>(JsonUtility.ToJson(_settings));

        private void OnValidate()
        {
            if (_settings != null && _settings.TryGetError(out string error))
            {
                Debug.LogWarning($"{name}: {error}", this);
            }
        }
    }
}
