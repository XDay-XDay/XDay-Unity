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
using XDay.UtilityAPI.Math;

namespace XDay.UtilityAPI.Editor
{
    /// <summary>
    /// 生成方形的边界Mesh,目前UV有循环问题,需要用横向的纯色图
    /// </summary>
    public class RectangleBorderCreator
    {
        public class CreateInfo
        {
            public string Name;
            public float Width;
            public Material BorderMaterial;
            public Material MeshMaterial;
            public Transform Parent;
            public Vector3 Center;
        }

        public GameObject Generate(CreateInfo param, List<Vector3> outline, out int vertexCount, out int indexCount)
        {
            Helper.RemoveDuplicatedFast(outline);

            var innerOutline = PolygonHelper.ExpandPolygon(outline, -param.Width, true)[0];

            PolygonHelper.GetPolygonDifference(outline, innerOutline, out var noneHoles, out var holes);
            PolygonTriangulator.Triangulate(noneHoles[0], holes, new(), out var vertices, out var indices);

            Reorder(innerOutline, outline);

            var uvs = GetUVs(outline, innerOutline, vertices);

            //DrawDebugPolygon(outline);
            //DrawDebugPolygon(innerOutline);

            List<Vector3> localVertices = new();
            foreach (var pos in vertices)
            {
                localVertices.Add(pos - param.Center);
            }

            var mesh = new Mesh();
            mesh.SetVertices(localVertices);
            mesh.uv = uvs;
            mesh.triangles = indices;
            mesh.RecalculateBounds();
            vertexCount = mesh.vertexCount;
            indexCount = indices.Length;
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.GetComponent<MeshFilter>().mesh = mesh;
            obj.GetComponent<MeshRenderer>().sharedMaterial = param.BorderMaterial;
            Helper.DestroyComponent<UnityEngine.BoxCollider>(obj);
            obj.name = param.Name;
            obj.transform.position = param.Center;
            obj.transform.SetParent(param.Parent, true);
            return obj;
        }

        private void Reorder(List<Vector3> innerOutline, List<Vector3> outline)
        {
            List<Vector3> reorderedInner = new();
            List<Vector3> reorderedOutter = new();
            FindNearestPair(innerOutline, outline, out var nearestOutlineIndex, out var nearestInnerOutlineIndex);

            for (var i = 0; i < innerOutline.Count; ++i)
            {
                reorderedInner.Add(innerOutline[(nearestInnerOutlineIndex + i) % innerOutline.Count]);
            }

            for (var i = 0; i < outline.Count; ++i)
            {
                reorderedOutter.Add(outline[(nearestOutlineIndex + i) % outline.Count]);
            }

            innerOutline.Clear();
            innerOutline.AddRange(reorderedInner);

            outline.Clear();
            outline.AddRange(reorderedOutter);
        }

        private void FindNearestPair(List<Vector3> innerOutline, List<Vector3> outline, out int nearestOutlineIndex, out int nearestInnerOutlineIndex)
        {
            nearestOutlineIndex = -1;
            nearestInnerOutlineIndex = -1;
            var minDistance = float.MaxValue;
            for (var i = 0; i < outline.Count; ++i)
            {
                for (var j = 0; j < innerOutline.Count; ++j)
                {
                    var dis = Vector3.Distance(innerOutline[j], outline[i]);
                    if (dis < minDistance)
                    {
                        minDistance = dis;
                        nearestOutlineIndex = i;
                        nearestInnerOutlineIndex = j;
                    }
                }
            }
        }

        //private void DrawDebugPolygon(List<Vector3> outline)
        //{
        //    var objddd = new GameObject();
        //    var dp11 = objddd.AddComponent<DrawPolyLineInEditor>();
        //    dp11.SetVertices(outline);
        //}

        private Vector2[] GetUVs(List<Vector3> outter, List<Vector3> inner, Vector3[] vertices)
        {
            Vector2[] uvs = new Vector2[vertices.Length];

            var esp = 0.01f;
            var idx = 0;
            for (var i = 0; i < vertices.Length; ++i)
            {
                var vert = vertices[i];
                var outterIdx = Helper.IndexOf(outter, vert, esp);
                if (outterIdx >= 0)
                {
                    uvs[idx] = new Vector2(0, 0);
                }
                else
                {
                    uvs[idx] = new Vector2(0, 1);
                }
                ++idx;
            }

            return uvs;
        }
    }
}
