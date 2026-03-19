using UnityEngine;

namespace Pathfinder.World
{
    public interface IPortal
    {
        void Teleport(GameObject player);
        string GetTargetMapId();
        string GetTargetPortalId();
    }
}