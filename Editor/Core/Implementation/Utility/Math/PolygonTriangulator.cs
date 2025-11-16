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
using System.Collections.Generic;
using System.Linq;
using TriangleNet;
using TriangleNet.Geometry;
using TriangleNet.Smoothing;
using TriangleNet.Meshing;
using System;

namespace XDay.UtilityAPI.Editor
{
    public struct TriangulationOption
    {
        public TriangulationOption(TrianglePool pool = null, 
            bool useDelaunayTriangulation = false, 
            float minimumAngle = 30.0f, 
            float maximumArea = 0)
        {
            UseDelaunayTriangulation = useDelaunayTriangulation;
            MinimumAngle = minimumAngle;
            MaximumArea = maximumArea;
            Pool = pool;
        }

        public float MinimumAngle { get; set; }
        public float MaximumArea { get; set; }
        //为了多线程时减少内存消耗,多线程下每个线程给一个triangle pool
        public TrianglePool Pool { get; set; }
        public bool UseDelaunayTriangulation { get; set; }
    }

    public static class PolygonTriangulator
    {
        static PolygonTriangulator()
        {
            m_TriangleThreadPools = new TrianglePool[System.Environment.ProcessorCount];
            for (int i = 0; i < m_TriangleThreadPools.Length; ++i)
            {
                m_TriangleThreadPools[i] = new TrianglePool();
            }
        }

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

        public static void Triangulate(List<Vector3> polygon, List<List<Vector3>> holes, TriangulationOption option,
            out Vector3[] meshVertices, out int[] meshIndices)
        {
            Triangulate(new List<List<Vector3>>() { polygon }, holes, option, out meshVertices, out meshIndices);
        }

        public static void Triangulate(List<List<Vector3>> polygons, List<List<Vector3>> holes, TriangulationOption option, out Vector3[] meshVertices, out int[] meshIndices)
        {
            option.Pool ??= GetPool(0);

            holes = FilterHoles(holes);
            DoTriangulate(polygons, holes, option.UseDelaunayTriangulation, option.MinimumAngle, option.MaximumArea, option.Pool, out meshVertices, out meshIndices);
        }

