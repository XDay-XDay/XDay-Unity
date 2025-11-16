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

namespace XDay.WorldAPI.Region
{
    internal partial class RegionSystem
    {
        protected override void LoadGameDataInternal(string pluginName, IWorld world)
        {
            var reader = world.QueryGameDataDeserializer(world.ID, $"region@{pluginName}");

            reader.ReadInt32("Region.Version");

            m_Name = reader.ReadString("Name");

            m_LODSystem = reader.ReadSerializable<IPluginLODSystem>("Plugin LOD System", true);

            m_Layers = new List<LayerBase>();

            LoadLayers(reader);

            reader.Uninit();
        }

        private void LoadLayers(IDeserializer reader)
        {
            var layerCount = reader.ReadInt32("Layer Count");
            for (var i = 0; i < layerCount; i++)
            {
                var name = reader.ReadString("Name");
                var horizontalGridCount = reader.ReadInt32("HorizontalGridCount");
                var verticalGridCount = reader.ReadInt32("VerticalGridCount");
                var gridWidth = reader.ReadSingle("GridWidth");
                var gridHeight = reader.ReadSingle("GridHeight");
                var origin = reader.ReadVector2("Origin");
                var height = reader.ReadSingle("Height");
                var layer = new Layer(name, horizontalGridCount, verticalGridCount, gridWidth, gridHeight, origin, height);
                m_Layers.Add(layer);

                LoadRegions(reader, layer);
            }
        }

        private void LoadRegions(IDeserializer reader, Layer layer)
        {
            var regionCount = reader.ReadInt32("Region Count");
            for (var i = 0; i < regionCount; ++i)
            {
                var configID = reader.ReadInt32("Region ID");
                var position = reader.ReadVector3("Region Center");
                var color = reader.ReadColor32("Region Color");
                var name = reader.ReadString("Region Name");
                var bounds = reader.ReadRect("Region Bounds");
                var lod0PrefabPath = reader.ReadString("LOD0 Prefab Path");
                var lod1PrefabPath = reader.ReadString("LOD1 Prefab Path");
                RegionObject region = new RegionObject(name, configID, position, color, bounds, lod0PrefabPath, lod1PrefabPath);
                layer.AddRegion(region);
            }
        }
    }
}

