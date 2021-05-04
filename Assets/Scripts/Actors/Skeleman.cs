using Actors.Behaviours;
using Cartography;
using Common;
using Common.Components;
using Enums;
using GoRogue;

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
            skeleman.Stats[StatTypes.MAX_HEALTH] = 8;
            skeleman.Stats[StatTypes.HEALTH] = 8;
            return skeleman;
        }
    }
}