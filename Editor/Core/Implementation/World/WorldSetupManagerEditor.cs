using UnityEngine;

namespace XDay.WorldAPI.Editor
{
    [UnityEditor.CustomEditor(typeof(WorldSetupManager))]
    internal class WorldSetupManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            UnityEditor.EditorGUILayout.TextArea("Edit in world editor!");

            GUI.enabled = false;
            base.OnInspectorGUI();
            GUI.enabled = true;
        }
    }
}
