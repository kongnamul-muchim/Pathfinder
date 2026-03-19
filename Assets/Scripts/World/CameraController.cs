using System.Collections;
using UnityEngine;

namespace Pathfinder.World
{
    /// <summary>
    /// 맵 전환 및 플레이어 추적 카메라 컨트롤러
    /// </summary>
    public class CameraController : MonoBehaviour, ICameraController
    {
        [Header("Follow Settings")]
        [Tooltip("카메라가 추적할 타겟")]
        [SerializeField] private Transform _target;
        
        [Tooltip("추적 부드러움 (낮을수록 부드럽게)")]
        [SerializeField] private float _followSpeed = 5f;
        
        [Tooltip("카메라 오프셋")]
        [SerializeField] private Vector3 _offset = new Vector3(0, 0, -10);
        
        [Header("Dead Zone")]
        [Tooltip("Dead Zone 사용 여부")]
        [SerializeField] private bool _useDeadZone = true;
        
        [Tooltip("Dead Zone 크기 (이 범위 내에서는 카메라 고정)")]
        [SerializeField] private Vector2 _deadZoneSize = new Vector2(2f, 1.5f);
        
        [Header("Map Bounds")]
        [Tooltip("맵 경계 사용 여부")]
        [SerializeField] private bool _useBounds = true;
        
        [Tooltip("최소 경계 (왼쪽 아래)")]
        [SerializeField] private Vector2 _minBounds;
        
        [Tooltip("최대 경계 (오른쪽 위)")]
        [SerializeField] private Vector2 _maxBounds;
        
        [Header("Transition")]
        [Tooltip("맵 전환 시 이동 시간")]
        [SerializeField] private float _transitionDuration = 0.5f;
        
        [Tooltip("맵 전환 시 사용할 커브")]
        [SerializeField] private AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        // 카메라
        private Camera _camera;
        
        // 상태
        private bool _isTransitioning = false;
        private Vector3 _targetPosition;
        
        // 고정 z 위치
        private const float FIXED_Z = -10f;
        
        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }
        
        private void Start()
        {
            // 타겟이 없으면 플레이어 찾기
            if (_target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _target = player.transform;
                }
            }
            
