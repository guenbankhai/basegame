namespace BaseGame.Playground.Stats
{
    /// <summary>What re-applying a running effect (same EffectId) does.</summary>
    /// <remarks>Assets store the number: append new values, never renumber.</remarks>
    public enum StackRule
    {
        Refresh = 0,
        Extend = 1,
        Ignore = 2,
        Stack = 3,
    }
}
