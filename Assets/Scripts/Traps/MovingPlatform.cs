using UnityEngine;

namespace Pathfinder.Traps
{
    /// <summary>
    /// 이동 플랫폼 - 플레이어 탑승 시 함께 이동
    /// 위치 동기화 방식 사용 (플레이어를 플랫폼의 자식으로 설정 + 위치 보정)
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
        
        // 플랫폼 이동량 추적
        private Vector2 _previousPosition;
        private Vector2 _platformDelta;
        
        // 플레이어 탑승 정보
        private Rigidbody2D _playerRb;
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
            _previousPosition = _startPosition;
            CalculateTargetPosition();
        }
        
        private void FixedUpdate()
        {
            // 현재 위치 저장
            Vector2 currentPosition = transform.position;
            
            // 플랫폼 이동량 계산 (이전 프레임 대비)
            _platformDelta = currentPosition - _previousPosition;
            _previousPosition = currentPosition;
            
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
            
            // 이동 전 위치 저장
            Vector2 positionBeforeMove = currentPosition;
            
            // 플랫폼 이동
            MovePlatform();
            
            // 실제 이동량 계산
            Vector2 actualDelta = (Vector2)transform.position - positionBeforeMove;
            
            // 플레이어 위치 동기화
            if (_hasPlayer && _playerRb != null)
            {
                SyncPlayerPosition(actualDelta);
            }
            
            // 다음 프레임을 위해 위치 업데이트
            _previousPosition = transform.position;
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
        /// 플랫폼 이동
        /// </summary>
        private void MovePlatform()
        {
            Vector2 currentPosition = transform.position;
            Vector2 direction = (_targetPosition - currentPosition).normalized;
            float distanceToTarget = Vector2.Distance(currentPosition, _targetPosition);
            
            // 목표에 도달했거나 지나쳤는지 확인
            float moveDistance = _moveSpeed * Time.fixedDeltaTime;
            
            if (distanceToTarget <= moveDistance)
            {
                // 목표 위치로 정확히 이동
                _rb.MovePosition(_targetPosition);
                
                // 대기 시작
                _isWaiting = true;
                _waitTimer = 0f;
            }
            else
            {
                // 목표 방향으로 이동
                Vector2 newPosition = currentPosition + (direction * moveDistance);
                _rb.MovePosition(newPosition);
            }
        }
        
        /// <summary>
        /// 플레이어 위치 동기화
        /// </summary>
        private void SyncPlayerPosition(Vector2 delta)
        {
            if (delta.magnitude < 0.001f) return;
            
            if (_playerRb == null)
            {
                DetachPlayer();
                return;
            }
            
            // 플레이어의 위치를 플랫폼 이동량만큼 이동
            Vector2 playerPos = _playerRb.position;
            _playerRb.MovePosition(playerPos + delta);
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
                    AttachPlayer(collision.gameObject);
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
        private void AttachPlayer(GameObject player)
        {
            if (_hasPlayer) return;
            
            _playerRb = player.GetComponent<Rigidbody2D>();
            if (_playerRb == null) return;
            
            _hasPlayer = true;
        }
        
        /// <summary>
        /// 플레이어를 플랫폼에서 분리
        /// </summary>
        private void DetachPlayer()
        {
            if (!_hasPlayer) return;
            
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
