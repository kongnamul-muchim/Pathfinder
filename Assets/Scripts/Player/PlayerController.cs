using UnityEngine;
using UnityEngine.InputSystem;
using Pathfinder.Core.DI;

namespace Pathfinder.Player
{
    /// <summary>
    /// 기본 플레이어 컨트롤러 - 이동 및 점프
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        
        [Header("Ground Detection")]
        [SerializeField] private float _groundCheckDistance = 0.1f;
        [SerializeField] private float _stepHeight = 0.2f;
        [SerializeField] private float _stepCheckDistance = 0.1f;
        
        [Header("Jump")]
        [SerializeField] private float _jumpForce = 10f;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private float _groundCheckRadius = 0.2f;
        
        private Rigidbody2D _rb;
        private bool _isGrounded;
        private float _horizontalInput;
        private bool _jumpRequested;
        
        private InputAction _moveAction;
        private InputAction _jumpAction;
        
        // DI 주입
        [Inject] private IAbilityManager _abilityManager;
        [Inject] private IDeathManager _deathManager;
        
        // 애니메이션
        private PlayerAnimator _playerAnimator;
        
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _playerAnimator = GetComponent<PlayerAnimator>();
            
            // Input Actions 설정
            _moveAction = new InputAction("Move", binding: "<Keyboard>/a");
            _moveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            
            _jumpAction = new InputAction("Jump", binding: "<Keyboard>/space");
            
            // GroundCheck가 없으면 자동 생성
            if (_groundCheck == null)
            {
                var groundCheckObj = new GameObject("GroundCheck");
                groundCheckObj.transform.SetParent(transform);
                groundCheckObj.transform.localPosition = new Vector3(0, -0.5f, 0);
                _groundCheck = groundCheckObj.transform;
            }
        }
        
        private void OnEnable()
        {
            _moveAction?.Enable();
            _jumpAction?.Enable();
        }
        
        private void OnDisable()
        {
            _moveAction?.Disable();
            _jumpAction?.Disable();
        }
        
        private void OnDestroy()
        {
            _moveAction?.Dispose();
            _jumpAction?.Dispose();
        }
        
        private void Start()
        {
            // RootContext에 의해 DI 주입됨
        }
        
        private void Update()
        {
            // 입력 처리
            _horizontalInput = _moveAction.ReadValue<float>();
            
            if (_jumpAction.WasPressedThisFrame() && _isGrounded)
            {
                _jumpRequested = true;
            }
            
            // 지면 체크
            bool wasGrounded = _isGrounded;
            _isGrounded = Physics2D.OverlapCircle(_groundCheck.position, _groundCheckRadius, _groundLayer);
            
            // 애니메이션 업데이트
            UpdateAnimations();
        }
        
        /// <summary>
        /// 애니메이션 상태 업데이트
        /// </summary>
        private void UpdateAnimations()
        {
            if (_playerAnimator == null) return;
            
            // 걷기 상태
            bool isWalking = Mathf.Abs(_horizontalInput) > 0.01f && _isGrounded;
            _playerAnimator.SetWalking(isWalking);
            
            // 점프 상태 (공중에 있을 때)
            bool isJumping = !_isGrounded && _rb.linearVelocity.y > 0.1f;
            _playerAnimator.SetJumping(isJumping);
            
            // 지면 상태
            _playerAnimator.SetGrounded(_isGrounded);
            
            // 방향 전환
            if (Mathf.Abs(_horizontalInput) > 0.01f)
            {
                _playerAnimator.SetFacingDirection(_horizontalInput);
            }
        }
        
        private void FixedUpdate()
        {
            // 이동
            Vector2 velocity = _rb.linearVelocity;
            velocity.x = _horizontalInput * _moveSpeed;
            _rb.linearVelocity = velocity;
            
            // 작은 단차(step) 자동 올라가기
            HandleStepUp();
            
            // 점프
            if (_jumpRequested)
            {
                _rb.AddForce(Vector2.up * _jumpForce, ForceMode2D.Impulse);
                _jumpRequested = false;
            }
        }
        
        /// <summary>
        /// 작은 단차(step)를 만나면 자연스럽게 위로 밀어 올림
        /// </summary>
        private void HandleStepUp()
        {
            if (Mathf.Abs(_horizontalInput) < 0.01f) return;
            
            // 이동 방향
            float direction = Mathf.Sign(_horizontalInput);
            Vector2 position = transform.position;
            
            // 캐릭터 앞쪽 아래 지점에서 Raycast
            Vector2 rayStart = position + Vector2.right * direction * 0.3f;
            rayStart.y -= 0.2f;
            
            // 작은 단차 감지 (낮은 높이의 장애물)
            RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, _stepCheckDistance, _groundLayer);
            if (hit.collider != null)
            {
                // 바닥이 있고, 현재 위치보다 조금 높은 경우
                float heightDiff = hit.point.y - (position.y - 0.5f);
                if (heightDiff > 0.01f && heightDiff < _stepHeight)
                {
                    // 부드럽게 위로 밀어 올림
                    _rb.position += Vector2.up * heightDiff * 0.5f;
                }
            }
            
            // 앞쪽 벽 감지하여 미끄러지게 처리
            Vector2 wallCheckStart = position + Vector2.up * 0.3f;
            RaycastHit2D wallHit = Physics2D.Raycast(wallCheckStart, Vector2.right * direction, 0.35f, _groundLayer);
            if (wallHit.collider != null)
            {
                // 벽에 닿았을 때 아래로 미끄러지도록
                if (_rb.linearVelocity.y > -1f)
                {
                    _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -1f);
                }
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            // 함정이나 적과 충돌
            if (other.CompareTag("Trap") || other.CompareTag("Enemy"))
            {
                Die();
            }
        }
        
        private void Die()
        {
            // 사망 애니메이션 트리거
            _playerAnimator?.TriggerDeath();
            _deathManager?.OnPlayerDeath();
        }
        
        private void OnDrawGizmos()
        {
            if (_groundCheck != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_groundCheck.position, _groundCheckRadius);
            }
            
            // Step detection visualization
            if (Application.isPlaying && Mathf.Abs(_horizontalInput) > 0.01f)
            {
                float direction = Mathf.Sign(_horizontalInput);
                Vector2 position = transform.position;
                
                // Step up check
                Vector2 rayStart = position + Vector2.right * direction * 0.3f;
                rayStart.y -= 0.2f;
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(rayStart, rayStart + Vector2.down * _stepCheckDistance);
                
                // Wall check
                Vector2 wallCheckStart = position + Vector2.up * 0.3f;
                Gizmos.color = Color.red;
                Gizmos.DrawLine(wallCheckStart, wallCheckStart + Vector2.right * direction * 0.35f);
            }
        }
    }
}
