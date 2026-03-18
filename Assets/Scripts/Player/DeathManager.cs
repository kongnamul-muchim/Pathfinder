using Pathfinder.Core.DI;
using Pathfinder.Core;
using Pathfinder.World;
using UnityEngine;

namespace Pathfinder.Player
{
    /// <summary>
    /// 사망 관리자 구현체
    /// DI Container에 의해 관리됨
    /// </summary>
    public class DeathManager : MonoBehaviour, IDeathManager
    {
        [Header("Death Settings")]
        [Tooltip("리스폰 시 무적 시간 (초)")]
        [SerializeField] private float _respawnInvincibilityTime = 1f;
        
        [Tooltip("최대 리스폰 시도 횟수")]
        [SerializeField] private int _maxRespawnAttempts = 3;
        
        // DI 주입
        [Inject] private IAbilityManager _abilityManager;
        [Inject] private ISaveManager _saveManager;
        
        private MapManager _mapManager;
        
        private void Awake()
        {
            // DI가 실패했을 경우 씬에서 직접 찾기
            if (_abilityManager == null)
            {
                _abilityManager = FindObjectOfType<AbilityManager>();
            }
            
            if (_saveManager == null)
            {
                _saveManager = FindObjectOfType<SaveManager>();
            }
            
            _mapManager = FindObjectOfType<MapManager>();
        }
        
        // 마지막 체크포인트 위치
        private Vector3 _lastCheckpoint;
        
        // 사망 횟수
        private int _deathCount = 0;
        
        // 리스폰 중인지 여부
        private bool _isRespawning = false;
        
        // 무적 상태
        private bool _isInvincible = false;
        
        /// <summary>
        /// 플레이어 사망 처리
        /// </summary>
        public void OnPlayerDeath()
        {
            if (_isInvincible) return;
            
            // 먼저 추가 목숨 체크
            if (_abilityManager != null && _abilityManager.GetExtraLives() > 0)
            {
                // 추가 목숨 소모
                if (_abilityManager.ConsumeExtraLife())
                {
                    Respawn();
                    return;
                }
            }
            
            // 추가 목숨이 없으면 저장 데이터 로드 시도
            _deathCount++;
            
            // 저장 데이터가 있으면 롤백
            if (_saveManager != null && _saveManager.HasSaveData())
            {
                // 저장된 맵 ID를 파일에서 직접 읽기 (LoadedMapId는 stale 할 수 있음)
                SaveManager saveManager = _saveManager as SaveManager;
                string savedMapId = saveManager?.GetSavedMapId();
                if (!string.IsNullOrEmpty(savedMapId) && _mapManager != null)
                {
                    _mapManager.SwitchToMap(savedMapId);
                }
                
                // 저장 데이터 로드 (위치 복원)
                _saveManager.Load();
                
                // Load 후에도 Respawn 호출하여 물리 리셋 및 무적 상태 시작
                RespawnFromSave();
            }
            else
            {
                // 저장 데이터가 없으면 마지막 체크포인트나 시작 위치로 리스폰
                Respawn();
            }
        }
        
        /// <summary>
        /// 저장 데이터에서 리스폰 처리 (Load 후 호출)
        /// </summary>
        private void RespawnFromSave()
        {
            if (_isRespawning) return;
            _isRespawning = true;
            
            // 플레이어 찾기
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // 물리 속도 리셋
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
                
                // 무적 상태 시작
                StartCoroutine(InvincibilityCoroutine());
            }
            
            _isRespawning = false;
        }
        
        /// <summary>
        /// 리스폰 처리
        /// </summary>
        public void Respawn()
        {
            if (_isRespawning) return;
            _isRespawning = true;
            
            // 플레이어 찾기
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // 체크포인트 위치로 이동
                Vector3 respawnPosition = _lastCheckpoint != Vector3.zero ? _lastCheckpoint : Vector3.zero;
                player.transform.position = respawnPosition;
                
                // 물리 속도 리셋
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
                
                // 무적 상태 시작
                StartCoroutine(InvincibilityCoroutine());
            }
            
            _isRespawning = false;
        }
        
        /// <summary>
        /// 무적 상태 코루틴
        /// </summary>
        private System.Collections.IEnumerator InvincibilityCoroutine()
        {
            _isInvincible = true;
            
            // 플레이어의 충돌 임시 비활성화
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var collider = player.GetComponent<Collider2D>();
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }
            
            yield return new WaitForSeconds(_respawnInvincibilityTime);
            
            // 충돌 다시 활성화
            if (player != null)
            {
                var collider = player.GetComponent<Collider2D>();
                if (collider != null)
                {
                    collider.enabled = true;
                }
            }
            
            _isInvincible = false;
        }
        
        /// <summary>
        /// 마지막 체크포인트 위치 반환
        /// </summary>
        public Vector3 GetLastCheckpoint()
        {
            return _lastCheckpoint;
        }
        
        /// <summary>
        /// 체크포인트 설정
        /// </summary>
        public void SetCheckpoint(Vector3 position)
        {
            _lastCheckpoint = position;
        }
        
        /// <summary>
        /// 사망 횟수 반환
        /// </summary>
        public int GetDeathCount()
        {
            return _deathCount;
        }
        
        /// <summary>
        /// 사망 횟수 리셋
        /// </summary>
        public void ResetDeathCount()
        {
            _deathCount = 0;
            _lastCheckpoint = Vector3.zero;
        }
    }
}
