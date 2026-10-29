using System.Collections.Generic;

namespace BaseGame.Playground.Stats
{
    /// <summary>Stats and resources whose value changed during one command; their events wait until it is done.</summary>
    internal sealed class StatChangeSet
    {
        private readonly List<Stat> _stats = new List<Stat>();
        private readonly List<Resource> _resources = new List<Resource>();

        public List<Stat> Stats => _stats;
        public List<Resource> Resources => _resources;

        public void Add(Stat stat)
        {
            if (!_stats.Contains(stat))
            {
                _stats.Add(stat);
            }
        }

        public void Add(Resource resource)
        {
            if (!_resources.Contains(resource))
            {
                _resources.Add(resource);
            }
        }
    }
}
