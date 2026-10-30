namespace BaseGame.Playground.Stats
{
    /// <summary>What happens to the losers of an effect group.</summary>
    /// <remarks>Assets store the number: append new values, never renumber.</remarks>
    public enum GroupPolicy
    {
        Replace = 0,
        Suppress = 1,
    }
}
