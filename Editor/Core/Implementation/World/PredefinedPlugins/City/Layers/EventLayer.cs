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
    class EventLayer : BaseLayer
    {
        public EventLayer()
        {
        }

        public EventLayer(int id, string name, int horizontalGridCount, int verticalGridCount, float gridSize)
            : base(id, name, horizontalGridCount, verticalGridCount, gridSize)
        {
        }

        protected override void OnInitialize()
        {
            m_RegionLayer = m_Grid.GetLayer<RegionLayer>();
        }

        public void SetEvent(EventTemplate e)
        {
            if (m_EventTemplate != e)
            {
                m_EventTemplate = e;

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

        protected override Color GetColor(int x, int y)
        {
            var regionID = m_RegionLayer.GetRegionID(x, y);
            if (m_DisplayRegionID != 0 && regionID != m_DisplayRegionID)
            {
                return CityEditorDefine.Gray;
            }

            if (m_EventTemplate != null && m_EventTemplate.Coordinates.Contains(new Vector2Int(x, y)))
            {
                return m_EventTemplate.Color;
            }
            return CityEditorDefine.Black;
        }

        public override void EditorSerialize(ISerializer writer, string label, IObjectIDConverter translator)
        {
            writer.WriteInt32(m_EventLayerVersion, "EventLayer.Version");

            base.EditorSerialize(writer, label, translator);
        }

        public override void EditorDeserialize(IDeserializer reader, string label)
        {
            reader.ReadInt32("EventLayer.Version");

            base.EditorDeserialize(reader, label);
        }

        public override string TypeName => "EventLayer";

        EventTemplate m_EventTemplate;
        int m_DisplayRegionID;
        RegionLayer m_RegionLayer;

        const int m_EventLayerVersion = 1;
    }
}
