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

using MiniJSON;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Region.Editor
{
    internal partial class RegionSystem
    {
        protected override void ValidateExportInternal(StringBuilder errorMessage)
        {
            var error = Validate();
            if (!string.IsNullOrEmpty(error))
            {
                errorMessage.AppendLine(error);
            }
        }

        protected override void GenerateGameDataInternal(IObjectIDConverter converter)
        {
            ISerializer serializer = ISerializer.CreateBinary();
            serializer.WriteInt32(m_RuntimeVersion, "Region.Version");

            serializer.WriteString(m_Name, "Name");

            serializer.WriteSerializable(m_PluginLODSystem, "Plugin LOD System", converter, true);

            ExportLayers(serializer);

            serializer.Uninit();

            EditorHelper.WriteFile(serializer.Data, GetGameFilePath("region"));

            ExportJson();
        }

        private void ExportLayers(ISerializer serializer)
        {
            serializer.WriteInt32(m_Layers.Count, "Layer Count");
            for (var i = 0; i < m_Layers.Count; i++)
            {
                var layer = m_Layers[i];
                var gridData = GetGridIndexData(layer);

                serializer.WriteString(layer.Name, "Name");
                serializer.WriteInt32(layer.HorizontalGridCount, "HorizontalGridCount");
                serializer.WriteInt32(layer.VerticalGridCount, "VerticalGridCount");
                serializer.WriteSingle(layer.GridWidth, "GridWidth");
                serializer.WriteSingle(layer.GridHeight, "GridHeight");
                serializer.WriteVector2(layer.Origin, "Origin");
                serializer.WriteSingle(layer.Renderer.Root.transform.position.y, "Height");
                serializer.WriteByteArray(gridData, "Grid");

                ExportRegions(serializer, m_Layers[i]);
            }
        }

        private byte[] GetGridIndexData(RegionSystemLayer layer)
        {
            byte[] ret = new byte[layer.HorizontalGridCount * layer.VerticalGridCount];
            var idx = 0;
            for (var i = 0; i < layer.VerticalGridCount; ++i)
            {
                for (var j = 0; j < layer.HorizontalGridCount; ++j)
                {
                    ret[idx++] = (byte)layer.GetRegionIndex(layer.GetGrid(j, i));
                }
            }
            return ret;
        }

        private void ExportRegions(ISerializer serializer, RegionSystemLayer layer)
        {
            var regions = GetValidRegions(layer, out var regionCoordinates);
            serializer.WriteInt32(regions.Count, "Region Count");
            foreach (var region in regions)
            {
                serializer.WriteInt32(region.ConfigID, "Region ID");
                serializer.WriteVector3(layer.GetRegionCenter(regionCoordinates[region.ID]), "Region Center");
                serializer.WriteColor32(region.Color, "Region Color");
                serializer.WriteString(region.Name, "Region Name");
                serializer.WriteRect(layer.GetRegionWorldBounds(regionCoordinates[region.ID]), "Region Bounds");
                var fullPrefabPath = $"{layer.GetRegionFullObjectNamePrefix(region.ConfigID)}.prefab";
                serializer.WriteString(fullPrefabPath, "Full Prefab Path");
            }
        }

        private List<RegionObject> GetValidRegions(RegionSystemLayer layer, out Dictionary<int, List<Vector2Int>> regionCoordinates)
        {
            List<RegionObject> validRegions = new();
            regionCoordinates = new();

            foreach (var region in layer.Regions)
            {
                var coords = layer.GetRegionCoordinates(region.ID);
                if (coords.Count > 0)
                {
                    regionCoordinates.Add(region.ID, coords);
                    validRegions.Add(region);
                }
            }
            return validRegions;
        }

        /// <summary>
        /// 导出服务器需要的配置
        /// </summary>
        private void ExportJson()
        {
            var data = GetRegionData(out var horizontalGridCount, out var verticalGridCount, out var neighbours, out var buildings);
            if (data != null)
            {
                Dictionary<string, object> root = new()
                {
                    ["width"] = horizontalGridCount,
                    ["height"] = verticalGridCount,
                    ["neighbours"] = neighbours,
                    ["regions"] = data,
                    ["buildings"] = buildings,
                };

                var text = Json.Serialize(root);

                EditorHelper.WriteFile(text, $"{World.GameFolder}/{WorldDefine.CONSTANT_FOLDER_NAME}/regions.json");
            }
            else
            {
                Debug.LogError("没有Layer,无法导出regions.json");
            }
        }

        private int[] GetRegionData(out int horizontalGridCount, out int verticalGridCount, 
            out Dictionary<string, object> neighbours, 
            out List<object> buildings)
        {
            int[] regionData = null;
            neighbours = null;
            buildings = null;
            horizontalGridCount = 0;
            verticalGridCount = 0;

            Dictionary<int, HashSet<int>> neighboursList = new();

            if (m_Layers.Count > 0)
            {
                var layer = m_Layers[0];
                horizontalGridCount = layer.HorizontalGridCount;
                verticalGridCount = layer.VerticalGridCount;
                regionData = new int[horizontalGridCount * verticalGridCount];

                for (var i = 0; i < verticalGridCount; ++i)
                {
                    for (var j = 0; j < horizontalGridCount; ++j)
                    {
                        var idx = i * horizontalGridCount + j;
                        var regionID = layer.GetRegionID(j, i);
                        var region = layer.GetRegion(regionID);
                        regionData[idx] = region != null ? region.ConfigID : 0;

                        for (var n = 0; n < 4; ++n)
                        {
                            var nx = j + m_Neighbours[n].x;
                            var ny = i + m_Neighbours[n].y;
                            var neighbourRegionID = layer.GetRegionID(nx, ny);
                            AddToNeighbour(neighboursList, regionID, neighbourRegionID);
                        }
                    }
                }

                neighbours = new();
                foreach (var kv in neighboursList)
                {
                    neighbours.Add(ToRegionConfigID(layer, kv.Key).ToString(), ToList(layer, kv.Value));
                }

                buildings = new();
                foreach (var region in layer.Regions)
                {
                    var entry = new Dictionary<string, object>
                    {
                        ["cfgId"] = region.ConfigID
                    };
                    var pos = region.BuildingPosition;
                    var coord = layer.PositionToCoordinate(pos);
                    entry["x"] = coord.x;
                    entry["z"] = coord.y;
                    entry["level"] = region.Level;
                    buildings.Add(entry);
                }
            }

            return regionData;
        }

        private object ToRegionConfigID(RegionSystemLayer layer, int regionID)
        {
            var region = layer.GetRegion(regionID);
            return region.ConfigID;
        }

        private object ToList(RegionSystemLayer layer, HashSet<int> neighbourRegionIDs)
        {
            List<int> list = new();
            foreach (var regionID in neighbourRegionIDs)
            {
                var region = layer.GetRegion(regionID);
                list.Add(region.ConfigID);
            }
            return list;
        }

        private void AddToNeighbour(Dictionary<int, HashSet<int>> neighboursList, int regionID, int neighbourRegionID)
        {
            if (regionID == 0 || regionID == neighbourRegionID)
            {
                return;
            }

            if (!neighboursList.TryGetValue(regionID, out var neighbours))
            {
                neighbours = new();
                neighboursList.Add(regionID, neighbours);
            }

            if (neighbourRegionID == 0)
            {
                return;
            }

            if (!neighbours.Contains(neighbourRegionID))
            {
                neighbours.Add(neighbourRegionID);
            }
        }

        public string Validate()
        {
            string errorMessage = "";
            if (m_Layers.Count > 0)
            {
                var layer = m_Layers[0];
                HashSet<int> ids = new();
                foreach (var region in layer.Regions)
                {
                    if (region.ConfigID == 0)
                    {
                        errorMessage += $"RegionLayer的层{layer.Name}里{region.Name}使用了无效的ConfigID: 0\n";
                    }
                    else
                    {
                        if (!ids.Contains(region.ConfigID))
                        {
                            ids.Add(region.ConfigID);
                        }
                        else
                        {
                            errorMessage += $"RegionLayer的层{layer.Name}里{region.Name}使用了重复的ConfigID: {region.ConfigID}\n";
                        }
                    }
                }
            }
            return errorMessage;
        }

        private Vector2Int[] m_Neighbours = new Vector2Int[4]
        {
            new(-1, 0),
            new(1, 0),
            new(0, -1),
            new(0, 1),
        };

        private const int m_RuntimeVersion = 1;
    }
}
