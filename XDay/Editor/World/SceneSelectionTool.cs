/*
 * Copyright (c) 2024 XDay
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

using XDay.UtilityAPI;
using System;
using UnityEditor;
using UnityEngine;

namespace XDay.WorldAPI.Editor
{
    public class SceneSelectionTool
    {
        public void SceneGUI(Camera camera, Action<Vector3, Vector3> onSelect)
        {
            var e = Event.current;
            var worldPosition = Helper.GUIRayCastWithXZPlane(e.mousePosition, camera);

            if (e.button == 0 && !e.alt)
            {
                if (e.type == EventType.MouseDown)
                {
                    m_MouseDown = true;
                    m_Start = worldPosition;
                    m_End = worldPosition;
                }
                else if (e.type == EventType.MouseDrag)
                {
                    m_End = worldPosition;
                }
                else if (e.type == EventType.MouseUp)
                {
                    if (m_MouseDown)
                    {
                        m_MouseDown = false;
                        m_End = worldPosition;
                        onSelect.Invoke(Vector3.Min(m_Start, m_End), Vector3.Max(m_Start, m_End));
                    }
                }
            }

            DrawGizmo();
        }

        private void DrawGizmo()
        {
            if (m_MouseDown)
            {
                var min = Vector3.Min(m_Start, m_End);
                var max = Vector3.Max(m_Start, m_End);
                Handles.DrawWireCube((min + max) * 0.5f, max - min);
            }
        }

        private Vector3 m_Start;
        private Vector3 m_End;
        private bool m_MouseDown = false;
    }
}


//XDay