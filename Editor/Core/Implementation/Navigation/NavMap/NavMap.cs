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

using System.Collections.Generic;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.NavigationAPI
{
    public class NavBorder
    {
        public int AreaIndex0 = -1;
        public int AreaIndex1 = -1;
        public int VertexIndex0 = -1;
        public int VertexIndex1 = -1;
    }

    public class NavArea
    {
        public int AreaType => m_AreaType;
        public List<NavBorder> Borders => m_Borders;
#if UNITY_EDITOR
        public List<Vector3> Vertices { get; set; }
        public Vector3[] LoopVertices { get; set; }
        public List<int> VertexIndices { get; set; }
        public Vector3 Center { get; set; }
        public int Index { get; set; }
#endif

        public NavArea(int type)
        {
            m_AreaType = type;
        }

        public void AddBorder(NavBorder border)
        {
            m_Borders.Add(border);
        }

        private readonly int m_AreaType;
        private readonly List<NavBorder> m_Borders = new();
    }

    public partial class NavMap
    {
        public List<NavArea> Areas => m_Areas;

        public void Create(List<Vector3> vertices, List<List<int>> areas, List<int> areaTypes)
        {
            Debug.Assert(areas.Count == areaTypes.Count);
            m_Vertices = vertices;
            for (var i = 0; i < areas.Count; ++i)
            {
                var navArea = new NavArea(areaTypes[i]);
                SetDebugData(navArea, i, areas[i], vertices);
                m_Areas.Add(navArea);
            }

            var navBorders = CreateNavBorders(areas);
#if UNITY_EDITOR
            m_Borders = navBorders;
#endif
            foreach (var border in navBorders)
            {
                m_Areas[border.AreaIndex0].AddBorder(border);
                m_Areas[border.AreaIndex1].AddBorder(border);
            }

            m_Renderer = new NavMapRenderer(this);
        }

        private void SetDebugData(NavArea navArea, int areaIndex, List<int> indices, List<Vector3> vertices)
        {
            var verts = new List<Vector3>();
            var loopVerts = new List<Vector3>();
            foreach (var index in indices)
            {
                verts.Add(vertices[index]);
            }
            loopVerts.AddRange(verts);
            loopVerts.Add(verts[0]);
            navArea.Vertices = vertices;
            navArea.LoopVertices = loopVerts.ToArray();
            navArea.VertexIndices = new(indices);
            navArea.Index = areaIndex;
            navArea.Center = Helper.CalculateCenter(verts);
        }

        public void OnDestroy()
        {
            m_Renderer?.OnDestroy();
        }

        public void Draw()
        {
            m_Renderer?.Draw();
        }

        private List<NavBorder> CreateNavBorders(List<List<int>> areas)
        {
            Dictionary<Vector2Int, NavBorder> navBorders = new();
            for (var i = 0; i < areas.Count; ++i)
            {
                var curArea = areas[i];
                for (var cur = 0; cur < curArea.Count; ++cur)
                {
                    var next = (cur + 1) % curArea.Count;
                    var start = curArea[cur];
                    var end = curArea[next];
                    var key = new Vector2Int(Mathf.Min(start, end), Mathf.Max(start, end));
                    if (navBorders.TryGetValue(key, out var border))
                    {
                        border.AreaIndex1 = i;
                    }
                    else
                    {
                        border = new NavBorder()
                        {
                            AreaIndex0 = i,
                            VertexIndex0 = key.x,
                            VertexIndex1 = key.y,
                        };
                        navBorders[key] = border;
                    }
                }
            }

            List<NavBorder> result = new();
            foreach (var border in navBorders.Values)
            {
                if (border.AreaIndex0 >= 0 &&
                    border.AreaIndex1 >= 0)
                {
                    result.Add(border);
                }
            }
            return result;
        }

        private List<Vector3> m_Vertices;
        private List<NavArea> m_Areas = new();
        private NavMapRenderer m_Renderer;
#if UNITY_EDITOR
        private List<NavBorder> m_Borders;
#endif
    }
}
