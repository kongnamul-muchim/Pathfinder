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
        
        [Header("Shader")]
        [Tooltip("Offset 프로퍼티 이름 (기본: _MainTex)")]
        [SerializeField] private string _textureProperty = "_MainTex";
        
        [Header("Sorting")]
        [Tooltip("렌더링 순서 (낮을수록 뒤)")]
        [SerializeField] private int _orderInLayer = -100;
        
        [Tooltip("Sorting Layer 이름")]
        [SerializeField] private string _sortingLayerName = "Background";
        
        private SpriteRenderer _spriteRenderer;
        private Material _material;
        private float _textureWidth;
        private int _texturePropertyId;
        
        private void Start()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            
            if (!Application.isPlaying) return;
            
            if (_spriteRenderer != null)
            {
                _material = _spriteRenderer.material;
                _texturePropertyId = Shader.PropertyToID(_textureProperty);
                
                if (_spriteRenderer.sprite != null)
                {
                    _textureWidth = _spriteRenderer.sprite.bounds.size.x;
                    Debug.Log($"[ParallaxLayer] TextureWidth: {_textureWidth}");
                }
                
                _material.SetTextureScale(_texturePropertyId, _tiling);
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
            if (_player == null)
            {
                Debug.LogWarning("[ParallaxLayer] Player is null!");
                return;
            }
            
            if (_material == null)
            {
                Debug.LogWarning("[ParallaxLayer] Material is null!");
                return;
            }
            
            if (_textureWidth <= 0)
            {
                Debug.LogWarning($"[ParallaxLayer] TextureWidth invalid: {_textureWidth}");
                return;
            }
            
            float offsetX = _player.position.x * (1 - _parallaxSpeed) / _textureWidth;
            Debug.Log($"[ParallaxLayer] Player X: {_player.position.x:F2}, Speed: {_parallaxSpeed}, Offset: {offsetX:F4}");
            
            _material.SetTextureOffset(_texturePropertyId, new Vector2(offsetX, 0));
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