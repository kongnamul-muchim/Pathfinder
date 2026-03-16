using UnityEngine;

namespace Pathfinder.Player
{
    /// <summary>
    /// 플레이어 애니메이션 상태 관리 컴포넌트
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("Animation Parameters")]
        [SerializeField] private string _isWalkingParam = "IsWalking";
        [SerializeField] private string _isJumpingParam = "IsJumping";
        [SerializeField] private string _isGroundedParam = "IsGrounded";
        [SerializeField] private string _triggerDeathParam = "Death";
        
        private Animator _animator;
        private SpriteRenderer _spriteRenderer;
        
        // 상태 캐싱 (불필요한 애니메이션 업데이트 방지)
        private bool _wasWalking;
        private bool _wasJumping;
        private bool _wasGrounded;
        private float _lastFacingDirection = 1f;
        
        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            
            ValidateAnimatorParameters();
        }
        
        /// <summary>
        /// Animator 파라미터 유효성 검사
        /// </summary>
        private void ValidateAnimatorParameters()
        {
            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning("[PlayerAnimator] Animator 또는 AnimatorController가 설정되지 않았습니다.");
                return;
            }
        }
        
        /// <summary>
        /// 이동 상태 업데이트
        /// </summary>
        /// <param name="isWalking">걷는 중인지 여부</param>
        public void SetWalking(bool isWalking)
        {
            if (_wasWalking != isWalking)
            {
                _wasWalking = isWalking;
                _animator?.SetBool(_isWalkingParam, isWalking);
            }
        }
        
        /// <summary>
        /// 점프 상태 업데이트
        /// </summary>
        /// <param name="isJumping">점프 중인지 여부</param>
        public void SetJumping(bool isJumping)
        {
            if (_wasJumping != isJumping)
            {
                _wasJumping = isJumping;
                _animator?.SetBool(_isJumpingParam, isJumping);
            }
        }
        
        /// <summary>
        /// 지면 접촉 상태 업데이트
        /// </summary>
        /// <param name="isGrounded">지면에 닿아있는지 여부</param>
        public void SetGrounded(bool isGrounded)
        {
            if (_wasGrounded != isGrounded)
            {
                _wasGrounded = isGrounded;
                _animator?.SetBool(_isGroundedParam, isGrounded);
            }
        }
        
        /// <summary>
        /// 캐릭터 방향 설정 (좌우 반전)
        /// </summary>
        /// <param name="direction">방향 (양수: 오른쪽, 음수: 왼쪽)</param>
        public void SetFacingDirection(float direction)
        {
            // 방향이 0에 가까우면 무시
            if (Mathf.Abs(direction) < 0.01f) return;
            
            float normalizedDirection = Mathf.Sign(direction);
            
            if (_lastFacingDirection != normalizedDirection)
            {
                _lastFacingDirection = normalizedDirection;
                _spriteRenderer.flipX = normalizedDirection < 0;
            }
        }
        
        /// <summary>
        /// 사망 애니메이션 트리거
        /// </summary>
        public void TriggerDeath()
        {
            _animator?.SetTrigger(_triggerDeathParam);
        }
        
        /// <summary>
        /// 현재 재생 중인 애니메이션 상태 정보
        /// </summary>
        public AnimatorStateInfo GetCurrentStateInfo(int layerIndex = 0)
        {
            return _animator?.GetCurrentAnimatorStateInfo(layerIndex) ?? default;
        }
        
        /// <summary>
        /// 현재 애니메이션이 특정 상태인지 확인
        /// </summary>
        public bool IsInState(string stateName, int layerIndex = 0)
        {
            if (_animator == null) return false;
            return _animator.GetCurrentAnimatorStateInfo(layerIndex).IsName(stateName);
        }
        
        /// <summary>
        /// 애니메이션 속도 설정 (0 = 정지, 1 = 정상)
        /// </summary>
        public void SetSpeed(float speed)
        {
            if (_animator != null)
            {
                _animator.speed = speed;
            }
        }
        
        /// <summary>
        /// 모든 상태 초기화
        /// </summary>
        public void ResetState()
        {
            _wasWalking = false;
            _wasJumping = false;
            _wasGrounded = true;
            
            _animator?.SetBool(_isWalkingParam, false);
            _animator?.SetBool(_isJumpingParam, false);
            _animator?.SetBool(_isGroundedParam, true);
            _animator?.ResetTrigger(_triggerDeathParam);
        }
    }
}
