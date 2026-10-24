using System;
using UnityEngine;

namespace GuenBanKhai.Calendar
{
    [Serializable]
    public struct SeasonDefinition
    {
        [SerializeField] private string _name;
        [Tooltip("Days until the season changes on its own. 0 = changes only through AdvanceSeason().")]
        [SerializeField] private int _lengthInDays;

        public SeasonDefinition(string name, int lengthInDays)
        {
            _name = name;
            _lengthInDays = lengthInDays;
        }

        public string Name { get => _name; set => _name = value; }
        public int LengthInDays { get => _lengthInDays; set => _lengthInDays = value; }
    }
}
