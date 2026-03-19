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
            RegisterServiceFromScene<AbilityManager, IAbilityManager>(container);
            RegisterServiceFromScene<DeathManager, IDeathManager>(container);
            RegisterServiceFromScene<MapManager, IMapManager>(container);
            RegisterServiceFromScene<SaveManager, ISaveManager>(container);
        }
        
        private void RegisterServiceFromScene<TImplementation, TInterface>(DIContainer container)
            where TImplementation : UnityEngine.Object, TInterface
            where TInterface : class
        {
            var service = UnityEngine.Object.FindFirstObjectByType<TImplementation>();
            if (service != null)
            {
                container.RegisterInstance<TInterface>(service);
            }
            else
            {
                Debug.LogWarning($"[{nameof(GameInstaller)}] {typeof(TImplementation).Name} not found in scene!");
            }
        }
    }
}
