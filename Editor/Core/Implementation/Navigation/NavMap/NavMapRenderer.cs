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
using UnityEngine;

namespace XDay.NavigationAPI
{
    internal class NavMapRenderer
    {
        public NavMapRenderer(NavMap navMap)
        {
            m_NavMap = navMap;
        }

        public void OnDestroy()
        {
        }

        public void Draw()
        {
            foreach (var area in m_NavMap.Areas)
            {
                DrawArea(area);
            }
        }

        private void DrawArea(NavArea area)
        {
            var handleColor = Handles.color;
            var color = GUI.skin.label.normal.textColor;
            Handles.DrawAAPolyLine(area.LoopVertices);
            GUI.skin.label.normal.textColor = Color.green;
            for (var i = 0; i < area.VertexIndices.Count; ++i)
            {
                Handles.Label(area.Vertices[area.VertexIndices[i]], area.VertexIndices[i].ToString());
            }
            GUI.skin.label.normal.textColor = Color.red;
            Handles.Label(area.Center, area.Index.ToString());

            Handles.color = Color.blue;
            foreach (var border in area.Borders)
            {
                Handles.DrawAAPolyLine(area.Vertices[border.VertexIndex0], area.Vertices[border.VertexIndex1]);
            }

            GUI.skin.label.normal.textColor = color;
            Handles.color = handleColor;
        }

        private readonly NavMap m_NavMap;
    }
}
