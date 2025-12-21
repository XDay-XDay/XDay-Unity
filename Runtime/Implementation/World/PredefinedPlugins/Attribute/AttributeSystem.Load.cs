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

namespace XDay.WorldAPI.Attribute
{
    internal partial class AttributeSystem
    {
        protected override void LoadGameDataInternal(string pluginName, IWorld world)
        {
            var reader = world.QueryGameDataDeserializer(world.ID, $"attribute@{pluginName}");

            reader.ReadInt32("Attribute.Version");

            m_Name = reader.ReadString("Name");

            m_Layers = new List<LayerBase>();

            LoadObstacleData(reader);

            var otherLayerCount = reader.ReadInt32("Other Layer Count");
            for (var i = 0; i < otherLayerCount; ++i)
            {
                var name = reader.ReadString("Name");
                var horizontalGridCount = reader.ReadInt32("Horizontal Grid Count");
                var verticalGridCount = reader.ReadInt32("Vertical Grid Count");
                var gridWidth = reader.ReadSingle("Grid Width");
                var gridHeight = reader.ReadSingle("Grid Height");
                var layerType = (LayerType)reader.ReadInt32("Layer Type");
                var origin = reader.ReadVector2("Origin");
                var data = reader.ReadUInt32Array("Data");
                if (horizontalGridCount > 0)
                {
                    var layer = new Layer(name, horizontalGridCount, verticalGridCount, gridWidth, gridHeight, origin, layerType, data);
                    m_Layers.Add(layer);
                }
            }

            reader.Uninit();
        }

        void LoadObstacleData(IDeserializer reader)
        {
            var horizontalGridCount = reader.ReadInt32("");
            var verticalGridCount = reader.ReadInt32("");
            var gridWidth = reader.ReadSingle("");
            var gridHeight = reader.ReadSingle("");
            var origin = reader.ReadVector2("");
            var data = reader.ReadUInt32Array("");
            var layer = new Layer("Obstacle", horizontalGridCount, verticalGridCount, gridWidth, gridHeight, origin, LayerType.Obstacle, data);
            if (horizontalGridCount > 0)
            {
                m_Layers.Add(layer);
            }
        }
    }
}

