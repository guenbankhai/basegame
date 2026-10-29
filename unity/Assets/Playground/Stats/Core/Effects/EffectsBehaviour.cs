using UnityEngine;

namespace BaseGame.Playground.Stats
{
    /// <summary>
    /// Thin driver: builds an EffectCollection on the StatsBehaviour's collection in Awake. Runs right after
    /// StatsBehaviour so other components can read Collection in their own Awake. Never calls Advance; time is the
    /// game's job. A null StatsBehaviour.Collection logs an error and disables the component.
    /// </summary>
    [DefaultExecutionOrder(-99)]
    [RequireComponent(typeof(StatsBehaviour))]
    public sealed class EffectsBehaviour : MonoBehaviour
    {
        public EffectCollection Collection { get; private set; }

        private void Awake()
        {
            // StatsBehaviour runs earlier (-100), so its Collection is already built or null.
            StatCollection stats = GetComponent<StatsBehaviour>().Collection;
            if (stats == null)
            {
                Debug.LogError($"{nameof(EffectsBehaviour)} on '{name}': {nameof(StatsBehaviour)} has no collection.", this);
                enabled = false;
                return;
            }

            Collection = new EffectCollection(stats);
        }
    }
}
