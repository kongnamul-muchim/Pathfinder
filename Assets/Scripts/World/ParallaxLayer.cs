using UnityEngine;

namespace Pathfinder.World
{
    [ExecuteInEditMode]
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("플레이어 Transform (필수)")]
        [SerializeField] private Transform _player;
        
        [Header("Parallax Settings")]
        [Tooltip("패럴랙스 속도 (0=고정, 1=Player 따라감)")]
        [SerializeField] private float _parallaxSpeed = 0.5f;
        
        [Tooltip("텍스처 반복 (가로, 세로)")]
        [SerializeField] private Vector2 _tiling = Vector2.one;
        
        [Header("Sorting")]
        [Tooltip("렌더링 순서 (낮을수록 뒤)")]
        [SerializeField] private int _orderInLayer = -100;
        
        [Tooltip("Sorting Layer 이름")]
        [SerializeField] private string _sortingLayerName = "Background";
        
        private SpriteRenderer _spriteRenderer;
        private Material _material;
        private float _textureWidth;
        
        private void Start()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            
            if (!Application.isPlaying) return;
            
            if (_spriteRenderer != null)
            {
                _material = _spriteRenderer.material;
                
                if (_spriteRenderer.sprite != null)
                {
                    _textureWidth = _spriteRenderer.sprite.bounds.size.x;
                }
                
                _material.mainTextureScale = _tiling;
            }
            
            ApplySortingSettings();
        }
        
        private void ApplySortingSettings()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingOrder = _orderInLayer;
                
                if (!string.IsNullOrEmpty(_sortingLayerName))
                {
                    _spriteRenderer.sortingLayerName = _sortingLayerName;
                }
            }
        }
        
        private void LateUpdate()
        {
            if (_player == null || _material == null || _textureWidth <= 0) return;
            
            float offsetX = _player.position.x * (1 - _parallaxSpeed) / _textureWidth;
            _material.mainTextureOffset = new Vector2(offsetX, 0);
        }
        
        public void SetParallaxSpeed(float speed)
        {
            _parallaxSpeed = Mathf.Clamp01(speed);
        }
        
        public void SetPlayer(Transform player)
        {
            _player = player;
        }
        
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                ApplySortingSettings();
            }
        }
    }
}