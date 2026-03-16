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
            
            // 점프
            if (_jumpRequested)
            {
                _rb.AddForce(Vector2.up * _jumpForce, ForceMode2D.Impulse);
                _jumpRequested = false;
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
        }
    }
}
