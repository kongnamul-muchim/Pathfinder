using Pathfinder.Core.DI;
using Pathfinder.Core;
using Pathfinder.World;
using Pathfinder.UI;
using UnityEngine;

namespace Pathfinder.Player
{
    public class DeathManager : MonoBehaviour, IDeathManager
    {
        [Header("GameOver UI")]
        [SerializeField] private GameOverUI _gameOverUI;
        
        [Inject] private IAbilityManager _abilityManager;
        [Inject] private ISaveManager _saveManager;
        
        private MapManager _mapManager;
        
        private void Awake()
        {
            if (_abilityManager == null)
            {
                _abilityManager = FindFirstObjectByType<AbilityManager>();
                if (_abilityManager == null)
                {
                    Debug.LogWarning("[DeathManager] AbilityManager not found!");
                }
            }
            
            if (_saveManager == null)
            {
                _saveManager = FindFirstObjectByType<SaveManager>();
                if (_saveManager == null)
                {
                    Debug.LogWarning("[DeathManager] SaveManager not found!");
                }
            }
            
            _mapManager = FindFirstObjectByType<MapManager>();
            if (_mapManager == null)
            {
                Debug.LogWarning("[DeathManager] MapManager not found!");
            }
            
            if (_gameOverUI == null)
            {
                _gameOverUI = FindFirstObjectByType<GameOverUI>();
            }
        }
        
        private Vector3 _lastCheckpoint;
        private int _deathCount = 0;
        private bool _isRespawning = false;
        
        public void OnPlayerDeath()
        {
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
            
            if (_saveManager is SaveManager saveManager)
            {
                string savedMapId = saveManager.GetSavedMapId();
                
                if (!string.IsNullOrEmpty(savedMapId) && _mapManager != null)
                {
                    _mapManager.SwitchToMap(savedMapId);
                }
                
                _saveManager.Load(false);
                _abilityManager?.ConsumeLife();
                
                int remainingLives = _abilityManager?.GetLives() ?? 0;
                saveManager.UpdateSavedLives(remainingLives);
            }
            
            RespawnFromSave();
        }
        
        private void RespawnAtSpawnPointWithLife()
        {
            Debug.Log("[DEATH] RespawnAtSpawnPointWithLife - Resetting progress and spawning at SpawnPoint");
            
            if (_saveManager is SaveManager saveManager)
            {
                saveManager.ResetAllProgress();
            }
            
            _abilityManager?.ConsumeLife();
            
            RespawnAtSpawnPoint();
        }
        
        private void RespawnAtSpawnPoint()
        {
            if (_mapManager == null) return;
            
            Vector3 spawnPosition = _mapManager.GetSpawnPosition(0);
            RespawnPlayerAt(spawnPosition);
        }
        
        private void RespawnPlayerAt(Vector3 position)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            
            player.transform.position = position;
            
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
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
        
        private void RespawnFromSave()
        {
            if (_isRespawning) return;
            _isRespawning = true;
            
            ResetPlayerVelocity();
            
            _isRespawning = false;
        }
        
        private void ResetPlayerVelocity()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
        
        public void Respawn()
        {
            if (_isRespawning) return;
            _isRespawning = true;
            
            Vector3 respawnPosition = _lastCheckpoint != Vector3.zero ? _lastCheckpoint : Vector3.zero;
            RespawnPlayerAt(respawnPosition);
            
            _isRespawning = false;
        }
        
        public Vector3 GetLastCheckpoint()
        {
            return _lastCheckpoint;
        }
        
        public void SetCheckpoint(Vector3 position)
        {
            _lastCheckpoint = position;
        }
        
        public int GetDeathCount()
        {
            return _deathCount;
        }
        
        public void ResetDeathCount()
        {
            _deathCount = 0;
            _lastCheckpoint = Vector3.zero;
        }
    }
}
