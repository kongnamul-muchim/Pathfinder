using UnityEngine;

namespace Pathfinder.Player
{
    public interface IDeathManager
    {
        void OnPlayerDeath();
        void Respawn();
        Vector3 GetLastCheckpoint();
        void SetCheckpoint(Vector3 position);
    }
}