        private static void DoTriangulate(List<List<Vector3>> polygons, List<List<Vector3>> holes, 
            bool conformingDelaunay, float minimumAngle, float maximumArea, TrianglePool pool, 
            out Vector3[] meshVertices, out int[] meshIndices)
        {
            meshVertices = null;
            meshIndices = null;
            if (polygons.Count == 0)
            {
                return;
            }

            Polygon poly = new();
            try
            {
                // Define Contour.
                if (polygons != null)
                {
                    for (int i = 0; i < polygons.Count; ++i)
                    {
                        var contour = ConvertPolygonToContour(polygons[i], 0);
                        if (contour != null)
                        {
                            poly.Add(contour);
                        }
                    }
                }
                // Add holes
                if (holes != null)
                {
                    for (int i = 0; i < holes.Count; ++i)
                    {
                        var hole = ConvertPolygonToContour(holes[i], 1);
                        if (hole != null)
                        {
                            poly.Add(hole, true);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.ToString());
            }

            TriangleNet.Mesh mesh = null;
            if (conformingDelaunay)
            {
                // Set quality and constraint options.
                var options = new ConstraintOptions() { ConformingDelaunay = true };
                var quality = new QualityOptions() { MinimumAngle = minimumAngle };

                // Generate mesh.
                var predicates = new RobustPredicates();
                var config = new Configuration()
                {
                    Predicates = () => predicates,
                    TrianglePool = () => pool.Restart()
                };

                var mesher = new GenericMesher(config);

                mesh = (TriangleNet.Mesh)mesher.Triangulate(poly, options, quality);

                var smoother = new SimpleSmoother();

                // Smooth mesh and re-apply quality options.
                smoother.Smooth(mesh);
                mesh.Refine(quality);

                // Calculate mesh quality
                //var statistic = new QualityMeasure();

                //statistic.Update(mesh);

                // Use the minimum triangle area for region refinement
                //double area = 1.75 * statistic.AreaMinimum;
                double area = maximumArea;

                foreach (var t in mesh.Triangles)
                {
                    // Set area constraint for all triangles in region 1
                    //if (t.Label == 1) t.Area = area;
                    t.Area = area;
                }

                // Use per triangle area constraint for next refinement
                quality.VariableArea = true;

                // Refine mesh to meet area constraint.
                mesh.Refine(quality);

                // Smooth once again.
                smoother.Smooth(mesh);

                GetDataFromMesh(mesh, out meshVertices, out meshIndices);
            }
            else
            {
                try
                {
                    Debug.Assert(pool != null);

                    var predicates = new RobustPredicates();
                    var config = new Configuration()
                    {
                        Predicates = () => predicates,
                        TrianglePool = () => pool.Restart()
                    };

                    var mesher = new TriangleNet.Meshing.GenericMesher(config);
                    mesh = (TriangleNet.Mesh)mesher.Triangulate(poly);
                    GetDataFromMesh(mesh, out meshVertices, out meshIndices);
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e.Message);
                }
            }
        }

        private static void GetDataFromMesh(TriangleNet.Mesh mesh, out Vector3[] meshVertices, out int[] meshIndices)
        {
            meshVertices = null;
            meshIndices = null;
            if (mesh != null)
            {
                var triangles = mesh.Triangles;
                Dictionary<Vector3Int, int> vMark = new Dictionary<Vector3Int, int>();
                List<Vector3Int> vertices = new List<Vector3Int>();
                List<int> indices = new List<int>();
                Vertex[] verts = mesh.Vertices.ToArray();
                int vCount = 0;
                int[] oneTriangle = new int[3];
                foreach (var triangle in triangles)
                {
                    for (int i = 2; i >= 0; --i)
                    {
                        var index = triangle.GetVertexID(i);
                        int x = (int)(verts[index].X * 1000);
                        int y = (int)(verts[index].Y * 1000);
                        Vector3Int v = new Vector3Int(x, 0, y);
                        int vIdx = 0;
                        if (!vMark.TryGetValue(v, out vIdx))
                        {
                            vIdx = vCount++;
                            vMark.Add(v, vIdx);
                            vertices.Add(v);
                        }

                        oneTriangle[i] = vIdx;
                    }

                    //check if triangle is valid
                    bool valid = true;
                    if (oneTriangle[0] == oneTriangle[1] || oneTriangle[0] == oneTriangle[2] || oneTriangle[1] == oneTriangle[2])
                    {
                        valid = false;
                    }
                    if (valid)
                    {
                        for (int i = 2; i >= 0; --i)
                        {
                            indices.Add(oneTriangle[i]);
                        }
                    }
                }

                meshVertices = new Vector3[vertices.Count];
                for (int i = 0; i < vertices.Count; ++i)
                {
                    meshVertices[i] = new Vector3(vertices[i].x / 1000.0f, 0, vertices[i].z / 1000.0f);
                }
                meshIndices = indices.ToArray();
            }
        }

        private static Contour ConvertPolygonToContour(List<Vector3> path, int label = 0)
        {
            if (path.Count > 0)
            {
                var points = new Vertex[path.Count];

                for (int i = 0; i < path.Count; ++i)
                {
                    points[i] = new Vertex(path[i].x, path[i].z);
                }

                return new TriangleNet.Geometry.Contour(points, label);
            }
            return null;
        }

        private static List<List<Vector3>> FilterHoles(List<List<Vector3>> holes)
        {
            List<List<Vector3>> validHoles = new();
            if (holes != null)
            {
                for (int i = 0; i < holes.Count; ++i)
                {
                    //这里删除重复点会有问题,暂时屏蔽
                    //var vertices = Helper.RemoveDuplicatedPoints(holes[i], 0.01f);
                    var vertices = holes[i];
                    if (vertices.Count >= 3)
                    {
                        validHoles.Add(vertices);
                    }
                }
            }
            return validHoles;
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

            //swap winding order
            var n = triangleIndices.Length / 3;
            for (var k = 0; k < n; ++k)
            {
                (triangleIndices[k * 3], triangleIndices[k * 3 + 2]) = (triangleIndices[k * 3 + 2], triangleIndices[k * 3]);
            }

            triangleVertices = new Vector3[mesh.Vertices.Count];
            foreach (var v in mesh.Vertices)
            {
                triangleVertices[v.ID] = new Vector3((float)v.X, 0, (float)v.Y);
            }
        }

        private static TrianglePool GetPool(int index)
        {
            return m_TriangleThreadPools[index];
        }

        private static TrianglePool[] m_TriangleThreadPools;
    }
}
