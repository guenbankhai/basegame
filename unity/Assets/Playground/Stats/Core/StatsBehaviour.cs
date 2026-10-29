using System;
using UnityEngine;

namespace BaseGame.Playground.Stats
{
    /// <summary>
    /// Thin driver: builds a StatCollection from the profile in Awake. Runs early so other components can read
    /// Collection in their own Awake. Bad config logs an error, disables the component and leaves Collection null.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class StatsBehaviour : MonoBehaviour
    {
        [SerializeField] private StatProfile _profile = null;

        public StatCollection Collection { get; private set; }

        private void Awake()
        {
            if (_profile == null)
            {
                Debug.LogError($"{nameof(StatsBehaviour)} on '{name}': no {nameof(StatProfile)} assigned.", this);
                enabled = false;
                return;
            }

            try
            {
                Collection = new StatCollection(_profile.CreateSettings());
            }
            catch (ArgumentException e)
            {
                Debug.LogError($"{nameof(StatsBehaviour)} on '{name}': profile '{_profile.name}' is invalid. {e.Message}", this);
                enabled = false;
            }
        }
    }
}
