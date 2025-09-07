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
using XDay.UtilityAPI;

namespace XDay.WorldAPI.House.Editor
{
    internal class HouseWalkableLayer : HouseBaseLayer
    {
        public override string TypeName => "HouseWalkableLayer";
        public bool[] Walkable => m_Walkable;

        public HouseWalkableLayer()
        {
        }

        public HouseWalkableLayer(int id, string name, int horizontalGridCount, int verticalGridCount, float gridSize)
            : base(id, name, horizontalGridCount, verticalGridCount, gridSize)
        {
            m_Walkable = new bool[verticalGridCount * horizontalGridCount];
            for (var i = 0; i < m_Walkable.Length; ++i)
            {
                m_Walkable[i] = true;
            }
        }

        protected override void OnResize(int horizontal, int vertical)
        {
            m_Walkable = new bool[horizontal * vertical];
        }

        public void SetWalkable(int x, int y, int width, int height, bool walkable)
        {
            var maxX = x + width - 1;
            var maxY = y + height - 1;
            for (var i = y; i <= maxY; ++i)
            {
                for (var j = x; j <= maxX; ++j)
                {
                    if (IsValidCoordinate(j, i))
                    {
                        m_Walkable[i * HorizontalGridCount + j] = walkable;
                    }
                }
            }
        }

        public bool IsWalkable(int x, int y, int width = 1, int height = 1)
        {
            var maxX = x + width - 1;
            var maxY = y + height - 1;
            for (var i = y; i <= maxY; ++i)
            {
                for (var j = x; j <= maxX; ++j)
                {
                    if (IsValidCoordinate(j, i))
                    {
                        if (!m_Walkable[i * HorizontalGridCount + j])
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public void SetAll(bool set)
        {
            for (var i = 0; i < m_Walkable.Length; ++i)
            {
                m_Walkable[i] = set;
            }
            UpdateColors();
        }

        public void CopyTo(HouseWalkableLayer layer)
        {
            layer.Resize(HorizontalGridCount, VerticalGridCount);

            for (var i = 0; i < m_Walkable.Length; ++i)
            {
                layer.m_Walkable[i] = m_Walkable[i];
            }
            layer.UpdateColors();
        }

        protected override Color GetColor(int x, int y)
        {
            return IsWalkable(x, y, 1, 1) ? Green : Black;
        }

        public override void EditorSerialize(ISerializer writer, string label, IObjectIDConverter translator)
        {
            writer.WriteInt32(m_WalkableLayerVersion, "WalkableLayer.Version");

            base.EditorSerialize(writer, label, translator);

            writer.WriteBooleanArray(m_Walkable, "Walkable");
        }

        public override void EditorDeserialize(IDeserializer reader, string label)
        {
            reader.ReadInt32("WalkableLayer.Version");

            base.EditorDeserialize(reader, label);

            m_Walkable = reader.ReadBooleanArray("Walkable");
        }

        public override void Rotate()
        {
            var arr2D = Helper.Rotate180(Helper.ToArray2D(m_Walkable, VerticalGridCount, HorizontalGridCount));
            m_Walkable = Helper.ToArray(arr2D, VerticalGridCount, HorizontalGridCount);

            UpdateColors();
        }

        private bool[] m_Walkable;
        private const int m_WalkableLayerVersion = 1;
        private static Color32 Green = new Color32(35, 232, 85, 255);
        private static Color32 Black = new Color32(30, 30, 30, 255);
    }
}
