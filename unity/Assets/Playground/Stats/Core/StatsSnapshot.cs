using System;
using UnityEngine;

namespace BaseGame.Playground.Stats
{
    /// <summary>
    /// Base values that differ from settings, and the current of every resource. Modifiers are not saved: each source
    /// re-applies its own before StatCollection.Restore.
    /// </summary>
    [Serializable]
    public sealed class StatsSnapshot
    {
        [SerializeField] private StatEntry[] _stats = Array.Empty<StatEntry>();
        [SerializeField] private ResourceEntry[] _resources = Array.Empty<ResourceEntry>();

        public StatEntry[] Stats { get => _stats; set => _stats = value; }
        public ResourceEntry[] Resources { get => _resources; set => _resources = value; }

        [Serializable]
        public sealed class StatEntry
        {
            [SerializeField] private string _id = "";
            [SerializeField] private float _baseValue = 0f;

            public StatEntry()
            {
            }

            public StatEntry(string id, float baseValue)
            {
                _id = id;
                _baseValue = baseValue;
            }

            public string Id { get => _id; set => _id = value; }
            public float BaseValue { get => _baseValue; set => _baseValue = value; }
        }

        [Serializable]
        public sealed class ResourceEntry
        {
            [SerializeField] private string _id = "";
            [SerializeField] private float _current = 0f;

            public ResourceEntry()
            {
            }

            public ResourceEntry(string id, float current)
            {
                _id = id;
                _current = current;
            }

            public string Id { get => _id; set => _id = value; }
            public float Current { get => _current; set => _current = value; }
        }
    }
}
