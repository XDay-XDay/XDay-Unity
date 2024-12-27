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


using System;
using System.Collections.Generic;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using UnityEngine;

namespace XDay.UtilityAPI.Editor
{
    public static class PolygonTriangulator
    {
        public static bool Triangulate(List<Vector3> polygonVertices, out Vector3[] triangleVertices, out int[] triangleIndices)
        {
            triangleVertices = null;
            triangleIndices = null;
            try
            {
                var vertices = new List<Vertex>();
                foreach (var v in polygonVertices)
                {
                    vertices.Add(new Vertex(v.x, v.z));
                }
                var contour = new Contour(vertices);

                var polygon = new Polygon();
                polygon.Add(contour);
                var mesh = polygon.Triangulate(new QualityOptions());
                MeshToTriangles(mesh, out triangleVertices, out triangleIndices);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Triangulation failed with error: {ex}");
                return false;
            }
            return true;
        }

        private static void MeshToTriangles(IMesh mesh, out Vector3[] triangleVertices, out int[] triangleIndices)
        {
            triangleIndices = new int[mesh.Triangles.Count * 3];
            var idx = 0;
            foreach (var triangle in mesh.Triangles)
            {
                for (var i = 0; i < 3; ++i)
                {
                    triangleIndices[idx++] = triangle.GetVertexID(i);
                }
            }

            triangleVertices = new Vector3[mesh.Vertices.Count];
            foreach (var v in mesh.Vertices)
            {
                triangleVertices[v.ID] = new Vector3((float)v.X, 0, (float)v.Y);
            }
        }
    }
}


//XDay