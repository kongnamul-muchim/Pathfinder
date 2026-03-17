using System.Collections.Generic;
using UnityEngine;

namespace Pathfinder.Player
{
    /// <summary>
    /// 플레이어 능력 관리 구현체
    /// DI Container에 의해 관리됨
    /// </summary>
    public class AbilityManager : MonoBehaviour, IAbilityManager
    {
        [Header("Initial Abilities")]
        [Tooltip("게임 시작 시 보유하는 능력들")]
        [SerializeField] private List<AbilityType> _initialAbilities = new List<AbilityType>();
        
        // 보유한 능력 집합
        private readonly HashSet<AbilityType> _unlockedAbilities = new HashSet<AbilityType>();
        
        // 추가 목숨
        private int _extraLives = 0;
        
        // 이벤트
        public event System.Action<AbilityType> OnAbilityUnlocked;
        public event System.Action<int> OnExtraLivesChanged;
        
        private void Awake()
        {
            // 초기 능력 설정
            foreach (var ability in _initialAbilities)
            {
                if (ability != AbilityType.None)
                {
                    _unlockedAbilities.Add(ability);
                }
            }
        }
        
        /// <summary>
        /// 특정 능력을 보유하고 있는지 확인
        /// </summary>
        public bool HasAbility(AbilityType ability)
        {
            if (ability == AbilityType.None) return true;
            return _unlockedAbilities.Contains(ability);
        }
        
        /// <summary>
        /// 능력 해금
        /// </summary>
        public void UnlockAbility(AbilityType ability)
        {
            if (ability == AbilityType.None) return;
            if (_unlockedAbilities.Contains(ability)) return;
            
            _unlockedAbilities.Add(ability);
            
            // 이벤트 발행
            OnAbilityUnlocked?.Invoke(ability);
        }
        
        /// <summary>
        /// 모든 능력을 보유하고 있는지 확인 (None 제외)
        /// </summary>
        public bool HasAllAbilities()
        {
            // 모든 능력 타입 체크 (None 제외)
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
        
        /// <summary>
        /// 보유한 능력 목록 반환
        /// </summary>
        public IReadOnlyCollection<AbilityType> GetUnlockedAbilities()
        {
            return _unlockedAbilities;
        }
        
        /// <summary>
        /// 능력 초기화 (테스트용)
        /// </summary>
        public void ResetAbilities()
        {
            _unlockedAbilities.Clear();
            _extraLives = 0;
            foreach (var ability in _initialAbilities)
            {
                if (ability != AbilityType.None)
                {
                    _unlockedAbilities.Add(ability);
                }
            }
            OnExtraLivesChanged?.Invoke(_extraLives);
        }
        
        #region Extra Lives Management
        
        /// <summary>
        /// 추가 목숨 수 반환
        /// </summary>
        public int GetExtraLives()
        {
            return _extraLives;
        }
        
        /// <summary>
        /// 추가 목숨 획득
        /// </summary>
        public void AddExtraLife()
        {
            _extraLives++;
            OnExtraLivesChanged?.Invoke(_extraLives);
        }
        
        /// <summary>
        /// 추가 목숨 소모 (죽을 때 사용)
        /// </summary>
        /// <returns>소모 성공 여부</returns>
        public bool ConsumeExtraLife()
        {
            if (_extraLives <= 0) return false;
            
            _extraLives--;
            OnExtraLivesChanged?.Invoke(_extraLives);
            return true;
        }
        
        #endregion
    }
}
