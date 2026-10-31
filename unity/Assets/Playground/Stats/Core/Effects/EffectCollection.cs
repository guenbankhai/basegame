using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace BaseGame.Playground.Stats
{
    /// <summary>
    /// Running effects of one entity. Each command changes modifiers in one batch through StatCollection: every stat
    /// recalculates once and resources clamp against the final max, then stat, resource and effect events follow.
    /// Within effect events, losers come before the winner.
    /// </summary>
    public sealed class EffectCollection
    {
        private readonly StatCollection _stats;
        private readonly List<EffectInstance> _instances = new List<EffectInstance>();
        private int _applySequence;

        public EffectCollection(StatCollection stats)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            Instances = new ReadOnlyCollection<EffectInstance>(_instances);
        }

        /// <summary>Suppressed ones included, in first-apply order.</summary>
        public IReadOnlyList<EffectInstance> Instances { get; }

        /// <summary>Smallest Remaining among timed effects, suppressed ones included; +infinity if none.</summary>
        public double TimeUntilNextExpiry
        {
            get
            {
                double min = double.PositiveInfinity;
                foreach (EffectInstance instance in _instances)
                {
                    min = Math.Min(min, instance.Remaining);
                }

                return min;
            }
        }

        public event Action<EffectInstance> Applied;
        public event Action<EffectInstance> Stacked;
        public event Action<EffectInstance> Refreshed;
        public event Action<EffectInstance> Suppressed;
        public event Action<EffectInstance> Resumed;
        public event Action<EffectInstance, EffectRemovalReason> Removed;
        public event Action Restored;

        /// <returns>The running instance, or null if a higher rank in a Replace group rejected it.</returns>
        public EffectInstance Apply(EffectSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _stats.ThrowIfTooDeep();
            EffectSettings copy = settings.Clone();
            if (copy.TryGetError(out string error) || TryGetGroupConflict(copy, out error))
            {
                throw new ArgumentException(error, nameof(settings));
            }

            var events = new PendingEvents();
            EffectInstance instance = Find(copy.Id);
            EffectInstance quiet = null;
            if (instance != null)
            {
                // A running effect keeps its first settings; the new ones only trigger its StackRule.
                if (!Reapply(instance, events))
                {
                    return instance;
                }
            }
            else
            {
                if (LosesInReplaceGroup(copy))
                {
                    return null;
                }

                instance = new EffectInstance(copy, ResolveTargets(copy))
                {
                    Remaining = copy.Duration,
                    LastApplySequence = ++_applySequence,
                };
                _instances.Add(instance);
                LogSkippedModifiers(instance);
                events.Main.Add(new PendingEvent(EffectEventKind.Applied, instance));
                // A new effect that loses straight away reports only Applied.
                quiet = instance;
            }

            var touched = new List<EffectInstance> { instance };
            ResolveGroup(instance.SettingsRef.Group, quiet, events, touched);
            CommitChanges(touched, events, null);
            return instance;
        }

        /// <returns>False if the effect is not running.</returns>
        public bool Remove(EffectId id)
        {
            ThrowIfInvalid(id);
            _stats.ThrowIfTooDeep();
            EffectInstance instance = Find(id);
            if (instance == null)
            {
                return false;
            }

            var events = new PendingEvents();
            var touched = new List<EffectInstance> { instance };
            RemoveInstance(instance);
            events.Losers.Add(new PendingEvent(EffectEventKind.Removed, instance, EffectRemovalReason.Removed));
            ResolveGroup(instance.SettingsRef.Group, null, events, touched);
            CommitChanges(touched, events, null);
            return true;
        }

        public bool Has(EffectId id)
        {
            ThrowIfInvalid(id);
            return Find(id) != null;
        }

        public bool TryGet(EffectId id, out EffectInstance instance)
        {
            ThrowIfInvalid(id);
            instance = Find(id);
            return instance != null;
        }

        /// <summary>
        /// Counts down every timed effect (suppressed too), removes the expired ones in expiry order, then resumes
        /// group winners. An effect that resumes and expires within one call raises no Resumed; cut delta at
        /// TimeUntilNextExpiry for every event.
        /// </summary>
        public void Advance(double delta)
        {
            if (!(delta >= 0) || double.IsInfinity(delta))
            {
                throw new ArgumentException($"Delta must be finite and >= 0, got {delta}.", nameof(delta));
            }

            _stats.ThrowIfRaisingEvents(nameof(Advance));
            if (delta == 0)
            {
                return;
            }

            var expired = new List<(EffectInstance Instance, double Before)>();
            foreach (EffectInstance instance in _instances)
            {
                if (instance.IsInfinite)
                {
                    continue;
                }

                double before = instance.Remaining;
                instance.Remaining -= delta;
                if (instance.Remaining <= 0)
                {
                    expired.Add((instance, before));
                }
            }

            if (expired.Count == 0)
            {
                return;
            }

            // OrderBy is stable, so equal expiry times keep first-apply order.
            expired = expired.OrderBy(e => e.Before).ToList();
            var events = new PendingEvents();
            var touched = new List<EffectInstance>();
            foreach (var (instance, _) in expired)
            {
                instance.Remaining = 0;
                RemoveInstance(instance);
                touched.Add(instance);
                events.Losers.Add(new PendingEvent(EffectEventKind.Removed, instance, EffectRemovalReason.Expired));
            }

            foreach (var (instance, _) in expired)
            {
                ResolveGroup(instance.SettingsRef.Group, null, events, touched);
            }

            events.After.Sort((a, b) => _instances.IndexOf(a.Instance).CompareTo(_instances.IndexOf(b.Instance)));
            CommitChanges(touched, events, null);
        }

        public EffectsSnapshot CaptureSnapshot()
        {
            var entries = new EffectsSnapshot.EffectEntry[_instances.Count];
            for (int i = 0; i < _instances.Count; i++)
            {
                EffectInstance instance = _instances[i];
                entries[i] = new EffectsSnapshot.EffectEntry(
                    instance.Id.Value, instance.Stacks, instance.IsInfinite ? 0d : instance.Remaining, instance.IsInfinite,
                    instance.LastApplySequence);
            }

            return new EffectsSnapshot { Effects = entries };
        }

        /// <summary>
        /// Replaces every running effect with the snapshot in one batch. Restore effects before StatCollection.
        /// Stats still raise ValueChanged so derived-stat handlers update; then Restored.
        /// </summary>
        public void Restore(EffectsSnapshot snapshot, Func<EffectId, EffectSettings> resolve)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (resolve == null)
            {
                throw new ArgumentNullException(nameof(resolve));
            }

            _stats.ThrowIfRaisingEvents(nameof(Restore));
            if (TryGetError(snapshot, resolve, out string error, out var restored))
            {
                throw new ArgumentException(error, nameof(snapshot));
            }

            var touched = new List<EffectInstance>();
            foreach (EffectInstance instance in _instances)
            {
                instance.IsRemoved = true;
                instance.Version++;
                touched.Add(instance);
            }

            _instances.Clear();
            _applySequence = 0;
            foreach (EffectInstance instance in restored)
            {
                _instances.Add(instance);
                LogSkippedModifiers(instance);
                touched.Add(instance);
                _applySequence = Math.Max(_applySequence, instance.LastApplySequence);
            }

            // Suppression is recomputed silently; Restored is the only effect event.
            var discarded = new PendingEvents();
            foreach (EffectInstance instance in restored)
            {
                ResolveGroup(instance.SettingsRef.Group, null, discarded, touched);
            }

            CommitChanges(touched, new PendingEvents(), () => EventInvoker.Invoke(Restored));
        }

        private bool Reapply(EffectInstance instance, PendingEvents events)
        {
            EffectSettings settings = instance.SettingsRef;
            EffectEventKind kind;
            switch (settings.StackRule)
            {
                case StackRule.Ignore:
                    return false;
                case StackRule.Refresh:
                    instance.Remaining = settings.Duration;
                    kind = EffectEventKind.Refreshed;
                    break;
                case StackRule.Extend:
                    instance.Remaining = Math.Min(instance.Remaining + settings.Duration, settings.MaxDuration);
                    kind = EffectEventKind.Refreshed;
                    break;
                default:
                    if (instance.Stacks < settings.MaxStacks)
                    {
                        instance.Stacks++;
                        kind = EffectEventKind.Stacked;
                    }
                    else
                    {
                        kind = EffectEventKind.Refreshed;
                    }

                    instance.Remaining = settings.Duration;
                    break;
            }

            // Re-applying counts as the latest apply, so it wins rank ties in its group.
            instance.LastApplySequence = ++_applySequence;
            instance.Version++;
            events.Main.Add(new PendingEvent(kind, instance));
            return true;
        }

        private bool LosesInReplaceGroup(EffectSettings settings)
        {
            EffectGroupSettings group = settings.Group;
            if (group == null || group.Policy != GroupPolicy.Replace)
            {
                return false;
            }

            return _instances.Any(i => IsInGroup(i, group.Id) && i.SettingsRef.Rank > settings.Rank);
        }

        /// <summary>Highest rank wins, ties go to the latest apply; losers are removed or suppressed by policy.</summary>
        private void ResolveGroup(EffectGroupSettings group, EffectInstance quiet, PendingEvents events, List<EffectInstance> touched)
        {
            if (group == null)
            {
                return;
            }

            EffectInstance winner = null;
            foreach (EffectInstance instance in _instances)
            {
                if (!IsInGroup(instance, group.Id))
                {
                    continue;
                }

                if (winner == null || instance.SettingsRef.Rank > winner.SettingsRef.Rank ||
                    (instance.SettingsRef.Rank == winner.SettingsRef.Rank && instance.LastApplySequence > winner.LastApplySequence))
                {
                    winner = instance;
                }
            }

            if (winner == null)
            {
                return;
            }

            bool replace = winner.SettingsRef.Group.Policy == GroupPolicy.Replace;
            foreach (EffectInstance instance in _instances.ToArray())
            {
                if (instance == winner || !IsInGroup(instance, group.Id))
                {
                    continue;
                }

                if (replace)
                {
                    RemoveInstance(instance);
                    touched.Add(instance);
                    events.Losers.Add(new PendingEvent(EffectEventKind.Removed, instance, EffectRemovalReason.Replaced));
                }
                else if (!instance.IsSuppressed)
                {
                    instance.IsSuppressed = true;
                    instance.Version++;
                    touched.Add(instance);
                    if (instance != quiet)
                    {
                        events.Losers.Add(new PendingEvent(EffectEventKind.Suppressed, instance));
                    }
                }
            }

            if (winner.IsSuppressed)
            {
                winner.IsSuppressed = false;
                winner.Version++;
                touched.Add(winner);
                events.After.Add(new PendingEvent(EffectEventKind.Resumed, winner));
            }
        }

        private void RemoveInstance(EffectInstance instance)
        {
            instance.IsRemoved = true;
            instance.Version++;
            _instances.Remove(instance);
        }

        private void CommitChanges(List<EffectInstance> touched, PendingEvents events, Action raiseLast)
        {
            var change = new StatChangeSet();
            foreach (EffectInstance instance in touched.Distinct())
            {
                SyncModifiers(instance, change);
            }

            // Versions are taken once all changes are done; a handler that changes the instance later drops the rest.
            List<PendingEvent> pending = events.Losers.Concat(events.Main).Concat(events.After)
                .Select(e => e.WithVersion(e.Instance.Version)).ToList();
            _stats.Commit(change, () =>
            {
                foreach (PendingEvent e in pending)
                {
                    Raise(e);
                }

                raiseLast?.Invoke();
            });
        }

        /// <summary>Puts each settings modifier on its stat with Value * Stacks, or takes it off while inactive.</summary>
        private static void SyncModifiers(EffectInstance instance, StatChangeSet change)
        {
            List<EffectModifierSettings> modifiers = instance.SettingsRef.Modifiers;
            bool active = !instance.IsRemoved && !instance.IsSuppressed;
            for (int i = 0; i < modifiers.Count; i++)
            {
                Stat stat = instance.Targets[i];
                if (stat == null)
                {
                    continue;
                }

                Modifier current = instance.AppliedModifiers[i];
                Modifier desired = null;
                if (active)
                {
                    float value = modifiers[i].Value * instance.Stacks;
                    desired = current != null && current.Value == value ? current : new Modifier(modifiers[i].Type, value, instance);
                }

                if (desired == current)
                {
                    continue;
                }

                if (current == null)
                {
                    stat.AddModifierRaw(desired, change);
                }
                else if (desired == null)
                {
                    stat.RemoveModifierRaw(current, change);
                }
                else
                {
                    stat.ReplaceModifierRaw(current, desired, change);
                }

                instance.AppliedModifiers[i] = desired;
            }
        }

        private void Raise(PendingEvent e)
        {
            if (e.Instance.Version != e.Version)
            {
                return;
            }

            switch (e.Kind)
            {
                case EffectEventKind.Applied:
                    EventInvoker.Invoke(Applied, e.Instance);
                    break;
                case EffectEventKind.Stacked:
                    EventInvoker.Invoke(Stacked, e.Instance);
                    break;
                case EffectEventKind.Refreshed:
                    EventInvoker.Invoke(Refreshed, e.Instance);
                    break;
                case EffectEventKind.Suppressed:
                    EventInvoker.Invoke(Suppressed, e.Instance);
                    break;
                case EffectEventKind.Resumed:
                    EventInvoker.Invoke(Resumed, e.Instance);
                    break;
                case EffectEventKind.Removed:
                    EventInvoker.Invoke(Removed, e.Instance, e.Reason);
                    break;
            }
        }

        private EffectInstance Find(EffectId id)
        {
            foreach (EffectInstance instance in _instances)
            {
                if (instance.Id == id)
                {
                    return instance;
                }
            }

            return null;
        }

        private static bool IsInGroup(EffectInstance instance, string groupId) =>
            instance.SettingsRef.Group != null && instance.SettingsRef.Group.Id == groupId;

        private static void ThrowIfInvalid(EffectId id)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("default(EffectId) is not a valid ID.", nameof(id));
            }
        }

        /// <summary>A null target means this entity lacks the stat: one effect can serve many kinds of entity.</summary>
        private Stat[] ResolveTargets(EffectSettings settings)
        {
            var targets = new Stat[settings.Modifiers.Count];
            for (int i = 0; i < targets.Length; i++)
            {
                _stats.TryGetStat(settings.Modifiers[i].Stat, out targets[i]);
            }

            return targets;
        }

        // Only when an instance is created, so re-applying the effect does not repeat the line. Skipping is normal
        // behaviour, so release builds stay silent.
        private static void LogSkippedModifiers(EffectInstance instance)
        {
            if (!Debug.isDebugBuild)
            {
                return;
            }

            string[] missing = instance.SettingsRef.Modifiers
                .Where((_, i) => instance.Targets[i] == null)
                .Select(m => m.Stat.Value)
                .ToArray();
            if (missing.Length > 0)
            {
                Debug.Log($"Effect '{instance.Id}': skipped modifiers on missing stats: {string.Join(", ", missing)}.");
            }
        }

        private bool TryGetGroupConflict(EffectSettings settings, out string error)
        {
            EffectGroupSettings group = settings.Group;
            foreach (EffectInstance instance in _instances)
            {
                if (group != null && instance.Id != settings.Id && IsInGroup(instance, group.Id) &&
                    instance.SettingsRef.Group.Policy != group.Policy)
                {
                    error = $"Effect '{settings.Id}': group '{group.Id}' is already running with another Policy.";
                    return true;
                }
            }

            error = null;
            return false;
        }

        private bool TryGetError(
            EffectsSnapshot snapshot, Func<EffectId, EffectSettings> resolve, out string error, out List<EffectInstance> restored)
        {
            restored = new List<EffectInstance>();
            if (snapshot.Effects == null)
            {
                error = "Snapshot array must not be null.";
                return true;
            }

            var ids = new HashSet<EffectId>();
            var sequences = new HashSet<int>();
            var groupPolicies = new Dictionary<string, GroupPolicy>();
            var replaceGroupsSeen = new HashSet<string>();
            foreach (EffectsSnapshot.EffectEntry entry in snapshot.Effects)
            {
                if (entry == null || !IdFormat.IsValid(entry.Id))
                {
                    error = "Snapshot has a null entry or an invalid effect ID.";
                    return true;
                }

                var id = new EffectId(entry.Id);
                if (!ids.Add(id))
                {
                    error = $"Snapshot has effect '{id}' twice.";
                    return true;
                }

                if (entry.LastApplySequence < 0 || !sequences.Add(entry.LastApplySequence))
                {
                    error = $"Snapshot effect '{id}': apply sequence {entry.LastApplySequence} is negative or repeated.";
                    return true;
                }

                EffectSettings settings = resolve(id)?.Clone();
                if (settings == null)
                {
                    error = $"resolve returned null for effect '{id}'.";
                    return true;
                }

                if (settings.TryGetError(out error))
                {
                    error = $"resolve returned invalid settings: {error}";
                    return true;
                }

                if (settings.Id != id)
                {
                    error = $"resolve returned settings '{settings.Id}' for effect '{id}'.";
                    return true;
                }

                int maxStacks = settings.StackCap;
                if (entry.Stacks < 1 || entry.Stacks > maxStacks)
                {
                    error = $"Snapshot effect '{id}': Stacks {entry.Stacks} is outside [1, {maxStacks}].";
                    return true;
                }

                if (entry.IsInfinite != settings.IsInfinite)
                {
                    error = $"Snapshot effect '{id}': IsInfinite does not match its settings.";
                    return true;
                }

                double maxRemaining = settings.RemainingCap;
                if (!entry.IsInfinite && !(entry.Remaining > 0 && entry.Remaining <= maxRemaining))
                {
                    error = $"Snapshot effect '{id}': Remaining {entry.Remaining} is outside (0, {maxRemaining}].";
                    return true;
                }

                EffectGroupSettings group = settings.Group;
                if (group != null)
                {
                    if (groupPolicies.TryGetValue(group.Id, out GroupPolicy policy) && policy != group.Policy)
                    {
                        error = $"Snapshot has group '{group.Id}' with two Policies.";
                        return true;
                    }

                    groupPolicies[group.Id] = group.Policy;
                    if (group.Policy == GroupPolicy.Replace && !replaceGroupsSeen.Add(group.Id))
                    {
                        error = $"Snapshot has two effects in Replace group '{group.Id}'.";
                        return true;
                    }
                }

                restored.Add(new EffectInstance(settings, ResolveTargets(settings))
                {
                    Stacks = entry.Stacks,
                    Remaining = entry.IsInfinite ? double.PositiveInfinity : entry.Remaining,
                    LastApplySequence = entry.LastApplySequence,
                });
            }

            error = null;
            return false;
        }

        private sealed class PendingEvents
        {
            public List<PendingEvent> Losers { get; } = new List<PendingEvent>();
            public List<PendingEvent> Main { get; } = new List<PendingEvent>();
            public List<PendingEvent> After { get; } = new List<PendingEvent>();
        }

        private readonly struct PendingEvent
        {
            public PendingEvent(EffectEventKind kind, EffectInstance instance, EffectRemovalReason reason = default, int version = 0)
            {
                Kind = kind;
                Instance = instance;
                Reason = reason;
                Version = version;
            }

            public EffectEventKind Kind { get; }
            public EffectInstance Instance { get; }
            public EffectRemovalReason Reason { get; }
            public int Version { get; }

            public PendingEvent WithVersion(int version) => new PendingEvent(Kind, Instance, Reason, version);
        }
    }
}
