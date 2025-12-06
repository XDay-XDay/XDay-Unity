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
using XDay.WorldAPI.Attribute;

namespace XDay.WorldAPI.Fog
{
    [Flags]
    public enum FogDataType : byte
    {
        Server = 1,
        Client = 2,
        All = Server | Client,
    }

    internal interface IFogSystemImpl
    {
        event Action<bool> EventFogStateChange;
        int HorizontalResolution { get; }
        int VerticalResolution { get; }

        void OnDestroy();
        void ResetFog();
        void BeginBatchOpen();
        void EndBatchOpen();
        void OpenCircle(FogDataType type, int minX, int minY, int maxX, int maxY, bool inner);
        void OpenRectangle(FogDataType type, int minX, int minY, int maxX, int maxY);
        bool IsOpen(int x, int y);
        bool IsUnlocked(int x, int y);
    }

    internal partial class FogSystemImpl : IFogSystemImpl
    {
        public event Action<bool> EventFogStateChange;
        public int HorizontalResolution => m_HorizontalResolution;
        public int VerticalResolution => m_VerticalResolution;

        public FogSystemImpl(FogSystem fog, int horizontalResolution, int verticalResolution, byte[] data)
        {
            m_Fog = fog;
            m_HorizontalResolution = horizontalResolution;
            m_VerticalResolution = verticalResolution;
            m_FogData = data.Clone() as byte[];
            for (var i = 0; i < m_FogData.Length; ++i)
            {
                if (m_FogData[i] == 1)
                {
                    m_FogData[i] = (byte)FogDataType.Client;
                }
            }
        }

        public void OnDestroy()
        {
        }

        public bool IsUnlocked(int x, int y)
        {
            if (IsInObstacle(x, y))
            {
                return false;
            }

            return IsOpen(x, y);
        }

        public void ResetFog()
        {
            var n = m_FogData.Length;
            for (var i = 0; i < n; ++i)
            {
                m_FogData[i] = 0;
            }
            EventFogStateChange.Invoke(true);
        }

        public void BeginBatchOpen()
        {
            m_BatchMode = true;
        }

        public void EndBatchOpen()
        {
            m_BatchMode = false;
            EventFogStateChange?.Invoke(false);
        }

        public void OpenCircle(FogDataType type, int minX, int minY, int maxX, int maxY, bool inner)
        {
            var circumCenterX = (minX + maxX) / 2.0f;
            var circumCenterY = (minY + maxY) / 2.0f;
            var circumRadius = Mathf.Sqrt((maxX - minX) * (maxX - minX) + (maxY - minY) * (maxY - minY)) / 2.0f;
            var incircleRadius = Mathf.Min(maxX - minX, maxY - minY) / 2.0f;

            List<Vector2Int> points;
            if (inner)
            {
                points = GetGridPointsInCircle(circumCenterX, circumCenterY, incircleRadius);
            }
            else
            {
                points = GetGridPointsInCircle(circumCenterX, circumCenterY, circumRadius);
            }

            foreach (var coord in points)
            {
                var x = coord.x;
                var y = coord.y;
                if (x >= 0 && x < m_HorizontalResolution &&
                    y >= 0 && y < m_HorizontalResolution)
                {
                    var idx = y * m_HorizontalResolution + x;
                    m_FogData[idx] |= (byte)type;
                }
            }

            if (points.Count > 0 && !m_BatchMode)
            {
                EventFogStateChange.Invoke(false);
            }
        }

        public void OpenRectangle(FogDataType type, int minX, int minY, int maxX, int maxY)
        {
            var changed = false;
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    if (x >= 0 && x < m_HorizontalResolution &&
                        y >= 0 && y < m_HorizontalResolution)
                    {
                        changed = true;
                        var idx = y * m_HorizontalResolution + x;
                        m_FogData[idx] |= (byte)type;
                    }
                }
            }

            if (changed && !m_BatchMode)
            {
                EventFogStateChange.Invoke(false);
            }
        }

        public bool IsOpen(int x, int y)
        {
            if (x >= 0 && x < m_HorizontalResolution &&
                y >= 0 && y < m_HorizontalResolution)
            {
                return (m_FogData[y * m_HorizontalResolution + x] & (int)FogDataType.All) != 0;
            }
            return false;
        }

        private List<Vector2Int> GetGridPointsInCircle(float centerX, float centerY, float radius)
        {
            var points = new List<Vector2Int>();
            var minX = Mathf.FloorToInt(centerX - radius);
            var maxX = Mathf.CeilToInt(centerX + radius);
            var minY = Mathf.FloorToInt(centerY - radius);
            var maxY = Mathf.CeilToInt(centerY + radius);
            var radiusSquared = radius * radius;
            var cx = centerX;
            var cy = centerY;

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var dx = x - cx;
                    var dy = y - cy;
                    var distSq = dx * dx + dy * dy;
                    if (distSq <= radiusSquared)
                    {
                        points.Add(new Vector2Int(x, y));
                    }
                }
            }

            return points;
        }

        private bool IsInObstacle(int x, int y)
        {
            m_ObstacleLayer ??= m_Fog.World.QueryPlugin<IAttributeSystem>().GetLayer("Obstacle");
            if (m_ObstacleLayer == null)
            {
                return false;
            }
            return m_ObstacleLayer.GetValue(x, y) != 0;
        }

        private bool m_BatchMode = false;
        private readonly int m_HorizontalResolution;
        private readonly int m_VerticalResolution;
        private readonly byte[] m_FogData;
        private readonly FogSystem m_Fog;
        private IAttributeSystemLayer m_ObstacleLayer;
    }
}
