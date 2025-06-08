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
    partial class CityEditor
    {
        protected override void GenerateGameDataInternal(IObjectIDConverter translator)
        {
            //ExportCityData(translator);

            //ExportPathFindingData();
        }

#if false
        private void ExportCityData(IObjectIDConverter translator)
        {
            ISerializer writer = IWriterFactory.CreateBinaryWriter();
            writer.WriteInt32(m_CityDataVersion, "CityData.Version");

            writer.WriteString(Name, "Name");

            ExportGrids(writer, translator);

            ExportBuildingTemplates(writer);

            writer.Close();
            FileHelper.CompareAndWrite(GetExportFilePath("city_data"), writer.Bytes);
        }

        private void ExportPathFindingData()
        {
            ISerializer writer = IWriterFactory.CreateBinaryWriter(useStringCache:false);
            writer.WriteInt32(m_PathFindingDataVersion, "PathData.Version");

            ExportGridLabels(writer, m_Grids[0]);

            ExportWaypoints(writer, m_Grids[0]);

            writer.Close();
            FileHelper.CompareAndWrite(GetExportFilePath("path_data"), writer.Bytes);
        }

        void ExportGrids(ISerializer writer, IObjectIDConverter translator)
        {
            var gridCount = m_Grids.Count;
            writer.WriteInt32(gridCount, "Grid Count");
            foreach (var grid in m_Grids)
            {
                writer.WriteObjectID(grid.ID, "ID", translator);
                writer.WriteString(grid.Name, "Name");
                writer.WriteInt32(grid.HorizontalGridCount, "Horizontal Grid Count");
                writer.WriteInt32(grid.VerticalGridCount, "Vertical Grid Count");
                writer.WriteSingle(grid.GridSize, "Grid Size");
                writer.WriteVector3(grid.Position, "Position");
                writer.WriteQuaternion(grid.Rotation, "Rotation");

                ExportGridStates(writer, grid);

                ExportRegionIDs(writer, grid);

                ExportAreaIDs(writer, grid);

                ExportInitialBuildings(writer, translator, grid);

                ExportRegions(writer, grid);

                ExportAreas(writer, grid);

                ExportLands(writer, grid);

                ExportEvents(writer, grid);

                ExportInteractivePoints(writer, grid);

                ExportRoomData(writer, translator, grid);

                ExportGridHeights(writer, grid);
            }
        }

        void ExportInteractivePoints(ISerializer writer, Grid grid)
        {
            var n = grid.InteractivePoints.Count;
            writer.WriteInt32(n, "");
            foreach (var point in grid.InteractivePoints)
            {
                writer.WriteInt32(point.ID, "");
                writer.WriteVector3(point.Start.Position, "");
                writer.WriteQuaternion(point.Start.Rotation, "");
                writer.WriteVector3(point.End.Position, "");
                writer.WriteQuaternion(point.End.Rotation, "");
            }
        }

        void ExportWaypoints(ISerializer writer, Grid grid)
        {
            var n = grid.Waypoints.Count;
            writer.WriteInt32(n, "");
            foreach (var point in grid.Waypoints)
            {
                writer.WriteInt32(point.ID, "");
                writer.WriteInt32(point.ConnectedID, "");
                writer.WriteInt32(point.EventID, "");
                writer.WriteString(point.Name, "");
                writer.WriteBoolean(point.Enabled, "");

                var pos = point.GameObject.transform.position;
                var coord = grid.WorldPositionToCoordinate(pos.x, pos.z);
                writer.WriteVector3(pos, "");
                writer.WriteVector2Int(coord, "");
            }
        }

        void ExportRoomData(ISerializer writer, IObjectIDConverter translator, Grid grid)
        {
            var rooms = grid.RoomEditor.Rooms;
            writer.WriteInt32(rooms.Count, "");
            foreach (var room in rooms)
            {
                ExportRoom(writer, translator, room);
            }
        }

        void ExportRoom(ISerializer writer, IObjectIDConverter translator, RoomPrefab room)
        {
            writer.WriteObjectID(room.ID, "", translator);
            writer.WriteString(room.Name, "");
            writer.WriteVector2Int(room.Size, "");
            var facilities = room.Facilities;
            writer.WriteInt32(facilities.Count, "");
            foreach (var facility in facilities)
            {
                ExportFacility(writer, facility);
            }
        }

        void ExportFacility(ISerializer writer, FacilityPrefab facility)
        {
            writer.WriteString(facility.Name, "");
            writer.WriteVector2Int(facility.Size, "");
            writer.WriteInt32(facility.ConfigID, "");
            var rotation = Quaternion.identity;
            if (facility.Prefab != null)
            {
                rotation = facility.Prefab.transform.rotation;
            }
            else
            {
                Debug.LogError($"设施{facility.Name}的Prefab未设置");
            }
            writer.WriteQuaternion(rotation, "");
            writer.WriteVector2Int(facility.LocalCoordinate, "");
        }

        void ExportBuildingTemplates(ISerializer writer)
        {
            var total = GetBuildingTemplateAndFacilityCount(m_Grids[0]);
            writer.WriteInt32(total, "Building Template Count");
            foreach (var template in m_BuildingTemplates)
            {
                writer.WriteInt32(template.ConfigID, "Building ID");
                writer.WriteVector2Int(template.Size, "Size");

                //房间设施也作为建筑导出
                var room = m_Grids[0].RoomEditor.GetRoomPrefabByID(template.RoomID);
                if (room != null)
                {
                    foreach (var facility in room.Facilities)
                    {
                        var id = facility.ConfigID + template.ConfigID;
                        writer.WriteInt32(id, "Building ID");
                        writer.WriteVector2Int(facility.Size, "Size");
                    }
                }
                else
                {
                    Debug.LogError($"房间{template.RoomID}未找到，不能导出其设施!");
                }
            }
        }

        void ExportGridHeights(ISerializer writer, Grid grid)
        {
            var heights = grid.CalculateGridHeights();
            writer.WriteSingleArray(heights, "Grid Heights");
        }

        void ExportGridStates(ISerializer writer, Grid grid)
        {
            var gridStates = new int[grid.VerticalGridCount * grid.HorizontalGridCount];
            var gridLabelLayer = grid.GetLayer<GridLabelLayer>();
            var buildableLayer = grid.GetLayer<BuildableLayer>();
            for (var i = 0; i < grid.VerticalGridCount; ++i)
            {
                for (var j = 0; j < grid.HorizontalGridCount; ++j)
                {
                    var state = 0;
                    if (gridLabelLayer.IsWalkable(j, i))
                    {
                        //editor setting walkable
                        state |= 1 << 4;
                    }
                    if (buildableLayer.IsBuildable(j, i, 1, 1))
                    {
                        state |= 1 << 1;
                    }
                    if (IsWaypoint(j, i, grid))
                    {
                        state |= 1 << 3;
                    }
                    gridStates[i * grid.HorizontalGridCount + j] = state;
                }
            }

            writer.WriteInt32Array(gridStates, "Grids State");
        }

        void ExportGridLabels(ISerializer writer, Grid grid)
        {
            var gridLabels = new byte[grid.VerticalGridCount * grid.HorizontalGridCount];
            var gridLabelLayer = grid.GetLayer<GridLabelLayer>();
            for (var i = 0; i < grid.VerticalGridCount; ++i)
            {
                for (var j = 0; j < grid.HorizontalGridCount; ++j)
                {
                    var gridLabel = grid.GetGridLabel(gridLabelLayer.GetGridLabelID(j, i));
                    if (gridLabel != null)
                    {
                        gridLabels[i * grid.HorizontalGridCount + j] = gridLabel.Value;
                    }
                }
            }
            writer.WriteByteArray(gridLabels, "Grid Labels");
        }

        void ExportRegionIDs(ISerializer writer, Grid grid)
        {
            var regionsID = new int[grid.VerticalGridCount * grid.HorizontalGridCount];
            var regionLayer = grid.GetLayer<RegionLayer>();
            for (var i = 0; i < grid.VerticalGridCount; ++i)
            {
                for (var j = 0; j < grid.HorizontalGridCount; ++j)
                {
                    var regionID = regionLayer.GetRegionID(j, i);
                    var region = grid.GetRegionTemplate(regionID);
                    regionsID[i * grid.HorizontalGridCount + j] = region == null ? 0 : region.ConfigID;
                }
            }

            writer.WriteInt32Array(regionsID, "Big Regions ID");
        }

        //数据已废弃
        void ExportAreaIDs(ISerializer writer, Grid grid)
        {
            var areaIDs = new int[grid.VerticalGridCount * grid.HorizontalGridCount];
            var areaLayer = grid.GetLayer<AreaLayer>();
            for (var i = 0; i < grid.VerticalGridCount; ++i)
            {
                for (var j = 0; j < grid.HorizontalGridCount; ++j)
                {
                    var areaID = areaLayer.GetAreaID(j, i);
                    var area = grid.GetAreaTemplate(areaID);
                    areaIDs[i * grid.HorizontalGridCount + j] = area == null ? 0 : area.ConfigID;
                }
            }

            writer.WriteInt32Array(areaIDs, "Small Regions ID");
        }

        void ExportInitialBuildings(ISerializer writer, IObjectIDConverter translator, Grid grid)
        {
            var totalCount = GetRoomAndFacilityCount(grid);

            writer.WriteInt32(totalCount, "Building Count");
            foreach (var building in grid.Buildings)
            {
                writer.WriteInt32(building.Template.ConfigID, "Building Type");
                writer.WriteInt32(building.Min.x, "X");
                writer.WriteInt32(building.Min.y, "Y");
                writer.WriteInt32(building.Size.x, "Width");
                writer.WriteInt32(building.Size.y, "Height");
                writer.WriteObjectID(building.Template.RoomID, "Room ID", translator);
                var height = building.GameObject.transform.position.y;
                writer.WriteSingle(height, "Ground Height");

                //房间设施也作为建筑导出
                var room = grid.RoomEditor.GetRoomPrefabByID(building.Template.RoomID);
                if (room != null)
                {
                    foreach (var facility in room.Facilities)
                    {
                        var id = facility.ConfigID + building.Template.ConfigID;
                        writer.WriteInt32(id, "Building Type");
                        var min = facility.LocalCoordinate + building.Min;
                        writer.WriteInt32(min.x, "X");
                        writer.WriteInt32(min.y, "Y");
                        writer.WriteInt32(facility.Size.x, "Width");
                        writer.WriteInt32(facility.Size.y, "Height");
                        writer.WriteObjectID(building.Template.RoomID, "Room ID", translator);
                        var facilityHeight = 0f;
                        if (facility.Instance != null)
                        {
                            facilityHeight = building.Template.RoomPrefab.FacilityLocalY + building.GameObject.transform.position.y;
                        }
                        else
                        {
                            Debug.LogError($"{room.Name}的设施{facility.Name}创建了但没有摆放到场景中");
                        }
                        writer.WriteSingle(facilityHeight, "Ground Height");
                    }
                }
                else
                {
                    Debug.LogError($"房间{building.Template.RoomID}未找到，不能导出其设施!");
                }
            }
        }

        int GetRoomAndFacilityCount(Grid grid)
        {
            var total = grid.Buildings.Count;
            foreach (var building in grid.Buildings)
            {
                var room = grid.RoomEditor.GetRoomPrefabByID(building.Template.RoomID);
                if (room != null)
                {
                    total += room.Facilities.Count;
                }
            }

            return total;
        }

        int GetBuildingTemplateAndFacilityCount(Grid grid)
        {
            var total = m_BuildingTemplates.Count;
            foreach (var template in m_BuildingTemplates)
            {
                var room = grid.RoomEditor.GetRoomPrefabByID(template.RoomID);
                if (room != null)
                {
                    total += room.Facilities.Count;
                }
            }

            return total;
        }

        void CalculateBounds(List<Vector2Int> coordinates, out Vector2Int min, out Vector2Int max)
        {
            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;
            foreach (var coord in coordinates)
            {
                if (coord.x > maxX)
                {
                    maxX = coord.x;
                }
                if (coord.x < minX)
                {
                    minX = coord.x;
                }

                if (coord.y > maxY)
                {
                    maxY = coord.y;
                }
                if (coord.y < minY)
                {
                    minY = coord.y;
                }
            }

            min = new Vector2Int(minX, minY);
            max = new Vector2Int(maxX, maxY);
        }

        void ExportRegions(ISerializer writer, Grid grid)
        {
            var n = grid.RegionTemplates.Count;
            writer.WriteInt32(n, "Big Region Count");
            foreach (var region in grid.RegionTemplates)
            {
                writer.WriteInt32(region.ConfigID, "Config ID");
                writer.WriteColor(region.Color, "Color");
                var coordinates = grid.GetRegionCoordinates(region.ObjectID);
                writer.WriteInt32(coordinates.Count, "Coordinate Count");
                CalculateBounds(coordinates, out var min, out var max);
                writer.WriteVector2Int(min, "Bounds Min");
                writer.WriteVector2Int(max, "Bounds Max");
                var landIDs = new List<int>(region.LandTemplateCount);
                for (var i = 0; i < region.LandTemplateCount; ++i)
                {
                    landIDs.Add(region.LandTemplates[i].ConfigID);
                }
                writer.WriteInt32List(landIDs, "Block IDs");
                var eventIDs = new List<int>(region.EventTemplateCount);
                for (var i = 0; i < region.EventTemplateCount; ++i)
                {
                    eventIDs.Add(region.EventTemplates[i].ConfigID);
                }
                writer.WriteInt32List(eventIDs, "Event IDs");
            }
        }

        void ExportAreas(ISerializer writer, Grid grid)
        {
            var allAreas = new List<KeyValuePair<AreaTemplate, int>>();
            foreach (var region in grid.RegionTemplates)
            {
                foreach (var area in region.AreaTemplates) {
                    allAreas.Add(new KeyValuePair<AreaTemplate, int>(area, region.ConfigID));
                }
            }

            var n = allAreas.Count;
            writer.WriteInt32(n, "Small Region Count");
            foreach (var kv in allAreas)
            {
                var area = kv.Key;
                writer.WriteInt32(area.ConfigID, "Config ID");
                writer.WriteInt32(kv.Value, "Big Region Config ID");
                writer.WriteColor(area.Color, "Color");
                var coordinates = new List<Vector2Int>(area.Coordinates);
                writer.WriteInt32(coordinates.Count, "Coordinate Count");
                CalculateBounds(coordinates, out var min, out var max);
                writer.WriteVector2Int(min, "Bounds Min");
                writer.WriteVector2Int(max, "Bounds Max");
                writer.WriteInt32((int)area.InitialGridType, "Initial Ground Type");

                ExportLocators(writer, area.Locators, grid);

                writer.WriteVector2IntList(coordinates, "Coordinates");
            }
        }

        void ExportLands(ISerializer writer, Grid grid)
        {
            var allLands = new List<KeyValuePair<LandTemplate, int>>();
            foreach (var region in grid.RegionTemplates)
            {
                foreach (var land in region.LandTemplates)
                {
                    allLands.Add(new KeyValuePair<LandTemplate, int>(land, region.ConfigID));
                }
            }

            var n = allLands.Count;
            writer.WriteInt32(n, "Block Count");
            foreach (var kv in allLands)
            {
                var land = kv.Key;
                writer.WriteInt32(land.ConfigID, "Config ID");
                writer.WriteInt32(kv.Value, "Big Region Config ID");
                writer.WriteColor(land.Color, "Color");
                var coordinates = grid.GetLandCoordinates(land.ObjectID);
                writer.WriteVector2IntList(coordinates, "Coordinates");
                CalculateBounds(coordinates, out var min, out var max);
                writer.WriteVector2Int(min, "Bounds Min");
                writer.WriteVector2Int(max, "Bounds Max");

                //export locators
                ExportLocators(writer, land.Locators, grid);
            }
        }

        void ExportEvents(ISerializer writer, Grid grid)
        {
            var allEvents = new List<KeyValuePair<EventTemplate, int>>();
            foreach (var region in grid.RegionTemplates)
            {
                foreach (var e in region.EventTemplates)
                {
                    allEvents.Add(new KeyValuePair<EventTemplate, int>(e, region.ConfigID));
                }
            }

            var n = allEvents.Count;
            writer.WriteInt32(n, "Event Count");
            foreach (var kv in allEvents)
            {
                var e = kv.Key;
                writer.WriteInt32(e.ConfigID, "Config ID");
                writer.WriteInt32(kv.Value, "Big Region Config ID");
                writer.WriteColor(e.Color, "Color");
                var coordinates = new List<Vector2Int>(e.Coordinates);
                writer.WriteVector2IntList(coordinates, "Coordinates");
                CalculateBounds(coordinates, out var min, out var max);
                writer.WriteVector2Int(min, "Bounds Min");
                writer.WriteVector2Int(max, "Bounds Max");

                //get event ground height
                var centerCoord = (min + max) / 2;
                var height = grid.GetGroundHeight(centerCoord.x, centerCoord.y);
                if (!e.UseGroundHeight)
                {
                    height = grid.GetGridHeight(centerCoord.x, centerCoord.y);
                }
                writer.WriteSingle(height, "Ground Height");
            }
        }

        void ExportLocators(ISerializer writer, List<Locator> locators, Grid grid)
        {
            writer.WriteInt32(locators.Count, "Locator Count");
            for (var i = 0; i < locators.Count; ++i)
            {
                writer.WriteString(locators[i].Name, "Name");
                var pos = locators[i].Position;
                var coord = grid.WorldPositionToCoordinate(pos.x, pos.z);
                writer.WriteVector2Int(coord, "Coordinate");
            }
        }

        bool IsWaypoint(int x, int y, Grid grid)
        {
            foreach (var point in grid.Waypoints)
            {
                if (point.Enabled)
                {
                    var pos = point.GameObject.transform.position;
                    var coord = grid.WorldPositionToCoordinate(pos.x, pos.z);
                    if (coord.x == x && coord.y == y)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        const int m_CityDataVersion = 1;
        const int m_PathFindingDataVersion = 1;
#endif
    }
}
