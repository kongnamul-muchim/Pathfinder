using Pathfinder.Data;

namespace Pathfinder.Core
{
    public interface ISaveManager
    {
        void Save();
        bool Load();
        bool Load(bool restoreExtraLifeChests);
        bool HasSaveData();
        void ClearSave();
        GameSaveData GetCurrentSaveData();
        void SavePlayerPosition(UnityEngine.Vector3 position);
        void UpdateSavedLives(int lives);
    }
}