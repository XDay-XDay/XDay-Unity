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
using System.Collections.Generic;
using System.Linq;
using XDay.UtilityAPI;
using MiniJSON;

namespace XDay.WorldAPI.Attribute.Editor
{
    public partial class AttributeSystem
    {
        protected override void GenerateGameDataInternal(IObjectIDConverter converter)
        {
            CalculateIfGridHasObstacles(false);

            ISerializer serializer = ISerializer.CreateBinary();
            serializer.WriteInt32(m_RuntimeVersion, "Attribute.Version");

            serializer.WriteString(m_Name, "Name");

            var layers = new List<LayerBase>(m_Layers);
            var data = GetObstacleData(out var horizontalGridCount, out var verticalGridCount, out var gridWidth, out var gridHeight, out var origin, out var obstacleLayers);

            var otherLayers = layers.Except(obstacleLayers).ToList();
            SaveObstacleData(serializer, data, horizontalGridCount, verticalGridCount, gridWidth, gridHeight, origin);

            serializer.WriteInt32(otherLayers.Count, "Other Layer Count");
            foreach (var layer in otherLayers)
            {
                SaveLayer(serializer, layer);
            }

            serializer.Uninit();

            EditorHelper.WriteFile(serializer.Data, GetGameFilePath("attribute"));

            ExportJson();
        }

        private void SaveLayer(ISerializer writer, LayerBase layerBase)
        {
            if (layerBase is Layer layer)
            {
                writer.WriteString(layer.Name, "Name");
                writer.WriteInt32(layer.HorizontalGridCount, "Horizontal Grid Count");
                writer.WriteInt32(layer.VerticalGridCount, "Vertical Grid Count");
                writer.WriteSingle(layer.GridWidth, "Grid Width");
                writer.WriteSingle(layer.GridHeight, "Grid Height");
                writer.WriteInt32((int)layer.Type, "Layer Type");
                writer.WriteVector2(layer.Origin, "Origin");
                writer.WriteUInt32Array(layer.Data, "Data");
            }
            else
            {
                Debug.Assert(false, "todo");
            }
        }

        private void SaveObstacleData(ISerializer writer, 
            uint[] data, 
            int horizontalGridCount, 
            int verticalGridCount, 
            float gridWidth, 
            float gridHeight, 
            Vector2 origin)
        {
            writer.WriteInt32(horizontalGridCount, "");
            writer.WriteInt32(verticalGridCount, "");
            writer.WriteSingle(gridWidth, "");
            writer.WriteSingle(gridHeight, "");
            writer.WriteVector2(origin, "");
            writer.WriteUInt32Array(data, "");
        }

        /// <summary>
        /// 导出服务器需要的配置
        /// </summary>
        private void ExportJson()
        {
            var data = GetObstacleData(out var horizontalGridCount, out var verticalGridCount, out var gridWidth, out var gridHeight, out _, out var obstacleLayers);
            if (data != null)
            {
                Dictionary<string, object> root = new();

                root["width"] = horizontalGridCount;
                root["height"] = verticalGridCount;
                root["tiles"] = data;

                var text = Json.Serialize(root);

                EditorHelper.WriteFile(text, $"{World.GameFolder}/{WorldDefine.CONSTANT_FOLDER_NAME}/tiles.json");
            }
            else
            {
                Debug.LogError("没有Obstacle类型的Layer,无法导出tiles.json");
            }
        }

        private uint[] GetObstacleData(out int horizontalGridCount, out int verticalGridCount, out float gridWidth, out float gridHeight, out Vector2 origin, out List<LayerBase> obstacleLayers)
        {
            obstacleLayers = new List<LayerBase>();

            var baseLayer = GetLayer(LayerType.AutoObstacle);
            if (baseLayer != null)
            {
                obstacleLayers.Add(baseLayer);
                foreach (var layer in m_Layers)
                {
                    if (layer.Type == LayerType.CustomObstacle && layer.SizeEqual(baseLayer))
                    {
                        obstacleLayers.Add(layer);
                    }
                }
            }

            uint[] obstacleData = null;
            horizontalGridCount = 0;
            verticalGridCount = 0;
            gridWidth = 0;
            gridHeight = 0;
            origin = Vector2.zero;
            if (obstacleLayers.Count > 0)
            {
                horizontalGridCount = obstacleLayers[0].HorizontalGridCount;
                verticalGridCount = obstacleLayers[0].VerticalGridCount;
                gridWidth = obstacleLayers[0].GridWidth;
                gridHeight = obstacleLayers[0].GridHeight;
                origin = obstacleLayers[0].Origin;
                obstacleData = new uint[horizontalGridCount * verticalGridCount];

                foreach (var layer in obstacleLayers)
                {
                    var obstacleLayer = layer as Layer;
                    for (var i = 0; i < verticalGridCount; ++i)
                    {
                        for (var j = 0; j < horizontalGridCount; ++j)
                        {
                            var idx = i * horizontalGridCount + j;
                            if (obstacleLayer.Data[idx] != 0)
                            {
                                obstacleData[idx] = 1u;
                            }
                        }
                    }
                }
            }

            return obstacleData;
        }

        private const int m_RuntimeVersion = 1;
    }
}
