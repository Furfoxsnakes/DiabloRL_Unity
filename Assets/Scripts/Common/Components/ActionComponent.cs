using Actors;
using Cartography;
using GoRogue;
using GoRogue.GameFramework;
using GoRogue.GameFramework.Components;

namespace Common.Components
{
    public class ActionComponent<T> : IGameObjectComponent where T : Actor
    {
        public IGameObject Parent { get; set; }
        public T Owner => Parent as T;
        protected GameMap Map;

        public ActionComponent(GameMap map)
        {
            Map = map;
        }

        public virtual bool Do(Coord destination)
        {
            return false;
        }
    }
}