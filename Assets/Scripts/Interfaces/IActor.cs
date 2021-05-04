using Common.Components;

namespace Interfaces
{
    public interface IActor
    {
        public string Name { get; }
        public Stats Stats { get; }
    }
}