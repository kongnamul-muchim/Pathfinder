using System.Collections;
using Pathfinder.Common;
using Pathfinder.Core;
using Pathfinder.Player;
using UnityEngine;

namespace Pathfinder.World
{
    public class WarpPoint : MonoBehaviour, ICheckpoint, IInteractable
    {
        private const string PLAYER_TAG = "Player";
        private const string CHECKPOINT_KEY_PREFIX = "Checkpoint_";
        private const string LAST_CHECKPOINT_KEY = "LastCheckpointId";

        [Header("Warp Point Settings")]
        [SerializeField] private string _warpPointId;
        [SerializeField] private string _targetMapId;
        [SerializeField] private string _targetWarpPointId;
        [SerializeField] private Transform _warpTarget;
        [SerializeField] private bool _startActivated;

        [Header("Visual")]
        [SerializeField] private GameObject _activationEffect;

        [Header("Interaction")]
        [SerializeField] private string _interactionText = "Press E";
        [SerializeField] private float _interactionRadius = 2f;

        [Header("Services")]
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private DeathManager _deathManager;
        [SerializeField] private MapManager _mapManager;

        private bool _isActivated;
        private bool _isPlayerInRange;
        private bool _isWarping;

        public delegate void WarpPointEvent(string warpPointId, Vector3 position);
        public event WarpPointEvent OnActivated;
        public event WarpPointEvent OnWarpStarted;
        public event WarpPointEvent OnWarpCompleted;

        private void Awake()
        {
            SetupCollider();
            GenerateIdIfEmpty();
            ResolveServices();
        }

        private void Start()
        {
            if (_startActivated)
                Activate();
        }

        private void OnEnable()
        {
            ResetState();
        }

