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
    /// 设置格子是否可建造
    /// </summary>
    class BuildableLayer : BaseLayer
    {
        public BuildableLayer()
        {
        }

        public BuildableLayer(int id, string name, int horizontalGridCount, int verticalGridCount, float gridSize)
            : base(id, name, horizontalGridCount, verticalGridCount, gridSize)
        {
            m_IsBuildable = new bool[verticalGridCount * horizontalGridCount];

            for (var i = 0; i < verticalGridCount; ++i)
            {
                for (var j = 0; j < horizontalGridCount; ++j)
                {
                    m_IsBuildable[i * horizontalGridCount + j] = true;
                }
            }
        }

        public void SetBuildableState(int x, int y, int width, int height, bool buildable)
        {
            var maxX = x + width - 1;
            var maxY = y + height - 1;
            for (var i = y; i <= maxY; ++i)
            {
                for (var j = x; j <= maxX; ++j)
                {
                    if (IsValidCoordinate(j, i))
                    {
                        m_IsBuildable[i * HorizontalGridCount + j] = buildable;
                    }
                }
            }
        }

        public bool IsBuildable(int x, int y, int width, int height)
        {
            var maxX = x + width - 1;
            var maxY = y + height - 1;
            for (var i = y; i <= maxY; ++i)
            {
                for (var j = x; j <= maxX; ++j)
                {
                    if (IsValidCoordinate(j, i))
                    {
                        if (!m_IsBuildable[i * HorizontalGridCount + j])
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
            for (var i = 0; i < m_IsBuildable.Length; ++i)
            {
                m_IsBuildable[i] = set;
            }
            UpdateColors();
        }

        protected override Color GetColor(int x, int y)
        {
            return IsBuildable(x, y, 1, 1) ? CityEditorDefine.Green : CityEditorDefine.Black;
        }

        public override void EditorSerialize(ISerializer writer, string label, IObjectIDConverter translator)
        {
            writer.WriteInt32(m_BuilableLayerVersion, "BuildableLayer.Version");

            base.EditorSerialize(writer, label, translator);

            writer.WriteBooleanArray(m_IsBuildable, "Builable");
        }

        public override void EditorDeserialize(IDeserializer reader, string label)
        {
            reader.ReadInt32("BuildableLayer.Version");

            base.EditorDeserialize(reader, label);

            m_IsBuildable = reader.ReadBooleanArray("Builable");
        }

        public override string TypeName => "BuilableLayer";

        //tile是否可放置建筑
        bool[] m_IsBuildable;

        const int m_BuilableLayerVersion = 1;
    }
}
