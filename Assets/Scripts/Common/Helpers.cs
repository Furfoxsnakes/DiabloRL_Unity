using System;
using UnityEngine;

namespace Common
{
    public class Helpers
    {
        /// <summary>
        /// Get the keycode of the key being pressed
        /// </summary>
        /// <returns></returns>
        public static KeyCode GetPressedKeyCode()
        {
            KeyCode result = KeyCode.Escape;

            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKey(key))
                    result = key;
            }

            return result;
        }
    }
}