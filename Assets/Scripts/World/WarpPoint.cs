using System.Collections;
using Pathfinder.Core.DI;
using Pathfinder.Player;
using UnityEngine;

namespace Pathfinder.World
{
    /// <summary>
    /// 워프 포인트 - 체크포인트 저장 + 맵 순간이동 기능 통합
    /// 플레이어가 E키로 활성화하면 체크포인트 저장 후 목표 맵으로 이동
    /// </summary>
    public class WarpPoint : MonoBehaviour
    {
        [Header("Warp Point Settings")]
        [Tooltip("이 워프 포인트의 고유 ID")]
        [SerializeField] private string _warpPointId;
        
        [Tooltip("목표 맵 ID (비어있으면 현재 맵의 다른 위치로)")]
        [SerializeField] private string _targetMapId;
        
        [Tooltip("목표 맵 내 도착할 워프 포인트 ID (비어있으면 스폰 포인트)")]
        [SerializeField] private string _targetWarpPointId;
        
        [Tooltip("초기 활성화 상태 (시작 지점용)")]
        [SerializeField] private bool _startActivated = false;
        
        [Header("Visual")]
        [Tooltip("활성화 시 스프라이트")]
        [SerializeField] private Sprite _activatedSprite;
        
        [Tooltip("비활성화 시 스프라이트")]
        [SerializeField] private Sprite _deactivatedSprite;
        
        [Tooltip("활성화 시 이펙트 프리팹")]
        [SerializeField] private GameObject _activationEffect;
        
        [Header("Interaction")]
        [Tooltip("상호작용 키 (기본: E)")]
        [SerializeField] private KeyCode _interactionKey = KeyCode.E;
        
        [Tooltip("상호작용 가능 거리")]
        [SerializeField] private float _interactionRadius = 2f;
        
        // DI 주입
        [Inject] private IDeathManager _deathManager;
        [Inject] private IMapManager _mapManager;
        
        // 상태
        private bool _isActivated = false;
        private bool _isPlayerInRange = false;
        private bool _isWarping = false;
        
        // 컴포넌트
        private SpriteRenderer _spriteRenderer;
        
        // 상수
        private const string PLAYER_TAG = "Player";
        
        // 이벤트
        public delegate void WarpPointEvent(string warpPointId, Vector3 position);
        public event WarpPointEvent OnActivated;
        public event WarpPointEvent OnWarpStarted;
        public event WarpPointEvent OnWarpCompleted;
        
        private void Awake()
        {
            InitializeComponents();
            SetupCollider();
            GenerateIdIfEmpty();
        }
        
        private void Start()
        {
            InitializeState();
        }
        
        private void Update()
        {
            HandleInput();
        }
        
        /// <summary>
        /// 컴포넌트 초기화
        /// </summary>
        private void InitializeComponents()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
            
            UpdateVisual();
        }
        
        /// <summary>
        /// 트리거 콜라이더 설정
        /// </summary>
        private void SetupCollider()
        {
            var collider = GetComponent<Collider2D>();
            if (collider == null)
            {
                var circleCollider = gameObject.AddComponent<CircleCollider2D>();
                circleCollider.isTrigger = true;
                circleCollider.radius = _interactionRadius;
            }
            else if (!collider.isTrigger)
            {
                collider.isTrigger = true;
            }
        }
        
        /// <summary>
        /// ID가 비어있으면 자동 생성
        /// </summary>
        private void GenerateIdIfEmpty()
        {
            if (string.IsNullOrEmpty(_warpPointId))
            {
                _warpPointId = $"{gameObject.name}_{GetInstanceID()}";
            }
        }
        
        /// <summary>
        /// 초기 상태 설정
        /// </summary>
        private void InitializeState()
        {
            if (_startActivated)
            {
                Activate();
            }
        }
        
        /// <summary>
        /// 입력 처리
        /// </summary>
        private void HandleInput()
        {
            if (_isPlayerInRange && !_isWarping && Input.GetKeyDown(_interactionKey))
            {
                StartCoroutine(WarpSequence());
            }
        }
        
