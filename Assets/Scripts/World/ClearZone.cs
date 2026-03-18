using UnityEngine;

namespace Pathfinder.World
{
    public class ClearZone : MonoBehaviour
    {
        private const string PLAYER_TAG = "Player";
        
        [Header("Settings")]
        [SerializeField] private bool _showDebugLog = true;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(PLAYER_TAG))
            {
                if (_showDebugLog)
                {
                    Debug.Log("[GAME CLEAR] Player reached the clear zone!");
                }
            }
        }
    }
}