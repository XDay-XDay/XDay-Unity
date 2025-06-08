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
using XDay.UtilityAPI;

namespace XDay.NavigationAPI
{
    [System.Flags]
    public enum PathFindingOption
    {
        None = 0,
        SimplifyPath = 1,
        WalkToNearestEdge = 2,
        XYZAxis = 4,
    }

    public class NavigationEdge
    {
        public int VertexIndex0;
        public int VertexIndex1;
        //removed later
        public int SharedTriangleIndex0 = -1;
        public int SharedTriangleIndex1 = -1;
    }

    public struct RaycastHitInfo
    {
        public Vector3 Intersection;
        public Vector3 Normal;
        public int TriangleIndex;
        public float Distance;
    }

    public class Triangle
    {
        public int Index;
        public int V0;
        public int V1;
        public int V2;
        public int EdgeIndex0 = -1;
        public int EdgeIndex1 = -1;
        public int EdgeIndex2 = -1;

        public Triangle(int index, int v0, int v1, int v2)
        {
            Index = index;
            V0 = v0;
            V1 = v1;
            V2 = v2;
        }

        public int GetNavigationEdgeCount()
        {
            if (EdgeIndex0 == -1)
            {
                return 0;
            }
            if (EdgeIndex1 == -1)
            {
                return 1;
            }
            if (EdgeIndex2 == -1)
            {
                return 2;
            }
            return 3;
        }

        public int GetNavigationEdgeIndex(int index)
        {
            if (index == 0)
            {
                return EdgeIndex0;
            }
            if (index == 1)
            {
                return EdgeIndex1;
            }
            if (index == 2)
            {
                return EdgeIndex2;
            }
            Debug.Assert(false, "error");
            return -1;
        }

        public void AddNavigationEdge(int edgeIndex)
        {
            if (EdgeIndex0 == -1)
            {
                EdgeIndex0 = edgeIndex;
            }
            else if (EdgeIndex1 == -1)
            {
                EdgeIndex1 = edgeIndex;
            }
            else if (EdgeIndex2 == -1)
            {
                EdgeIndex2 = edgeIndex;
            }
            else
            {
                Debug.Assert(false, "error");
            }
        }
    }

    internal struct EdgeRef
    {
        public int RefCount;
        public int TriangleIndex0;
        public int TriangleIndex1;

        public EdgeRef(int refCount, int triangleIndex0, int triangleIndex1)
        {
            RefCount = refCount;
            TriangleIndex0 = triangleIndex0;
            TriangleIndex1 = triangleIndex1;
        }
    }

    public partial class MyNavigationSystem
    {
        public List<NavigationEdge> NavigationEdges { get { return m_NavigationEdges; } }
        public Vector3[] Vertices { get { return m_Vertices; } }
        public int[] Indices { get { return m_Indices; } }

        public void Init(Vector3[] vertices, int[] indices)
        {
            Rect bounds = Helper.CalculateRect(vertices);
            float maxExt = Mathf.Max(bounds.width, bounds.height);
            MeshDataCombiner combiner = new MeshDataCombiner(0.001f, maxExt / 10.0f);
            combiner.AddData(vertices, indices);
            combiner.Combine(out var optimizedVertices, out var optimizedIndices);

            m_Vertices = optimizedVertices;
            m_Indices = optimizedIndices;

            BuildNavigationEdges();
            BuildEdgeSearchGraph();

            mDefaultContext = new ThreadContextData(m_NavigationEdges.Count);
        }

        public Vector3 GetVertex(int index)
        {
            return m_Vertices[index];
        }

        public void GetNavigationEdgeVertex(int edgeIndex, out Vector3 start, out Vector3 end)
        {
            var edge = m_NavigationEdges[edgeIndex];
            start = m_Vertices[edge.VertexIndex0];
            end = m_Vertices[edge.VertexIndex1];
        }

        public void GetNavigationEdgeLeftRightVertex(int edgeIndex, int leftVertexIndex, out Vector3 left, out Vector3 right)
        {
            var edge = m_NavigationEdges[edgeIndex];
            if (leftVertexIndex == 0)
            {
                left = m_Vertices[edge.VertexIndex0];
                right = m_Vertices[edge.VertexIndex1];
            }
            else
            {
                left = m_Vertices[edge.VertexIndex1];
                right = m_Vertices[edge.VertexIndex0];
            }
        }

        public Vector3 GetEdgeMiddlePoint(int edgeIndex)
        {
            GetNavigationEdgeVertex(edgeIndex, out var start, out var end);
            return (start + end) * 0.5f;
        }

        public void GetTriangleVertexPositions(int triangleIndex, out Vector3 v0, out Vector3 v1, out Vector3 v2)
        {
            var triangle = m_Triangles[triangleIndex];
            v0 = m_Vertices[triangle.V0];
            v1 = m_Vertices[triangle.V1];
            v2 = m_Vertices[triangle.V2];
        }

