namespace Pathfinder.Player
{
    public interface IAbilityManager
    {
        bool HasAbility(AbilityType ability);
        void UnlockAbility(AbilityType ability);
        bool HasAllAbilities();
        
        System.Collections.Generic.IReadOnlyCollection<AbilityType> GetUnlockedAbilities();
        
        void ResetAbilities();
        void ResetAbilitiesOnly();
        
        event System.Action<AbilityType> OnAbilityUnlocked;
        
        int GetLives();
        int GetExtraLives();
        void AddExtraLife();
        bool ConsumeLife();
        void SetLives(int lives);
        event System.Action<int> OnLivesChanged;
    }
}