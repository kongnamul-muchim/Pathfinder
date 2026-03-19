using System.Collections;
using Pathfinder.Player;
using Pathfinder.Core.DI;
using UnityEngine;

namespace Pathfinder.World
{
    /// <summary>
    /// 맵 간 이동 포탈 - 검은 화면 전환
    /// </summary>
    public class Portal : MonoBehaviour, IPortal
    {
        private const string PLAYER_TAG = "Player";
        private static readonly Vector2 DefaultColliderSize = new Vector2(1f, 2f);
        
        [Header("Portal Settings")]
        [Tooltip("이 포탈의 고유 ID")]
        [SerializeField] private string _portalId;
        
        [Tooltip("목표 맵 ID")]
        [SerializeField] private string _targetMapId;
        
        [Tooltip("목표 맵 내 도착 포탈 ID (비어있으면 맵의 SpawnPoint로)")]
        [SerializeField] private string _targetPortalId;
        
        [Header("Transition")]
        [Tooltip("화면 전환 시간 (초)")]
        [SerializeField] private float _fadeDuration = 0.5f;
        
        [Tooltip("전환 대기 시간 (초)")]
        [SerializeField] private float _transitionDelay = 0.2f;
        
        // DI 주입
        [Inject] private IMapManager _mapManager;
        
        // 이동 중인지 여부
        private bool _isTeleporting = false;
        
        private void Awake()
        {
            // 트리거 콜라이더 확인
            var collider = GetComponent<Collider2D>();
            if (collider == null)
            {
                // 자동으로 트리거 콜라이더 추가
                var boxCollider = gameObject.AddComponent<BoxCollider2D>();
                boxCollider.isTrigger = true;
                boxCollider.size = DefaultColliderSize;
            }
            else if (!collider.isTrigger)
            {
                collider.isTrigger = true;
            }
            
            // Portal ID가 없으면 자동 생성
            if (string.IsNullOrEmpty(_portalId))
            {
                _portalId = gameObject.name + "_" + GetInstanceID();
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isTeleporting) return;
            
            if (other.CompareTag(PLAYER_TAG))
            {
                Teleport(other.gameObject);
            }
        }
        
        /// <summary>
        /// 포탈로 이동
        /// </summary>
        public void Teleport(GameObject player)
        {
            if (_isTeleporting) return;
            if (player == null) return;
            if (string.IsNullOrEmpty(_targetMapId)) return;
            
            _isTeleporting = true;
            StartCoroutine(TeleportCoroutine(player));
        }
        
        /// <summary>
        /// 이동 코루틴 (화면 페이드)
        /// </summary>
        private IEnumerator TeleportCoroutine(GameObject player)
        {
            // 1. 화면 검게 페이드 (ScreenFade 필요)
            yield return StartCoroutine(FadeScreen(true));
            
            // 2. 잠시 대기
            yield return new WaitForSeconds(_transitionDelay);
            
            // 3. 맵 전환
            if (_mapManager != null)
            {
                _mapManager.SwitchToMap(_targetMapId);
            }
            
            // 4. 플레이어 위치 이동
            MovePlayerToTarget(player);
            
            // 5. 잠시 대기
            yield return new WaitForSeconds(_transitionDelay);
            
            // 6. 화면 밝게 페이드
            yield return StartCoroutine(FadeScreen(false));
            
            _isTeleporting = false;
        }
        
        /// <summary>
        /// 플레이어를 목표 위치로 이동
        /// </summary>
        private void MovePlayerToTarget(GameObject player)
        {
            Vector3 targetPosition = Vector3.zero;
            
            // 목표 포탈이 지정된 경우
            if (!string.IsNullOrEmpty(_targetPortalId))
            {
                var targetPortal = FindTargetPortal(_targetPortalId);
                if (targetPortal != null)
                {
                    targetPosition = targetPortal.transform.position;
                }
            }
            
            // 목표 포탈을 찾지 못하면 맵의 SpawnPoint 사용
            if (targetPosition == Vector3.zero && _mapManager is MapManager mapMgr)
            {
                targetPosition = mapMgr.GetSpawnPosition(_targetMapId);
            }
            
            // 플레이어 이동
            player.transform.position = targetPosition;
        }
        
        /// <summary>
        /// 목표 포탈 찾기
        /// </summary>
        private Portal FindTargetPortal(string portalId)
        {
            var portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
            foreach (var portal in portals)
            {
                if (portal._portalId == portalId && portal != this)
                {
                    return portal;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 화면 페이드 (ScreenFade 필요)
        /// </summary>
        private IEnumerator FadeScreen(bool fadeIn)
        {
            // TODO: ScreenFade 시스템 구현 후 연결
            // 현재는 간단한 대기로 처리
            yield return new WaitForSeconds(_fadeDuration);
        }
        
        /// <summary>
        /// 목표 맵 ID 반환
        /// </summary>
        public string GetTargetMapId()
        {
            return _targetMapId;
        }
        
        /// <summary>
        /// 목표 포탈 ID 반환
        /// </summary>
        public string GetTargetPortalId()
        {
            return _targetPortalId;
        }
        
        /// <summary>
        /// 포탈 ID 반환
        /// </summary>
        public string GetPortalId()
        {
            return _portalId;
        }
        
        /// <summary>
        /// 포탈 ID 설정 (런타임용)
        /// </summary>
        public void SetPortalId(string portalId)
        {
            _portalId = portalId;
        }
        
        /// <summary>
        /// 목표 설정 (런타임용)
        /// </summary>
        public void SetTarget(string mapId, string portalId = null)
        {
            _targetMapId = mapId;
            _targetPortalId = portalId;
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(DefaultColliderSize.x, DefaultColliderSize.y, 0.1f));
            
            // 목표 포탈 표시
            if (!string.IsNullOrEmpty(_targetMapId))
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.5f);
            }
        }
    }
}
