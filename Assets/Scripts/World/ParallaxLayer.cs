using UnityEngine;

namespace Pathfinder.World
{
    [ExecuteInEditMode]
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("카메라 Transform (필수)")]
        [SerializeField] private Transform _camera;
        
        [Header("Parallax Settings")]
        [Tooltip("패럴랙스 속도 (0=완전 고정, 1=카메라 완전 따라감)")]
        [SerializeField] private float _parallaxSpeed = 0.5f;
        
        [Tooltip("텍스처 반복 (가로, 세로)")]
        [SerializeField] private Vector2 _tiling = Vector2.one;
        
        [Header("Shader")]
        [Tooltip("Offset 프로퍼티 이름")]
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
        private Vector3 _initialPosition;
        
        private void Start()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _initialPosition = transform.position;
            
            if (_camera == null)
                _camera = Camera.main?.transform;
            
            if (!Application.isPlaying) return;
            
            if (_spriteRenderer != null)
            {
                _material = _spriteRenderer.material;
                _texturePropertyId = Shader.PropertyToID(_textureProperty);
                
                if (_spriteRenderer.sprite != null)
                {
                    _textureWidth = _spriteRenderer.sprite.bounds.size.x;
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
            if (_camera == null)
            {
                _camera = Camera.main?.transform;
                if (_camera == null) return;
            }
            
            float cameraX = _camera.position.x;
            
            // Transform 위치 업데이트 (카메라 따라감)
            Vector3 newPosition = transform.position;
            newPosition.x = _initialPosition.x + cameraX * _parallaxSpeed;
            transform.position = newPosition;
            
            if (!Application.isPlaying) return;
            
            if (_material == null || _textureWidth <= 0) return;
            
            // Texture Offset (패럴랙스 효과)
            float offsetX = cameraX * (1 - _parallaxSpeed) / _textureWidth;
            _material.SetTextureOffset(_texturePropertyId, new Vector2(offsetX, 0));
        }
        
        public void SetParallaxSpeed(float speed)
        {
            _parallaxSpeed = Mathf.Clamp01(speed);
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