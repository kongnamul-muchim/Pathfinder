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
                _abilityManager = FindFirstObjectByType<AbilityManager>();
            
            if (_saveManager == null)
                _saveManager = FindFirstObjectByType<SaveManager>();
            
            _mapManager = FindFirstObjectByType<MapManager>();
            
            if (_gameOverUI == null)
                _gameOverUI = FindFirstObjectByType<GameOverUI>();
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
        
        private void RespawnFromSave()
        {
            if (_isRespawning) return;
            _isRespawning = true;
            
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
            }
            
            _isRespawning = false;
        }
        
        public void Respawn()
        {
            if (_isRespawning) return;
            _isRespawning = true;
            
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 respawnPosition = _lastCheckpoint != Vector3.zero ? _lastCheckpoint : Vector3.zero;
                player.transform.position = respawnPosition;
                
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
            }
            
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
