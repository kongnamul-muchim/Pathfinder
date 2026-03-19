using Pathfinder.Core.DI;
using Pathfinder.Player;
using UnityEngine;

namespace Pathfinder.World
{
    /// <summary>
    /// 세이브포인트 - 플레이어 리스폰 위치 저장
    /// </summary>
    public class Checkpoint : MonoBehaviour, ICheckpoint
    {
        [Header("Checkpoint Settings")]
        [Tooltip("체크포인트 고유 ID (비어있으면 자동 생성)")]
        [SerializeField] private string _checkpointId;
        
        [Tooltip("초기 활성화 상태")]
        [SerializeField] private bool _startActivated = false;
        
        [Tooltip("활성화 시 변경할 스프라이트 (선택적)")]
        [SerializeField] private Sprite _activatedSprite;
        
        [Tooltip("비활성화 시 스프라이트")]
        [SerializeField] private Sprite _deactivatedSprite;
        
        [Header("Visual")]
        [Tooltip("활성화 시 이펙트")]
        [SerializeField] private GameObject _activationEffect;
        
        // DI 주입
        [Inject] private IDeathManager _deathManager;
        
        // 활성화 상태
        private bool _isActivated = false;
        
        // 스프라이트 렌더러
        private SpriteRenderer _spriteRenderer;
        
        // 플레이어 태그
        private const string PLAYER_TAG = "Player";
        
        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                _spriteRenderer.sprite = _deactivatedSprite;
            }
            
            // 트리거 콜라이더 확인
            var collider = GetComponent<Collider2D>();
            if (collider == null)
            {
                var boxCollider = gameObject.AddComponent<BoxCollider2D>();
                boxCollider.isTrigger = true;
                boxCollider.size = new Vector2(1f, 1.5f);
            }
            else if (!collider.isTrigger)
            {
                collider.isTrigger = true;
            }
            
            // ID 자동 생성
            if (string.IsNullOrEmpty(_checkpointId))
            {
                _checkpointId = gameObject.name + "_" + GetInstanceID();
            }
        }
        
        private void Start()
        {
            // 초기 상태 설정
            if (_startActivated)
            {
                Activate();
            }
            else
            {
                UpdateVisual();
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(PLAYER_TAG))
            {
                if (!_isActivated)
                {
                    Activate();
                }
            }
        }
        
        /// <summary>
        /// 체크포인트 활성화
        /// </summary>
        public void Activate()
        {
            if (_isActivated) return;
            
            _isActivated = true;
            
            // DeathManager에 체크포인트 위치 설정
            if (_deathManager != null)
            {
                _deathManager.SetCheckpoint(transform.position);
            }
            else
            {
                // DeathManager가 없으면 직접 PlayerPrefs에 저장
                SaveCheckpointPosition(transform.position);
            }
            
            // 시각적 업데이트
            UpdateVisual();
            
            // 이펙트 재생
            PlayActivationEffect();
            
            // 이벤트 발행
            OnCheckpointActivated?.Invoke(_checkpointId, transform.position);
        }
        
        /// <summary>
        /// 시각적 업데이트
        /// </summary>
        private void UpdateVisual()
        {
            if (_spriteRenderer != null)
            {
                if (_isActivated && _activatedSprite != null)
                {
                    _spriteRenderer.sprite = _activatedSprite;
                    _spriteRenderer.color = Color.green;
                }
                else if (_deactivatedSprite != null)
                {
                    _spriteRenderer.sprite = _deactivatedSprite;
                    _spriteRenderer.color = Color.gray;
                }
                else
                {
                    // 기본 색상만 변경
                    _spriteRenderer.color = _isActivated ? Color.green : Color.gray;
                }
            }
        }
        
        /// <summary>
        /// 활성화 이펙트 재생
        /// </summary>
        private void PlayActivationEffect()
        {
            if (_activationEffect != null)
            {
                Instantiate(_activationEffect, transform.position, Quaternion.identity);
            }
        }
        
        /// <summary>
        /// 체크포인트 위치 저장 (PlayerPrefs)
        /// </summary>
        private void SaveCheckpointPosition(Vector3 position)
        {
            PlayerPrefs.SetFloat("Checkpoint_X", position.x);
            PlayerPrefs.SetFloat("Checkpoint_Y", position.y);
            PlayerPrefs.SetFloat("Checkpoint_Z", position.z);
            PlayerPrefs.SetString("Checkpoint_ID", _checkpointId);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// 활성화 상태 반환
        /// </summary>
        public bool IsActivated()
        {
            return _isActivated;
        }
        
        /// <summary>
        /// 위치 반환
        /// </summary>
        public Vector3 GetPosition()
        {
            return transform.position;
        }
        
        /// <summary>
        /// 체크포인트 ID 반환
        /// </summary>
        public string GetCheckpointId()
        {
            return _checkpointId;
        }
        
        /// <summary>
        /// 체크포인트 ID 설정
        /// </summary>
        public void SetCheckpointId(string checkpointId)
        {
            _checkpointId = checkpointId;
        }
        
        /// <summary>
        /// 활성화 이벤트
        /// </summary>
        public delegate void CheckpointActivatedEvent(string checkpointId, Vector3 position);
        public event CheckpointActivatedEvent OnCheckpointActivated;
        
        private void OnDrawGizmos()
        {
            Gizmos.color = _isActivated ? Color.green : Color.gray;
            Gizmos.DrawWireCube(transform.position, new Vector3(1f, 1.5f, 0.1f));
            
            // 활성화된 경우 체크 표시
            if (_isActivated)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position + Vector3.left * 0.2f, transform.position + Vector3.up * 0.3f);
                Gizmos.DrawLine(transform.position + Vector3.up * 0.3f, transform.position + Vector3.right * 0.4f);
            }
        }
    }
}
