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
using XDay.CameraAPI;
using XDay.UtilityAPI;

namespace XDay.WorldAPI
{
    internal class WorldDebugger : MonoBehaviour
    {
        public float MoveDistance = 500;

        public void Init(IWorld world)
        {
            m_World = world as World;
        }

        private void Update()
        {
            if (m_World == null || m_World.CameraManipulator == null)
            {
                return;
            }

            if (Input.GetKeyUp(KeyCode.W))
            {
                m_World.CameraManipulator.SetPosition(m_World.CameraManipulator.RenderPosition + new Vector3(0, 0, MoveDistance));
            }

            if (Input.GetKeyUp(KeyCode.S))
            {
                m_World.CameraManipulator.SetPosition(m_World.CameraManipulator.RenderPosition + new Vector3(0, 0, -MoveDistance));
            }

            if (Input.GetKeyUp(KeyCode.A))
            {
                m_World.CameraManipulator.SetPosition(m_World.CameraManipulator.RenderPosition + new Vector3(-MoveDistance, 0, 0));
            }

            if (Input.GetKeyUp(KeyCode.D))
            {
                m_World.CameraManipulator.SetPosition(m_World.CameraManipulator.RenderPosition + new Vector3(MoveDistance, 0, 0));
            }
        }

        private void OnDrawGizmos()
        {
            if (m_World == null)
            {
                return;
            }
            //绘制相机视野
            m_World.CameraVisibleAreaCalculator.DebugDraw();

            //绘制相机移动范围
            if (m_World.CameraManipulator != null)
            {
                var old = Gizmos.color;
                Gizmos.color = Color.magenta;
                var bounds = m_World.CameraManipulator.Setup.FocusPointBounds;
                var min = bounds.min.ToVector3XZ();
                var max = bounds.max.ToVector3XZ();
                Gizmos.DrawWireCube((min + max) / 2f, max - min);
                Gizmos.color = old;
            }
        }

        private void OnGUI()
        {
            if (m_World == null || !m_World.ShowDebugInfo) 
            {
                return;
            }

            if (m_StyleBig == null)
            {
                m_StyleBig = new GUIStyle(GUI.skin.label);
                m_StyleBig.fontSize = 40;
                m_StyleBig.normal.textColor = Color.green;
            }

            if (m_StyleSmall == null)
            {
                m_StyleSmall = new GUIStyle(GUI.skin.label);
                m_StyleSmall.fontSize = 30;
                m_StyleSmall.normal.textColor = Color.green;
            }

            GUILayout.Label($"地图大小: {m_World.Bounds.size}", m_StyleBig);
            if (m_World.CameraManipulator != null)
            {
                GUILayout.Label($"相机高度: {m_World.CameraManipulator.RenderPosition.y}. FOV: {m_World.CameraManipulator.Camera.fieldOfView}", m_StyleBig);
            }
            GUILayout.Label($"地图LOD: {m_World.CurrentLOD}", m_StyleBig);
            foreach (var plugin in m_World.QueryPlugins<WorldPlugin>())
            {
                if (plugin.LODSystem != null)
                {
                    GUILayout.Label($"{plugin.Name} LOD: {plugin.LODSystem.CurrentLOD}", m_StyleBig);
                }
            }

            if (m_CameraDebugger == null)
            {
                var debuggers = Object.FindObjectsByType<CameraDebugger>(FindObjectsSortMode.None);
                if (debuggers.Length > 0)
                {
                    m_CameraDebugger = debuggers[0];
                }
            }
            if (m_CameraDebugger != null)
            {
                var visibleArea = m_World.CameraVisibleAreaCalculator.VisibleArea;
                var expandedVisibleArea = m_World.CameraVisibleAreaCalculator.ExpandedArea;
                GUILayout.Label($"视野范围: {visibleArea}", m_StyleSmall);
                GUILayout.Label($"扩大的视野范围: {expandedVisibleArea}", m_StyleSmall);
            }
        }

        private World m_World;
        private GUIStyle m_StyleBig;
        private GUIStyle m_StyleSmall;
        private CameraDebugger m_CameraDebugger;
    }
}

//XDay