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
            _saveManager ??= FindObjectOfType<SaveManager>();
            _deathManager ??= FindObjectOfType<DeathManager>();
            _mapManager ??= FindObjectOfType<MapManager>();
        }

        private void ResetState()
        {
            _isPlayerInRange = false;
            _isWarping = false;
        }

        private IEnumerator WarpSequence()
        {
            if (_isWarping) yield break;
            _isWarping = true;

            try
            {
                // 출발지 저장 제거 - 도착지에서만 저장

                OnWarpStarted?.Invoke(_warpPointId, transform.position);

                if (!string.IsNullOrEmpty(_targetMapId))
                    yield return StartCoroutine(PerformWarp());

                OnWarpCompleted?.Invoke(_targetWarpPointId, GetTargetPosition());
                
                // 도착지에서 저장 (맵 전환 완료 후)
                // 맵 전환과 위치 확정이 완료된 후 저장
                yield return new WaitForSeconds(0.1f); // 맵 전환 완료 대기
                if (_saveManager != null)
                {
                    _saveManager.Save();
                }
                
                // 체크포인트도 도착지로 설정
                if (_deathManager != null)
                {
                    GameObject player = GameObject.FindGameObjectWithTag(PLAYER_TAG);
                    if (player != null)
                    {
                        _deathManager.SetCheckpoint(player.transform.position);
                    }
                }
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
            Vector3 targetPosition = GetTargetPosition();

            GameObject player = GameObject.FindGameObjectWithTag(PLAYER_TAG);
            if (player == null) yield break;

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            player.transform.position = targetPosition;
            yield return new WaitForFixedUpdate();

            if (_mapManager != null && !string.IsNullOrEmpty(_targetMapId))
            {
                _mapManager.SwitchToMap(_targetMapId);
                yield return null;
                player.transform.position = targetPosition;
            }
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
            var warpPoints = FindObjectsOfType<WarpPoint>();
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
            PlayerPrefs.SetFloat($"Checkpoint_{_warpPointId}_X", transform.position.x);
            PlayerPrefs.SetFloat($"Checkpoint_{_warpPointId}_Y", transform.position.y);
            PlayerPrefs.SetFloat($"Checkpoint_{_warpPointId}_Z", transform.position.z);
            PlayerPrefs.SetString("LastCheckpointId", _warpPointId);
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