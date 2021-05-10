using Cartography;
using Common.Components;
using Enums;
using GoRogue.GameFramework;
using UnityEngine.XR;

namespace Actors.Behaviours
{
    public class MoveAndAttackBehaviour : EnemyBehaviour
    {
        private GameMap _map;
        
        public MoveAndAttackBehaviour(Enemy owner, GameMap map) : base(owner)
        {
            owner.AddComponent(new MoveAction(map));
            owner.AddComponent(new AttackAction(map, 1,owner.Stats));
            _map = map;
        }

        public override void Do(Actor target)
        {
            Owner.FOV.Calculate(Owner.Position, Owner.Stats[StatTypes.AWARENESS]);

            if (!Owner.FOV.BooleanFOV[target.Position]) return;
            
            // first try to attack the target if possible
            var attackAction = Owner.GetComponent<AttackAction>();
            if (attackAction != null)
            {
                //check if target is in range for attack action
                var distanceToTarget = (int)_map.DistanceMeasurement.Calculate(Owner.Position, target.Position);
                if (distanceToTarget <= attackAction.Distance)
                {
                    attackAction.Do(target.Position);
                }
            }
            
            //try to move towards the target 
            var moveaction = Owner.GetComponent<MoveAction>();
            if (moveaction == null) return;
            if (moveaction.Do(target.Position)) return;
            
            // enemy could not attack nor move

            // if (Owner.MoveAction.Do(target.Position)) return;
        }
    }
}