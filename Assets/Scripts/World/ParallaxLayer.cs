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
        [Tooltip("패럴랙스 속도 (0~1: 낮을수록 풍경 변화가 느림)")]
        [SerializeField] private float _parallaxSpeed = 0.5f;
        
        [Tooltip("Offset 배수 (값이 클수록 풍경 변화가 큼)")]
        [SerializeField] private float _offsetMultiplier = 1f;
        
        [Tooltip("부드러운 이동 속도")]
        [SerializeField] private float _smoothSpeed = 5f;
        
        [Header("Shader")]
        [Tooltip("Offset 프로퍼티 이름 (Shader Graph: _Offset, 일반 셰이더: _MainTex)")]
        [SerializeField] private string _offsetProperty = "_Offset";
        
        [Tooltip("Shader Graph 사용 여부 (true: SetVector, false: SetTextureOffset)")]
        [SerializeField] private bool _useShaderGraph = true;
        
        [Header("Sorting")]
        [Tooltip("렌더링 순서 (낮을수록 뒤)")]
        [SerializeField] private int _orderInLayer = -100;
        
        [Tooltip("Sorting Layer 이름")]
        [SerializeField] private string _sortingLayerName = "Background";
        
        private SpriteRenderer _spriteRenderer;
        private Material _material;
        private int _offsetPropertyId;
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
                _offsetPropertyId = Shader.PropertyToID(_offsetProperty);
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
            
            Vector3 targetPosition = new Vector3(cameraX, _initialPosition.y, _initialPosition.z);
            transform.position = Vector3.Lerp(transform.position, targetPosition, _smoothSpeed * Time.deltaTime);
            
            if (!Application.isPlaying) return;
            
            if (_material == null) return;
            
            float offsetX = cameraX * _parallaxSpeed * _offsetMultiplier;
            
            if (_useShaderGraph)
            {
                _material.SetVector(_offsetPropertyId, new Vector2(offsetX, 0));
            }
            else
            {
                _material.SetTextureOffset(_offsetPropertyId, new Vector2(offsetX, 0));
            }
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