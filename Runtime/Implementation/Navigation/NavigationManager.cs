

using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.NavigationAPI
{
    internal class NavigationManager : INavigationManager
    {
        public void OnDestroy()
        {
        }

        public IGridBasedPathFinder CreateGridPathFinder(ITaskSystem taskSystem, IGridData gridData, int neighbourCount)
        {
            return new GridBasedAStarPathFinder(taskSystem, gridData, neighbourCount);
        }

        public IGridNavigationAgent Create(GameObject overrideGameObject, Transform parent, Vector3 position, Quaternion rotation)
        {
            return new GridBasedNavAgent(overrideGameObject, parent, position, rotation);
        }
    }
}
