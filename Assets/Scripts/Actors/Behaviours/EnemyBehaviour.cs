using Cartography;

namespace Actors.Behaviours
{
    public abstract class EnemyBehaviour
    {
        protected Enemy Owner;

        public EnemyBehaviour(Enemy owner)
        {
            Owner = owner;
        }

        public abstract void Do(Actor target);
    }
}