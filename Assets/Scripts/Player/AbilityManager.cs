using System.Collections.Generic;
using UnityEngine;

namespace Pathfinder.Player
{
    public class AbilityManager : MonoBehaviour, IAbilityManager
    {
        [Header("Life Settings")]
        [SerializeField] private int _baseLives = 3;
        
        private readonly HashSet<AbilityType> _unlockedAbilities = new HashSet<AbilityType>();
        
        private int _lives;
        
        public event System.Action<AbilityType> OnAbilityUnlocked;
        public event System.Action<int> OnLivesChanged;
        
        private void Awake()
        {
            _lives = _baseLives;
        }
        
        public bool HasAbility(AbilityType ability)
        {
            if (ability == AbilityType.None) return true;
            return _unlockedAbilities.Contains(ability);
        }
        
        public void UnlockAbility(AbilityType ability)
        {
            if (ability == AbilityType.None) return;
            if (_unlockedAbilities.Contains(ability)) return;
            
            _unlockedAbilities.Add(ability);
            Debug.Log($"[ABILITY] UnlockAbility: {ability}");
            OnAbilityUnlocked?.Invoke(ability);
        }
        
        public bool HasAllAbilities()
        {
            var allAbilities = System.Enum.GetValues(typeof(AbilityType));
            foreach (AbilityType ability in allAbilities)
            {
                if (ability != AbilityType.None && !_unlockedAbilities.Contains(ability))
                {
                    return false;
                }
            }
            return true;
        }
        
        public IReadOnlyCollection<AbilityType> GetUnlockedAbilities()
        {
            return _unlockedAbilities;
        }
        
        public void ResetAbilities()
        {
            _unlockedAbilities.Clear();
            _lives = _baseLives;
            OnLivesChanged?.Invoke(_lives);
            Debug.Log($"[ABILITY] ResetAbilities - Lives reset to {_lives}");
        }
        
        public void ResetAbilitiesOnly()
        {
            _unlockedAbilities.Clear();
            Debug.Log($"[ABILITY] ResetAbilitiesOnly - Abilities cleared, lives kept at {_lives}");
        }
        
        #region Lives Management
        
        public int GetLives()
        {
            return _lives;
        }
        
        public int GetExtraLives()
        {
            return Mathf.Max(0, _lives - _baseLives);
        }
        
        public void AddExtraLife()
        {
            _lives++;
            Debug.Log($"[ABILITY] AddExtraLife - Total lives: {_lives}");
            OnLivesChanged?.Invoke(_lives);
        }
        
        public bool ConsumeLife()
        {
            if (_lives <= 0) return false;
            
            _lives--;
            Debug.Log($"[ABILITY] ConsumeLife - Remaining lives: {_lives}");
            OnLivesChanged?.Invoke(_lives);
            return true;
        }
        
        public void SetLives(int lives)
        {
            _lives = lives;
            OnLivesChanged?.Invoke(_lives);
        }
        
        #endregion
    }
}