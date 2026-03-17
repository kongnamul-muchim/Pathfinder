using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pathfinder.Core.DI
{
    /// <summary>
    /// Orchestrates installers in a scene and manages dependency injection scope.
    /// Place this on a GameObject at the root of your scene.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class RootContext : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Installers to execute in order. Drag and drop to reorder.")]
        private List<Installer> _installers = new();

        [SerializeField]
        [Tooltip("Whether to auto-inject dependencies into child GameObjects after installation")]
        private bool _injectChildren = true;

        [SerializeField]
        [Tooltip("Include inactive GameObjects when injecting children")]
        private bool _includeInactive = true;

        [SerializeField]
        [Tooltip("Execution order relative to other RootContexts (lower = earlier)")]
        private int _executionOrder = 0;

        public int ExecutionOrder => _executionOrder;

        private void Awake()
        {
            // RootContext is responsible for:
            // 1. Being detected by DIContainerManager during scene load
            // 2. Executing its installers
            // 3. Optionally injecting into child objects
        }

        internal void ExecuteInstallers(DIContainer container)
        {
            foreach (var installer in _installers)
            {
                if (installer == null) continue;

                try
                {
                    installer.Install(container);
                }
                catch (Exception ex)
                {
                    // DI 주입 실패 - 콘솔 출력 제거
                }
            }

            if (_injectChildren)
            {
                InjectIntoChildren();
            }
        }

        private void InjectIntoChildren()
        {
            var childBehaviours = GetComponentsInChildren<MonoBehaviour>(_includeInactive);

            foreach (var behaviour in childBehaviours)
            {
                if (behaviour == null) continue;
                if (behaviour == this) continue; // Don't inject RootContext itself
                if (behaviour is Installer) continue; // Installers are already injected

                try
                {
                    DIContainerManager.InjectInto(behaviour);
                }
                catch (Exception ex)
                {
                    // DI 주입 실패 - 콘솔 출력 제거
                }
            }
        }

        /// <summary>
        /// Manually trigger injection into all child objects.
        /// Call this if objects are instantiated dynamically after scene load.
        /// </summary>
        public void TriggerInjection()
        {
            InjectIntoChildren();
        }

        /// <summary>
        /// Add an installer at runtime (before scene fully loads).
        /// </summary>
        public void AddInstaller(Installer installer)
        {
            if (!_installers.Contains(installer))
            {
                _installers.Add(installer);
            }
        }

        /// <summary>
        /// Remove an installer.
        /// </summary>
        public void RemoveInstaller(Installer installer)
        {
            _installers.Remove(installer);
        }
    }
}
