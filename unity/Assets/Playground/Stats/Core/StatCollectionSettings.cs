using System.Collections.Generic;

namespace BaseGame.Playground.Stats
{
    public sealed class StatCollectionSettings
    {
        public List<StatSettings> Stats { get; set; } = new List<StatSettings>();
        public List<ResourceSettings> Resources { get; set; } = new List<ResourceSettings>();
    }
}
