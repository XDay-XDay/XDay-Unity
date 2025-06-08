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
    class AreaLayer : BaseLayer
    {
        public AreaLayer()
        {
        }

        public AreaLayer(int id, string name, int horizontalGridCount, int verticalGridCount, float gridSize) 
            : base(id, name, horizontalGridCount, verticalGridCount, gridSize)
        {
            m_AreaIDs = new int[verticalGridCount * horizontalGridCount];
        }

        protected override void OnInitialize()
        {
            m_RegionLayer = m_Grid.GetLayer<RegionLayer>();
        }

        public void SetArea(AreaTemplate e)
        {
            if (m_AreaTemplate != e)
            {
                m_AreaTemplate = e;

                UpdateColors();
            }
        }

        public void Grayout(int displayRegionID)
        {
            m_DisplayRegionID = displayRegionID;

            UpdateColors();
        }

        public void DisableGrayDisplay()
        {
            m_DisplayRegionID = 0;
        }

        public int GetAreaID(int x, int y)
        {
            if (IsValidCoordinate(x, y))
            {
                return m_AreaIDs[y * HorizontalGridCount + x];
            }
            return 0;
        }

        public void Clear(int regionID)
        {
            for (var i = 0; i < VerticalGridCount; ++i)
            {
                for (var j = 0; j < HorizontalGridCount; ++j)
                {
                    if (m_AreaIDs[i * HorizontalGridCount + j] == regionID)
                    {
                        m_AreaIDs[i * HorizontalGridCount + j] = 0;
                    }
                }
            }
        }

        protected override Color GetColor(int x, int y)
        {
            var regionID = m_RegionLayer.GetRegionID(x, y);
            if (m_DisplayRegionID != 0 && regionID != m_DisplayRegionID)
            {
                return CityEditorDefine.Gray;
            }

            if (m_AreaTemplate != null && m_AreaTemplate.Coordinates.Contains(new Vector2Int(x, y)))
            {
                return m_AreaTemplate.Color;
            }

            return CityEditorDefine.Black;
        }

        public override void EditorSerialize(ISerializer writer, string label, IObjectIDConverter translator)
        {
            writer.WriteInt32(m_AreaLayerVersion, "SmallRegionLayer.Version");

            base.EditorSerialize(writer, label, translator);

            var regionIDs = new int[m_AreaIDs.Length];
            for (var i = 0; i < m_AreaIDs.Length; ++i)
            {
                regionIDs[i] = translator.Convert(m_AreaIDs[i]);
            }
            writer.WriteInt32Array(regionIDs, "Regions ID");
        }

        public override void EditorDeserialize(IDeserializer reader, string label)
        {
            reader.ReadInt32("SmallRegionLayer.Version");

            base.EditorDeserialize(reader, label);

            m_AreaIDs = reader.ReadInt32Array("Regions ID");
        }

        public override string TypeName => "SmallRegionLayer";

        int[] m_AreaIDs;
        int m_DisplayRegionID;
        RegionLayer m_RegionLayer;
        AreaTemplate m_AreaTemplate;

        const int m_AreaLayerVersion = 1;
    }
}
