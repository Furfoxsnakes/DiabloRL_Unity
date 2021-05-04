using Common;
using GoRogue;
using GoRogue.GameFramework;

namespace Cartography
{
    public class TerrainFactory
    {
        public static MapTerrain Floor(Coord pos) => new MapTerrain(pos, true, true, true, S.FLOOR);

        public static MapTerrain Wall(Coord pos) => new MapTerrain(pos, true, false, false, S.WALL);
    }
}