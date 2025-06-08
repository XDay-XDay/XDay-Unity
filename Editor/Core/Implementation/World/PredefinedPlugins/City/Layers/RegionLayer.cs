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

namespace XDay.WorldAPI.City.Editor
{
    class RegionLayer : BaseLayer
    {
        public RegionLayer()
        {
        }

        public RegionLayer(int id, string name, int horizontalGridCount, int verticalGridCount, float gridSize)
            : base(id, name, horizontalGridCount, verticalGridCount, gridSize)
        {
            m_RegionIDs = new int[verticalGridCount * horizontalGridCount];
        }

        public void SetRegionID(int x, int y, int width, int height, int regionID)
        {
            var maxX = x + width - 1;
            var maxY = y + height - 1;
            for (var i = y; i <= maxY; ++i)
            {
                for (var j = x; j <= maxX; ++j)
                {
                    if (IsValidCoordinate(j, i))
                    {
                        var oldRegionID = m_RegionIDs[i * HorizontalGridCount + j];
                        if (!m_Grid.IsRegionLocked(oldRegionID)) 
                        {
                            m_RegionIDs[i * HorizontalGridCount + j] = regionID;
                        }
                    }
                }
            }
        }

        public int GetRegionID(int x, int y)
        {
            if (IsValidCoordinate(x, y))
            {
                return m_RegionIDs[y * HorizontalGridCount + x];
            }
            return 0;
        }

        public List<Vector2Int> GetRegionCoordinates(int regionID)
        {
            var coordinates = new List<Vector2Int>();
            for (var y = 0; y < VerticalGridCount; ++y)
            {
                for (var x = 0; x < HorizontalGridCount; ++x)
                {
                    var idx = y * HorizontalGridCount + x;
                    if (m_RegionIDs[idx] == regionID)
                    {
                        coordinates.Add(new Vector2Int(x, y));
                    }
                }
            }
            return coordinates;
        }

        public void Clear(int regionID)
        {
            for (var i = 0; i < VerticalGridCount; ++i)
            {
                for (var j = 0; j < HorizontalGridCount; ++j)
                {
                    var idx = i * HorizontalGridCount + j;
                    if (m_RegionIDs[idx] == regionID)
                    {
                        m_RegionIDs[idx] = 0;
                    }
                }
            }
        }

        protected override Color GetColor(int x, int y)
        {
            var id = GetRegionID(x, y);
            if (id == 0)
            {
                return CityEditorDefine.Black;
            }
            var bigRegion = m_Grid.GetRegionTemplate(id);
            return bigRegion.Color;
        }

        public override void EditorSerialize(ISerializer writer, string label, IObjectIDConverter translator)
        {
            writer.WriteInt32(m_RegionLayerVersion, "BigRegionLayer.Version");

            base.EditorSerialize(writer, label, translator);

            var regionIDs = new int[m_RegionIDs.Length];
            for (var i = 0; i < m_RegionIDs.Length; ++i)
            {
                regionIDs[i] = translator.Convert(m_RegionIDs[i]);
            }
            writer.WriteInt32Array(regionIDs, "Regions ID");
        }

        public override void EditorDeserialize(IDeserializer reader, string label)
        {
            reader.ReadInt32("BigRegionLayer.Version");

            base.EditorDeserialize(reader, label);

            m_RegionIDs = reader.ReadInt32Array("Regions ID");
        }

        public override string TypeName => "BigRegionLayer";

        int[] m_RegionIDs;

        const int m_RegionLayerVersion = 1;
    }
}
