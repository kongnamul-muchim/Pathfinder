using UnityEngine;
using TMPro;

namespace Pathfinder.Player
{
    /// <summary>
    /// 플레이어 머리 위에 표시되는 월드 공간 보상 팝업
    /// </summary>
    public class PlayerRewardPopup : MonoBehaviour
    {
        [Header("UI Components")]
        [Tooltip("메시지 텍스트 (World Space용 TextMeshPro)")]
        [SerializeField] private TextMeshPro _messageText;
        
        [Header("Position")]
        [Tooltip("플레이어 머리 위 오프셋")]
        [SerializeField] private Vector3 _offset = new Vector3(0f, 1.5f, 0f);
        
        [Header("Animation")]
        [Tooltip("표시 시간")]
        [SerializeField] private float _displayTime = 2f;
        
        [Tooltip("페이드 아웃 시간")]
        [SerializeField] private float _fadeOutTime = 0.5f;
        
        [Tooltip("위로 올라가는 속도")]
        [SerializeField] private float _moveSpeed = 1f;
        
        // 상태
        private float _timer = 0f;
        private bool _isShowing = false;
        private Vector3 _startPosition;
        private Color _startColor;
        
        private void Awake()
        {
            // 자식에서 TextMeshPro 찾기
            if (_messageText == null)
            {
                _messageText = GetComponentInChildren<TextMeshPro>();
            }
            
            // TextMeshPro가 없으면 생성
            if (_messageText == null)
            {
                CreateTextMeshPro();
            }
            
            // 초기 상태: 숨김
            if (_messageText != null)
            {
                _startColor = _messageText.color;
                _messageText.enabled = false;
            }
        }
        
        /// <summary>
        /// TextMeshPro 생성
        /// </summary>
        private void CreateTextMeshPro()
        {
            GameObject textObj = new GameObject("RewardText");
            textObj.transform.SetParent(transform, false);
            
            _messageText = textObj.AddComponent<TextMeshPro>();
            _messageText.alignment = TextAlignmentOptions.Center;
            _messageText.fontSize = 10; // 글씨 크기 키움 (기존 3 → 10)
            _messageText.color = Color.yellow;
            _startColor = Color.yellow;
        }
        
        private void Update()
        {
            if (_isShowing)
            {
                _timer -= Time.deltaTime;
                
                // 위로 올라가기
                if (_messageText != null)
                {
                    _messageText.transform.position += Vector3.up * _moveSpeed * Time.deltaTime;
                }
                
                // 페이드 아웃
                if (_timer <= _fadeOutTime && _messageText != null)
                {
                    float alpha = _timer / _fadeOutTime;
                    Color newColor = _startColor;
                    newColor.a = Mathf.Clamp01(alpha);
                    _messageText.color = newColor;
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
        public void ShowReward(string message)
        {
            if (_messageText == null) return;
            
            // 메시지 설정
            _messageText.text = message;
            _messageText.enabled = true;
            
            // 위치 설정 (플레이어 머리 위)
            _startPosition = transform.position + _offset;
            _messageText.transform.position = _startPosition;
            
            // 색상 리셋
            _messageText.color = _startColor;
            
            // 상태 설정
            _isShowing = true;
            _timer = _displayTime;
            
            // 카메라를 바라보게 설정
            _messageText.transform.rotation = Quaternion.identity;
        }
        
        /// <summary>
        /// 능력 획득 메시지
        /// </summary>
        public void ShowAbilityReward(string abilityName)
        {
            ShowReward($"{abilityName} Get!");
        }
        
        /// <summary>
        /// 목숨 획득 메시지
        /// </summary>
        public void ShowExtraLifeReward(int count)
        {
            ShowReward($"Extra Life +{count}!");
        }
        
        /// <summary>
        /// 일반 보상 메시지
        /// </summary>
        public void ShowGenericReward(string message)
        {
            ShowReward(message);
        }
        
        /// <summary>
        /// 숨기기
        /// </summary>
        public void Hide()
        {
            _isShowing = false;
            if (_messageText != null)
            {
                _messageText.enabled = false;
            }
        }
    }
}
