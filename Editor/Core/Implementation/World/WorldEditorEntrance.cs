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



using UnityEditor;

namespace XDay.WorldAPI.Editor
{
    internal class WorldEditorEntrance : EditorWindow
    {
        [MenuItem("XDay/地图/打开编辑器")]
        private static void Open()
        {
            GetWindow<WorldEditorEntrance>("地图编辑器").Show();

            WorldEditor.CreateScene();
        }

        private void OnEnable()
        {
            SetEvents(clear: true);
            SetEvents(clear: false);
            autoRepaintOnSceneChange = true;
        }

        private void OnDisable()
        {
            SetEvents(clear: true);
        }

        private void OnGUI()
        {
            WorldEditor.InspectorGUI();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            WorldEditor.SceneGUI();
        }

        private void RepaintInternal()
        {
            Repaint();
        }

        private void UpdateScene()
        {
            double currentTime = EditorApplication.timeSinceStartup;
            var dt = currentTime - m_LastExecutionTime;
            if (dt > 0.1)
            {
                dt = 0.1;
            }
            m_LastExecutionTime = currentTime;

            WorldEditor.Update((float)dt);
        }

        private void SetEvents(bool clear)
        {
            if (clear)
            {
                SceneView.duringSceneGui -= OnSceneGUI;
                EditorApplication.update -= UpdateScene;
                WorldEditor.EventRepaint -= RepaintInternal;
            }
            else
            {
                SceneView.duringSceneGui += OnSceneGUI;
                EditorApplication.update += UpdateScene;
                WorldEditor.EventRepaint += RepaintInternal;
            }
        }

        private static double m_LastExecutionTime;
    }
}

//XDay