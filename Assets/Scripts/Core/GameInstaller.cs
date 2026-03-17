using Pathfinder.Core.DI;
using Pathfinder.Player;
using Pathfinder.World;
using UnityEngine;

namespace Pathfinder.Core
{
    /// <summary>
    /// 게임 전역 서비스 등록용 Installer
    /// </summary>
    public class GameInstaller : Installer
    {
        public override void Install(DIContainer container)
        {
            // 능력 관리자 (싱글톤)
            container.RegisterInstance<IAbilityManager>(new AbilityManager());
            
            // 사망 관리자 (싱글톤)
            container.RegisterInstance<IDeathManager>(new DeathManager());
            
            // 맵 관리자 (씬에서 찾아서 등록)
            var mapManager = UnityEngine.Object.FindObjectOfType<MapManager>();
            if (mapManager != null)
            {
                container.RegisterInstance<IMapManager>(mapManager);
                Debug.Log("[GameInstaller] MapManager registered");
            }
            else
            {
                Debug.LogError("[GameInstaller] MapManager not found in scene!");
            }
            
            Debug.Log("[GameInstaller] Services registered");
        }
    }
    
    /// <summary>
    /// 능력 관리자 구현
    /// </summary>
    public class AbilityManager : IAbilityManager
    {
        private System.Collections.Generic.HashSet<AbilityType> _unlockedAbilities = new();
        
        public bool HasAbility(AbilityType ability)
        {
            return _unlockedAbilities.Contains(ability);
        }
        
        public void UnlockAbility(AbilityType ability)
        {
            _unlockedAbilities.Add(ability);
            Debug.Log($"[AbilityManager] Unlocked: {ability}");
        }
        
        public bool HasAllAbilities()
        {
            return _unlockedAbilities.Contains(AbilityType.DoubleJump) && 
                   _unlockedAbilities.Contains(AbilityType.PerspectiveShift);
        }
    }
    
    /// <summary>
    /// 사망 관리자 구현
    /// </summary>
    public class DeathManager : IDeathManager
    {
        private Vector3 _lastCheckpoint;
        private int _deathCount = 0;
        
        public void OnPlayerDeath()
        {
            _deathCount++;
            Debug.Log($"[DeathManager] Player died. Death count: {_deathCount}");
            Respawn();
        }
        
        public void Respawn()
        {
            // 플레이어 찾아서 리스폰
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = _lastCheckpoint != Vector3.zero ? _lastCheckpoint : Vector3.zero;
                Debug.Log($"[DeathManager] Respawned at {_lastCheckpoint}");
            }
        }
        
        public Vector3 GetLastCheckpoint()
        {
            return _lastCheckpoint;
        }
        
        public void SetCheckpoint(Vector3 position)
        {
            _lastCheckpoint = position;
            Debug.Log($"[DeathManager] Checkpoint set: {position}");
        }
    }
}
