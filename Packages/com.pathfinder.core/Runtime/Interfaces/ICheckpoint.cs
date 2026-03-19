using UnityEngine;

namespace Pathfinder.World
{
    public interface ICheckpoint
    {
        void Activate();
        bool IsActivated();
        Vector3 GetPosition();
    }
}