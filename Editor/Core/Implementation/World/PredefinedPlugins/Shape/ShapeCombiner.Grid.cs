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

namespace XDay.WorldAPI.Shape.Editor
{
    internal partial class ShapeCombiner
    {
        private class Vertex
        {
            public int Index;
            public ShapeObject Shape;
            public Vector3 Position;
        }

        private class Overlap
        {
            public HashSet<Vertex> Vertices = new();
        }

        private class Grid
        {
            public Grid(Vector2 min, Vector2 max, float gridSize)
            {
                m_Min = min;
                m_GridSize = gridSize;

                m_XCellCount = Mathf.CeilToInt((max.x - min.x) / gridSize);
                m_YCellCount = Mathf.CeilToInt((max.y - min.y) / gridSize);
                m_Cells = new Cell[m_XCellCount * m_YCellCount];
                for (var i = 0; i < m_Cells.Length; ++i)
                {
                    m_Cells[i] = new() { X = i % m_XCellCount, Y = i / m_YCellCount };
                }
            }

            public void GetPotentialCollisions(Vertex vertex, List<Vertex> collisions)
            {
                var pos = vertex.Position;
                var center = PositionToCoord(pos.x, pos.z);
                var minX = center.x - 1;
                var minY = center.y - 1;
                var maxX = center.x + 1;
                var maxY = center.y + 1;
                for (var y = minY; y <= maxY; ++y)
                {
                    for (var x = minX; x <= maxX; ++x)
                    {
                        var cell = GetCell(x, y);
                        if (cell != null)
                        {
                            collisions.AddRange(cell.Vertices);
                        }
                    }
                }
            }

            public void Add(Vertex vertex)
            {
                Vector2Int coord = PositionToCoord(vertex.Position.x, vertex.Position.z);
                var newCell = GetCell(coord.x, coord.y);
                if (newCell != null)
                {
                    newCell.Vertices.Add(vertex);
                }
                else
                {
                    Debug.LogError($"{vertex.Shape.Name}的顶点{vertex.Index}超出ShapeSystem边界,无法合并!");
                }
            }

            private Vector2Int PositionToCoord(float x, float y)
            {
                return new Vector2Int(
                    Mathf.FloorToInt((x - m_Min.x) / m_GridSize),
                    Mathf.FloorToInt((y - m_Min.y) / m_GridSize)
                    );
            }

            private Cell GetCell(int x, int y)
            {
                if (x >= 0 && x < m_XCellCount &&
                    y >= 0 && y < m_YCellCount)
                {
                    return m_Cells[y * m_XCellCount + x];
                }
                return null;
            }

            internal List<Overlap> FindOverlappedVertices(float radius)
            {
                List<Vertex> temp = new();
                List<Overlap> ret = new();
                foreach (var cell in m_Cells)
                {
                    var overlaps = cell.FindOverlaps(radius, temp);
                    if (overlaps != null)
                    {
                        ret.AddRange(overlaps);
                    }
                }
                return ret;
            }

            private Vector2 m_Min;
            private float m_GridSize;
            private int m_XCellCount;
            private int m_YCellCount;
            private Cell[] m_Cells;

            public class Cell
            {
                public List<Vertex> Vertices = new();
                public int X;
                public int Y;

                internal List<Overlap> FindOverlaps(float radius, List<Vertex> otherVertices)
                {
                    otherVertices.AddRange(Vertices);
                    List<Overlap> ret = new();
                    for (var i = 0; i < otherVertices.Count; i++)
                    {
                        for (var j = i + 1; j < otherVertices.Count; j++)
                        {
                            if (Collide(otherVertices[i], otherVertices[j], radius))
                            {
                                AddToOverlap(ret, otherVertices[i], otherVertices[j]);
                            }
                        }
                    }

                    return ret;
                }

                private void AddToOverlap(List<Overlap> overlaps, Vertex vertex1, Vertex vertex2)
                {
                    foreach (var overlap in overlaps)
                    {
                        foreach (var vert in overlap.Vertices)
                        {
                            if (vert == vertex1 || vert == vertex2)
                            {
                                if (!overlap.Vertices.Contains(vertex1))
                                {
                                    overlap.Vertices.Add(vertex1);
                                }
                                if (!overlap.Vertices.Contains(vertex2))
                                {
                                    overlap.Vertices.Add(vertex2);
                                }
                                return;
                            }
                        }
                    }

                    var newOverlap = new Overlap();
                    newOverlap.Vertices.Add(vertex1);
                    newOverlap.Vertices.Add(vertex2);
                    overlaps.Add(newOverlap);
                }

                private bool Collide(Vertex vertex1, Vertex vertex2, float radius)
                {
                    if ((vertex1.Position - vertex2.Position).sqrMagnitude <= radius * radius)
                    {
                        return true;
                    }
                    return false;
                }
            }
        }
    }
}