            // 초기 위치 설정
            if (_target != null)
            {
                SnapToTarget();
            }
        }
        
        private void LateUpdate()
        {
            if (_isTransitioning) return;
            if (_target == null) return;
            
            Vector3 targetPos = CalculateTargetPosition();
            Vector3 currentPos = transform.position;
            
            if (_useDeadZone)
            {
                float deltaX = targetPos.x - currentPos.x;
                float deltaY = targetPos.y - currentPos.y;
                
                if (Mathf.Abs(deltaX) < _deadZoneSize.x) deltaX = 0;
                if (Mathf.Abs(deltaY) < _deadZoneSize.y) deltaY = 0;
                
                if (deltaX == 0 && deltaY == 0) return;
                
                targetPos = currentPos + new Vector3(deltaX, deltaY, 0);
            }
            
            Vector3 newPosition = Vector3.Lerp(transform.position, targetPos, _followSpeed * Time.deltaTime);
            newPosition.z = FIXED_Z;
            transform.position = newPosition;
        }
        
        /// <summary>
        /// 타겟 위치 계산 (경계 적용)
        /// </summary>
        private Vector3 CalculateTargetPosition()
        {
            Vector3 targetPos = _target.position + _offset;
            targetPos.z = FIXED_Z;
            
            if (_useBounds && _camera != null)
            {
                // 카메라 뷰포트 크기 계산
                float vertExtent = _camera.orthographicSize;
                float horzExtent = vertExtent * _camera.aspect;
                
                // 경계 적용
                targetPos.x = Mathf.Clamp(targetPos.x, _minBounds.x + horzExtent, _maxBounds.x - horzExtent);
                targetPos.y = Mathf.Clamp(targetPos.y, _minBounds.y + vertExtent, _maxBounds.y - vertExtent);
            }
            
            return targetPos;
        }
        
        /// <summary>
        /// 타겟 설정
        /// </summary>
        public void SetTarget(Transform target)
        {
            _target = target;
        }
        
        /// <summary>
        /// 즉시 타겟 위치로 이동
        /// </summary>
        public void SnapToTarget()
        {
            if (_target == null) return;
            
            _targetPosition = CalculateTargetPosition();
            transform.position = _targetPosition;
        }
        
        /// <summary>
        /// 맵 경계 설정
        /// </summary>
        public void SetMapBounds(Vector2 min, Vector2 max)
        {
            _minBounds = min;
            _maxBounds = max;
        }
        
        /// <summary>
        /// 특정 위치로 전환 (맵 전환용)
        /// </summary>
        public void TransitionToPosition(Vector3 position)
        {
            if (_isTransitioning) return;
            
            StartCoroutine(TransitionCoroutine(position));
        }
        
        /// <summary>
        /// 전환 코루틴
        /// </summary>
        private IEnumerator TransitionCoroutine(Vector3 targetPosition)
        {
            _isTransitioning = true;
            
            Vector3 startPosition = transform.position;
            targetPosition.z = FIXED_Z;
            
            float elapsedTime = 0f;
            
            while (elapsedTime < _transitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / _transitionDuration;
                float curveValue = _transitionCurve.Evaluate(t);
                
                transform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);
                
                yield return null;
            }
            
            transform.position = targetPosition;
            _isTransitioning = false;
        }
        
        /// <summary>
        /// 플레이어 스폰 위치로 카메라 이동
        /// </summary>
        public void MoveToSpawn(Vector3 spawnPosition)
        {
            Vector3 cameraPos = spawnPosition + _offset;
            cameraPos.z = FIXED_Z;
            
            if (_useBounds && _camera != null)
            {
                float vertExtent = _camera.orthographicSize;
                float horzExtent = vertExtent * _camera.aspect;
                
                cameraPos.x = Mathf.Clamp(cameraPos.x, _minBounds.x + horzExtent, _maxBounds.x - horzExtent);
                cameraPos.y = Mathf.Clamp(cameraPos.y, _minBounds.y + vertExtent, _maxBounds.y - vertExtent);
            }
            
            TransitionToPosition(cameraPos);
        }
        
        /// <summary>
        /// 추적 활성화/비활성화
        /// </summary>
        public void SetFollowEnabled(bool enabled)
        {
            if (!enabled)
            {
                _isTransitioning = true;
            }
            else
            {
                _isTransitioning = false;
            }
        }
        
        private void OnDrawGizmos()
        {
            if (_useBounds)
            {
                Gizmos.color = Color.cyan;
                Vector3 center = new Vector3((_minBounds.x + _maxBounds.x) / 2, (_minBounds.y + _maxBounds.y) / 2, 0);
                Vector3 size = new Vector3(_maxBounds.x - _minBounds.x, _maxBounds.y - _minBounds.y, 0.1f);
                Gizmos.DrawWireCube(center, size);
            }
            
            if (_useDeadZone)
            {
                Gizmos.color = Color.green;
                Vector3 deadZoneCenter = transform.position;
                Vector3 deadZoneSize = new Vector3(_deadZoneSize.x * 2, _deadZoneSize.y * 2, 0.1f);
                Gizmos.DrawWireCube(deadZoneCenter, deadZoneSize);
            }
            
            if (_camera != null)
            {
                Gizmos.color = Color.yellow;
                float vertExtent = _camera.orthographicSize;
                float horzExtent = vertExtent * _camera.aspect;
                Vector3 camSize = new Vector3(horzExtent * 2, vertExtent * 2, 0.1f);
                Gizmos.DrawWireCube(transform.position, camSize);
            }
        }
    }
}
