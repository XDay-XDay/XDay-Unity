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
using XDay.NavigationAPI;
using XDay.UtilityAPI;
using XDay.WorldAPI.Editor;
using XDay.WorldAPI.Navigation.Editor;

namespace XDay.WorldAPI.Shape.Editor
{
    internal partial class ShapeSystem
    {
        private NavMap CreateNavMap()
        {
            List<Vector3> vertices = new();
            List<List<int>> areas = new();
            List<int> areaTypes = new();
            foreach (var shape in m_Shapes.Values)
            {
                List<int> area = new();
                List<Vector3> shapeWorldVertices = new();
                for (var i = 0; i < shape.VertexCount; ++i)
                {
                    var pos = shape.GetVertexWorldPosition(i);
                    shapeWorldVertices.Add(pos);
                }
                if (Helper.IsClockwiseWinding(shapeWorldVertices))
                {
                    shapeWorldVertices.Reverse();
                }
                for (var i = 0; i < shape.VertexCount; ++i)
                {
                    area.Add(AddVertex(shapeWorldVertices[i], vertices));
                }
                areas.Add(area);
                areaTypes.Add(shape.AreaID);
            }

            var navMap = new NavMap();
            navMap.Create(vertices, areas, areaTypes);

            var plugin = World.QueryPlugin<NavigationPlugin>();
            plugin.SetNavMap(navMap);
            WorldEditor.SelectedPluginIndex = World.QueryPluginIndex(plugin);

            return navMap;
        }

        private int AddVertex(Vector3 pos, List<Vector3> vertices)
        {
            var index = vertices.IndexOf(pos);
            if (index == -1)
            {
                vertices.Add(pos);
                return vertices.Count - 1;
            }
            return index;
        }
    }
}
