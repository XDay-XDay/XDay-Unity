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
using UnityEngine.Scripting;

namespace XDay.WorldAPI.Tile
{
    [Preserve]
    public abstract partial class TileSystemBase : WorldPlugin
    {
        public override IPluginLODSystem LODSystem => m_LODSystem;
        public int XTileCount => m_XTileCount;
        public int YTileCount => m_YTileCount;
        public Vector3 Center => m_Center;
        public float TileWidth => m_TileWidth;
        public float TileHeight => m_TileHeight;
        public override Quaternion Rotation { get => m_Rotation; set => throw new System.NotImplementedException(); }
        public override string Name { get => m_Name; set => throw new System.NotImplementedException(); }

        public TileSystemBase()
        {
        }

        protected override void InitInternal()
        {
            m_Size.x = m_XTileCount * m_TileWidth;
            m_Size.y = m_YTileCount * m_TileHeight;
            m_Center = new(m_Origin.x + m_Size.x * 0.5f, 0, m_Origin.y + m_Size.y * 0.5f);
            m_CameraVisibleAreaUpdater = ICameraVisibleAreaUpdater.Create(World.CameraVisibleAreaCalculator);
        }

        public Vector3 CoordinateToLocalPosition(int x, int y)
        {
            return new Vector3(
                x * m_TileWidth - m_Size.x * 0.5f,
                0,
                y * m_TileHeight - m_Size.y * 0.5f);
        }

        public Vector2Int WorldPositionToCoordinate(float x, float z)
        {
            var pos = WorldPositionToLocal(x, z);
            return new Vector2Int(
                Mathf.FloorToInt((pos.x - m_Origin.x) / m_TileWidth),
                Mathf.FloorToInt((pos.z - m_Origin.y) / m_TileHeight));
        }

        protected RectInt CalculateCoordinateBounds(Rect rect)
        {
            var min = WorldPositionToCoordinate(rect.xMin, rect.yMin);
            var max = WorldPositionToCoordinate(rect.xMax, rect.yMax);
            return new RectInt(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        private Vector3 WorldPositionToLocal(float x, float z)
        {
            var center = Center;
            return Quaternion.Inverse(m_Rotation) * new Vector3(x - center.x, 0, z - center.z) + new Vector3(center.x, 0, center.z);
        }

        protected IPluginLODSystem m_LODSystem;
        protected Vector2 m_Size;
        protected int m_XTileCount;
        protected int m_YTileCount;
        protected Vector3 m_Center;
        protected float m_TileWidth;
        protected float m_TileHeight;
        protected Quaternion m_Rotation;
        protected string m_Name;
        protected ICameraVisibleAreaUpdater m_CameraVisibleAreaUpdater;
        protected Vector2 m_Origin;
    }
}

//XDay
