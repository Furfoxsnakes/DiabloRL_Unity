using System.Linq;
using Actors;
using Common.Components;
using GoRogue;
using UnityEngine;

namespace Common
{
    public class InputManager
    {
        /// <summary>
        /// Handles the input on player's turn
        /// </summary>
        /// <param name="keyCode"></param>
        /// <returns>True if the action was successful</returns>
        public bool HandleInput(KeyCode keyCode)
        {
            return HandleMovement(keyCode);

            // TODO add other handlers like pickup, casting, etc
        }

        private bool HandleMovement(KeyCode keyCode)
        {
            var player = Engine.E.Player;
            
            var direction = Direction.NONE;
            if (Player.MovementKeyMapping.ContainsKey(keyCode))
                direction = Player.MovementKeyMapping[keyCode];

            if (direction == Direction.NONE) return false;

            if (player.MoveIn(direction)) return true;

            // couldn't move
            // try to attack the target location
            var targetLocation = player.Position + direction;
            var attackAction = player.GetComponent<AttackAction>();
            if (attackAction == null) return false;
            return attackAction.Do(targetLocation);
        }
    }
}