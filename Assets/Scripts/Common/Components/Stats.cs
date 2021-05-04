using System.Collections.Generic;
using Actors;
using Enums;
using GoRogue.GameFramework;
using GoRogue.GameFramework.Components;

namespace Common.Components
{
    public class Stats : IGameObjectComponent
    {
        public IGameObject Parent { get; set; }
        public Actor Actor => Parent as Actor;

        public int this[StatTypes type]
        {
            get => _data[(int) type];
            set => SetValue(type, value);
        }

        private readonly int[] _data = new int[(int) StatTypes.Count];

        private static Dictionary<StatTypes, string> _statChangedNotifications = new Dictionary<StatTypes, string>();
        public static string StatChangedNotification(StatTypes type)
        {
            if (!_statChangedNotifications.ContainsKey(type))
                _statChangedNotifications.Add(type, $"Stats.{type}Changed");
            return _statChangedNotifications[type];
        }
        
        private void SetValue(StatTypes type, int newValue)
        {
            var oldValue = _data[(int) type];

            if (oldValue == newValue) return;

            _data[(int) type] = newValue;
            
            // post notification
            this.PostNotification(StatChangedNotification(type), oldValue);
        }
    }
}