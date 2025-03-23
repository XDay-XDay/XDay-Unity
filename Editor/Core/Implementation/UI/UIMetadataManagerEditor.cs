
using UnityEngine;

namespace XDay.GUIAPI.Editor
{
    [UnityEditor.CustomEditor(typeof(UIMetadataManager))]
    public class UIMetadataManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            GUI.enabled = false;
            base.OnInspectorGUI();
            GUI.enabled = true;
        }
    }
}
