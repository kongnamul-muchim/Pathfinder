namespace Pathfinder.Player
{
    /// <summary>
    /// 플레이어 능력 관리 인터페이스
    /// </summary>
    public interface IAbilityManager
    {
        bool HasAbility(AbilityType ability);
        void UnlockAbility(AbilityType ability);
        bool HasAllAbilities();
        
        /// <summary>
        /// 능력 해금 이벤트
        /// </summary>
        event System.Action<AbilityType> OnAbilityUnlocked;
    }
    
    public enum AbilityType
    {
        None,
        DoubleJump,
        Dash
    }
}
