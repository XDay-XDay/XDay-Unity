using UnityEngine;

namespace XDay.GUIAPI
{
    internal class DynamicAtlasMgrBehaviour : MonoBehaviour
    {
        void Update()
        {
            DynamicAtlasMgr.S.Update();
        }

        private void OnApplicationPause(bool pause)
        {
            DynamicAtlasMgr.S.Pause = pause;
        }

        private void OnApplicationFocus(bool focus)
        {
            DynamicAtlasMgr.S.Pause = !focus;
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(DynamicAtlasMgrBehaviour))]
    internal class DynamicAtlasMgrEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            foreach (var kv in DynamicAtlasMgr.S.Sets)
            {
                DrawSet(kv.Key, kv.Value);
            }
        }

        private void DrawSet(DynamicAtlasController d, DynamicAtlasSet set)
        {
            UnityEditor.EditorGUILayout.Foldout(true, $"Set: {set.Name}");
            UnityEditor.EditorGUI.indentLevel++;
            UnityEditor.EditorGUILayout.LabelField($"使用者: {d.name}");
            foreach (var atlas in set.Atlases.Values)
            {
                DrawAtlas(atlas);
            }
            UnityEditor.EditorGUI.indentLevel--;
        }

        private void DrawAtlas(DynamicAtlas atlas)
        {
            foreach (var page in atlas.Pages)
            {
                DrawPage(page);
            }
        }

        private void DrawPage(DynamicAtlasPage page)
        {
            UnityEditor.EditorGUILayout.Foldout(true, $"Page: {page.Index}");
            UnityEditor.EditorGUI.indentLevel++;
            UnityEditor.EditorGUILayout.IntField("Free List", page.FreeAreasList.Count);
            UnityEditor.EditorGUILayout.ObjectField("Texture", page.Texture, typeof(Texture2D), false);
            UnityEditor.EditorGUI.indentLevel--;
        }
    }
#endif
}
