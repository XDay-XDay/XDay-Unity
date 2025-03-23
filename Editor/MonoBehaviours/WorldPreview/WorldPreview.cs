/*
 * Copyright (c) 2024-2025 XDay
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using XDay.API;
using XDay.UtilityAPI;
using XDay.WorldAPI;


internal partial class WorldPreview : MonoBehaviour
{
    private async void Start()
    {
        Application.runInBackground = true;

        m_XDay = IXDayContext.Create(EditorHelper.QueryAssetFilePath<WorldSetupManager>(), new EditorWorldAssetLoader(), true, true);
        await m_XDay.WorldManager.LoadWorldAsync("");
    }

    private void OnDestroy()
    {
        m_XDay.OnDestroy();
    }

    private void Update()
    {
        m_XDay.Update();

        UpdateTests();
    }

    private void LateUpdate()
    {
        m_XDay.LateUpdate();
    }

    private void OnGUI()
    {
        if (m_Style == null)
        {
            m_Style = new GUIStyle(GUI.skin.label);
            m_Style.fontSize = 50;
        }

        if (m_XDay.WorldManager != null)
        {
            var world = m_XDay.WorldManager.FirstWorld;
            if (world != null)
            {
                foreach (var plugin in world.QueryPlugins<WorldPlugin>())
                {
                    if (plugin.LODSystem != null)
                    {
                        GUILayout.Label($"{plugin.Name} LOD: {plugin.LODSystem.CurrentLOD}", m_Style);
                    }
                }
            }
        }
    }

    [MenuItem("XDay/World/Preview", false, 1)]
    static void Open()
    {
        var sceneGUIDs = AssetDatabase.FindAssets("t:Scene");
        if (sceneGUIDs.Length > 0)
        {
            foreach (var guid in sceneGUIDs)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = Path.GetFileName(path);
                if (name == "WorldPreview.unity")
                {
                    EditorSceneManager.OpenScene(path);
                }
            }
        }
    }

    private GUIStyle m_Style;
    private IXDayContext m_XDay;
}


#endif