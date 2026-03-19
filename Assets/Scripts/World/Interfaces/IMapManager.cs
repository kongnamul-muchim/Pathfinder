namespace Pathfinder.World
{
    public interface IMapManager
    {
        void SwitchToMap(int mapIndex);
        void SwitchToMap(string mapId);
        int GetCurrentMapIndex();
        string GetCurrentMapId();
        bool IsMapActive(int mapIndex);
        bool IsMapActive(string mapId);
    }
}