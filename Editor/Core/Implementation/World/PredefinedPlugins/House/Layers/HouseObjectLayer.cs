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
    internal class HouseObjectLayer : HouseBaseLayer
    {
        public override string TypeName => "HouseObjectLayer";

        public HouseObjectLayer()
        {
        }

        public HouseObjectLayer(int id, string name, int horizontalGridCount, int verticalGridCount, float gridSize)
            : base(id, name, horizontalGridCount, verticalGridCount, gridSize)
        {
            m_ObjectIDs = new int[verticalGridCount * horizontalGridCount];
        }

        public void SetObjectID(int x, int y, int width, int height, int id)
        {
            var maxX = x + width - 1;
            var maxY = y + height - 1;
            for (var i = y; i <= maxY; ++i)
            {
                for (var j = x; j <= maxX; ++j)
                {
                    if (IsValidCoordinate(j, i))
                    {
                        m_ObjectIDs[i * HorizontalGridCount + j] = id;
                    }
                }
            }
        }

        public int GetObjectID(int x, int y)
        {
            if (IsValidCoordinate(x, y))
            {
                return m_ObjectIDs[y * HorizontalGridCount + x];
            }
            return 0;
        }

        protected override Color GetColor(int x, int y)
        {
            var empty = GetObjectID(x, y) == 0;
            return empty ? new Color32(35, 232, 85, 255) : new Color32(30, 30, 30, 255);
        }

        public override void EditorSerialize(ISerializer writer, string label, IObjectIDConverter translator)
        {
            writer.WriteInt32(m_ObjectLayerVersion, "ObjectLayer.Version");

            base.EditorSerialize(writer, label, translator);

            var objectIDs = new int[m_ObjectIDs.Length];
            for (var i = 0; i < m_ObjectIDs.Length; ++i)
            {
                objectIDs[i] = translator.Convert(m_ObjectIDs[i]);
            }
            writer.WriteInt32Array(objectIDs, "Objects ID");
        }

        public override void EditorDeserialize(IDeserializer reader, string label)
        {
            reader.ReadInt32("ObjectLayer.Version");

            base.EditorDeserialize(reader, label);

            var objectIDs = reader.ReadInt32Array("Objects ID");
            m_ObjectIDs = new int[objectIDs.Length];
        }

        public override void Rotate()
        {
            var arr2D = Helper.Rotate180(Helper.ToArray2D(m_ObjectIDs, VerticalGridCount, HorizontalGridCount));
            m_ObjectIDs = Helper.ToArray(arr2D, VerticalGridCount, HorizontalGridCount);
        }

        protected override void OnResize(int horizontal, int vertical)
        {
            m_ObjectIDs = new int[horizontal * vertical];
        }

        private int[] m_ObjectIDs;
        private const int m_ObjectLayerVersion = 1;
    }
}
