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

using Poly2Tri;
using Poly2Tri.Triangulation;
using Poly2Tri.Triangulation.Polygon;
using System.Collections.Generic;
using UnityEngine;

namespace XDay.UtilityAPI.Editor
{
    public static class SimpleTriangulator
    {
        public static void Triangulate(List<Vector3> polygonVertices, out Vector3[] meshVerticesOut, out int[] meshIndicesOut, bool swapIndex = false)
        {
            var points = new PolygonPoint[polygonVertices.Count];
            for (int i = 0; i < polygonVertices.Count; ++i)
            {
                points[i] = new PolygonPoint(polygonVertices[i].x, polygonVertices[i].z);
            }

            var polygon = new Polygon(points);

            //三角划分多边形
            P2T.Triangulate(polygon);

            List<Vector3> meshVertices = new();
            List<int> meshIndices = new();

            var triangles = polygon.Triangles;
            for (int i = 0; i < triangles.Count; ++i)
            {
                for (int j = 0; j < 3; ++j)
                {
                    var pt = triangles[i].Points[j];
                    AddVertex(pt, meshVertices, meshIndices);
                }
            }

            if (swapIndex)
            {
                int triCount = meshIndices.Count / 3;
                for (int i = 0; i < triCount; ++i)
                {
                    (meshIndices[i * 3 + 2], meshIndices[i * 3]) = (meshIndices[i * 3], meshIndices[i * 3 + 2]);
                }
            }

            meshVerticesOut = meshVertices.ToArray();
            meshIndicesOut = meshIndices.ToArray();
        }

        static void AddVertex(TriangulationPoint pt, List<Vector3> vertices, List<int> indices)
        {
            int vtxIdx = -1;
            float x = (float)pt.X;
            float z = (float)pt.Y;

            for (int i = 0; i < vertices.Count; ++i)
            {
                if (Mathf.Approximately(x, vertices[i].x) &&
                    Mathf.Approximately(z, vertices[i].z))
                {
                    vtxIdx = i;
                    break;
                }
            }

            if (vtxIdx == -1)
            {
                vertices.Add(new Vector3(x, 0, z));
                vtxIdx = vertices.Count - 1;
            }
            indices.Add(vtxIdx);
        }
    }
}
