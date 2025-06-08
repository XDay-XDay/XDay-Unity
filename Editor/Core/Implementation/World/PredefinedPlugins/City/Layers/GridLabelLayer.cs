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
    class GridLabelLayer : BaseLayer
    {
        public GridLabelLayer()
        {
        }

        public GridLabelLayer(int id, string name, int horizontalGridCount, int verticalGridCount, float gridSize)
            : base(id, name, horizontalGridCount, verticalGridCount, gridSize)
        {
            m_GridLabelIDs = new int[verticalGridCount * horizontalGridCount];
        }

        protected override void OnInitialize()
        {
        }

        public void SetGridLabelID(int x, int y, int width, int height, int id)
        {
            var maxX = x + width - 1;
            var maxY = y + height - 1;
            for (var i = y; i <= maxY; ++i)
            {
                for (var j = x; j <= maxX; ++j)
                {
                    if (IsValidCoordinate(j, i))
                    {
                        var oldID = m_GridLabelIDs[i * HorizontalGridCount + j];
                        if (!m_Grid.IsRegionLocked(oldID))
                        {
                            m_GridLabelIDs[i * HorizontalGridCount + j] = id;
                        }
                    }
                }
            }
        }

        public int GetGridLabelID(int x, int y)
        {
            if (IsValidCoordinate(x, y))
            {
                return m_GridLabelIDs[y * HorizontalGridCount + x];
            }
            return 0;
        }

        public bool IsWalkable(int x, int y)
        {
            var id = GetGridLabelID(x, y);
            var label = m_Grid.GetGridLabel(id);
            if (label != null)
            {
                return label.Walkable;
            }
            //默认不可走
            return false;
        }

        public void Clear(int gridLabelID)
        {
            for (var i = 0; i < VerticalGridCount; ++i)
            {
                for (var j = 0; j < HorizontalGridCount; ++j)
                {
                    if (m_GridLabelIDs[i * HorizontalGridCount + j] == gridLabelID)
                    {
                        m_GridLabelIDs[i * HorizontalGridCount + j] = 0;
                    }
                }
            }
        }

        protected override Color GetColor(int x, int y)
        {
            var id = GetGridLabelID(x, y);

            var gridLabel = m_Grid.GetGridLabel(id);
            if (gridLabel != null)
            {
                return gridLabel.Color;
            }

            return CityEditorDefine.Black;
        }

        public override void EditorSerialize(ISerializer writer, string label, IObjectIDConverter translator)
        {
            writer.WriteInt32(m_GridLabelLayerVersion, "GridLabelLayer.Version");

            base.EditorSerialize(writer, label, translator);

            var regionIDs = new int[m_GridLabelIDs.Length];
            for (var i = 0; i < m_GridLabelIDs.Length; ++i)
            {
                regionIDs[i] = translator.Convert(m_GridLabelIDs[i]);
            }
            writer.WriteInt32Array(regionIDs, "GridLabel ID");
        }

        public override void EditorDeserialize(IDeserializer reader, string label)
        {
            reader.ReadInt32("GridLabelLayer.Version");

            base.EditorDeserialize(reader, label);

            m_GridLabelIDs = reader.ReadInt32Array("GridLabel ID");
        }

        public override string TypeName => "GridLabelLayer";

        int[] m_GridLabelIDs;
        
        const int m_GridLabelLayerVersion = 1;
    }
}
