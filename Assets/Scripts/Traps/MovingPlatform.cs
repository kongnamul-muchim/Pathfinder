using UnityEngine;

namespace Pathfinder.Traps
{
    /// <summary>
    /// 이동 플랫폼 - 플레이어 탑승 시 함께 이동
    /// 위치 동기화 방식 사용 (이동량을 미리 계산하여 동시에 적용)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("이동 속도")]
        [SerializeField] private float _moveSpeed = 2f;
        
        [Tooltip("이동 거리")]
        [SerializeField] private float _moveDistance = 5f;
        
        [Tooltip("이동 방향 (좌우: (-1,0) 또는 (1,0), 상하: (0,-1) 또는 (0,1))")]
        [SerializeField] private Vector2 _moveDirection = Vector2.right;
        
        [Header("Timing")]
        [Tooltip("양 끝에서 대기 시간 (초)")]
        [SerializeField] private float _waitTime = 0.5f;
        
        [Header("Player")]
        [Tooltip("플레이어 태그")]
        [SerializeField] private string _playerTag = "Player";
        
        private Rigidbody2D _rb;
        private Vector2 _startPosition;
        private Vector2 _targetPosition;
        private bool _movingForward = true;
        private float _waitTimer = 0f;
        private bool _isWaiting = false;
        
        // 플레이어 탑승 정보
        private Transform _playerTransform;
        private Rigidbody2D _playerRb;
        private Vector3 _relativePosition;
        private bool _hasPlayer;
        
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            
            // 이동 방향 정규화
            _moveDirection = _moveDirection.normalized;
            if (_moveDirection == Vector2.zero)
            {
                _moveDirection = Vector2.right;
            }
        }
        
        private void Start()
        {
            _startPosition = transform.position;
            CalculateTargetPosition();
        }
        
        private void FixedUpdate()
        {
            // 대기 중이면 이동하지 않음
            if (_isWaiting)
            {
                _waitTimer += Time.fixedDeltaTime;
                if (_waitTimer >= _waitTime)
                {
                    _isWaiting = false;
                    _waitTimer = 0f;
                    _movingForward = !_movingForward;
                    CalculateTargetPosition();
                }
                return;
            }
            
            // 이번 프레임에 이동할 delta 계산
            Vector2 currentPosition = transform.position;
            Vector2 direction = (_targetPosition - currentPosition).normalized;
            float distanceToTarget = Vector2.Distance(currentPosition, _targetPosition);
            float moveDistance = _moveSpeed * Time.fixedDeltaTime;
            
            Vector2 delta;
            Vector2 newPlatformPosition;
            
            if (distanceToTarget <= moveDistance)
            {
                // 목표에 도달
                delta = _targetPosition - currentPosition;
                newPlatformPosition = _targetPosition;
                _isWaiting = true;
                _waitTimer = 0f;
            }
            else
            {
                // 일반 이동
                delta = direction * moveDistance;
                newPlatformPosition = currentPosition + delta;
            }
            
            // 플랫폼 이동
            _rb.MovePosition(newPlatformPosition);
            
            // 플레이어를 플랫폼과 함께 이동 (상대 위치 유지)
            if (_hasPlayer && _playerTransform != null)
            {
                MovePlayerWithPlatform();
            }
        }
        
        /// <summary>
        /// 목표 위치 계산
        /// </summary>
        private void CalculateTargetPosition()
        {
            if (_movingForward)
            {
                _targetPosition = _startPosition + (_moveDirection * _moveDistance);
            }
            else
            {
                _targetPosition = _startPosition;
            }
        }
        
        /// <summary>
        /// 플레이어를 플랫폼과 함께 이동 (상대 위치 유지)
        /// </summary>
        private void MovePlayerWithPlatform()
        {
            if (_playerRb == null) return;
            
            // 플레이어를 플랫폼의 상대 위치로 이동
            Vector3 newPosition = transform.position + _relativePosition;
            _playerRb.MovePosition(newPosition);
        }
        
        /// <summary>
        /// 플레이어가 플랫폼 위에 있는지 확인
        /// </summary>
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!collision.gameObject.CompareTag(_playerTag)) return;
            
            // 플레이어가 플랫폼 위에 있는지 확인 (접촉점이 플랫폼 위쪽인지)
            foreach (var contact in collision.contacts)
            {
                // 플레이어가 위에서 내려온 경우 (법선 벡터가 위쪽)
                if (contact.normal.y < -0.5f)
                {
                    AttachPlayer(collision.transform);
                    break;
                }
            }
        }
        
        private void OnCollisionExit2D(Collision2D collision)
        {
            if (!collision.gameObject.CompareTag(_playerTag)) return;
            
            DetachPlayer();
        }
        
        /// <summary>
        /// 플레이어를 플랫폼에 부착
        /// </summary>
        private void AttachPlayer(Transform player)
        {
            if (_hasPlayer) return;
            
            _playerRb = player.GetComponent<Rigidbody2D>();
            if (_playerRb == null) return;
            
            _playerTransform = player;
            
            // 플레이어의 플랫폼에 대한 상대 위치 계산
            _relativePosition = player.position - transform.position;
            
            _hasPlayer = true;
        }
        
        /// <summary>
        /// 플레이어를 플랫폼에서 분리
        /// </summary>
        private void DetachPlayer()
        {
            if (!_hasPlayer) return;
            
            _playerTransform = null;
            _playerRb = null;
            _hasPlayer = false;
        }
        
        /// <summary>
        /// 디버그 시각화
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
            {
                // 에디터에서 시작 위치와 목표 위치 표시
                Vector2 start = transform.position;
                Vector2 direction = _moveDirection.normalized;
                if (direction == Vector2.zero) direction = Vector2.right;
                
                Vector2 end = start + (direction * _moveDistance);
                
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(start, 0.2f);
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(end, 0.2f);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(start, end);
            }
            else
            {
                // 플레이 중에는 이동 경로 표시
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(_startPosition, 0.2f);
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(_startPosition + (_moveDirection * _moveDistance), 0.2f);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(_startPosition, _startPosition + (_moveDirection * _moveDistance));
            }
        }
    }
}
