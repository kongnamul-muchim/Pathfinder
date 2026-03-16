using UnityEngine;

namespace Pathfinder.Core.DI
{
    /// <summary>
    /// Base class for MonoBehaviour-based installers.
    /// Implement Install() to register scene-specific services.
    /// </summary>
    public abstract class Installer : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Higher priority installers execute first")]
        private int _priority = 0;

        public int Priority => _priority;

        /// <summary>
        /// Called by RootContext or DIContainerManager to register services.
        /// </summary>
        /// <param name="container">The scene-specific DI container</param>
        public abstract void Install(DIContainer container);

        protected virtual void Awake()
        {
            // Installers are typically executed by RootContext or auto-collected
            // This Awake is for any installer-specific initialization if needed
        }
    }
}
