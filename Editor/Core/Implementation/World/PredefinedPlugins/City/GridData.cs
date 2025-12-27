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

using System;
using UnityEngine;
using XDay.NavigationAPI;

namespace XDay.UtilityAPI.Editor.Navigation
{
    public class GridData : IGridData
    {
        public int NeighbourCount => m_NeighbourCoordinateOffsets.Length;
        public int HorizontalGridCount => m_HorizontalGridCount;
        public int VerticalGridCount => m_VerticalGridCount;
        public ITaskSystem JobSystem => m_JobSystem;

        public GridData(int horizontalGridCount, int verticalGridCount, float gridSize, Vector3 gridPosition, float yRotation, float[] costs, int neighbourCount, ITaskSystem jobSystem)
        {
            m_HorizontalGridCount = horizontalGridCount;
            m_VerticalGridCount = verticalGridCount;
            m_GridSize = gridSize;
            m_GridPosition = gridPosition;
            m_GridRotation = Quaternion.Euler(0, yRotation, 0);
            m_GridRotationInverse = Quaternion.Inverse(m_GridRotation);
            m_Costs = costs;
            m_Width = m_GridSize * m_HorizontalGridCount;
            m_Height = m_GridSize * m_VerticalGridCount;
            m_JobSystem = jobSystem;

            if (neighbourCount == 8)
            {
                m_NeighbourCoordinateOffsets = new Vector2Int[]
                {
                    new Vector2Int(-1, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(0, -1),
                    new Vector2Int(0, 1),

                    new Vector2Int(1, 1),
                    new Vector2Int(-1, -1),
                    new Vector2Int(-1, 1),
                    new Vector2Int(1, -1),
                };
            }
            else if (neighbourCount == 4)
            {
                m_NeighbourCoordinateOffsets = new Vector2Int[]
                {
                    new Vector2Int(-1, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(0, -1),
                    new Vector2Int(0, 1),
                };
            }
            else
            {
                Debug.Assert(false);
            }
        }

        public void OnDestroy()
        {
        }

        public Vector3 CoordinateToGridCenterPosition(int x, int y)
        {
            var localX = m_GridSize * (x + 0.5f) - m_Width * 0.5f;
            var localZ = m_GridSize * (y + 0.5f) - m_Height * 0.5f;
            return m_GridPosition + m_GridRotation * new Vector3(localX, 0, localZ);
        }

        public Vector2Int PositionToCoordinate(Vector3 worldPos)
        {
            var localPosition = m_GridRotationInverse * (worldPos - m_GridPosition);

            var xCoord = Mathf.FloorToInt((localPosition.x + m_Width * 0.5f) / m_GridSize);
            var yCoord = Mathf.FloorToInt((localPosition.z + m_Height * 0.5f) / m_GridSize);
            return new Vector2Int(xCoord, yCoord);
        }

        public float GetGridCost(int x, int y, Func<int, float> costOverride = null)
        {
            return m_Costs[y * m_HorizontalGridCount + x];
        }

        public bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= m_HorizontalGridCount || y < 0 || y >= m_VerticalGridCount)
            {
                return false;
            }
            var idx = y * m_HorizontalGridCount + x;
            return m_Costs[idx] < BLOCK_COST_MIN;
        }

        public bool IsWalkableNoCheck(int x, int y)
        {
            return IsWalkable(x, y);
        }

        public Vector2Int GetNeighbourCoordinate(int x, int y, int index)
        {
            return new Vector2Int(m_NeighbourCoordinateOffsets[index].x + x, m_NeighbourCoordinateOffsets[index].y + y);
        }

        public Vector2Int FindNearestWalkableCoordinate(int x, int y, int maxTryDistance)
        {
            Debug.Assert(false, "todo");
            return new Vector2Int(x, y);
        }

        public Vector2Int FindNearestWalkableCoordinate(int x, int y, Vector2Int referencePoint, int maxTryDistance)
        {
            for (var distance = 1; distance <= maxTryDistance; ++distance)
            {
                var minX = x - distance;
                var maxX = x + distance;
                var minY = y - distance;
                var maxY = y + distance;
                for (var yy = minY; yy <= maxY; ++yy)
                {
                    for (var xx = minX; xx <= maxX; ++xx)
                    {
                        var dx = Mathf.Abs(xx - x);
                        var dy = Mathf.Abs(yy - y);
                        if (dx != distance && dy != distance)
                        {
                            continue;
                        }

                        if (IsWalkable(xx, yy))
                        {
                            return new Vector2Int(xx, yy);
                        }
                    }
                }
            }

            Debug.LogWarning($"FindNearestWalkableCoordinate failed! {x}_{y}");
            return new Vector2Int(x, y);
        }

        public bool IsTeleporter(int x, int y)
        {
            throw new System.NotImplementedException();
        }

        public Vector2Int GetConnectedTeleporterCoordinate(int x, int y)
        {
            throw new System.NotImplementedException();
        }

        public void SetTeleporterState(int id, bool enable)
        {
            throw new System.NotImplementedException();
        }

        public bool GetTeleporterState(int id)
        {
            throw new System.NotImplementedException();
        }

        private readonly int m_HorizontalGridCount;
        private readonly int m_VerticalGridCount;
        private readonly float m_GridSize;
        private readonly float m_Width;
        private readonly float m_Height;
        private Vector3 m_GridPosition;
        private Quaternion m_GridRotation;
        private Quaternion m_GridRotationInverse;
        private readonly float[] m_Costs;
        private ITaskSystem m_JobSystem;
        private static Vector2Int[] m_NeighbourCoordinateOffsets;
        public const float BLOCK_COST = 10000.0f;
        public const float BLOCK_COST_MIN = 9000.0f;
    }
}
