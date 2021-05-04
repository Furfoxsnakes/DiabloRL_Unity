using Actors;
using Cartography;
using GoRogue;
using UnityEngine;

namespace Common.Components
{
    public class AttackAction : ActionComponent<Actor>
    {
        public int Distance { get; private set; }
        public int Amount { get; private set; }
        
        public AttackAction(GameMap map, int distance, int amount) : base(map)
        {
            Distance = distance;
            Amount = amount;
        }
        
        public override bool Do(Coord destination)
        {
            // TODO : do an attack
            var target = Map.GetEntity<Actor>(destination);
            if (target == null)
            {
                Debug.Log($"Could not find a valid target at {destination}");
                return false;
            }

            target.TakeDamage(Amount);
            
            return true;
        }
    }
}