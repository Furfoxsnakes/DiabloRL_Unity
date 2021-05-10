using Actors;
using Cartography;
using Enums;
using GoRogue;
using GoRogue.DiceNotation;
using GoRogue.DiceNotation.Terms;
using UnityEngine;

namespace Common.Components
{
    public class AttackAction : ActionComponent<Actor>
    {
        public int Distance { get; private set; }
        public int Amount { get; private set; }
        private Stats _stats;
        private string _attackMessage;
        private string _defenseMessage;
        
        public AttackAction(GameMap map, int distance, Stats stats) : base(map)
        {
            Distance = distance;
            _stats = stats;
        }
        
        public override bool Do(Coord destination)
        {
            // TODO : do an attack
            var target = Map.GetEntity<Actor>(destination);
            if (target == null) return false;

            _attackMessage = string.Empty;
            _defenseMessage = string.Empty;

            var hits = ResolveAttack(target);
            var blocks = ResolveDefense(target, hits);
            Engine.E.MessageLog.AddMessage(_attackMessage);
            Engine.E.MessageLog.AddMessage(_defenseMessage);
            var damage = hits - blocks;
            if (damage > 0)
                target.TakeDamage(damage);
            // target.TakeDamage(Amount);
            
            return true;
        }

        private int ResolveAttack(Actor defender)
        {
            var hits = 0;

            _attackMessage += $"{Owner.Name} attacks {defender.Name} and rolls: ";

            for (var i = 0; i < _stats[StatTypes.ATTACK]; i++)
            {
                var result = Dice.Roll("1d100");
                _attackMessage += $"{result}, ";
                if (result <= _stats[StatTypes.ATTACK_CHANCE])
                    hits++;
            }

            return hits;
        }

        private int ResolveDefense(Actor defender, int hits)
        {
            var blocks = 0;

            if (hits <= 0)
            {
                _attackMessage += " and misses completely";
                return blocks;
            }
            
            _attackMessage += $"scoring {hits} hits.";
            _defenseMessage += $"{defender.Name} defends and rolls: ";

            for (var i = 0; i < defender.Stats[StatTypes.DEFENSE]; i++)
            {
                var result = Dice.Roll("1d100");
                _defenseMessage += $"{result}, ";
                if (result <= defender.Stats[StatTypes.DEFENSE_CHANCE])
                    blocks++;
            }

            _defenseMessage += $"resulting in {blocks} blocks";

            if (blocks == hits)
                _defenseMessage += ", blocking all damage";

            return blocks;
        }
    }
}