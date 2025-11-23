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
using System.Collections.Generic;
using UnityEngine;
using XDay.NavigationAPI;

namespace XDay.WorldAPI.House
{
    internal class HouseData : IGridData, IHouse
    {
        public int ConfigID => m_ConfigID;
        public Vector3 Position => m_Position;
        public string PrefabPath => m_PrefabPath;
        public List<IInteractivePoint> InteractivePoints => m_InteractivePoints;
        public List<Teleporter> Teleporters => m_Teleporters;
        public int HorizontalGridCount => m_HorizontalGridCount;
        public int VerticalGridCount => m_VerticalGridCount;
        public float GridHeight => m_GridHeight;
        public float GridSize => m_GridSize;
        public Bounds WorldBounds => m_WorldBounds;
        public string Name => m_Name;
        public bool[] Walkable => m_Walkable;

        public HouseData(int configID,
            string name,
            int horizontalGridCount,
            int verticalGridCount,
            float gridSize,
            float gridHeight,
            Bounds worldBounds,
            Vector3 position,
            string prefabPath,
            List<IInteractivePoint> interactivePoints,
            List<Teleporter> teleporters,
            bool[] walkable)
        {
            m_ConfigID = configID;
            m_Name = name;
            m_Position = position;
            m_PrefabPath = prefabPath;
            m_HorizontalGridCount = horizontalGridCount;
            m_VerticalGridCount = verticalGridCount;
            m_GridSize = gridSize;
            m_GridHeight = gridHeight;
            m_InteractivePoints = interactivePoints;
            m_Teleporters = teleporters;
            m_Walkable = walkable;
            m_WorldBounds = worldBounds;

            foreach (var teleporter in teleporters)
            {
                teleporter.SetHouse(this);
            }
        }

        public void Init(ITaskSystem taskSystem)
        {
            m_PathFinder = IGridBasedPathFinder.Create(taskSystem, this, m_NeighbourCoordinateOffsets.Length);
        }

        public bool IsTeleporter(int x, int y)
        {
            return false;
        }

        public Vector2Int GetConnectedTeleporterCoordinate(int x, int y)
        {
            return new Vector2Int(x, y);
        }

        public bool SetTeleporterStateWithCheck(int configID, bool on)
        {
            foreach (var teleporter in m_Teleporters)
            {
                if (teleporter.ConfigID == configID)
                {
                    teleporter.Enabled = on;
                    return true;
                }
            }
            return false;
        }

        public void SetTeleporterState(int configID, bool on)
        {
            foreach (var teleporter in m_Teleporters)
            {
                if (teleporter.ConfigID == configID)
                {
                    teleporter.Enabled = on;
                    return;
                }
            }
        }

        public bool GetTeleporterStateWithCheck(int configID, out bool on)
        {
            foreach (var teleporter in m_Teleporters)
            {
                if (teleporter.ConfigID == configID)
                {
                    on = teleporter.Enabled;
                    return true;
                }
            }
            Debug.LogError($"获取传送点{configID}状态失败");
            on = false;
            return false;
        }

        public bool GetTeleporterState(int configID)
        {
            foreach (var teleporter in m_Teleporters)
            {
                if (teleporter.ConfigID == configID)
                {
                    return teleporter.Enabled;
                }
            }
            return false;
        }

        public float GetGridCost(int x, int y, Func<int, float> costOverride = null)
        {
            return 1.0f;
        }

        public bool IsWalkable(int x, int y)
        {
            if (x >= 0 && x < m_HorizontalGridCount &&
                y >= 0 && y < m_VerticalGridCount)
            {
                return m_Walkable[y * m_HorizontalGridCount + x];
            }
            return false;
        }

        public Vector2Int GetNeighbourCoordinate(int selfX, int selfY, int neighbourIndex)
        {
            return new Vector2Int(m_NeighbourCoordinateOffsets[neighbourIndex].x + selfX, m_NeighbourCoordinateOffsets[neighbourIndex].y + selfY);
        }

        public Vector3 CoordinateToGridCenterPosition(int x, int y)
        {
            var localX = m_GridSize * (x + 0.5f);
            var localZ = m_GridSize * (y + 0.5f);
            return m_WorldBounds.min + new Vector3(localX, 0, localZ);
        }

        public Vector2Int PositionToCoordinate(Vector3 worldPos)
        {
            var localPosition = worldPos - m_WorldBounds.min;
            var xCoord = Mathf.FloorToInt(localPosition.x / m_GridSize);
            var yCoord = Mathf.FloorToInt(localPosition.z / m_GridSize);
            return new Vector2Int(xCoord, yCoord);
        }

