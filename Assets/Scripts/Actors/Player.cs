using System.Collections.Generic;
using Cartography;
using Common;
using Common.Components;
using Enums;
using GoRogue;
using GoRogue.GameFramework;
using Scenes;
using UnityEngine;

namespace Actors
{
    public class Player : Actor
    {
        public static readonly Dictionary<KeyCode, Direction> MovementKeyMapping = new Dictionary<KeyCode, Direction>()
        {
            {KeyCode.Keypad8, Direction.UP},
            {KeyCode.Keypad9, Direction.UP_RIGHT},
            {KeyCode.Keypad6, Direction.RIGHT},
            {KeyCode.Keypad3, Direction.DOWN_RIGHT},
            {KeyCode.Keypad2, Direction.DOWN},
            {KeyCode.Keypad1, Direction.DOWN_LEFT},
            {KeyCode.Keypad4, Direction.LEFT},
            {KeyCode.Keypad7, Direction.UP_LEFT}
        };
        
        public Player(Coord pos, string name) : base(pos, name, 1, S.BARBARIAN)
        {
            Moved += OnPlayerMove;
            Stats[StatTypes.MAX_HEALTH] = 10;
            Stats[StatTypes.HEALTH] = 10;
            Stats[StatTypes.AWARENESS] = 4;
            this.AddObserver(OnHealthChanged, Stats.StatChangedNotification(StatTypes.HEALTH));
        }

        private void OnHealthChanged(object sender, object args)
        {
            var stats = sender as Stats;
            Debug.Log(stats.Parent);
        }

        private void OnPlayerMove(object sender, ItemMovedEventArgs<IGameObject> e)
        {
            // Engine.E.Map.CalculateFOV(Position, Awareness);
            CurrentMap.CalculateFOV(Position, Stats[StatTypes.AWARENESS]);
        }

        public override void OnMapChanged(Map newMap)
        {
            base.OnMapChanged(newMap);
            AddComponent(new AttackAction((GameMap)newMap, 1, 5));
        }
    }
}