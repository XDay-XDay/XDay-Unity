using UnityEngine;

namespace XDay.WorldAPI.Fog
{
    public interface IFogSystem : IWorldPlugin
    {
        void ResetFog();
        void BeginBatchOpen();
        void EndBatchOpen();
        void OpenCircle(FogDataType type, int minX, int minY, int maxX, int maxY, bool inner);
        void OpenRectangle(FogDataType type, int minX, int minY, int maxX, int maxY);
        bool IsOpen(int x, int y);
        bool IsUnlocked(int x, int y);
        Vector2Int PositionToCoord(Vector3 pos);
    }
}
