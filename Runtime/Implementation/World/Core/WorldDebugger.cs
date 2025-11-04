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

using UnityEngine;

namespace XDay.WorldAPI
{
    internal class WorldDebugger : MonoBehaviour
    {
        public void Init(IWorld world)
        {
            m_World = world as World;
        }

        private void OnDrawGizmos()
        {
            m_World?.CameraVisibleAreaCalculator.DebugDraw();
        }

        private void OnGUI()
        {
            if (m_World == null || !m_World.ShowDebugInfo) 
            {
                return;
            }

            if (m_Style == null)
            {
                m_Style = new GUIStyle(GUI.skin.label);
                m_Style.fontSize = 50;
            }
            
            GUILayout.Label($"地图大小: {m_World.Bounds.size}", m_Style);
            if (m_World.CameraManipulator != null)
            {
                GUILayout.Label($"相机高度: {m_World.CameraManipulator.RenderPosition.y}", m_Style);
            }
            GUILayout.Label($"地图LOD: {m_World.CurrentLOD}", m_Style);
            foreach (var plugin in m_World.QueryPlugins<WorldPlugin>())
            {
                if (plugin.LODSystem != null)
                {
                    GUILayout.Label($"{plugin.Name} LOD: {plugin.LODSystem.CurrentLOD}", m_Style);
                }
            }
        }

        private World m_World;
        private GUIStyle m_Style;
    }
}

//XDay