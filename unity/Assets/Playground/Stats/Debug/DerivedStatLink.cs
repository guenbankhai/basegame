using System;

namespace BaseGame.Playground.Stats
{
    /// <summary>
    /// target += source * factor, wired the way the README tells a game to: one Flat modifier with its own source,
    /// swapped with ReplaceModifier so a resource using target as max is not clamped midway.
    /// </summary>
    public sealed class DerivedStatLink : IDisposable
    {
        private readonly Stat _source;
        private readonly Stat _target;
        private readonly float _factor;
        private Modifier _current;

        public DerivedStatLink(Stat source, Stat target, float factor)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _factor = factor;
            _current = new Modifier(ModifierType.Flat, source.Value * factor, this);
            _target.AddModifier(_current);
            _source.ValueChanged += OnSourceChanged;
        }

        public void Dispose()
        {
            _source.ValueChanged -= OnSourceChanged;
            _target.RemoveModifier(_current);
        }

        private void OnSourceChanged(float oldValue, float newValue)
        {
            var next = new Modifier(ModifierType.Flat, newValue * _factor, this);
            _target.ReplaceModifier(_current, next);
            _current = next;
        }
    }
}
