using UnityEngine;

namespace XDay.GUIAPI
{
    [AddComponentMenu("XDay/UI/DynamicAtlasController", 0)]
    public class DynamicAtlasController : MonoBehaviour
    {
        public bool EnableDynamicAtlas => m_EnableDynamicAtlas;

        private void OnDisable()
        {
            DynamicAtlasMgr.S.ClearSet(this);
        }

        [SerializeField]
        private bool m_EnableDynamicAtlas = true;
    }
}
