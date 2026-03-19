using System.Collections.Generic;
using UnityEngine;

namespace Pathfinder.Core.DI
{
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

        public void ExecuteInstallers(DIContainer container)
        {
            foreach (var installer in _installers)
            {
                if (installer == null) continue;

                try
                {
                    installer.Install(container);
                }
                catch
                {
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
                if (behaviour == this) continue;
                if (behaviour is Installer) continue;

                try
                {
                    DIContainerManager.InjectInto(behaviour);
                }
                catch
                {
                }
            }
        }

        public void TriggerInjection()
        {
            InjectIntoChildren();
        }

        public void AddInstaller(Installer installer)
        {
            if (!_installers.Contains(installer))
            {
                _installers.Add(installer);
            }
        }

        public void RemoveInstaller(Installer installer)
        {
            _installers.Remove(installer);
        }
    }
}