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

namespace XDay.UtilityAPI
{
    public class MeshDataCombiner
    {
        public MeshDataCombiner(float distanceError, float gridSize)
        {
            m_GridSize = gridSize;
            m_DistanceError = distanceError;
        }

        public void AddData(Vector3[] vertices, int[] indices, Matrix4x4 transform)
        {
            if (vertices != null && indices != null)
            {
                m_Meshes.Add(new MeshData(vertices, indices, transform));
            }
        }

        public void AddData(Vector3[] vertices, int[] indices)
        {
            if (vertices != null && indices != null)
            {
                m_Meshes.Add(new MeshData(vertices, indices));
            }
        }

        public void Combine(out Vector3[] vertices, out int[] indices)
        {
            vertices = null;
            indices = null;
            int meshCount = m_Meshes.Count;
            if (meshCount == 0)
            {
                return;
            }

            for (int i = 0; i < meshCount; ++i)
            {
                m_Meshes[i].Transform();
            }

            m_EmptyBounds = true;
            CalculateBounds();

            if (m_EmptyBounds == false)
            {
                m_HorizontalGridCount = Mathf.CeilToInt((m_MaxX - m_MinX) / m_GridSize) + 1;
                m_VerticalGridCount = Mathf.CeilToInt((m_MaxZ - m_MinZ) / m_GridSize) + 1;

                m_Grids = new Grid[m_VerticalGridCount, m_HorizontalGridCount];
                for (int i = 0; i < m_VerticalGridCount; ++i)
                {
                    for (int j = 0; j < m_HorizontalGridCount; ++j)
                    {
                        m_Grids[i, j] = new Grid();
                    }
                }

                int n = m_Meshes.Count;
                int totalIndexCount = GetTotalIndexCount();
                indices = new int[totalIndexCount];
                int offset = 0;
                for (int i = 0; i < n; ++i)
                {
                    if (m_Meshes[i].Indices != null)
                    {
                        var mi = m_Meshes[i].Indices;
                        var mv = m_Meshes[i].TransformedVertices;
                        for (int j = 0; j < mi.Length; ++j)
                        {
                            var combinedIndex = GetVertexIndex(mv[mi[j]]);
                            indices[offset] = combinedIndex;
                            ++offset;
                        }
                    }
                }

                vertices = m_CombinedVertices.ToArray();
            }
        }

        private void CalculateBounds()
        {
            m_MinX = float.MaxValue;
            m_MinZ = float.MaxValue;
            m_MaxX = float.MinValue;
            m_MaxZ = float.MinValue;
            foreach (var mesh in m_Meshes)
            {
                foreach (var v in mesh.TransformedVertices)
                {
                    m_EmptyBounds = false;
                    if (v.x > m_MaxX)
                    {
                        m_MaxX = v.x;
                    }
                    if (v.z > m_MaxZ)
                    {
                        m_MaxZ = v.z;
                    }
                    if (v.x < m_MinX)
                    {
                        m_MinX = v.x;
                    }
                    if (v.z < m_MinZ)
                    {
                        m_MinZ = v.z;
                    }
                }
            }
        }

        private int GetVertexIndex(Vector3 pos)
        {
            int index = SearchVertexIndex(pos);
            if (index < 0)
            {
                m_CombinedVertices.Add(pos);
                index = m_CombinedVertices.Count - 1;
                AddToGrid(pos, index);
            }

            return index;
        }

        private void AddToGrid(Vector3 pos, int index)
        {
            Vector2Int coordinate = GetGridCoordinate(pos);
            m_Grids[coordinate.y, coordinate.x].Add(pos, index);
        }

        private int SearchVertexIndex(Vector3 pos)
        {
            Vector2Int coordinate = GetGridCoordinate(pos);
            int searchMinX = coordinate.x - 1;
            int searchMinY = coordinate.y - 1;
            int searchMaxX = coordinate.x + 1;
            int searchMaxY = coordinate.y + 1;

            for (int i = searchMinY; i <= searchMaxY; ++i)
            {
                for (int j = searchMinX; j <= searchMaxX; ++j)
                {
                    if (i >= 0 && j >= 0 && i < m_VerticalGridCount && j < m_HorizontalGridCount)
                    {
                        var grid = m_Grids[i, j];
                        int index = grid.Search(pos, m_DistanceError);
                        if (index >= 0)
                        {
                            return index;
                        }
                    }
                }
            }
            return -1;
        }

        private Vector2Int GetGridCoordinate(Vector3 pos)
        {
            int x = Mathf.FloorToInt((pos.x - m_MinX) / m_GridSize);
            int y = Mathf.FloorToInt((pos.z - m_MinZ) / m_GridSize);
            return new Vector2Int(x, y);
        }

        private int GetTotalIndexCount()
        {
            int n = 0;
            for (int i = 0; i < m_Meshes.Count; ++i)
            {
                if (m_Meshes[i].Indices != null)
                {
                    n += m_Meshes[i].Indices.Length;
                }
            }
            return n;
        }

        private List<Vector3> m_CombinedVertices = new List<Vector3>();
        private List<MeshData> m_Meshes = new List<MeshData>();
        private float m_MinX;
        private float m_MinZ;
        private float m_MaxX;
        private float m_MaxZ;
        private Grid[,] m_Grids;
        private int m_HorizontalGridCount;
        private int m_VerticalGridCount;
        private float m_GridSize;
        private float m_DistanceError;
        private bool m_EmptyBounds = true;

        private class MeshData
        {
            public Vector3[] Vertices;
            public int[] Indices;
            public Matrix4x4 TransformMatrix;
            public Vector3[] TransformedVertices;

            public MeshData(Vector3[] vertices, int[] indices, Matrix4x4 transform)
            {
                Vertices = vertices;
                Indices = indices;
                TransformMatrix = transform;
                TransformedVertices = vertices;
                m_UseTransform = true;
            }

            public MeshData(Vector3[] vertices, int[] indices)
            {
                Vertices = vertices;
                Indices = indices;
                m_UseTransform = false;
            }

            public void Transform()
            {
                if (m_UseTransform)
                {
                    TransformedVertices = new Vector3[Vertices.Length];
                    int n = Vertices.Length;
                    for (int i = 0; i < n; ++i)
                    {
                        TransformedVertices[i] = TransformMatrix.MultiplyPoint(Vertices[i]);
                    }
                }
                else
                {
                    TransformedVertices = Vertices;
                }
            }

            private readonly bool m_UseTransform;
        }

        private class Vertex
        {
            public int VertexIndex;
            public Vector3 Position;

            public Vertex(int vertexIndex, Vector3 position)
            {
                VertexIndex = vertexIndex;
                Position = position;
            }
        }

        private class Grid
        {
            public List<Vertex> Vertices = new List<Vertex>();

            public void Add(Vector3 pos, int index)
            {
                Vertices.Add(new Vertex(index, pos));
            }

            public int Search(Vector3 pos, float distanceError)
            {
                foreach (var v in Vertices)
                {
                    if (Vector3.Distance(pos, v.Position) <= distanceError)
                    {
                        return v.VertexIndex;
                    }
                }
                return -1;
            }
        }
    }
}