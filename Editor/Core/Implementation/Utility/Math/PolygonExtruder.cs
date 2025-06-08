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

namespace XDay.UtilityAPI.Editor
{
    public static class PolygonExtruder
    {
        public static void Extrude(List<Vector3> polygonOutline, float minHeight, float maxHeight, out Vector3[] meshVertices, out int[] meshIndices)
        {
            var bottom = new List<Vector3>(polygonOutline.Count);
            var top = new List<Vector3>(polygonOutline.Count);
            for (int i = 0; i < polygonOutline.Count; ++i)
            {
                bottom.Add(new Vector3(polygonOutline[i].x, minHeight, polygonOutline[i].z));
                top.Add(new Vector3(polygonOutline[i].x, maxHeight, polygonOutline[i].z));
            }

            bool isPolygonWindingCW = polygonOutline.IsClockwiseWinding();
            int n = polygonOutline.Count;

            meshVertices = new Vector3[n * 2];
            for (int i = 0; i < n; ++i)
            {
                meshVertices[i] = bottom[i];
            }
            for (int i = 0; i < n; ++i)
            {
                meshVertices[i + n] = top[i];
            }

            List<int> indices = new(n * 6);
            for (int i = 0; i < n; ++i)
            {
                int a = i;
                int b = (i + 1) % n;
                int c = (a + n) % (n * 2);
                int d = (b + n) % (n * 2);
                //根据polygon的winding order来生成三角形
                if (isPolygonWindingCW)
                {
                    indices.Add(a);
                    indices.Add(d);
                    indices.Add(c);
                }
                else
                {
                    indices.Add(a);
                    indices.Add(c);
                    indices.Add(d);
                }

                if (isPolygonWindingCW)
                {
                    indices.Add(a);
                    indices.Add(b);
                    indices.Add(d);
                }
                else
                {
                    indices.Add(a);
                    indices.Add(d);
                    indices.Add(b);
                }
            }

            PolygonTriangulator.Triangulate(top, out var polygonVertices, out var polygonIndices);
            Helper.ReverseArray(polygonIndices);
            for (int i = 0; i < polygonVertices.Length; ++i)
            {
                polygonVertices[i] = new Vector3(polygonVertices[i].x, maxHeight, polygonVertices[i].z);
            }
            for (int i = 0; i < polygonIndices.Length; ++i)
            {
                var pos = polygonVertices[polygonIndices[i]];
                var realIndex = GetIndex(meshVertices, pos);
                polygonIndices[i] = realIndex;
            }
            indices.AddRange(polygonIndices);

            PolygonTriangulator.Triangulate(polygonOutline, out polygonVertices, out polygonIndices);
            for (int i = 0; i < polygonVertices.Length; ++i)
            {
                polygonVertices[i] = new Vector3(polygonVertices[i].x, minHeight, polygonVertices[i].z);
            }

            Helper.ReverseArray(polygonIndices);
            for (int i = 0; i < polygonIndices.Length; ++i)
            {
                var pos = polygonVertices[polygonIndices[i]];
                var realIndex = GetIndex(meshVertices, pos);
                polygonIndices[i] = realIndex;
            }
            indices.AddRange(polygonIndices);

            meshIndices = indices.ToArray();

#if false
            var mesh = new LargeMesh(meshVertices, null, meshIndices);
            var obj = new GameObject("mesh");
            obj.AddComponent<MeshRenderer>();
            var filter = obj.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh.Mesh;
#endif
        }

        static int GetIndex(Vector3[] vertices, Vector3 pos)
        {
            for (int i = 0; i < vertices.Length; ++i)
            {
                if (Helper.Approximately(vertices[i].x, pos.x) &&
                    Helper.Approximately(vertices[i].y, pos.y) &&
                    Helper.Approximately(vertices[i].z, pos.z))
                {
                    return i;
                }
            }
            Debug.Assert(false);
            return -1;
        }
    }
}
