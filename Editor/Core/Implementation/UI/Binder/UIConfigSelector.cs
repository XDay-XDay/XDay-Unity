

using UnityEngine;

namespace XDay.GUIAPI.Editor
{
    [CreateAssetMenu(fileName = "UIConfigSelector.asset", menuName = "XDay/UI/Config Selector")]
    public class UIConfigSelector : ScriptableObject
    {
        public UIBinderConfig ActiveConfig;
        public UIMetadataManager ActiveMetadataManager;
    }
}
