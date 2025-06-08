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

namespace XDay
{
    public class Grid
    {
        public Grid(FixedVector2 min, FixedVector2 max, FixedPoint gridSize)
        {
            m_Min = min;
            m_GridSize = gridSize;

            m_XCellCount = FixedMath.FloorToInt((max.X - min.X) / gridSize).IntValue;
            m_YCellCount = FixedMath.FloorToInt((max.Y - min.Y) / gridSize).IntValue;
            m_Cells = new Cell[m_XCellCount * m_YCellCount];
            for (var i = 0; i < m_Cells.Length; ++i)
            {
                m_Cells[i] = new();
            }
        }

        public void GetPotentialColliders(Rigidbody body, List<Rigidbody> result)
        {
            var pos = body.Position;
            var center = PositionToCoord(pos.X, pos.Y);
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
                        result.AddRange(cell.Rigidbodies);
                    }
                }
            }
        }

        public void UpdateRigidbody(FixedVector2 oldPos, Rigidbody body)
        {
            //注意算法没有考虑到单位大小变化的情况
            Vector2Int oldCellCoord = PositionToCoord(oldPos.X, oldPos.Y);
            var pos = body.Position;
            Vector2Int newCellCoord = PositionToCoord(pos.X, pos.Y);
            if (oldCellCoord != newCellCoord)
            {
                RemoveFromCell(ref oldCellCoord, body);
                var newCell = GetCell(newCellCoord.x, newCellCoord.y);
                newCell?.Rigidbodies.Add(body);
            }
        }

        public void RemoveRigidbody(Rigidbody body)
        {
            var pos = body.Position;
            Vector2Int oldCellCoord = PositionToCoord(pos.X, pos.Y);
            RemoveFromCell(ref oldCellCoord, body);
        }

        private void RemoveFromCell(ref Vector2Int coord, Rigidbody body)
        {
            var oldCell = GetCell(coord.x, coord.y);
            if (oldCell == null)
            {
                return;
            }
            var oldList = oldCell.Rigidbodies;
            var idx = oldList.IndexOf(body);
            if (idx >= 0)
            {
                oldList[idx] = oldList[^1];
                oldList.RemoveAt(oldList.Count - 1);
            }
        }

        private Vector2Int PositionToCoord(FixedPoint x, FixedPoint y)
        {
            return new Vector2Int(
                FixedMath.FloorToInt((x - m_Min.X) / m_GridSize).IntValue,
                FixedMath.FloorToInt((y - m_Min.Y) / m_GridSize).IntValue
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

        private FixedVector2 m_Min;
        private FixedPoint m_GridSize;
        private int m_XCellCount;
        private int m_YCellCount;
        private Cell[] m_Cells;

        public class Cell
        {
            public List<Rigidbody> Rigidbodies = new();
        }
    }
}
