using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Pathfinder.UI
{
    /// <summary>
    /// 보상 획득 시 표시되는 팝업 UI
    /// 상자 열림, 능력 획득, 목숨 획득 등
    /// </summary>
    public class RewardPopupUI : MonoBehaviour
    {
        private static readonly Color AbilityColor = new Color(0.2f, 0.8f, 0.2f, 0.9f);
        private static readonly Color ExtraLifeColor = new Color(0.9f, 0.3f, 0.3f, 0.9f);
        private static readonly Color GenericColor = new Color(0.9f, 0.9f, 0.2f, 0.9f);
        
        [Header("UI Components")]
        [Tooltip("메시지 텍스트")]
        [SerializeField] private TextMeshProUGUI _messageText;
        
        [Tooltip("배경 이미지")]
        [SerializeField] private Image _backgroundImage;
        
        [Header("Animation")]
        [Tooltip("표시 애니메이션 컴포넌트")]
        [SerializeField] private Animator _animator;
        
        [Tooltip("표시 시간")]
        [SerializeField] private float _displayTime = 2f;
        
        [Tooltip("페이드 아웃 시간")]
        [SerializeField] private float _fadeOutTime = 0.3f;
        
        // 상태
        private float _timer = 0f;
        private bool _isShowing = false;
        private CanvasGroup _canvasGroup;
        
        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            // 초기 상태: CanvasGroup으로 숨김 (GameObject는 활성화 상태 유지)
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
        
        private void Update()
        {
            if (_isShowing)
            {
                _timer -= Time.deltaTime;
                
                if (_timer <= _fadeOutTime)
                {
                    // 페이드 아웃
                    float alpha = _timer / _fadeOutTime;
                    _canvasGroup.alpha = Mathf.Clamp01(alpha);
                }
                
                if (_timer <= 0)
                {
                    Hide();
                }
            }
        }
        
        /// <summary>
        /// 보상 메시지 표시
        /// </summary>
        public void ShowReward(string message, Color? backgroundColor = null)
        {
            if (_messageText != null)
            {
                _messageText.text = message;
            }
            
            if (_backgroundImage != null && backgroundColor.HasValue)
            {
                _backgroundImage.color = backgroundColor.Value;
            }
            
            // 애니메이션 트리거
            if (_animator != null)
            {
                _animator.SetTrigger("Show");
            }
            
            // 표시
            _canvasGroup.alpha = 1f;
            gameObject.SetActive(true);
            
            _isShowing = true;
            _timer = _displayTime;
        }
        
        public void ShowAbilityReward(string abilityName)
        {
            ShowReward($"{abilityName} Get!", AbilityColor);
        }
        
        public void ShowExtraLifeReward(int count)
        {
            ShowReward($"Extra Life +{count}!", ExtraLifeColor);
        }
        
        public void ShowGenericReward(string message)
        {
            ShowReward(message, GenericColor);
        }
        
        /// <summary>
        /// 숨기기
        /// </summary>
        public void Hide()
        {
            _isShowing = false;
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            // GameObject는 비활성화하지 않음 (FindObjectOfType 때문에)
        }
    }
}
