using TMPro;
using UnityEngine;

namespace Pathfinder.UI
{
    /// <summary>
    /// 상호작용 프롬프트 UI 컴포넌트
    /// World Space Canvas에 부착하여 사용
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [Header("UI Settings")]
        [Tooltip("텍스트 컴포넌트")]
        [SerializeField] private TextMeshProUGUI _promptText;
        
        [Tooltip("기본 텍스트 (비어있으면 오브젝트에서 가져옴)")]
        [SerializeField] private string _defaultText = "Press E";
        
        [Tooltip("Y축 오프셋 (오브젝트 머리 위)")]
        [SerializeField] private float _yOffset = 1f;
        
        [Tooltip("텍스트 색상")]
        [SerializeField] private Color _textColor = Color.white;
        
        [Tooltip("아웃라인 색상")]
        [SerializeField] private Color _outlineColor = Color.black;
        
        // 타겟 Transform
        private Transform _targetTransform;
        
        // Canvas 컴포넌트
        private Canvas _canvas;
        
        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            
            // TextMeshPro 컴포넌트 자동 찾기
            if (_promptText == null)
            {
                _promptText = GetComponentInChildren<TextMeshProUGUI>();
            }
            
            // 텍스트 스타일 설정
            SetupTextStyle();
        }
        
        /// <summary>
        /// 텍스트 스타일 초기 설정
        /// </summary>
        private void SetupTextStyle()
        {
            if (_promptText == null) return;
            
            // 색상 설정
            _promptText.color = _textColor;
            
            // 아웃라인 설정 (material 문제로 일단 주석 처리)
            // _promptText.outlineColor = _outlineColor;
            // _promptText.outlineWidth = _outlineThickness;
        }
        
        /// <summary>
        /// 프롬프트 표시
        /// </summary>
        /// <param name="target">따라다닐 타겟 Transform</param>
        /// <param name="text">표시할 텍스트 (null이면 기본값 사용)</param>
        public void Show(Transform target, string text = null)
        {
            _targetTransform = target;
            
            if (_promptText != null)
            {
                _promptText.text = string.IsNullOrEmpty(text) ? _defaultText : text;
            }
            
            gameObject.SetActive(true);
        }
        
        /// <summary>
        /// 프롬프트 숨김
        /// </summary>
        public void Hide()
        {
            _targetTransform = null;
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// 텍스트 업데이트
        /// </summary>
        public void SetText(string text)
        {
            if (_promptText != null)
            {
                _promptText.text = text;
            }
        }
        
        private void LateUpdate()
        {
            // 타겟이 있으면 위치 업데이트
            if (_targetTransform != null)
            {
                Vector3 targetPosition = _targetTransform.position;
                targetPosition.y += _yOffset;
                transform.position = targetPosition;
            }
        }
    }
}
