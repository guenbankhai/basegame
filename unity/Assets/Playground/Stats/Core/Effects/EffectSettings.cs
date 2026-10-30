using System;
using System.Collections.Generic;

namespace BaseGame.Playground.Stats
{
    public sealed class EffectSettings
    {
        public EffectSettings()
        {
        }

        public EffectSettings(EffectId id, StackRule stackRule)
        {
            Id = id;
            StackRule = stackRule;
        }

        public EffectId Id { get; set; }
        /// <summary>Null, empty or whitespace falls back to Id.Value.</summary>
        public string DisplayName { get; set; }
        /// <summary>In the game's own time unit; +infinity means it lasts until removed.</summary>
        public double Duration { get; set; } = double.PositiveInfinity;
        /// <summary>Cap for Extend.</summary>
        public double MaxDuration { get; set; } = double.PositiveInfinity;
        public StackRule StackRule { get; set; }
        /// <summary>Used by Stack.</summary>
        public int MaxStacks { get; set; } = 1;
        /// <summary>Null stacks freely with every other effect.</summary>
        public EffectGroupSettings Group { get; set; }
        public int Rank { get; set; }
        public List<EffectModifierSettings> Modifiers { get; set; } = new List<EffectModifierSettings>();

        internal bool IsInfinite => double.IsPositiveInfinity(Duration);
        /// <summary>Most stacks the instance can reach; only Stack goes past 1.</summary>
        internal int StackCap => StackRule == StackRule.Stack ? MaxStacks : 1;
        /// <summary>Most Remaining the instance can reach; only Extend goes past Duration.</summary>
        internal double RemainingCap => StackRule == StackRule.Extend ? MaxDuration : Duration;

        internal EffectSettings Clone()
        {
            var copy = new EffectSettings(Id, StackRule)
            {
                DisplayName = DisplayName,
                Duration = Duration,
                MaxDuration = MaxDuration,
                MaxStacks = MaxStacks,
                Group = Group?.Clone(),
                Rank = Rank,
                Modifiers = null,
            };

            if (Modifiers != null)
            {
                copy.Modifiers = new List<EffectModifierSettings>(Modifiers.Count);
                foreach (EffectModifierSettings modifier in Modifiers)
                {
                    copy.Modifiers.Add(modifier?.Clone());
                }
            }

            return copy;
        }

        internal bool TryGetError(out string error)
        {
            error = GetError();
            return error != null;
        }

        private string GetError()
        {
            if (!Id.IsValid)
            {
                return "Effect has an invalid ID.";
            }

            if (!(Duration > 0))
            {
                return $"Effect '{Id}': Duration must be > 0, got {Duration}.";
            }

            if (!Enum.IsDefined(typeof(StackRule), StackRule))
            {
                return $"Effect '{Id}': unknown StackRule {StackRule}.";
            }

            // MaxDuration and MaxStacks only mean something to their own rule; other rules ignore them entirely.
            if (StackRule == StackRule.Extend && !(MaxDuration >= Duration))
            {
                return $"Effect '{Id}': MaxDuration must be >= Duration for Extend, got {MaxDuration}.";
            }

            if (StackRule == StackRule.Stack && MaxStacks < 1)
            {
                return $"Effect '{Id}': MaxStacks must be >= 1 for Stack, got {MaxStacks}.";
            }

            if (Group != null && (string.IsNullOrWhiteSpace(Group.Id) || !Enum.IsDefined(typeof(GroupPolicy), Group.Policy)))
            {
                return $"Effect '{Id}': group needs a non-empty Id and a valid Policy.";
            }

            if (Modifiers == null)
            {
                return $"Effect '{Id}': Modifiers must not be null.";
            }

            foreach (EffectModifierSettings modifier in Modifiers)
            {
                if (modifier == null || !modifier.Stat.IsValid || !Enum.IsDefined(typeof(ModifierType), modifier.Type) ||
                    !float.IsFinite(modifier.Value))
                {
                    return $"Effect '{Id}': a modifier is null or has an invalid stat, type or value.";
                }

                // Same float multiply the instance uses, so settings that pass never throw at the stack cap.
                if (modifier.Type == ModifierType.PercentMult && (modifier.Value < -1f || modifier.Value * StackCap < -1f))
                {
                    return $"Effect '{Id}': PercentMult {modifier.Value} x {StackCap} stack(s) must be >= -1.";
                }
            }

            return null;
        }
    }
}
