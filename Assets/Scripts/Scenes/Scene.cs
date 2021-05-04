namespace Scenes
{
    public abstract class Scene
    {
        public abstract void Enter();
        public abstract void Exit();
        public abstract void Update();
        public abstract void Render();
    }
}