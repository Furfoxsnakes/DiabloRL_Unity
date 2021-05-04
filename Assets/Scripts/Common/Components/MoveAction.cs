using Actors;
using Cartography;
using GoRogue;
using GoRogue.MapViews;
using GoRogue.Pathing;

namespace Common.Components
{
    public class MoveAction : ActionComponent<Actor>
    {
        public FastAStar AStar;

        public MoveAction(GameMap map) : base(map)
        {
            AStar = new FastAStar(map.WalkabilityView, Distance.CHEBYSHEV);
        }
        
        public override bool Do(Coord destination)
        {
            var path = AStar.ShortestPath(Owner.Position, destination);

            if (path == null) return false;
            
            var direction = Direction.GetDirection(Owner.Position, path.GetStep(0));

            if (direction == Direction.NONE) return false;

            return Owner.MoveIn(direction);
        }
    }
}