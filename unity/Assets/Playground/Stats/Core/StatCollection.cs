using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BaseGame.Playground.Stats
{
    /// <summary>
    /// Stats and resources of one entity. Every command updates all values first, then raises events: stats in
    /// registration order, then resources, then whatever the caller adds (effect events). Commands called from a
    /// handler run nested at once; the depth counter is shared with EffectCollection.
    /// </summary>
    public sealed class StatCollection
    {
        /// <summary>Only stops handlers that call commands in a loop; a real game never gets near it.</summary>
        public const int MaxEventDepth = 32;

        private readonly List<Stat> _stats = new List<Stat>();
        private readonly List<Resource> _resources = new List<Resource>();
        private readonly Dictionary<StatId, Stat> _statById = new Dictionary<StatId, Stat>();
        private readonly Dictionary<ResourceId, Resource> _resourceById = new Dictionary<ResourceId, Resource>();

        private int _eventDepth;
        private bool _silenceResources;

        public StatCollection(StatCollectionSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (TryGetError(settings, out string error))
            {
                throw new ArgumentException(error, nameof(settings));
            }

            foreach (StatSettings statSettings in settings.Stats)
            {
                var stat = new Stat(this, _stats.Count, statSettings);
                _stats.Add(stat);
                _statById.Add(stat.Id, stat);
            }

            foreach (ResourceSettings resourceSettings in settings.Resources)
            {
                var resource = new Resource(this, _resources.Count, resourceSettings, _statById[resourceSettings.MaxStat]);
                _resources.Add(resource);
                _resourceById.Add(resource.Id, resource);
            }

            Stats = new ReadOnlyCollection<Stat>(_stats);
            Resources = new ReadOnlyCollection<Resource>(_resources);
        }

        public IReadOnlyList<Stat> Stats { get; }
        public IReadOnlyList<Resource> Resources { get; }

        internal bool IsRaisingEvents => _eventDepth > 0;

        /// <summary>End of Restore; resources were silent during it.</summary>
        public event Action Restored;

        public Stat GetStat(StatId id)
        {
            ThrowIfInvalid(id);
            if (!_statById.TryGetValue(id, out Stat stat))
            {
                throw new ArgumentException($"No stat '{id}' in this collection.", nameof(id));
            }

            return stat;
        }

        public bool TryGetStat(StatId id, out Stat stat)
        {
            ThrowIfInvalid(id);
            return _statById.TryGetValue(id, out stat);
        }

        public Resource GetResource(ResourceId id)
        {
            ThrowIfInvalid(id);
            if (!_resourceById.TryGetValue(id, out Resource resource))
            {
                throw new ArgumentException($"No resource '{id}' in this collection.", nameof(id));
            }

            return resource;
        }

        public bool TryGetResource(ResourceId id, out Resource resource)
        {
            ThrowIfInvalid(id);
            return _resourceById.TryGetValue(id, out resource);
        }

        /// <summary>Removes every modifier of the source from every stat; each stat raises at most one event.</summary>
        /// <returns>Number of modifiers removed.</returns>
        public int RemoveAllFromSource(object source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            ThrowIfTooDeep();
            var change = new StatChangeSet();
            int removed = 0;
            foreach (Stat stat in _stats)
            {
                removed += stat.RemoveAllFromSourceRaw(source, change);
            }

            Commit(change, null);
            return removed;
        }

        public StatsSnapshot CaptureSnapshot()
        {
            var stats = new List<StatsSnapshot.StatEntry>();
            foreach (Stat stat in _stats)
            {
                // Bases equal to settings are left out, so tuning the profile reaches old saves.
                if (stat.BaseValue != stat.DefaultBaseValue)
                {
                    stats.Add(new StatsSnapshot.StatEntry(stat.Id.Value, stat.BaseValue));
                }
            }

            var resources = new StatsSnapshot.ResourceEntry[_resources.Count];
            for (int i = 0; i < _resources.Count; i++)
            {
                resources[i] = new StatsSnapshot.ResourceEntry(_resources[i].Id.Value, _resources[i].Current);
            }

            return new StatsSnapshot { Stats = stats.ToArray(), Resources = resources };
        }

        /// <summary>
        /// Call after every modifier source has restored. 1: set bases and raise Stat.ValueChanged so derived-stat
        /// handlers rebuild max; 2: set currents against the final max; 3: raise Restored. Resources stay silent.
        /// </summary>
        public void Restore(StatsSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            ThrowIfRaisingEvents(nameof(Restore));
            if (TryGetError(snapshot, out string error, out var bases, out var currents))
            {
                throw new ArgumentException(error, nameof(snapshot));
            }

            _silenceResources = true;
            try
            {
                var change = new StatChangeSet();
                foreach (Stat stat in _stats)
                {
                    stat.SetBaseRaw(bases.TryGetValue(stat.Id, out float value) ? value : stat.DefaultBaseValue, change);
                }

                Commit(change, null);

                // Resources missing from the save start like a new entity, against the max after step 1.
                var discard = new StatChangeSet();
                foreach (Resource resource in _resources)
                {
                    resource.SetCurrentRaw(currents.TryGetValue(resource.Id, out float value) ? value : resource.InitialValue, discard);
                }
            }
            finally
            {
                _silenceResources = false;
                foreach (Resource resource in _resources)
                {
                    resource.ResetReported();
                }
            }

            RaiseInEventScope(() => EventInvoker.Invoke(Restored));
        }

        internal void ThrowIfTooDeep()
        {
            if (_eventDepth >= MaxEventDepth)
            {
                throw new InvalidOperationException(
                    $"Event depth reached {nameof(MaxEventDepth)} ({MaxEventDepth}): handlers are calling commands in a loop.");
            }
        }

        internal void ThrowIfRaisingEvents(string command)
        {
            if (_eventDepth > 0)
            {
                throw new InvalidOperationException(
                    $"{command} cannot be called from a Stats or Effects event handler; defer it to a later frame.");
            }
        }

        /// <summary>Clamps every resource to its final max, then raises pending events, then raiseAfter.</summary>
        internal void Commit(StatChangeSet change, Action raiseAfter)
        {
            foreach (Resource resource in _resources)
            {
                resource.ClampToRange(change);
            }

            change.Stats.Sort((a, b) => a.Index.CompareTo(b.Index));
            change.Resources.Sort((a, b) => a.Index.CompareTo(b.Index));
            RaiseInEventScope(() =>
            {
                foreach (Stat stat in change.Stats)
                {
                    stat.RaiseIfChanged();
                }

                if (!_silenceResources)
                {
                    foreach (Resource resource in change.Resources)
                    {
                        resource.RaiseIfChanged();
                    }
                }

                raiseAfter?.Invoke();
            });
        }

        internal void RaiseInEventScope(Action raise)
        {
            _eventDepth++;
            try
            {
                raise();
            }
            finally
            {
                _eventDepth--;
            }
        }

        private static void ThrowIfInvalid(StatId id)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("default(StatId) is not a valid ID.", nameof(id));
            }
        }

        private static void ThrowIfInvalid(ResourceId id)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("default(ResourceId) is not a valid ID.", nameof(id));
            }
        }

        private static bool TryGetError(StatCollectionSettings settings, out string error)
        {
            if (settings.Stats == null || settings.Resources == null)
            {
                error = "Stats and Resources must not be null.";
                return true;
            }

            var statIds = new HashSet<StatId>();
            foreach (StatSettings stat in settings.Stats)
            {
                if (stat == null)
                {
                    error = "Stats contains a null element.";
                    return true;
                }

                if (!stat.Id.IsValid)
                {
                    error = "A stat has an invalid ID.";
                    return true;
                }

                if (!statIds.Add(stat.Id))
                {
                    error = $"Duplicate stat ID '{stat.Id}'.";
                    return true;
                }

                if (!float.IsFinite(stat.BaseValue))
                {
                    error = $"Stat '{stat.Id}': BaseValue must be finite.";
                    return true;
                }

                if (float.IsNaN(stat.Min) || float.IsNaN(stat.Max) || stat.Min > stat.Max)
                {
                    error = $"Stat '{stat.Id}': Min and Max must not be NaN, and Min <= Max (got {stat.Min}, {stat.Max}).";
                    return true;
                }
            }

            var resourceIds = new HashSet<ResourceId>();
            foreach (ResourceSettings resource in settings.Resources)
            {
                if (resource == null)
                {
                    error = "Resources contains a null element.";
                    return true;
                }

                if (!resource.Id.IsValid)
                {
                    error = "A resource has an invalid ID.";
                    return true;
                }

                if (!resourceIds.Add(resource.Id))
                {
                    error = $"Duplicate resource ID '{resource.Id}'.";
                    return true;
                }

                if (!resource.MaxStat.IsValid || !statIds.Contains(resource.MaxStat))
                {
                    error = $"Resource '{resource.Id}': MaxStat '{resource.MaxStat}' is not a stat in these settings.";
                    return true;
                }

                if (!float.IsFinite(resource.Min))
                {
                    error = $"Resource '{resource.Id}': Min must be finite.";
                    return true;
                }

                if (!(resource.InitialFraction >= 0f && resource.InitialFraction <= 1f))
                {
                    error = $"Resource '{resource.Id}': InitialFraction must be in [0, 1], got {resource.InitialFraction}.";
                    return true;
                }
            }

            error = null;
            return false;
        }

        private bool TryGetError(
            StatsSnapshot snapshot, out string error,
            out Dictionary<StatId, float> bases, out Dictionary<ResourceId, float> currents)
        {
            bases = new Dictionary<StatId, float>();
            currents = new Dictionary<ResourceId, float>();
            if (snapshot.Stats == null || snapshot.Resources == null)
            {
                error = "Snapshot arrays must not be null.";
                return true;
            }

            foreach (StatsSnapshot.StatEntry entry in snapshot.Stats)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id))
                {
                    error = "Snapshot has a null stat entry or an invalid stat ID.";
                    return true;
                }

                var id = new StatId(entry.Id);
                if (!float.IsFinite(entry.BaseValue))
                {
                    error = $"Snapshot stat '{id}': base must be finite.";
                    return true;
                }

                if (!_statById.ContainsKey(id))
                {
                    error = $"Snapshot stat '{id}' is not in this collection.";
                    return true;
                }

                if (bases.ContainsKey(id))
                {
                    error = $"Snapshot has stat '{id}' twice.";
                    return true;
                }

                bases.Add(id, entry.BaseValue);
            }

            foreach (StatsSnapshot.ResourceEntry entry in snapshot.Resources)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id))
                {
                    error = "Snapshot has a null resource entry or an invalid resource ID.";
                    return true;
                }

                var id = new ResourceId(entry.Id);
                if (!float.IsFinite(entry.Current))
                {
                    error = $"Snapshot resource '{id}': current must be finite.";
                    return true;
                }

                if (!_resourceById.ContainsKey(id))
                {
                    error = $"Snapshot resource '{id}' is not in this collection.";
                    return true;
                }

                if (currents.ContainsKey(id))
                {
                    error = $"Snapshot has resource '{id}' twice.";
                    return true;
                }

                currents.Add(id, entry.Current);
            }

            error = null;
            return false;
        }
    }
}
