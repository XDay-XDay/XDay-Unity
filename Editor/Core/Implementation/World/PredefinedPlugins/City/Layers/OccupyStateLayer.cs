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

namespace XDay.WorldAPI.City.Editor
{
    /// <summary>
    /// 格子是否被占领
    /// </summary>
    class OccupyStateLayer : BaseLayer
    {
        public OccupyStateLayer()
        {
        }

        public OccupyStateLayer(int id, string name, int horizontalGridCount, int verticalGridCount, float gridSize)
            : base(id, name, horizontalGridCount, verticalGridCount, gridSize)
        {
            m_IsEmpty = new bool[verticalGridCount * horizontalGridCount];
            for (var i = 0; i < m_IsEmpty.Length; ++i)
            {
                m_IsEmpty[i] = true;
            }
        }

        public void SetEmpty(int x, int y, int width, int height, bool empty)
        {
            var maxX = x + width - 1;
            var maxY = y + height - 1;
            for (var i = y; i <= maxY; ++i)
            {
                for (var j = x; j <= maxX; ++j)
                {
                    if (IsValidCoordinate(j, i))
                    {
                        m_IsEmpty[i * HorizontalGridCount + j] = empty;
                    }
                }
            }
        }

        public bool IsEmpty(int x, int y, int width, int height)
        {
            var maxX = x + width - 1;
            var maxY = y + height - 1;
            for (var i = y; i <= maxY; ++i)
            {
                for (var j = x; j <= maxX; ++j)
                {
                    if (IsValidCoordinate(j, i))
                    {
                        if (!m_IsEmpty[i * HorizontalGridCount + j])
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        protected override Color GetColor(int x, int y)
        {
            return IsEmpty(x, y, 1, 1) ? CityEditorDefine.Green : CityEditorDefine.Black;
        }

        public override void EditorSerialize(ISerializer writer, string label, IObjectIDConverter translator)
        {
            writer.WriteInt32(m_OccupyStateLayerVersion, "OccupyStateLayer.Version");

            base.EditorSerialize(writer, label, translator);

            writer.WriteBooleanArray(m_IsEmpty, "Is Empty");
        }

        public override void EditorDeserialize(IDeserializer reader, string label)
        {
            reader.ReadInt32("OccupyStateLayer.Version");

            base.EditorDeserialize(reader, label);

            m_IsEmpty = reader.ReadBooleanArray("Is Empty");

            for (var i = 0; i < m_IsEmpty.Length; ++i)
            {
                m_IsEmpty[i] = true;
            }
        }

        public override string TypeName => "OccupyStateLayer";

        //tile是否是空地
        bool[] m_IsEmpty;

        const int m_OccupyStateLayerVersion = 1;
    }
}
