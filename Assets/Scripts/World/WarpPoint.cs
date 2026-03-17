using System.Collections;
using Pathfinder.Core.DI;
using Pathfinder.Common;
using Pathfinder.Core;
using Pathfinder.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pathfinder.World
{
    /// <summary>
    /// 워프 포인트 - 체크포인트 저장 + 맵 순간이동 기능 통합
    /// 플레이어가 E키로 활성화하면 체크포인트 저장 후 목표 맵으로 이동
    /// </summary>
    public class WarpPoint : MonoBehaviour, ICheckpoint, IInteractable
    {
        [Header("Warp Point Settings")]
        [Tooltip("이 워프 포인트의 고유 ID")]
        [SerializeField] private string _warpPointId;
        
        [Tooltip("목표 맵 ID (비어있으면 현재 맵의 다른 위치로)")]
        [SerializeField] private string _targetMapId;
        
        [Tooltip("목표 맵 내 도착할 워프 포인트 ID (비어있으면 스폰 포인트)")]
        [SerializeField] private string _targetWarpPointId;
        
        [Tooltip("도착 위치 (비어있으면 _targetWarpPointId의 위치나 스폰 포인트 사용)")]
        [SerializeField] private Transform _warpTarget;
        
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
        [Tooltip("상호작용 프롬프트 텍스트 (비어있으면 'Press E' 사용)")]
        [SerializeField] private string _interactionText = "Press E";
        
        [Tooltip("상호작용 가능 거리")]
        [SerializeField] private float _interactionRadius = 2f;
        
        [Header("Services")]
        [Tooltip("저장 관리자 (비어있으면 씬에서 자동 찾기)")]
        [SerializeField] private SaveManager _saveManager;
        
        [Tooltip("사망 관리자 (비어있으면 씬에서 자동 찾기)")]
        [SerializeField] private DeathManager _deathManager;
        
        [Tooltip("맵 관리자 (비어있으면 씬에서 자동 찾기)")]
        [SerializeField] private MapManager _mapManager;
        
        // 상태
        private bool _isActivated = false;
        private bool _isPlayerInRange = false;
        private bool _isWarping = false;
        
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
            
            // DI 실패 시 fallback
            if (_saveManager == null)
            {
                _saveManager = FindObjectOfType<SaveManager>();
            }
            if (_deathManager == null)
            {
                _deathManager = FindObjectOfType<DeathManager>();
            }
            if (_mapManager == null)
            {
                _mapManager = FindObjectOfType<MapManager>();
            }
        }
        
        private void Start()
        {
            InitializeState();
        }
        
        /// <summary>
        /// 컴포넌트 초기화
        /// </summary>
        private void InitializeComponents()
        {
            // SpriteRenderer 사용 안함 - 시각적 표시는 Tilemap에서 처리
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
        /// 트리거 진입
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(PLAYER_TAG))
            {
                _isPlayerInRange = true;
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
            }
        }
        
        /// <summary>
        /// 워프 시퀀스 (체크포인트 저장 + 맵 이동)
        /// </summary>
        private IEnumerator WarpSequence()
        {
            if (_isWarping) yield break;
            _isWarping = true;
            
            try
            {
                // 1. 체크포인트 활성화
                Activate();
                
                // 2. 게임 저장 (자동 저장)
                if (_saveManager != null)
                {
                    _saveManager.Save();
                }
                
                // 3. 워프 시작 이벤트
                OnWarpStarted?.Invoke(_warpPointId, transform.position);
                
                // 4. 목표 맵으로 이동
                if (!string.IsNullOrEmpty(_targetMapId))
                {
                    yield return StartCoroutine(PerformWarp());
                }
                
                // 5. 워프 완료 이벤트
                OnWarpCompleted?.Invoke(_targetWarpPointId, GetTargetPosition());
            }
            finally
            {
                // 항상 _isWarping을 false로 설정
                _isWarping = false;
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
                SaveCheckpointToPlayerPrefs();
            }
            
            // 시각적 업데이트 제거 - Tilemap에서 처리
            // UpdateVisual();
            
            // 이펙트 재생
            PlayActivationEffect();
            
            // 이벤트 발행
            OnActivated?.Invoke(_warpPointId, transform.position);
        }
        
        /// <summary>
        /// 실제 워프 수행
        /// </summary>
        private IEnumerator PerformWarp()
        {
            // 목표 위치 계산
            Vector3 targetPosition = GetTargetPosition();
            
            GameObject player = GameObject.FindGameObjectWithTag(PLAYER_TAG);
            if (player == null)
            {
                yield break;
            }
            
            // 플레이어 물리 속도 리셋 (벽에 막히는 문제 방지)
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            
            // TODO: ScreenFade와 연동
            // yield return StartCoroutine(ScreenFade.FadeOut());
            
            // 1. 플레이어를 목표 위치로 먼저 이동
            player.transform.position = targetPosition;
            
            // 2. 한 프레임 대기 (물리 엔진 업데이트)
            yield return new WaitForFixedUpdate();
            
            // 3. 맵 전환
            if (_mapManager != null && !string.IsNullOrEmpty(_targetMapId))
            {
                _mapManager.SwitchToMap(_targetMapId);
                
                // 맵 전환 후 다시 한번 위치 확인
                yield return null;
                player.transform.position = targetPosition;
            }
            
            // TODO: ScreenFade와 연동
            // yield return StartCoroutine(ScreenFade.FadeIn());
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
            }
        }
        
        /// <summary>
        /// 목표 위치 반환
        /// </summary>
        private Vector3 GetTargetPosition()
        {
            // 1. 직접 지정된 도착 위치가 있으면 우선 사용
            if (_warpTarget != null)
            {
                return _warpTarget.position;
            }
            
            // 2. 목표 워프 포인트가 지정된 경우
            if (!string.IsNullOrEmpty(_targetWarpPointId))
            {
                WarpPoint targetWarp = FindTargetWarpPoint(_targetWarpPointId);
                if (targetWarp != null)
                {
                    // 대상 워프 포인트의 _warpTarget이 있으면 그 위치 사용
                    if (targetWarp._warpTarget != null)
                    {
                        return targetWarp._warpTarget.position;
                    }
                    return targetWarp.transform.position;
                }
            }
            
            // 3. 목표 워프 포인트를 찾지 못하면 맵의 스폰 포인트 사용
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
        
        #region IInteractable Implementation
        
        /// <summary>
        /// 상호작용 프롬프트 텍스트 반환
        /// </summary>
        public string GetInteractionText() => _interactionText;
        
        /// <summary>
        /// 현재 상호작용 가능한지 여부
        /// </summary>
        public bool CanInteract() => _isPlayerInRange && !_isWarping;
        
        /// <summary>
        /// 상호작용 실행 (워프 시작)
        /// </summary>
        public void OnInteract()
        {
            if (CanInteract())
            {
                StartCoroutine(WarpSequence());
            }
        }
        
        /// <summary>
        /// 프롬프트 UI 위치 반환
        /// </summary>
        public Transform GetPromptTransform() => transform;
        
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
