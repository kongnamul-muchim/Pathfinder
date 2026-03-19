using UnityEngine;

namespace Pathfinder.Core.DI
{
    public abstract class Installer : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Higher priority installers execute first")]
        private int _priority = 0;

        public int Priority => _priority;

        public abstract void Install(DIContainer container);
    }
}