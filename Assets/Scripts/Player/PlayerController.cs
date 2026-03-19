using UnityEngine;
using UnityEngine.InputSystem;
using Pathfinder.Abilities;
using Pathfinder.Core.DI;
using Pathfinder.Common;
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
        private const float DOUBLE_TAP_RESET_TIME = -999f;
        private const float INPUT_THRESHOLD = 0.5f;
        private const float VELOCITY_THRESHOLD = 0.1f;
        private const float ANIMATION_THRESHOLD = 0.01f;
        private const float GROUND_CHECK_LOCAL_Y = -0.5f;
        
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        
        [Header("Ground Detection")]
        [SerializeField] private float _stepHeight = 0.2f;
        [SerializeField] private float _stepCheckDistance = 0.1f;
        
        [Header("Slope Handling")]
        [Tooltip("경사로 이동을 활성화")]
        [SerializeField] private bool _useSlopeMovement = true;
        
        [Tooltip("최대 경사로 각도 (도)")]
        [SerializeField] private float _maxSlopeAngle = 45f;
        
        [Header("Jump")]
        [SerializeField] private float _jumpForce = 7f;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private float _groundCheckRadius = 0.2f;
        
        [Header("Abilities")]
        [Tooltip("더블점프 활성화")]
        [SerializeField] private bool _enableDoubleJump = true;
        
        [Tooltip("대쉬 활성화")]
        [SerializeField] private bool _enableDash = true;
        
        [Tooltip("대쉬 힘")]
        [SerializeField] private float _dashForce = 20f;
        
        [Tooltip("대쉬 지속 시간")]
        [SerializeField] private float _dashDuration = 0.25f;
        
        [Tooltip("대쉬 쿨타임")]
        [SerializeField] private float _dashCooldown = 1f;
        
        [Tooltip("더블탭 감지 시간 (초)")]
        [SerializeField] private float _doubleTapTime = 0.3f;
        
        [Header("Dash Collider")]
        [Tooltip("대쉬 시 Collider 높이 감소량")]
        [SerializeField] private float _dashColliderHeightReduction = 1f;
        
        [Tooltip("빈 공간 최대 탐색 거리")]
        [SerializeField] private float _safePositionSearchDistance = 3f;
        
        [Tooltip("빈 공간 탐색 간격")]
        [SerializeField] private float _safePositionSearchStep = 0.5f;
        
        [Header("Interaction")]
        [Tooltip("상호작용 프롬프트 UI 프리팹")]
        [SerializeField] private InteractionPromptUI _interactionPromptPrefab;
        
        [Tooltip("상호작용 감지 반경")]
        [SerializeField] private float _interactionRadius = 2f;
        
        private Rigidbody2D _rb;
        private bool _isGrounded;
        private float _horizontalInput;
        private bool _jumpRequested;
        
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _interactAction;
        private InputAction _forceDeathAction;
        
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
        private bool _isDoubleJump = false;
        
        // 대쉬
        private bool _canDash = true;
        private bool _isDashing = false;
        private float _lastLeftTapTime = DOUBLE_TAP_RESET_TIME;
        private float _lastRightTapTime = DOUBLE_TAP_RESET_TIME;
        private bool _lastLeftKeyState = false;
        private bool _lastRightKeyState = false;
        
        // 대쉬 Collider
        private BoxCollider2D _boxCollider;
        private Vector2 _originalColliderOffset;
        private Vector2 _originalColliderSize;
        
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _playerAnimator = GetComponent<PlayerAnimator>();
            
            // BoxCollider2D 초기화
            _boxCollider = GetComponent<BoxCollider2D>();
            if (_boxCollider != null)
            {
                _originalColliderOffset = _boxCollider.offset;
                _originalColliderSize = _boxCollider.size;
            }
            
            // Input Actions 설정
            _moveAction = new InputAction("Move", binding: "<Keyboard>/a");
            _moveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            
            _jumpAction = new InputAction("Jump", binding: "<Keyboard>/space");
            
            // 상호작용 입력 설정 (E키)
            _interactAction = new InputAction("Interact", binding: "<Keyboard>/e");
            
            // 강제 사망 입력 설정 (P키 - 테스트용)
            _forceDeathAction = new InputAction("ForceDeath", binding: "<Keyboard>/p");
            
            // GroundCheck가 없으면 자동 생성
            if (_groundCheck == null)
            {
                var groundCheckObj = new GameObject("GroundCheck");
                groundCheckObj.transform.SetParent(transform);
                groundCheckObj.transform.localPosition = new Vector3(0, GROUND_CHECK_LOCAL_Y, 0);
                _groundCheck = groundCheckObj.transform;
            }
        }
        
        private void OnEnable()
        {
            _moveAction?.Enable();
            _jumpAction?.Enable();
            _interactAction?.Enable();
            _forceDeathAction?.Enable();
        }
        
        private void OnDisable()
        {
            _moveAction?.Disable();
            _jumpAction?.Disable();
            _interactAction?.Disable();
            _forceDeathAction?.Disable();
        }
        
        private void OnDestroy()
        {
            _moveAction?.Dispose();
            _jumpAction?.Dispose();
            _interactAction?.Dispose();
            _forceDeathAction?.Dispose();
        }
        
        private void Start()
        {
            if (_abilityManager == null)
            {
                _abilityManager = FindFirstObjectByType<AbilityManager>();
                if (_abilityManager == null)
                {
                    Debug.LogWarning("[PlayerController] AbilityManager not found!");
                }
            }
            
            if (_deathManager == null)
            {
                _deathManager = FindFirstObjectByType<DeathManager>();
                if (_deathManager == null)
                {
                    Debug.LogWarning("[PlayerController] DeathManager not found!");
                }
            }
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
                // 파괴된 오브젝트인지 확인
                try
                {
                    _currentInteractable.OnInteract();
                }
                catch (MissingReferenceException)
                {
                    _currentInteractable = null;
                    if (_currentPrompt != null)
                    {
                        _currentPrompt.Hide();
                    }
                }
            }
            
            // 강제 사망 입력 처리 (P키 - 테스트용)
            if (_forceDeathAction.WasPressedThisFrame())
            {
                Die();
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
            
            // Dash 중에는 점프 불가
            if (_isDashing) return;
            
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
                _isDoubleJump = _jumpCount >= 1; // 1단 점프 후면 더블점프
                _jumpRequested = true;
                _jumpCount++;
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
            
            // 현재 키 상태 확인
            bool leftKeyPressed = _horizontalInput < -INPUT_THRESHOLD;
            bool rightKeyPressed = _horizontalInput > INPUT_THRESHOLD;
            
            // 왼쪽 키가 방금 눌림 (이전에는 안 려있었고 지금은 눌려있음)
            if (leftKeyPressed && !_lastLeftKeyState)
            {
                // 이전 탭이 왼쪽이었고 시간 내에 다시 눌렀는지 체크
                if (currentTime - _lastLeftTapTime < _doubleTapTime)
                {
                    StartCoroutine(DashCoroutine(-1));
                    _lastLeftTapTime = DOUBLE_TAP_RESET_TIME;
                }
                else
                {
                    _lastLeftTapTime = currentTime;
                }
            }
            
            // 오른쪽 키가 방금 눌림
            if (rightKeyPressed && !_lastRightKeyState)
            {
                // 이전 탭이 오른쪽이었고 시간 내에 다시 눌렀는지 체크
                if (currentTime - _lastRightTapTime < _doubleTapTime)
                {
                    StartCoroutine(DashCoroutine(1));
                    _lastRightTapTime = DOUBLE_TAP_RESET_TIME;
                }
                else
                {
                    _lastRightTapTime = currentTime;
                }
            }
            
            // 키 상태 저장
            _lastLeftKeyState = leftKeyPressed;
            _lastRightKeyState = rightKeyPressed;
        }
        
        /// <summary>
        /// 대쉬 코루틴
        /// </summary>
        private System.Collections.IEnumerator DashCoroutine(int direction)
        {
            _isDashing = true;
            _canDash = false;
            
            // 대쉬 애니메이션 시작
            _playerAnimator?.TriggerDash();
            
            // Collider 축소
            SetDashCollider(true);
            
            // 대쉬 시작
            float originalGravity = _rb.gravityScale;
            
            try
            {
                _rb.gravityScale = 0; // 중력 임시 제거
                _rb.linearVelocity = new Vector2(direction * _dashForce, 0);
                
                yield return new WaitForSeconds(_dashDuration);
            }
            finally
            {
                // 항상 중력 복구 (예외 발생 시에도)
                _rb.gravityScale = originalGravity;
                _isDashing = false;
                
                // 안전하게 Collider 복원 시도
                TryRestoreCollider();
            }
            
            // 쿨타임 대기
            yield return new WaitForSeconds(_dashCooldown);
            
            // 바닥에 닿아있으면 대쉬 가능 (착지 시 리셋)
            if (_isGrounded)
            {
                _canDash = true;
            }
        }
        
        /// <summary>
        /// 대쉬 시 Collider 축소/복원
        /// </summary>
        private void SetDashCollider(bool enable)
        {
            if (_boxCollider == null) return;
            
            if (enable)
            {
                float newHeight = _originalColliderSize.y - _dashColliderHeightReduction;
                _boxCollider.size = new Vector2(_originalColliderSize.x, newHeight);
                
                float offsetReduction = _dashColliderHeightReduction / 2f;
                _boxCollider.offset = new Vector2(_originalColliderOffset.x, _originalColliderOffset.y - offsetReduction);
            }
            else
            {
                _boxCollider.size = _originalColliderSize;
                _boxCollider.offset = _originalColliderOffset;
            }
        }
        
        /// <summary>
        /// 안전하게 Collider 복원 시도
        /// </summary>
        private bool TryRestoreCollider()
        {
            if (_boxCollider == null) return true;
            
            Vector2 checkPosition = (Vector2)transform.position + _originalColliderOffset;
            
            if (!Physics2D.OverlapBox(checkPosition, _originalColliderSize, 0f, _groundLayer))
            {
                SetDashCollider(false);
                return true;
            }
            
            Vector2? safePosition = FindSafePositionX();
            
            if (safePosition.HasValue)
            {
                transform.position = new Vector3(safePosition.Value.x, transform.position.y, transform.position.z);
                SetDashCollider(false);
                return true;
            }
            
            SetDashCollider(false);
            return false;
        }
        
        /// <summary>
        /// X축으로 빈 공간 탐색
        /// </summary>
        private Vector2? FindSafePositionX()
        {
            float direction = Mathf.Sign(_rb.linearVelocity.x);
            if (Mathf.Abs(_rb.linearVelocity.x) < VELOCITY_THRESHOLD) direction = 1f;
            
            Vector2? found = SearchInDirection(direction);
            if (found.HasValue) return found;
            
            return SearchInDirection(-direction);
        }
        
        /// <summary>
        /// 지정된 방향으로 빈 공간 탐색
        /// </summary>
        private Vector2? SearchInDirection(float direction)
        {
            for (float dist = _safePositionSearchStep; dist <= _safePositionSearchDistance; dist += _safePositionSearchStep)
            {
                Vector2 testPosition = (Vector2)transform.position + Vector2.right * direction * dist;
                Vector2 checkPos = testPosition + _originalColliderOffset;
                
                if (!Physics2D.OverlapBox(checkPos, _originalColliderSize, 0f, _groundLayer))
                {
                    return testPosition;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 애니메이션 상태 업데이트
        /// </summary>
        private void UpdateAnimations()
        {
            if (_playerAnimator == null) return;
            
            // 걷기 상태
            bool isWalking = Mathf.Abs(_horizontalInput) > ANIMATION_THRESHOLD && _isGrounded;
            _playerAnimator.SetWalking(isWalking);
            
            bool isJumping = !_isGrounded && _rb.linearVelocity.y > VELOCITY_THRESHOLD;
            _playerAnimator.SetJumping(isJumping);
            
            _playerAnimator.SetGrounded(_isGrounded);
            
            if (Mathf.Abs(_horizontalInput) > ANIMATION_THRESHOLD)
            {
                _playerAnimator.SetFacingDirection(_horizontalInput);
            }
        }
        
        private void FixedUpdate()
        {
            Vector2 velocity = _rb.linearVelocity;
            
            if (!_isDashing)
            {
                if (_useSlopeMovement && _isGrounded)
                {
                    velocity = GetSlopeMovementVelocity();
                }
                else
                {
                    velocity.x = _horizontalInput * _moveSpeed;
                }
                
                _rb.linearVelocity = velocity;
            }
            
            HandleStepUp();
            
            if (!_isDashing && Mathf.Abs(_horizontalInput) > VELOCITY_THRESHOLD && Mathf.Abs(_rb.linearVelocity.x) < VELOCITY_THRESHOLD)
            {
                velocity = _rb.linearVelocity;
                velocity.x = _horizontalInput * 0.5f;
                _rb.linearVelocity = velocity;
            }
            
            if (_jumpRequested)
            {
                if (_isDoubleJump)
                {
                    Vector2 jumpVel = _rb.linearVelocity;
                    jumpVel.y = 0;
                    _rb.linearVelocity = jumpVel;
                    _isDoubleJump = false;
                }
                
                _rb.AddForce(Vector2.up * _jumpForce, ForceMode2D.Impulse);
                _jumpRequested = false;
            }
        }
        
        /// <summary>
        /// 작은 단차(step)를 만나면 자연스럽게 위로 밀어 올림
        /// </summary>
        private void HandleStepUp()
        {
            if (Mathf.Abs(_horizontalInput) < ANIMATION_THRESHOLD) return;
            
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
            Debug.Log($"[DIE] Called - _deathManager: {_deathManager != null}");
            
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
            
            if (Application.isPlaying && Mathf.Abs(_horizontalInput) > 0.01f)
            {
                float direction = Mathf.Sign(_horizontalInput);
                Vector2 position = transform.position;
                
                Vector2 rayStart = position + Vector2.right * direction * 0.3f;
                rayStart.y -= 0.2f;
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(rayStart, rayStart + Vector2.down * _stepCheckDistance);
            }
        }
        
        #region Interaction
        
        /// <summary>
        /// 주변 상호작용 가능한 오브젝트 감지
        /// </summary>
        private void CheckForInteractables()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _interactionRadius);
            
            IInteractable nearestInteractable = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var collider in colliders)
            {
                // 파괴된 오브젝트는 스킵
                if (collider == null || collider.gameObject == null)
                    continue;
                
                // IInteractable 인터페이스 찾기
                IInteractable interactable = null;
                
                // WarpPoint 확인
                var warpPoint = collider.GetComponent<WarpPoint>();
                if (warpPoint != null)
                {
                    interactable = warpPoint as IInteractable;
                }
                
                // AbilityChest 확인
                if (interactable == null)
                {
                    var abilityChest = collider.GetComponent<AbilityChest>();
                    if (abilityChest != null)
                    {
                        interactable = abilityChest as IInteractable;
                    }
                }
                
                // 직접 IInteractable 컴포넌트도 확인
                if (interactable == null)
                {
                    interactable = collider.GetComponent<IInteractable>();
                }
                
                if (interactable != null)
                {
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
            }
            
            // 상호작용 가능한 오브젝트가 변경되었을 때
            if (nearestInteractable != _currentInteractable)
            {
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
            if (_interactionPromptPrefab == null) return;
            
            // 프롬프트 UI가 없으면 생성
            if (_currentPrompt == null)
            {
                _currentPrompt = Instantiate(_interactionPromptPrefab);
            }
            
            // 프롬프트 표시 - 플레이어 머리 위에 표시
            var text = interactable.GetInteractionText();
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
