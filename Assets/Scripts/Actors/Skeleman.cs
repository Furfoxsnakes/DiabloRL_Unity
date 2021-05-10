using Actors.Behaviours;
using Cartography;
using Common;
using Common.Components;
using Enums;
using GoRogue;
using GoRogue.DiceNotation;

namespace Actors
{
    public class Skeleman : Enemy
    {
        public Skeleman(Coord pos) : base(pos, "Skeleman", S.SKELEMAN)
        {
            
        }

        public static Skeleman Create(Coord pos, int lvl)
        {
            var skeleman = new Skeleman(pos);
            var health = Dice.Roll("2d5");
            skeleman.Stats[StatTypes.MAX_HEALTH] = health;
            skeleman.Stats[StatTypes.HEALTH] = health;
            skeleman.Stats[StatTypes.ATTACK] = Dice.Roll("1d3") + lvl / 3;
            skeleman.Stats[StatTypes.ATTACK_CHANCE] = Dice.Roll("25d3");
            skeleman.Stats[StatTypes.DEFENSE] = Dice.Roll("1d3") + lvl / 3;
            skeleman.Stats[StatTypes.DEFENSE_CHANCE] = Dice.Roll("10d4");
            skeleman.Stats[StatTypes.GOLD] = Dice.Roll("5d5");
            return skeleman;
        }
    }
}