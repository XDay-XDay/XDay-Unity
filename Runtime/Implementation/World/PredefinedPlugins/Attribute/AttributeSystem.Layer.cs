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

namespace XDay.WorldAPI.Attribute
{
    internal partial class AttributeSystem
    {
        internal enum LayerType
        {
            //障碍物层,由AutoObstacle和用户手绘的格子障碍合并得来
            Obstacle,
            //用户定义，对编辑器无特殊意义
            UserDefined,
        }

        internal abstract class LayerBase
        {
            public string Name { get => m_Name; set => m_Name = value; }
            public int HorizontalGridCount => m_HorizontalGridCount;
            public int VerticalGridCount => m_VerticalGridCount;
            public float GridWidth => m_GridWidth;
            public float GridHeight => m_GridHeight;
            public Vector2 Origin => m_Origin;
            public LayerType Type { get => m_Type; set => m_Type = value; }

            public LayerBase()
            {
            }

            public LayerBase(string name, int horizontalGridCount, int verticalGridCount, float gridWidth, float gridHeight, Vector2 origin, LayerType type)
            {
                m_Name = name;
                m_HorizontalGridCount = horizontalGridCount;
                m_VerticalGridCount = verticalGridCount;
                m_GridWidth = gridWidth;
                m_GridHeight = gridHeight;
                m_Origin = origin;
                m_Type = type;
            }

            internal Vector2Int PositionToCoordinate(float x, float z)
            {
                var coordX = Mathf.FloorToInt((x - m_Origin.x) / m_GridWidth);
                var coordY = Mathf.FloorToInt((z - m_Origin.y) / m_GridHeight);
                return new Vector2Int(coordX, coordY);
            }

            internal Vector3 CoordinateToPosition(int x, int y)
            {
                return new Vector3(
                    x * m_GridWidth + m_Origin.x,
                    0,
                    y * m_GridHeight + m_Origin.y);
            }

            internal Vector3 CoordinateToCenterPosition(int x, int y)
            {
                return new Vector3(
                    (x + 0.5f) * m_GridWidth + m_Origin.x,
                    0,
                    (y + 0.5f) * m_GridHeight + m_Origin.y);
            }

            internal bool SizeEqual(LayerBase otherLayer)
            {
                return
                    Mathf.Approximately(m_GridWidth, otherLayer.GridWidth) &&
                    Mathf.Approximately(m_GridHeight, otherLayer.GridHeight) &&
                    Mathf.Approximately(m_HorizontalGridCount, otherLayer.HorizontalGridCount) &&
                    Mathf.Approximately(m_VerticalGridCount, otherLayer.VerticalGridCount);
            }

            [SerializeField]
            private string m_Name;
            [SerializeField]
            private float m_GridWidth;
            [SerializeField]
            private float m_GridHeight;
            [SerializeField]
            private int m_HorizontalGridCount;
            [SerializeField]
            private int m_VerticalGridCount;
            [SerializeField]
            private Vector2 m_Origin;
            [SerializeField]
            private LayerType m_Type = LayerType.UserDefined;
        }

        internal class Layer : LayerBase
        {
            public uint[] Data => m_Data;

            public Layer()
            {
            }

            public Layer(string name, int horizontalGridCount, int verticalGridCount, float gridWidth, float gridHeight, Vector2 origin, LayerType type, uint[] data) : base(name, horizontalGridCount, verticalGridCount, gridWidth, gridHeight, origin, type)
            {
                m_Data = data;
            }

            public void Set(int x, int y, uint type)
            {
                if (x >= 0 && x < HorizontalGridCount && y >= 0 && y < VerticalGridCount)
                {
                    var index = y * HorizontalGridCount + x;
                    m_Data[index] = type;
                }
            }

            public uint Get(int x, int y)
            {
                if (x >= 0 && x < HorizontalGridCount && y >= 0 && y < VerticalGridCount)
                {
                    return m_Data[y * HorizontalGridCount + x];
                }

                return default;
            }

            [SerializeField]
            private uint[] m_Data;
        }
    }
}
