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

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.House.Editor
{
    internal partial class HouseEditor
    {
        protected override void ValidateExportInternal(StringBuilder errorMessage) 
        {
            if (string.IsNullOrEmpty(m_PlaceholderModelPath))
            {
                errorMessage.AppendLine("先设置替代模型,当房间Prefab丢失时使用替代模型");
                return;
            }

            bool ok = ValidateIDs(errorMessage);
            if (!ok)
            {
                return;
            }

            ok = ValidatePositions();
            if (!ok)
            {
                return;
            }
        }

        /// <summary>
        /// 检查物体坐标是否有效
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private bool ValidatePositions()
        {
            StringBuilder builder = new();
            foreach (var houseInstance in m_HouseInstances)
            {
                foreach (var teleporter in houseInstance.TeleporterInstances)
                {
                    var coord = houseInstance.PositionToCoordinate(teleporter.WorldPosition);
                    if (!houseInstance.IsWalkable(coord.x, coord.y))
                    {
                        builder.AppendLine($"\"{houseInstance.Name}\"的传送点\"{teleporter.Name}\"坐标不可通行,无法传送!");
                    }
                }

                foreach (var interactivePoint in houseInstance.InteractivePointInstance)
                {
                    var startCoord = houseInstance.PositionToCoordinate(interactivePoint.Start.Position);
                    if (!houseInstance.IsWalkable(startCoord.x, startCoord.y))
                    {
                        builder.AppendLine($"\"{houseInstance.Name}\"的交互点\"{interactivePoint.Name}\"的起点坐标不可通行!");
                    }

                    var endCoord = houseInstance.PositionToCoordinate(interactivePoint.End.Position);
                    if (!houseInstance.IsWalkable(endCoord.x, endCoord.y))
                    {
                        builder.AppendLine($"\"{houseInstance.Name}\"的交互点\"{interactivePoint.Name}\"的终点坐标不可通行!");
                    }
                }
            }

            if (builder.Length > 0)
            {
                var errorMsg = builder.ToString().Substring(0, 60);
                EditorUtility.DisplayDialog("有以下报错,可以导出数据,但是角色无法正确寻路", errorMsg, "确定");
                Debug.LogError(errorMsg);
            }

            return true;
        }

        protected override void GenerateGameDataInternal(IObjectIDConverter converter)
        {
            ISerializer serializer = ISerializer.CreateBinary();
            serializer.WriteInt32(m_RuntimeVersion, "HouseSystem.Version");

            serializer.WriteString(m_Name, "Name");

            serializer.WriteInt32(m_HouseInstances.Count, "House Count");
            foreach (var house in m_HouseInstances)
            {
                serializer.WriteInt32(house.ConfigID, "House Config ID");
                serializer.WriteString(house.Name, "House Name");
                serializer.WriteVector3(house.Position, "House Position");
                serializer.WriteString(house.GetPath(0), "House Prefab Path");
                serializer.WriteSingle(house.GridSize, "Grid Size");
                serializer.WriteSingle(house.GridHeight, "Grid Height");
                serializer.WriteBounds(house.WorldBounds, "World Bounds");
                if (house.WorldBounds.extents == Vector3.zero)
                {
                    Debug.LogError($"{house.Name} invalid world bounds");
                }
                serializer.WriteInt32(house.HorizontalGridCount, "Horizontal Grid Count");
                serializer.WriteInt32(house.VerticalGridCount, "Vertical Grid Count");
                var walkable = GetWalkable(house);
                serializer.WriteBooleanArray(walkable, "Walkable");

                var teleporterCount = house.TeleporterInstances.Count;
                serializer.WriteInt32(teleporterCount, "Teleporter Count");
                foreach (var teleporter in house.TeleporterInstances)
                {
                    serializer.WriteInt32(teleporter.ConfigID, "Teleporter Config ID");
                    serializer.WriteInt32(teleporter.ConnectedID, "Teleporter Connected ID");
                    serializer.WriteVector3(teleporter.WorldPosition, "Teleporter World Position");
                    serializer.WriteString(teleporter.Name, "Teleporter Name");
                    serializer.WriteBoolean(teleporter.Enabled, "Teleporter State");
                }

                var pointCount = house.InteractivePointInstance.Count;
                serializer.WriteInt32(pointCount, "Interactive Point Count");
                foreach (var point in house.InteractivePointInstance)
                {
                    serializer.WriteInt32(point.ConfigID, "Interactive Point Config ID");
                    serializer.WriteVector3(point.Start.Position, "Start World Position");
                    serializer.WriteVector3(point.End.Position, "End World Position");
                    serializer.WriteQuaternion(point.Start.Rotation, "Start Rotation");
                    serializer.WriteQuaternion(point.End.Rotation, "End Rotation");
                }
            }

            serializer.Uninit();

            EditorHelper.WriteFile(serializer.Data, GetGameFilePath("house"));
        }

        private bool[] GetWalkable(HouseInstance house)
        {
            bool[] walkable = new bool[house.HorizontalGridCount * house.VerticalGridCount];
            var layer = house.GetLayer<HouseWalkableLayer>();
            var idx = 0;
            for (var i = 0; i < house.VerticalGridCount; ++i)
            {
                for (var j = 0; j < house.HorizontalGridCount; ++j)
                {
                    var k = i * layer.HorizontalGridCount + j;
                    if (k >= layer.Walkable.Length || k < 0)
                    {
                        walkable[idx] = false;
                    }
                    else
                    {
                        walkable[idx] = layer.Walkable[k];
                    }
                    ++idx;
                }
            }
            return walkable;
        }

        private bool ValidateIDs(StringBuilder errorMsg)
        {
            Dictionary<int, HouseInstance> houses = new();
            foreach (var houseInstance in m_HouseInstances)
            {
                if (houseInstance.ConfigID == 0)
                {
                    errorMsg.AppendLine($"{houseInstance.Name}的ID为0");
                }
                else
                {
                    if (houses.ContainsKey(houseInstance.ConfigID))
                    {
                        errorMsg.AppendLine($"{houseInstance.Name}的ID和{houses[houseInstance.ConfigID].Name}相同");
                    }
                    else
                    {
                        houses.Add(houseInstance.ConfigID, houseInstance);
                    }
                }

                Dictionary<int, HouseTeleporterInstance> teleporters = new();
                foreach (var teleporter in houseInstance.TeleporterInstances)
                {
                    if (teleporter.ConfigID == 0)
                    {
                        errorMsg.AppendLine($"房间{houseInstance.Name}的传送点{teleporter.Name}的ID为0");
                    }
                    else
                    {
                        if (teleporters.ContainsKey(teleporter.ConfigID))
                        {
                            errorMsg.AppendLine($"房间{houseInstance.Name}的传送点{teleporter.Name}的ID和{teleporters[teleporter.ConfigID].Name}相同");
                        }
                        else
                        {
                            teleporters.Add(teleporter.ConfigID, teleporter);
                        }
                    }
                }

                Dictionary<int, HouseInteractivePointInstance> interactorPoints = new();
                foreach (var interactivePoint in houseInstance.InteractivePointInstance)
                {
                    if (interactivePoint.ConfigID == 0)
                    {
                        errorMsg.AppendLine($"房间{houseInstance.Name}的交互点{interactivePoint.Name}的ID为0");
                    }
                    else
                    {
                        if (interactorPoints.ContainsKey(interactivePoint.ConfigID))
                        {
                            errorMsg.AppendLine($"房间{houseInstance.Name}的交互点{interactivePoint.Name}的ID和{interactorPoints[interactivePoint.ConfigID].Name}相同");
                        }
                        else
                        {
                            interactorPoints.Add(interactivePoint.ConfigID, interactivePoint);
                        }
                    }
                }
            }

            return errorMsg.Length == 0;
        }

        private const int m_RuntimeVersion = 1;
    }
}
