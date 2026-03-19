using UnityEngine;

namespace Pathfinder.World
{
    public interface ICameraController
    {
        void SetTarget(Transform target);
        void SnapToTarget();
        void SetMapBounds(Vector2 min, Vector2 max);
    }
}