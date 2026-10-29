using System;
using UnityEngine;

namespace BaseGame.Playground.Stats
{
    /// <summary>
    /// Every effect, suppressed ones too, in first-apply order. Settings are looked up again through resolve, and
    /// IsSuppressed is recomputed from the groups.
    /// </summary>
    [Serializable]
    public sealed class EffectsSnapshot
    {
        [SerializeField] private EffectEntry[] _effects = Array.Empty<EffectEntry>();

        public EffectEntry[] Effects { get => _effects; set => _effects = value; }

        [Serializable]
        public sealed class EffectEntry
        {
            [SerializeField] private string _id = "";
            [SerializeField] private int _stacks = 1;
            [SerializeField] private double _remaining = 0d;
            // JsonUtility cannot round-trip infinity, so an infinite effect is a flag.
            [SerializeField] private bool _isInfinite = false;
            [SerializeField] private int _lastApplySequence = 0;

            public EffectEntry()
            {
            }

            public EffectEntry(string id, int stacks, double remaining, bool isInfinite, int lastApplySequence)
            {
                _id = id;
                _stacks = stacks;
                _remaining = remaining;
                _isInfinite = isInfinite;
                _lastApplySequence = lastApplySequence;
            }

            public string Id { get => _id; set => _id = value; }
            public int Stacks { get => _stacks; set => _stacks = value; }
            /// <summary>Ignored when IsInfinite.</summary>
            public double Remaining { get => _remaining; set => _remaining = value; }
            public bool IsInfinite { get => _isInfinite; set => _isInfinite = value; }
            public int LastApplySequence { get => _lastApplySequence; set => _lastApplySequence = value; }
        }
    }
}
