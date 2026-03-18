using Pathfinder.Core.DI;
using Pathfinder.Core;
using Pathfinder.World;
using Pathfinder.UI;
using UnityEngine;

namespace Pathfinder.Player
{
    public class DeathManager : MonoBehaviour, IDeathManager
    {
        [Header("Death Settings")]
        [Tooltip("리스폰 시 무적 시간 (초)")]
        [SerializeField] private float _respawnInvincibilityTime = 1f;
        
        [Header("GameOver UI")]
        [SerializeField] private GameOverUI _gameOverUI;
        
        [Inject] private IAbilityManager _abilityManager;
        [Inject] private ISaveManager _saveManager;
        
        private MapManager _mapManager;
        
        private void Awake()
        {
            if (_abilityManager == null)
                _abilityManager = FindObjectOfType<AbilityManager>();
            
            if (_saveManager == null)
                _saveManager = FindObjectOfType<SaveManager>();
            
            _mapManager = FindObjectOfType<MapManager>();
            
            if (_gameOverUI == null)
                _gameOverUI = FindObjectOfType<GameOverUI>();
        }
        
        // 마지막 체크포인트 위치
        private Vector3 _lastCheckpoint;
        
        // 사망 횟수
        private int _deathCount = 0;
        
        // 리스폰 중인지 여부
        private bool _isRespawning = false;
        
        // 무적 상태
        private bool _isInvincible = false;
        
        public void OnPlayerDeath()
        {
            if (_isInvincible) return;
            
            bool hasSave = _saveManager != null && _saveManager.HasSaveData();
            int lives = _abilityManager?.GetLives() ?? 0;
            
            Debug.Log($"[DEATH] OnPlayerDeath - HasSave: {hasSave}, Lives: {lives}");
            
            _deathCount++;
            
            if (hasSave)
            {
                if (lives > 1)
                {
                    RollbackWithLife();
                }
                else
                {
                    TriggerGameOver();
                }
            }
            else
            {
                if (lives > 1)
                {
                    RespawnAtSpawnPointWithLife();
                }
                else
                {
                    TriggerGameOver();
                }
            }
        }
        
        private void RollbackWithLife()
        {
            Debug.Log("[DEATH] RollbackWithLife - Loading save and consuming life");
            
            var saveManager = _saveManager as SaveManager;
            string savedMapId = saveManager?.GetSavedMapId();
            
            if (!string.IsNullOrEmpty(savedMapId) && _mapManager != null)
                _mapManager.SwitchToMap(savedMapId);
            
            _saveManager.Load(false);
            _abilityManager?.ConsumeLife();
            
            int remainingLives = _abilityManager?.GetLives() ?? 0;
            saveManager?.UpdateSavedLives(remainingLives);
            
            RespawnFromSave();
        }
        
        private void RespawnAtSpawnPointWithLife()
        {
            Debug.Log("[DEATH] RespawnAtSpawnPointWithLife - Resetting progress and spawning at SpawnPoint");
            
            var saveManager = _saveManager as SaveManager;
            saveManager?.ResetAllProgress();
            
            _abilityManager?.ConsumeLife();
            
            RespawnAtSpawnPoint();
        }
        
        private void RespawnAtSpawnPoint()
        {
            if (_mapManager != null)
            {
                Vector3 spawnPosition = _mapManager.GetSpawnPosition(0);
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    player.transform.position = spawnPosition;
                    
                    var rb = player.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector2.zero;
                        rb.angularVelocity = 0f;
                    }
                    
                    StartCoroutine(InvincibilityCoroutine());
                }
            }
        }
        
        private void TriggerGameOver()
        {
            Debug.Log("[DEATH] GameOver triggered");
            
            if (_gameOverUI != null)
            {
                _gameOverUI.Show();
            }
            else
            {
                Debug.LogError("[DEATH] GameOverUI is null! Cannot show GameOver screen.");
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