        /// <summary>
        /// 트리거 진입
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(PLAYER_TAG))
            {
                _isPlayerInRange = true;
                ShowInteractionPrompt();
            }
        }
        
        /// <summary>
        /// 트리거 이탈
        /// </summary>
        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag(PLAYER_TAG))
            {
                _isPlayerInRange = false;
                HideInteractionPrompt();
            }
        }
        
        /// <summary>
        /// 상호작용 프롬프트 표시
        /// </summary>
        private void ShowInteractionPrompt()
        {
            // TODO: UI 매니저와 연동하여 E키 표시
            Debug.Log($"[WarpPoint] Press '{_interactionKey}' to activate {_warpPointId}");
        }
        
        /// <summary>
        /// 상호작용 프롬프트 숨김
        /// </summary>
        private void HideInteractionPrompt()
        {
            // TODO: UI 매니저와 연동하여 프롬프트 숨김
        }
        
        /// <summary>
        /// 워프 시퀀스 (체크포인트 저장 + 맵 이동)
        /// </summary>
        private IEnumerator WarpSequence()
        {
            if (_isWarping) yield break;
            _isWarping = true;
            
            // 1. 체크포인트 활성화
            Activate();
            
            // 2. 워프 시작 이벤트
            OnWarpStarted?.Invoke(_warpPointId, transform.position);
            
            // 3. 목표 맵으로 이동
            if (!string.IsNullOrEmpty(_targetMapId))
            {
                yield return StartCoroutine(PerformWarp());
            }
            
            // 4. 워프 완료 이벤트
            OnWarpCompleted?.Invoke(_targetWarpPointId, GetTargetPosition());
            
            _isWarping = false;
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
                SaveCheckpointToPlayerPrefs();
            }
            
            // 시각적 업데이트
            UpdateVisual();
            
            // 이펙트 재생
            PlayActivationEffect();
            
            // 이벤트 발행
            OnActivated?.Invoke(_warpPointId, transform.position);
            
            Debug.Log($"[WarpPoint] Activated: {_warpPointId}");
        }
        
        /// <summary>
        /// 실제 워프 수행
        /// </summary>
        private IEnumerator PerformWarp()
        {
            Debug.Log($"[WarpPoint] Warping to {_targetMapId}");
            
            // TODO: ScreenFade와 연동
            // yield return StartCoroutine(ScreenFade.FadeOut());
            
            // 맵 전환
            if (_mapManager != null)
            {
                _mapManager.SwitchToMap(_targetMapId);
            }
            
            // 플레이어 위치 이동
            yield return new WaitForSeconds(0.1f); // 맵 전환 대기
            MovePlayerToTarget();
            
            // TODO: ScreenFade와 연동
            // yield return StartCoroutine(ScreenFade.FadeIn());
            
            Debug.Log($"[WarpPoint] Warp completed to {_targetMapId}");
        }
        
        /// <summary>
        /// 플레이어를 목표 위치로 이동
        /// </summary>
        private void MovePlayerToTarget()
        {
            Vector3 targetPosition = GetTargetPosition();
            
            GameObject player = GameObject.FindGameObjectWithTag(PLAYER_TAG);
            if (player != null)
            {
                player.transform.position = targetPosition;
                Debug.Log($"[WarpPoint] Player moved to {targetPosition}");
            }
        }
        
        /// <summary>
        /// 목표 위치 반환
        /// </summary>
        private Vector3 GetTargetPosition()
        {
            // 목표 워프 포인트가 지정된 경우
            if (!string.IsNullOrEmpty(_targetWarpPointId))
            {
                WarpPoint targetWarp = FindTargetWarpPoint(_targetWarpPointId);
                if (targetWarp != null)
                {
                    return targetWarp.transform.position;
                }
            }
            
            // 목표 워프 포인트를 찾지 못하면 맵의 스폰 포인트 사용
            if (_mapManager is MapManager mapMgr)
            {
                return mapMgr.GetSpawnPosition(_targetMapId);
            }
            
            return Vector3.zero;
        }
        
        /// <summary>
        /// 목표 워프 포인트 찾기
        /// </summary>
        private WarpPoint FindTargetWarpPoint(string warpPointId)
        {
            var warpPoints = FindObjectsOfType<WarpPoint>();
            foreach (var warpPoint in warpPoints)
            {
                if (warpPoint._warpPointId == warpPointId && warpPoint != this)
                {
                    return warpPoint;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 시각적 업데이트
        /// </summary>
        private void UpdateVisual()
        {
            if (_spriteRenderer == null) return;
            
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
                _spriteRenderer.color = _isActivated ? Color.green : Color.gray;
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
        /// 체크포인트 위치를 PlayerPrefs에 저장
        /// </summary>
        private void SaveCheckpointToPlayerPrefs()
        {
            PlayerPrefs.SetFloat($"Checkpoint_{_warpPointId}_X", transform.position.x);
            PlayerPrefs.SetFloat($"Checkpoint_{_warpPointId}_Y", transform.position.y);
            PlayerPrefs.SetFloat($"Checkpoint_{_warpPointId}_Z", transform.position.z);
            PlayerPrefs.SetString("LastCheckpointId", _warpPointId);
            PlayerPrefs.Save();
        }
        
        #region Public API
        
        /// <summary>
        /// 워프 포인트 ID 반환
        /// </summary>
        public string GetWarpPointId() => _warpPointId;
        
        /// <summary>
        /// 활성화 상태 반환
        /// </summary>
        public bool IsActivated() => _isActivated;
        
        /// <summary>
        /// 목표 맵 ID 반환
        /// </summary>
        public string GetTargetMapId() => _targetMapId;
        
        /// <summary>
        /// 목표 워프 포인트 ID 반환
        /// </summary>
        public string GetTargetWarpPointId() => _targetWarpPointId;
        
        /// <summary>
        /// 워프 포인트 위치 반환
        /// </summary>
        public Vector3 GetPosition() => transform.position;
        
        /// <summary>
        /// 목표 설정 (런타임용)
        /// </summary>
        public void SetTarget(string mapId, string warpPointId = null)
        {
            _targetMapId = mapId;
            _targetWarpPointId = warpPointId;
        }
        
        /// <summary>
        /// ID 설정 (런타임용)
        /// </summary>
        public void SetWarpPointId(string id)
        {
            _warpPointId = id;
        }
        
        #endregion
        
        #region Gizmos
        
        private void OnDrawGizmos()
        {
            // 상호작용 범위
            Gizmos.color = _isActivated ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _interactionRadius);
            
            // 워프 포인트 중심
            Gizmos.color = _isActivated ? Color.green : Color.gray;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.5f, 1f, 0.1f));
            
            // 활성화된 경우 체크 표시
            if (_isActivated)
            {
                Gizmos.color = Color.green;
                Vector3 center = transform.position;
                Gizmos.DrawLine(center + Vector3.left * 0.2f, center + Vector3.up * 0.3f);
                Gizmos.DrawLine(center + Vector3.up * 0.3f, center + Vector3.right * 0.4f);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // 목표 연결선
            if (!string.IsNullOrEmpty(_targetMapId))
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2.2f, $"To: {_targetMapId}");
                #endif
            }
        }
        
        #endregion
    }
}
