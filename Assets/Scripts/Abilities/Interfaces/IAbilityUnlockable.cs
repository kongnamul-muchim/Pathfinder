using Pathfinder.Player;

namespace Pathfinder.Abilities
{
    public interface IAbilityUnlockable
    {
        AbilityType GetAbilityType();
        void Unlock();
        bool IsUnlocked();
    }
}