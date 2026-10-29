namespace BaseGame.Playground.Stats
{
    /// <summary>
    /// One running effect; at most one per EffectId. It is the Source of every modifier it adds, and keeps a copy of
    /// the settings it was first applied with.
    /// </summary>
    public sealed class EffectInstance
    {
        private readonly EffectSettings _settings;

        internal EffectInstance(EffectSettings settingsCopy, Stat[] targets)
        {
            _settings = settingsCopy;
            Targets = targets;
            AppliedModifiers = new Modifier[targets.Length];
            DisplayName = string.IsNullOrWhiteSpace(settingsCopy.DisplayName) ? settingsCopy.Id.Value : settingsCopy.DisplayName;
        }

        public EffectId Id => _settings.Id;
        public string DisplayName { get; }
        /// <summary>A new copy on every get.</summary>
        public EffectSettings Settings => _settings.Clone();
        public int Stacks { get; internal set; } = 1;
        /// <summary>+infinity for an infinite effect.</summary>
        public double Remaining { get; internal set; }
        public bool IsInfinite => double.IsPositiveInfinity(Remaining);
        public bool IsSuppressed { get; internal set; }

        internal EffectSettings SettingsRef => _settings;
        internal Stat[] Targets { get; }
        /// <summary>Modifier currently on Targets[i] for settings modifier i; null while suppressed or removed.</summary>
        internal Modifier[] AppliedModifiers { get; }
        internal int LastApplySequence { get; set; }
        internal bool IsRemoved { get; set; }
        /// <summary>Bumped on every state change, so a pending event can tell a handler changed this instance since.</summary>
        internal int Version { get; set; }

        public override string ToString() => $"{Id} x{Stacks} {(IsInfinite ? "inf" : Remaining.ToString("0.##"))}{(IsSuppressed ? " suppressed" : "")}";
    }
}
