using UnityEngine;
using UnityEngine.InputSystem;
using Pathfinder.Core.DI;
using Pathfinder.Interfaces;
using Pathfinder.UI;
using Pathfinder.World;

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
        
        [Header("Slope Handling")]
        [Tooltip("경사로 이동을 활성화")]
        [SerializeField] private bool _useSlopeMovement = true;
        
        [Tooltip("최대 경사로 각도 (도)")]
        [SerializeField] private float _maxSlopeAngle = 45f;
        
        [Header("Wall Detection")]
        [SerializeField] private float _wallCheckDistance = 0.35f;
        [SerializeField] private float _wallSlideSpeed = 2f;
        [SerializeField] private LayerMask _wallLayer;
        
        [Header("Jump")]
        [SerializeField] private float _jumpForce = 10f;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private float _groundCheckRadius = 0.2f;
        
        [Header("Abilities")]
        [Tooltip("더블점프 활성화")]
        [SerializeField] private bool _enableDoubleJump = true;
        
        [Tooltip("대쉬 활성화")]
        [SerializeField] private bool _enableDash = true;
        
        [Tooltip("대쉬 힘")]
        [SerializeField] private float _dashForce = 15f;
        
        [Tooltip("대쉬 지속 시간")]
        [SerializeField] private float _dashDuration = 0.2f;
        
        [Tooltip("대쉬 쿨타임")]
        [SerializeField] private float _dashCooldown = 1f;
        
        [Tooltip("더블탭 감지 시간 (초)")]
        [SerializeField] private float _doubleTapTime = 0.3f;
        
        [Header("Interaction")]
        [Tooltip("상호작용 프롬프트 UI 프리팹")]
        [SerializeField] private InteractionPromptUI _interactionPromptPrefab;
        
        [Tooltip("상호작용 감지 반경")]
        [SerializeField] private float _interactionRadius = 2f;
        
        private bool _isTouchingWall;
        private float _wallDirection;
        private Rigidbody2D _rb;
        private bool _isGrounded;
        private float _horizontalInput;
        private bool _jumpRequested;
        
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _interactAction;
        
        // 상호작용
        private InteractionPromptUI _currentPrompt;
        private IInteractable _currentInteractable;
        
        // DI 주입
        [Inject] private IAbilityManager _abilityManager;
        [Inject] private IDeathManager _deathManager;
        
        // 애니메이션
        private PlayerAnimator _playerAnimator;
        
        // 더블점프
        private int _jumpCount = 0;
        private int _maxJumpCount = 1; // 기본 1, 더블점프 시 2
        
        // 대쉬
        private bool _canDash = true;
        private bool _isDashing = false;
        private float _lastDashTime = -999f;
        private float _lastLeftTapTime = -999f;
        private float _lastRightTapTime = -999f;
        private int _lastTapDirection = 0;
        
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
            
            // 상호작용 입력 설정 (Q키로 변경)
            _interactAction = new InputAction("Interact", binding: "<Keyboard>/q");
            
            // GroundCheck가 없으면 자동 생성
            if (_groundCheck == null)
            {
                var groundCheckObj = new GameObject("GroundCheck");
                groundCheckObj.transform.SetParent(transform);
                groundCheckObj.transform.localPosition = new Vector3(0, -0.5f, 0);
                _groundCheck = groundCheckObj.transform;
            }
            
            // Wall Layer가 설정되지 않았으면 Ground Layer 사용
            if (_wallLayer == 0)
            {
                _wallLayer = _groundLayer;
            }
        }
        
        private void OnEnable()
        {
            _moveAction?.Enable();
            _jumpAction?.Enable();
            _interactAction?.Enable();
        }
        
        private void OnDisable()
        {
            _moveAction?.Disable();
            _jumpAction?.Disable();
            _interactAction?.Disable();
        }
        
        private void OnDestroy()
        {
            _moveAction?.Dispose();
            _jumpAction?.Dispose();
            _interactAction?.Dispose();
        }
        
        private void Start()
        {
            // RootContext에 의해 DI 주입됨
        }
        
        private void Update()
        {
            // 입력 처리
            _horizontalInput = _moveAction.ReadValue<float>();
            
            // 더블점프 및 점프 처리
            HandleJumpInput();
            
            // 대쉬 입력 처리 (더블탭)
            HandleDashInput();
            
            // 상호작용 입력 처리
            if (_interactAction.WasPressedThisFrame() && _currentInteractable != null)
            {
                _currentInteractable.OnInteract();
            }
            
            // 지면 체크
            bool wasGrounded = _isGrounded;
            _isGrounded = Physics2D.OverlapCircle(_groundCheck.position, _groundCheckRadius, _groundLayer);
            
            // 지면에 착지 시 점프 카운트 리셋 및 대쉬 가능
            if (!wasGrounded && _isGrounded)
            {
                _jumpCount = 0;
                _canDash = true; // 착지 시 대쉬 리셋
            }
            
            // 상호작용 오브젝트 감지
            CheckForInteractables();
            
            // 애니메이션 업데이트
            UpdateAnimations();
        }
        
        /// <summary>
        /// 점프 입력 처리 (더블점프 포함)
        /// </summary>
        private void HandleJumpInput()
        {
            if (!_jumpAction.WasPressedThisFrame()) return;
            
            // 지면에 있으면 기본 점프
            if (_isGrounded)
            {
                _jumpRequested = true;
                _jumpCount = 1;
                return;
            }
            
            // 공중에서 더블점프 체크
            if (_enableDoubleJump && _abilityManager?.HasAbility(AbilityType.DoubleJump) == true)
            {
                _maxJumpCount = 2;
            }
            else
            {
                _maxJumpCount = 1;
            }
            
            // 더블점프 가능 여부 체크
            if (_jumpCount < _maxJumpCount)
            {
                _jumpRequested = true;
                _jumpCount++;
                Debug.Log($"[PlayerController] DoubleJump! Count: {_jumpCount}/{_maxJumpCount}");
            }
        }
        
        /// <summary>
        /// 대쉬 입력 처리 (방향키 더블탭)
        /// </summary>
        private void HandleDashInput()
        {
            if (!_enableDash || _abilityManager?.HasAbility(AbilityType.Dash) != true) return;
            if (!_canDash || _isDashing) return;
            
            float currentTime = Time.time;
            int currentDirection = 0;
            
            // 왼쪽 방향키 체크
            if (_horizontalInput < -0.5f)
            {
                currentDirection = -1;
                // 이전 탭이 왼쪽이었고 시간 내에 다시 눌렀는지 체크
                if (_lastTapDirection == -1 && currentTime - _lastLeftTapTime < _doubleTapTime)
                {
                    StartCoroutine(DashCoroutine(-1));
                    _lastTapDirection = 0; // 리셋
                    return;
                }
                _lastLeftTapTime = currentTime;
            }
            // 오른쪽 방향키 체크
            else if (_horizontalInput > 0.5f)
            {
                currentDirection = 1;
                // 이전 탭이 오른쪽이었고 시간 내에 다시 눌렀는지 체크
                if (_lastTapDirection == 1 && currentTime - _lastRightTapTime < _doubleTapTime)
                {
                    StartCoroutine(DashCoroutine(1));
                    _lastTapDirection = 0; // 리셋
                    return;
                }
                _lastRightTapTime = currentTime;
            }
            
            _lastTapDirection = currentDirection;
        }
        
        /// <summary>
        /// 대쉬 코루틴
        /// </summary>
        private System.Collections.IEnumerator DashCoroutine(int direction)
        {
            _isDashing = true;
            _canDash = false;
            
            // 대쉬 시작
            float originalGravity = _rb.gravityScale;
            _rb.gravityScale = 0; // 중력 임시 제거
            _rb.linearVelocity = new Vector2(direction * _dashForce, 0);
            
            Debug.Log($"[PlayerController] Dash started! Direction: {direction}");
            
            yield return new WaitForSeconds(_dashDuration);
            
            // 대쉬 종료
            _rb.gravityScale = originalGravity;
            _isDashing = false;
            
            Debug.Log($"[PlayerController] Dash ended. Cooldown: {_dashCooldown}s");
            
            // 쿨타임 대기
            yield return new WaitForSeconds(_dashCooldown);
            
            // 바닥에 닿아있으면 대쉬 가능 (착지 시 리셋)
            if (_isGrounded)
            {
                _canDash = true;
                Debug.Log("[PlayerController] Dash reset (grounded)");
            }
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
            // 벽 감지
            CheckWallCollision();
            
            // 이동 (벽에 닿지 않았을 때만)
            if (!_isTouchingWall)
            {
                Vector2 velocity = _rb.linearVelocity;
                
                // 경사로 이동 처리
                if (_useSlopeMovement && _isGrounded)
                {
                    velocity = GetSlopeMovementVelocity();
                }
                else
                {
                    // 일반 수평 이동
                    velocity.x = _horizontalInput * _moveSpeed;
                }
                
                _rb.linearVelocity = velocity;
            }
            else
            {
                // 벽에 닿았을 때는 X축 이동 멈춤, Y축은 자유롭게 (미끄러짐)
                Vector2 velocity = _rb.linearVelocity;
                velocity.x = 0;
                // 벽에 붙어있을 때 아래로 미끄러지게
                if (!_isGrounded && velocity.y > -_wallSlideSpeed)
                {
                    velocity.y = -_wallSlideSpeed;
                }
                _rb.linearVelocity = velocity;
            }
            
            // 작은 단차(step) 자동 올라가기 (벽에 닿지 않았을 때만)
            if (!_isTouchingWall)
            {
                HandleStepUp();
            }
            
            // 점프
            if (_jumpRequested)
            {
                _rb.AddForce(Vector2.up * _jumpForce, ForceMode2D.Impulse);
                _jumpRequested = false;
            }
        }
        
        /// <summary>
        /// 벽 충돌 감지
        /// </summary>
        private void CheckWallCollision()
        {
            if (Mathf.Abs(_horizontalInput) < 0.01f)
            {
                _isTouchingWall = false;
                return;
            }
            
            float direction = Mathf.Sign(_horizontalInput);
            Vector2 position = transform.position;
            
            // 여러 지점에서 벽 체크 (더 정확한 감지)
            Vector2[] checkPoints = new Vector2[]
            {
                position + Vector2.up * 0.5f,    // 상단
                position,                         // 중앙
                position + Vector2.down * 0.3f    // 하단
            };
            
            _isTouchingWall = false;
            foreach (var checkPoint in checkPoints)
            {
                RaycastHit2D wallHit = Physics2D.Raycast(
                    checkPoint, 
                    Vector2.right * direction, 
                    _wallCheckDistance, 
                    _wallLayer
                );
                
                if (wallHit.collider != null)
                {
                    _isTouchingWall = true;
                    _wallDirection = direction;
                    break;
                }
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
        }
        
        /// <summary>
        /// 경사로 방향으로 이동 속도 계산
        /// </summary>
        private Vector2 GetSlopeMovementVelocity()
        {
            // 바닥 각도 감지
            Vector2 position = transform.position;
            Vector2 rayStart = position + Vector2.down * 0.4f;
            
            // Raycast로 바닥 각도 측정
            RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, 0.3f, _groundLayer);
            if (hit.collider == null) return new Vector2(_horizontalInput * _moveSpeed, _rb.linearVelocity.y);
            
            // 바닥의 법선 벡터로부터 각도 계산
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            
            // 경사로가 너무 가파르면 일반 이동
            if (slopeAngle > _maxSlopeAngle)
            {
                return new Vector2(_horizontalInput * _moveSpeed, _rb.linearVelocity.y);
            }
            
            // 경사로 표면에 평행한 방향 계산
            Vector2 slopeDirection = new Vector2(hit.normal.y, -hit.normal.x);
            
            // 이동 방향에 따라 경사로 방향 조정
            if (_horizontalInput < 0)
            {
                slopeDirection = -slopeDirection;
            }
            else if (_horizontalInput > 0)
            {
                // 오른쪽 이동 시 경사로 방향 유지
            }
            else
            {
                // 입력 없음
                return new Vector2(0, _rb.linearVelocity.y);
            }
            
            // 이동 속도 적용
            return slopeDirection * _moveSpeed;
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            // 함정이나 적과 충돌
            if (other.CompareTag("Trap"))
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
                
                // Wall check (3 points)
                Vector2[] checkPoints = new Vector2[]
                {
                    position + Vector2.up * 0.5f,
                    position,
                    position + Vector2.down * 0.3f
                };
                
                Gizmos.color = _isTouchingWall ? Color.magenta : Color.red;
                foreach (var checkPoint in checkPoints)
                {
                    Gizmos.DrawLine(checkPoint, checkPoint + Vector2.right * direction * _wallCheckDistance);
                }
            }
        }
        
        #region Interaction
        
        /// <summary>
        /// 주변 상호작용 가능한 오브젝트 감지
        /// </summary>
        private void CheckForInteractables()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _interactionRadius);
            
            Debug.Log($"[PlayerController] Found {colliders.Length} colliders in range");
            
            IInteractable nearestInteractable = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var collider in colliders)
            {
                Debug.Log($"[PlayerController] Checking collider: {collider.name}");
                
                // IInteractable 인터페이스 찾기 (GetComponent 직접 호출)
                var warpPoint = collider.GetComponent<WarpPoint>();
                IInteractable interactable = warpPoint as IInteractable;
                
                if (interactable != null)
                {
                    Debug.Log($"[PlayerController] Found IInteractable on {collider.name}, CanInteract: {interactable.CanInteract()}");
                    
                    if (interactable.CanInteract())
                    {
                        float distance = Vector2.Distance(transform.position, collider.transform.position);
                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                            nearestInteractable = interactable;
                        }
                    }
                }
                else
                {
                    Debug.Log($"[PlayerController] No IInteractable found on {collider.name}");
                }
            }
            
            // 상호작용 가능한 오브젝트가 변경되었을 때
            if (nearestInteractable != _currentInteractable)
            {
                Debug.Log($"[PlayerController] Interactable changed from {(_currentInteractable != null ? "something" : "null")} to {(nearestInteractable != null ? "something" : "null")}");
                
                // 이전 프롬프트 숨김
                if (_currentPrompt != null)
                {
                    _currentPrompt.Hide();
                }
                
                _currentInteractable = nearestInteractable;
                
                // 새 프롬프트 표시
                if (_currentInteractable != null)
                {
                    ShowInteractionPrompt(_currentInteractable);
                }
            }
        }
        
        /// <summary>
        /// 상호작용 프롬프트 표시
        /// </summary>
        private void ShowInteractionPrompt(IInteractable interactable)
        {
            Debug.Log($"[PlayerController] ShowInteractionPrompt called, prefab: {(_interactionPromptPrefab != null ? "not null" : "NULL")}");
            
            if (_interactionPromptPrefab == null)
            {
                Debug.LogError("[PlayerController] InteractionPromptPrefab is NULL! Please assign it in the Inspector.");
                return;
            }
            
            // 프롬프트 UI가 없으면 생성
            if (_currentPrompt == null)
            {
                Debug.Log("[PlayerController] Creating new prompt instance");
                _currentPrompt = Instantiate(_interactionPromptPrefab);
            }
            
            // 프롬프트 표시 - 플레이어 머리 위에 표시
            var text = interactable.GetInteractionText();
            Debug.Log($"[PlayerController] Showing prompt on player head, text: {text}");
            _currentPrompt.Show(transform, text);
        }
        
        private void OnDrawGizmosSelected()
        {
            // 상호작용 범위 시각화
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _interactionRadius);
            
            // 경사로 감지 시각화
            if (_useSlopeMovement && Application.isPlaying)
            {
                Vector2 position = transform.position;
                Vector2 rayStart = position + Vector2.down * 0.4f;
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(rayStart, rayStart + Vector2.down * 0.3f);
                
                RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, 0.3f, _groundLayer);
                if (hit.collider != null)
                {
                    // 경사로 방향 표시
                    Vector2 slopeDirection = new Vector2(hit.normal.y, -hit.normal.x);
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(hit.point, slopeDirection * 0.5f);
                }
            }
        }
        
        #endregion
    }
}
