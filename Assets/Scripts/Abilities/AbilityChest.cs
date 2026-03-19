using Pathfinder.Core.DI;
using Pathfinder.Common;
using Pathfinder.Player;
using UnityEngine;

namespace Pathfinder.Abilities
{
    /// <summary>
    /// 능력 상자 - 상호작용 시 능력 또는 목숨 보상 제공
    /// </summary>
    public class AbilityChest : MonoBehaviour, IInteractable
    {
        [Header("Reward Settings")]
        [Tooltip("보상 타입")]
        [SerializeField] private RewardType _rewardType;
        
        [Tooltip("상자 고유 ID (중복 불가)")]
        [SerializeField] private string _chestId;
        
        [Header("Visual")]
        [Tooltip("닫힌 상태 스프라이트")]
        [SerializeField] private Sprite _closedSprite;
        
        [Tooltip("열린 상태 스프라이트")]
        [SerializeField] private Sprite _openedSprite;
        
        [Tooltip("상호작용 프롬프트 텍스트")]
        [SerializeField] private string _interactionText = "Open Chest (E)";
        
        [Header("Size Settings")]
        [Tooltip("스프라이트 크기 배율 (기본 1)")]
        [SerializeField] private Vector2 _spriteScale = new Vector2(1f, 1f);
        
        // 컴포넌트
        private SpriteRenderer _spriteRenderer;
        private Collider2D _collider;
        
        // 상태
        private bool _isOpened = false;
        
        // DI 주입
        [Inject] private IAbilityManager _abilityManager;
        
        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();
            
            // DI가 안 되었으면 씬에서 찾기
            if (_abilityManager == null)
            {
                var abilityManager = FindFirstObjectByType<AbilityManager>();
                if (abilityManager != null)
                {
                    _abilityManager = abilityManager;
                }
            }
            
            // 스프라이트 크기 적용
            transform.localScale = new Vector3(_spriteScale.x, _spriteScale.y, 1f);
            
            // ID가 비어있으면 자동 생성
            if (string.IsNullOrEmpty(_chestId))
            {
                _chestId = $"{gameObject.name}_{GetInstanceID()}";
            }
            
            // 초기 상태: 닫힘
            UpdateVisual();
        }
        
        #region IInteractable Implementation
        
        /// <summary>
        /// 상호작용 프롬프트 텍스트
        /// </summary>
        public string GetInteractionText() => _isOpened ? "" : _interactionText;
        
        /// <summary>
        /// 상호작용 가능 여부
        /// </summary>
        public bool CanInteract() => !_isOpened;
        
        /// <summary>
        /// 상호작용 실행
        /// </summary>
        public void OnInteract()
        {
            if (_isOpened) return;
            
            OpenChest();
        }
        
        /// <summary>
        /// 프롬프트 위치 반환
        /// </summary>
        public Transform GetPromptTransform() => transform;
        
        #endregion
        
        /// <summary>
        /// 상자 열기
        /// </summary>
        private void OpenChest()
        {
            _isOpened = true;
            UpdateVisual();
            
            // 보상 제공
            GiveReward();
        }
        
        /// <summary>
        /// 보상 제공
        /// </summary>
        private void GiveReward()
        {
            if (_abilityManager == null)
            {
                return;
            }
            
            string rewardMessage = "";
            
            switch (_rewardType)
            {
                case RewardType.DoubleJump:
                    _abilityManager.UnlockAbility(AbilityType.DoubleJump);
                    rewardMessage = "Double Jump Get!";
                    break;
                    
                case RewardType.Dash:
                    _abilityManager.UnlockAbility(AbilityType.Dash);
                    rewardMessage = "Dash Get!";
                    break;
                    
                case RewardType.ExtraLife:
                    _abilityManager.AddExtraLife();
                    rewardMessage = "Extra Life +1!";
                    break;
            }
            
            // 보상 팝업 표시
            ShowRewardPopup(rewardMessage);
        }
        
        /// <summary>
        /// 보상 팝업 표시 - 플레이어 머리 위에 표시
        /// </summary>
        private void ShowRewardPopup(string message)
        {
            // 플레이어 찾기
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                // 플레이어의 PlayerRewardPopup 찾기
                var playerPopup = player.GetComponent<PlayerRewardPopup>();
                if (playerPopup != null)
                {
                    playerPopup.ShowGenericReward(message);
                }
                else
                {
                    // 없으면 추가하고 실행
                    playerPopup = player.gameObject.AddComponent<PlayerRewardPopup>();
                    playerPopup.ShowGenericReward(message);
                }
            }
            else
            {
                // 콘솔 출력 제거
            }
        }
        
        /// <summary>
        /// 시각적 상태 업데이트
        /// </summary>
        private void UpdateVisual()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = _isOpened ? _openedSprite : _closedSprite;
            }
        }
        
        /// <summary>
        /// 상자 ID 반환
        /// </summary>
        public string GetChestId() => _chestId;
        
        /// <summary>
        /// 보상 타입 반환
        /// </summary>
        public RewardType GetRewardType() => _rewardType;
        
        /// <summary>
        /// 상자가 열렸는지 확인
        /// </summary>
        public bool IsOpened() => _isOpened;
        
        /// <summary>
        /// 상자 상태 설정 (저장/로드용)
        /// </summary>
        public void SetOpened(bool opened)
        {
            _isOpened = opened;
            UpdateVisual();
        }
        
        /// <summary>
        /// 상자 리셋 (죽었을 때 롤백용)
        /// </summary>
        public void ResetChest()
        {
            if (_isOpened)
            {
                _isOpened = false;
                UpdateVisual();
            }
        }
        
        private void OnDrawGizmos()
        {
            // 상자 범위 표시
            Gizmos.color = _isOpened ? Color.gray : Color.yellow;
            Gizmos.DrawWireCube(transform.position, new Vector3(1f, 1f, 0.1f));
            
            // 보상 타입 표시
            #if UNITY_EDITOR
            if (!string.IsNullOrEmpty(_chestId))
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, $"ID: {_chestId}");
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, _rewardType.ToString());
            }
            #endif
        }
    }
}
