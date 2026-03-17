using Pathfinder.Core.DI;
using Pathfinder.Player;
using Pathfinder.World;
using Pathfinder.Core;
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
            // 능력 관리자 (씬에서 찾거나 없으면 생성)
            var abilityManager = UnityEngine.Object.FindObjectOfType<AbilityManager>();
            if (abilityManager == null)
            {
                // Player 오브젝트 찾기
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    abilityManager = player.GetComponent<AbilityManager>();
                    if (abilityManager == null)
                    {
                        abilityManager = player.AddComponent<AbilityManager>();
                    }
                }
                else
                {
                    // Player 태그가 없으면 AbilityManager를 가진 오브젝트 찾기
                    abilityManager = UnityEngine.Object.FindObjectOfType<AbilityManager>();
                }
            }
            
            if (abilityManager != null)
            {
                container.RegisterInstance<IAbilityManager>(abilityManager);
            }
            
            // 사망 관리자 (씬에서 찾아서 등록)
            var deathManager = UnityEngine.Object.FindObjectOfType<DeathManager>();
            if (deathManager != null)
            {
                container.RegisterInstance<IDeathManager>(deathManager);
            }
            
            // 맵 관리자 (씬에서 찾아서 등록)
            var mapManager = UnityEngine.Object.FindObjectOfType<MapManager>();
            if (mapManager != null)
            {
                container.RegisterInstance<IMapManager>(mapManager);
            }
            
            // 저장 관리자 (씬에서 찾아서 등록)
            var saveManager = UnityEngine.Object.FindObjectOfType<SaveManager>();
            if (saveManager != null)
            {
                container.RegisterInstance<ISaveManager>(saveManager);
            }
        }
    }
}