        private void OnDisable()
        {
            ResetState();
            StopAllCoroutines();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(PLAYER_TAG))
                _isPlayerInRange = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag(PLAYER_TAG))
                _isPlayerInRange = false;
        }

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

        private void GenerateIdIfEmpty()
        {
            if (string.IsNullOrEmpty(_warpPointId))
                _warpPointId = $"{gameObject.name}_{GetInstanceID()}";
        }

        private void ResolveServices()
        {
            _saveManager ??= FindFirstObjectByType<SaveManager>();
            _deathManager ??= FindFirstObjectByType<DeathManager>();
            _mapManager ??= FindFirstObjectByType<MapManager>();
            
            if (_saveManager == null)
                Debug.LogError($"[WARP] {gameObject.name}: SaveManager not found!");
            if (_deathManager == null)
                Debug.LogError($"[WARP] {gameObject.name}: DeathManager not found!");
            if (_mapManager == null)
                Debug.LogError($"[WARP] {gameObject.name}: MapManager not found!");
        }

        private void ResetState()
        {
            _isPlayerInRange = false;
            _isWarping = false;
        }

        private IEnumerator WarpSequence()
        {
            Debug.Log($"[WARP] WarpSequence START - _isWarping: {_isWarping}");
            if (_isWarping) yield break;
            _isWarping = true;

            try
            {
                Debug.Log($"[WARP] Target: {_targetMapId}, SaveManager: {_saveManager != null}");

                OnWarpStarted?.Invoke(_warpPointId, transform.position);

                // 워프 후 저장 예약 (맵 전환 완료 후 SaveManager.OnMapChanged에서 실행)
                Vector3 targetPosition = GetTargetPosition();
                if (_saveManager != null && !string.IsNullOrEmpty(_targetMapId))
                {
                    Debug.Log($"[WARP] Reserving warp save - Map: {_targetMapId}, Position: {targetPosition}");
                    _saveManager.ReserveWarpSave(_targetMapId, targetPosition);
                }
                
                // 체크포인트도 목적지로 설정
                if (_deathManager != null)
                {
                    _deathManager.SetCheckpoint(targetPosition);
                    Debug.Log($"[WARP] Checkpoint set to target: {targetPosition}");
                }

                if (!string.IsNullOrEmpty(_targetMapId))
                {
                    Debug.Log("[WARP] Starting PerformWarp...");
                    yield return StartCoroutine(PerformWarp());
                    Debug.Log("[WARP] PerformWarp completed (or interrupted by map change)");
                }

                OnWarpCompleted?.Invoke(_targetWarpPointId, GetTargetPosition());
                
                Debug.Log("[WARP] WarpSequence END");
            }
            finally
            {
                _isWarping = false;
            }
        }

        public void Activate()
        {
            if (_isActivated) return;
            _isActivated = true;

            if (_deathManager != null)
                _deathManager.SetCheckpoint(transform.position);
            else
                SaveCheckpointToPlayerPrefs();

            PlayActivationEffect();
            OnActivated?.Invoke(_warpPointId, transform.position);
        }

        private IEnumerator PerformWarp()
        {
            Debug.Log($"[WARP] PerformWarp - Getting target position...");
            Vector3 targetPosition = GetTargetPosition();
            Debug.Log($"[WARP] PerformWarp - Target position: {targetPosition}");

            GameObject player = GameObject.FindGameObjectWithTag(PLAYER_TAG);
            Debug.Log($"[WARP] PerformWarp - Player found: {player != null}");
            if (player == null)
            {
                Debug.LogError("[WARP] PerformWarp - Player is NULL! Yield break.");
                yield break;
            }

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            Debug.Log($"[WARP] PerformWarp - Moving player to {targetPosition}");
            player.transform.position = targetPosition;
            
            Debug.Log("[WARP] PerformWarp - Waiting for FixedUpdate...");
            yield return new WaitForFixedUpdate();
            Debug.Log("[WARP] PerformWarp - After WaitForFixedUpdate");

            if (_mapManager != null && !string.IsNullOrEmpty(_targetMapId))
            {
                Debug.Log($"[WARP] PerformWarp - Switching to map: {_targetMapId}");
                _mapManager.SwitchToMap(_targetMapId);
                yield return null;
                player.transform.position = targetPosition;
                Debug.Log("[WARP] PerformWarp - Map switched, position set");
            }
            
            Debug.Log("[WARP] PerformWarp - END");
        }

        private Vector3 GetTargetPosition()
        {
            if (_warpTarget != null)
                return _warpTarget.position;

            if (!string.IsNullOrEmpty(_targetWarpPointId))
            {
                WarpPoint targetWarp = FindTargetWarpPoint(_targetWarpPointId);
                if (targetWarp != null)
                {
                    if (targetWarp._warpTarget != null)
                        return targetWarp._warpTarget.position;
                    return targetWarp.transform.position;
                }
            }

            if (_mapManager != null)
                return _mapManager.GetSpawnPosition(_targetMapId);

            return Vector3.zero;
        }

        private WarpPoint FindTargetWarpPoint(string warpPointId)
        {
            var warpPoints = FindObjectsByType<WarpPoint>(FindObjectsSortMode.None);
            foreach (var warpPoint in warpPoints)
            {
                if (warpPoint._warpPointId == warpPointId && warpPoint != this)
                    return warpPoint;
            }
            return null;
        }

        private void PlayActivationEffect()
        {
            if (_activationEffect != null)
                Instantiate(_activationEffect, transform.position, Quaternion.identity);
        }

        private void SaveCheckpointToPlayerPrefs()
        {
            string prefix = $"{CHECKPOINT_KEY_PREFIX}{_warpPointId}_";
            PlayerPrefs.SetFloat($"{prefix}X", transform.position.x);
            PlayerPrefs.SetFloat($"{prefix}Y", transform.position.y);
            PlayerPrefs.SetFloat($"{prefix}Z", transform.position.z);
            PlayerPrefs.SetString(LAST_CHECKPOINT_KEY, _warpPointId);
            PlayerPrefs.Save();
        }

        #region Public API

        public string GetWarpPointId() => _warpPointId;
        public bool IsActivated() => _isActivated;
        public string GetTargetMapId() => _targetMapId;
        public string GetTargetWarpPointId() => _targetWarpPointId;
        public Vector3 GetPosition() => transform.position;

        public void SetTarget(string mapId, string warpPointId = null)
        {
            _targetMapId = mapId;
            _targetWarpPointId = warpPointId;
        }

        public void SetWarpPointId(string id) => _warpPointId = id;

        #endregion

        #region IInteractable Implementation

        public string GetInteractionText() => _interactionText;
        public bool CanInteract() => _isPlayerInRange && !_isWarping;
        public Transform GetPromptTransform() => transform;

        public void OnInteract()
        {
            Debug.Log($"[WARP] OnInteract called - CanInteract: {CanInteract()}, _isPlayerInRange: {_isPlayerInRange}, _isWarping: {_isWarping}");
            if (CanInteract())
                StartCoroutine(WarpSequence());
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            Gizmos.color = _isActivated ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _interactionRadius);

            Gizmos.color = _isActivated ? Color.green : Color.gray;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.5f, 1f, 0.1f));

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