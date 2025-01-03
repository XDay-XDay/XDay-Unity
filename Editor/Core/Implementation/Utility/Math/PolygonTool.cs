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

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace XDay.UtilityAPI.Editor
{
    public class PolygonTool
    {
        public void Reset()
        {
            m_Polygon.Clear();
        }

        public void SceneUpdate(Color color, Action<List<Vector3>> onCreate, float y = 0)
        {
            var e = Event.current;
            if (e.alt)
            {
                return;
            }

            var pos = Helper.GUIRayCastWithXZPlane(e.mousePosition, SceneView.currentDrawingSceneView.camera, y);

            if (e.type == EventType.MouseDown && 
                e.button == 0)
            {
                if (m_Polygon.Count == 0)
                {
                    m_Polygon.Add(pos);
                }
                m_Polygon.Add(pos);
            }
            else if (e.type == EventType.MouseMove)
            {
                if (m_Polygon.Count > 0)
                {
                    m_Polygon[^1] = pos;
                }
            }
            else if (e.type == EventType.MouseUp && 
                e.button == 1)
            {
                if (m_Polygon.Count > 3)
                {
                    m_Polygon.RemoveAt(m_Polygon.Count - 1);
                    onCreate(m_Polygon);
                }

                Reset();
            }

            DrawPolygon(color);
        }

        private void DrawPolygon(Color color)
        {
            if (m_Polygon.Count > 0)
            {
                var oldColor = Handles.color;
                Handles.color = color;
                Handles.DrawPolyLine(m_Polygon.ToArray());
                Handles.color = oldColor;
            }
        }

        private List<Vector3> m_Polygon = new();
    }
}


//XDay