using System.Linq;
using Actors;
using Common.Components;
using Enums;
using GoRogue;
using UnityEngine;
using UnityEngine.XR;

namespace Common
{
    public class InputManager
    {
        /// <summary>
        /// Handles the input on player's turn
        /// </summary>
        /// <param name="keyCode"></param>
        /// <returns>True if the action was successful</returns>
        public Results HandleInput(KeyCode keyCode, GameState _gameState)
        {
            return _gameState switch
            {
                GameState.PLAYER_TURN when HandleMovement(keyCode) => Results.MOVED,
                GameState.PLAYER_TURN => keyCode switch
                {
                    KeyCode.Keypad5 => Results.WAIT,
                    KeyCode.C => Results.CHARACTER_MENU,
                    _ => Results.NONE
                },
                GameState.PLAYER_MENU => keyCode switch
                {
                    KeyCode.Escape => Results.ESCAPE,
                    _ => Results.NONE
                },
                _ => Results.NONE
            };

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