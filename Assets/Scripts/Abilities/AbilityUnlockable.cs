using Pathfinder.Core.DI;
using Pathfinder.Player;
using UnityEngine;

namespace Pathfinder.Abilities
{
    /// <summary>
    /// 능력 구체 - 획득 시 능력 해금
    /// </summary>
    public class AbilityUnlockable : MonoBehaviour, IAbilityUnlockable
    {
        [Header("Ability Settings")]
        [Tooltip("해금할 능력 종류")]
        [SerializeField] private AbilityType _abilityType = AbilityType.DoubleJump;
        
        [Tooltip("획득 시 파괴 여부")]
        [SerializeField] private bool _destroyOnCollect = true;
        
        [Header("Visual")]
        [Tooltip("능력 구체 스프라이트")]
        [SerializeField] private Sprite _abilitySprite;
        
        [Tooltip("회전 속도")]
        [SerializeField] private float _rotationSpeed = 100f;
        
        [Tooltip("부유 효과 진폭")]
        [SerializeField] private float _floatAmplitude = 0.2f;
        
        [Tooltip("부유 효과 주기")]
        [SerializeField] private float _floatFrequency = 2f;
        
        [Tooltip("획득 이펙트")]
        [SerializeField] private GameObject _collectEffect;
        
        [Tooltip("획득 사운드 (선택적)")]
        [SerializeField] private AudioClip _collectSound;
        
        // DI 주입
        [Inject] private IAbilityManager _abilityManager;
        
        // 컴포넌트
        private SpriteRenderer _spriteRenderer;
        private AudioSource _audioSource;
        
        // 초기 위치 (부유 효과용)
        private Vector3 _initialPosition;
        private float _time;
        
        // 획득 여부
        private bool _isCollected = false;
        
        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _audioSource = GetComponent<AudioSource>();
            
            // 트리거 콜라이더 설정
            var collider = GetComponent<Collider2D>();
            if (collider == null)
            {
                var circleCollider = gameObject.AddComponent<CircleCollider2D>();
                circleCollider.isTrigger = true;
                circleCollider.radius = 0.5f;
            }
            else if (!collider.isTrigger)
            {
                collider.isTrigger = true;
            }
            
            // 스프라이트 설정
            if (_spriteRenderer != null && _abilitySprite != null)
            {
                _spriteRenderer.sprite = _abilitySprite;
            }
            
            // 색상 설정 (능력별)
            SetAbilityColor();
        }
        
        private void Start()
        {
            _initialPosition = transform.position;
            
            // 이미 해금된 능력이면 숨김
            if (_abilityManager != null && _abilityManager.HasAbility(_abilityType))
            {
                gameObject.SetActive(false);
            }
        }
        
        private void Update()
        {
            if (_isCollected) return;
            
            _time += Time.deltaTime;
            
            // 회전
            transform.Rotate(0, 0, _rotationSpeed * Time.deltaTime);
            
            // 부유 효과
            float floatY = Mathf.Sin(_time * _floatFrequency) * _floatAmplitude;
            transform.position = _initialPosition + Vector3.up * floatY;
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isCollected) return;
            
            if (other.CompareTag("Player"))
            {
                Unlock();
            }
        }
        
        /// <summary>
        /// 능력 해금
        /// </summary>
        public void Unlock()
        {
            if (_isCollected) return;
            _isCollected = true;
            
            // 능력 해금
            if (_abilityManager != null)
            {
                _abilityManager.UnlockAbility(_abilityType);
            }
            
            // 이펙트 재생
            PlayCollectEffect();
            
            // 사운드 재생
            PlaySound();
            
            // 이벤트 발행
            OnAbilityUnlocked?.Invoke(_abilityType);
            
            // 파괴 또는 비활성화
            if (_destroyOnCollect)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// 능력 종류 반환
        /// </summary>
        public AbilityType GetAbilityType()
        {
            return _abilityType;
        }
        
        /// <summary>
        /// 획득 여부 반환
        /// </summary>
        public bool IsUnlocked()
        {
            return _isCollected;
        }
        
        /// <summary>
        /// 능력 색상 설정
        /// </summary>
        private void SetAbilityColor()
        {
            if (_spriteRenderer == null) return;
            
            switch (_abilityType)
            {
                case AbilityType.DoubleJump:
                    _spriteRenderer.color = new Color(1f, 0.8f, 0.2f); // 금색
                    break;
                case AbilityType.Dash:
                    _spriteRenderer.color = new Color(0.2f, 0.8f, 1f); // 청색
                    break;
                default:
                    _spriteRenderer.color = Color.white;
                    break;
            }
        }
        
        /// <summary>
        /// 획득 이펙트 재생
        /// </summary>
        private void PlayCollectEffect()
        {
            if (_collectEffect != null)
            {
                Instantiate(_collectEffect, transform.position, Quaternion.identity);
            }
        }
        
        /// <summary>
        /// 사운드 재생
        /// </summary>
        private void PlaySound()
        {
            if (_collectSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_collectSound);
            }
        }
        
        /// <summary>
        /// 능력 해금 이벤트
        /// </summary>
        public delegate void AbilityUnlockedEvent(AbilityType abilityType);
        public static event AbilityUnlockedEvent OnAbilityUnlocked;
        
        private void OnDrawGizmos()
        {
            Gizmos.color = _abilityType == AbilityType.DoubleJump ? Color.yellow : Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            
            // 능력 아이콘 표시
            Gizmos.color = Color.white;
            string label = _abilityType.ToString();
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.8f, label);
            #endif
        }
    }
}