        public List<int> GetNeighbourNavigationEdges(Vector3 pointWorldPos, float edgeColliderSize)
        {
            for (int i = 0; i < m_NavigationEdges.Count; ++i)
            {
                GetNavigationEdgeVertex(i, out var start, out var end);
                bool inBetween = Helper.PointToSegmentDistance(pointWorldPos.ToVector2(), start.ToVector2(), end.ToVector2(), 0, out var nearestPoint, out float distance);
                if (inBetween && distance <= edgeColliderSize)
                {
                    return m_ConnectedNavigationEdges[i];
                }
            }
            return null;
        }

        public Triangle FindTriangleContains2D(Vector3 pointWorldPos)
        {
            for (int i = 0; i < m_Triangles.Count; ++i)
            {
                Vector3 v0 = m_Vertices[m_Triangles[i].V0];
                Vector3 v1 = m_Vertices[m_Triangles[i].V1];
                Vector3 v2 = m_Vertices[m_Triangles[i].V2];
                if (Helper.PointInTriangle2D(pointWorldPos.ToVector2(), v0.ToVector2(), v1.ToVector2(), v2.ToVector2()))
                {
                    return m_Triangles[i];
                }
            }
            return null;
        }

        public Triangle FindTriangleContains3D(Vector3 pointWorldPos, out Vector3 posInTriangle)
        {
            bool hit = Raycast(pointWorldPos + Vector3.up * 10000, Vector3.down, out var hitInfo);
            if (hit)
            {
                posInTriangle = hitInfo.Intersection;
                return m_Triangles[hitInfo.TriangleIndex];
            }

            posInTriangle = Vector3.zero;
            return null;
        }

        //todo,optimize,找到与射线相交的三角形
        public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHitInfo info)
        {
            float minDistance = float.MaxValue;
            info = new RaycastHitInfo();
            bool hit = false;
            for (int i = 0; i < m_Triangles.Count; ++i)
            {
                Vector3 v0 = m_Vertices[m_Triangles[i].V0];
                Vector3 v1 = m_Vertices[m_Triangles[i].V1];
                Vector3 v2 = m_Vertices[m_Triangles[i].V2];

                bool intersected = Helper.TriangleRayIntersectionTest3D(v0, v1, v2, origin, direction, out Vector3 barycentricCoordinate, out var normal, out float distance);
                if (intersected)
                {
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        hit = true;
                        info.Intersection = v0 * barycentricCoordinate.x + v1 * barycentricCoordinate.y + v2 * barycentricCoordinate.z;
                        info.Distance = distance;
                        info.Normal = normal;
                        info.TriangleIndex = i;
                    }
                }
            }

            return hit;
        }

        private void BuildNavigationEdges()
        {
            int triangleCount = m_Indices.Length / 3;
            m_Triangles = new List<Triangle>(triangleCount);
            for (int t = 0; t < triangleCount; ++t)
            {
                int v0 = m_Indices[t * 3];
                int v1 = m_Indices[t * 3 + 1];
                int v2 = m_Indices[t * 3 + 2];

                var triangle = new Triangle(t, v0, v1, v2);
                m_Triangles.Add(triangle);

                var edge0 = new Vector2Int(Mathf.Min(v0, v1), Mathf.Max(v0, v1));
                var edge1 = new Vector2Int(Mathf.Min(v1, v2), Mathf.Max(v1, v2));
                var edge2 = new Vector2Int(Mathf.Min(v2, v0), Mathf.Max(v2, v0));
                CheckEdge(edge0, t);
                CheckEdge(edge1, t);
                CheckEdge(edge2, t);
            }
        }

        private void CheckEdge(Vector2Int edge, int triangleIndex)
        {
            bool found = m_EdgeRef.TryGetValue(edge, out EdgeRef edgeRef);
            if (found)
            {
                Debug.Assert(edgeRef.RefCount == 1);
                edgeRef.RefCount = 2;
                edgeRef.TriangleIndex1 = triangleIndex;
                m_EdgeRef[edge] = edgeRef;

                var navigationEdge = new NavigationEdge();
                navigationEdge.VertexIndex0 = edge.x;
                navigationEdge.VertexIndex1 = edge.y;
                navigationEdge.SharedTriangleIndex0 = edgeRef.TriangleIndex0;
                navigationEdge.SharedTriangleIndex1 = edgeRef.TriangleIndex1;
                m_NavigationEdges.Add(navigationEdge);

                m_Triangles[edgeRef.TriangleIndex0].AddNavigationEdge(m_NavigationEdges.Count - 1);
                m_Triangles[edgeRef.TriangleIndex1].AddNavigationEdge(m_NavigationEdges.Count - 1);
            }
            else
            {
                edgeRef = new EdgeRef();
                edgeRef.RefCount = 1;
                edgeRef.TriangleIndex0 = triangleIndex;
                m_EdgeRef.Add(edge, edgeRef);
            }
        }

        private Vector3[] m_Vertices;
        private int[] m_Indices;
        private ThreadContextData mDefaultContext;
        private List<Triangle> m_Triangles = new();
        private List<NavigationEdge> m_NavigationEdges = new();
        private Dictionary<Vector2Int, EdgeRef> m_EdgeRef = new();
        private List<int>[] m_ConnectedNavigationEdges;
    }
}
