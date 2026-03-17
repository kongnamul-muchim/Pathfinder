using Pathfinder.Core.DI;
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
                    Debug.Log("[DeathManager] Extra life consumed! Respawning...");
                    Respawn();
                    return;
                }
            }
            
            // 추가 목숨이 없으면 정상 사망 처리
            _deathCount++;
            Debug.Log($"[DeathManager] Player died. Death count: {_deathCount}");
            
            // 리스폰 시작
            Respawn();
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
                
                Debug.Log($"[DeathManager] Respawned at {respawnPosition}");
                
                // 무적 상태 시작
                StartCoroutine(InvincibilityCoroutine());
            }
            else
            {
                Debug.LogError("[DeathManager] Player not found!");
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
            Debug.Log($"[DeathManager] Checkpoint set: {position}");
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
