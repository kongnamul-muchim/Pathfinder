using UnityEngine;

namespace Pathfinder.World
{
    [ExecuteInEditMode]
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("카메라 Transform (비워두면 Camera.main 사용)")]
        [SerializeField] private Transform _camera;
        
        [Header("Parallax Settings")]
        [Tooltip("패럴랙스 속도 (낮을수록 배경 풍경이 천천히 변화)")]
        [SerializeField] private float _parallaxSpeed = 0.5f;
        
        [Tooltip("Offset 배수 (값이 클수록 풍경 변화가 빠름)")]
        [SerializeField] private float _offsetMultiplier = 1f;
        
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
            
            // Transform: 카메라 따라감 (화면에 항상 보임)
            transform.position = new Vector3(cameraX, _initialPosition.y, _initialPosition.z);
            
            if (!Application.isPlaying) return;
            
            if (_material == null || _textureWidth <= 0) return;
            
            // Texture Offset: Speed와 Multiplier 조합
            float offsetX = cameraX * (1 - _parallaxSpeed) * _offsetMultiplier / _textureWidth;
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