        public Vector2Int FindNearestWalkableCoordinate(int x, int y, Vector2Int referencePoint, int searchDistance)
        {
            for (var distance = 1; distance <= searchDistance; ++distance)
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

        public Vector2Int FindNearestWalkableCoordinate(int x, int y, int searchDistance)
        {
            for (var distance = 1; distance <= searchDistance; ++distance)
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

        public bool Contains(Vector3 position)
        {
            position.y += 0.2f;
            return m_WorldBounds.Contains(position);
        }

        public void FindPath(Vector3 start, Vector3 end, List<Vector3> path)
        {
            m_PathFinder.CalculatePath(start, end, path);
        }

        public Vector3 GetLeftTeleporterPosition()
        {
            if (m_Teleporters.Count == 0)
            {
                return Vector3.zero;
            }
            return m_Teleporters[0].WorldPosition;
        }

        public Vector3 GetRightTeleporterPosition()
        {
            if (m_Teleporters.Count == 0)
            {
                return Vector3.zero;
            }
            return m_Teleporters[1].WorldPosition;
        }

        public Vector3 GetRandomWalkablePosition(Vector3 curPos)
        {
            var x = UnityEngine.Random.Range(m_WorldBounds.min.x, m_WorldBounds.max.x);
            var z = UnityEngine.Random.Range(m_WorldBounds.min.z, m_WorldBounds.max.z);
            var size = m_WorldBounds.size;
            var searchDistance = Mathf.Max(Mathf.CeilToInt(size.x / m_GridSize), Mathf.CeilToInt(size.z / m_GridSize));
            var newCoord = PositionToCoordinate(new Vector3(x, 0, z));
            var ret = FindNearestWalkableCoordinate(newCoord.x, newCoord.y, searchDistance);
            return CoordinateToGridCenterPosition(ret.x, ret.y);
        }

        private readonly int m_ConfigID;
        private readonly string m_Name;
        private readonly int m_HorizontalGridCount;
        private readonly int m_VerticalGridCount;
        private readonly Bounds m_WorldBounds;
        private readonly bool[] m_Walkable;
        private readonly float m_GridSize;
        private readonly float m_GridHeight;
        private readonly Vector3 m_Position;
        private readonly string m_PrefabPath;
        private readonly List<IInteractivePoint> m_InteractivePoints = new();
        private readonly List<Teleporter> m_Teleporters = new();
        private IGridBasedPathFinder m_PathFinder;
        private readonly Vector2Int[] m_NeighbourCoordinateOffsets = new Vector2Int[]
        {
            new(-1, 0),
            new(1, 0),
            new(0, -1),
            new(0, 1),
            new(1, 1),
            new(-1, -1),
            new(-1, 1),
            new(1, -1),
        };
    }

    internal class InteractivePoint : IInteractivePoint
    {
        public int ConfigID => m_ConfigID;
        public Vector3 StartPosition => m_StartPosition;
        public Quaternion StartRotation => m_StartRotation;
        public Vector3 EndPosition => m_EndPosition;
        public Quaternion EndRotation => m_EndRotation;

        public InteractivePoint(int configID, Vector3 startPosition, Quaternion startRotation, Vector3 endPosition, Quaternion endRotation)
        {
            m_ConfigID = configID;
            m_StartPosition = startPosition;
            m_StartRotation = startRotation;
            m_EndPosition = endPosition;
            m_EndRotation = endRotation;
        }

        private readonly int m_ConfigID;
        private Vector3 m_StartPosition;
        private Vector3 m_EndPosition;
        private Quaternion m_StartRotation;
        private Quaternion m_EndRotation;
    }

    internal class Teleporter
    {
        public int ConfigID => m_ConfigID;
        public int ConnectedID => m_ConnectedID;
        public Vector3 WorldPosition => m_WorldPosition;
        public string Name => m_Name;
        public HouseData House => m_House;
        public bool Enabled { get => m_Enabled; set => m_Enabled = value; }

        public Teleporter(int configID, int connectedID, string name, Vector3 position, bool enabled)
        {
            m_ConfigID = configID;
            m_ConnectedID = connectedID;
            m_Name = name;
            m_WorldPosition = position;
            m_Enabled = enabled;
        }

        internal void SetHouse(HouseData house)
        {
            m_House = house;
        }

        private readonly int m_ConfigID;
        private readonly int m_ConnectedID;
        private readonly string m_Name;
        private Vector3 m_WorldPosition;
        private HouseData m_House;
        private bool m_Enabled;
    }
}
