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
using UnityEngine;
using XDay.NavigationAPI;
using XDay.UtilityAPI;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.City.Editor
{
    internal partial class Grid : IGridData
    {
		public int NeighbourCount => m_NeighbourCoordinateOffsets.Length;
		public Vector3 Position { get => m_View.Position; set => m_View.Position = value; }
		public Quaternion Rotation
		{
			get => m_View.Rotation;
			set => m_View.Rotation = value;
		}
		public Quaternion RotationInverse => Quaternion.Inverse(Rotation);
		public int ID => m_ID;
		public string Name { get => m_Name; set { m_View.Name = value; m_Name = value; } }
		public int HorizontalGridCount => m_HorizontalGridCount;
		public int VerticalGridCount => m_VerticalGridCount;
		public float GridSize => m_GridSize;
		public bool ShowInInspector { get => m_ShowInInspector; set => m_ShowInInspector = value; }
		public GameObject RootGameObject => m_View.RootGameObject;
		public CityEditor CityEditor => m_CityEditor;
		public List<BuildingInstance> Buildings => m_Buildings;
		public int SelectedRegionIndex { get => m_SelectedRegionIndex; set => m_SelectedRegionIndex = value; }
		public int SelectedGridLabelIndex { get => m_SelectedGridLabelIndex; set => m_SelectedGridLabelIndex = value; }
		public int SelectedInteractivePointIndex { get => m_SelectedInteractivePointIndex; set => m_SelectedInteractivePointIndex = value; }
		public int SelectedWaypointIndex { get => m_SelectedWaypointIndex; set => m_SelectedWaypointIndex = value; }
		public List<RegionTemplate> RegionTemplates => m_RegionTemplates;
		public List<InteractivePoint> InteractivePoints => m_InteractivePoints;
		public List<Waypoint> Waypoints => m_Waypoints;
		public List<GridLabelTemplate> GridLabels => m_GridLabels;
		public int GridLabelCount => m_GridLabels.Count;
		public string[] RegionNames => m_RegionNames;
		public GameObject ScenePrefabRoot => m_ScenePrefabRoot;
		public GameObject EventPrefabRoot => m_EventPrefabRoot;
		public GameObject InteractivePointRoot => m_InteractivePointRoot;
		public GameObject WaypointRoot => m_WaypointRoot;
		public RoomEditor RoomEditor => m_RoomEditor;
		public GameObject DefaultInteractivePointStartPrefab => m_DefaultInteractivePointStartPrefab;
		public GameObject DefaultInteractivePointEndPrefab => m_DefaultInteractivePointEndPrefab;


		public Grid()
        {
        }

        public Grid(int id, string name, int horizontalGridCount, int verticalGridCount, float gridSize, Vector3 position, Quaternion rotation, List<BaseLayer> layers)
        {
            m_ID = id;
            m_Name = name;
            m_HorizontalGridCount = horizontalGridCount;
            m_VerticalGridCount = verticalGridCount;
            m_GridSize = gridSize;
            m_Position = position;
            m_Rotation = rotation;
            m_Layers = layers;
        }

        public void Initialize(CityEditor cityEditor, Transform parent, IWorld world)
        {
            m_CityEditor = cityEditor;

            m_Width = m_GridSize * m_HorizontalGridCount;
            m_Height = m_GridSize * m_VerticalGridCount;

            m_View = new GridView(m_Name, m_HorizontalGridCount, m_VerticalGridCount, m_GridSize, m_Position, m_Rotation, parent, this, cityEditor);

            m_BuildingRoot = CreateItemRoot("建筑");
            m_ScenePrefabRoot = CreateItemRoot("场景模型");
            m_EventPrefabRoot = CreateItemRoot("事件模型");
            m_AgentRoot = CreateItemRoot("NPC");
            m_InteractivePointRoot = CreateItemRoot("交互点");
            m_WaypointRoot = CreateItemRoot("捷径点");

            m_DisplayGridCost = new DisplayGridCost(m_HorizontalGridCount, m_VerticalGridCount);

            m_DefaultInteractivePointStartPrefab = EditorHelper.LoadAssetByGUID<GameObject>(m_StartPrefabGUID);
            m_DefaultInteractivePointEndPrefab = EditorHelper.LoadAssetByGUID<GameObject>(m_EndPrefabGUID);

            m_View.IsLineActive = m_GridLineVisible;

            m_RoomEditor.Initialize(this);

            var localPosition = new Vector3(0, m_LayerOffset, 0);
            EnsureLayers(cityEditor);
            foreach (var layer in m_Layers)
            {
                layer.Initialize(m_View.RootGameObject.transform, localPosition, this);
            }

            foreach (var region in m_RegionTemplates)
            {
                region.Initialize(this);
            }

            foreach (var building in m_Buildings)
            {
                building.Initialize(cityEditor, m_BuildingRoot.transform, this, world);
            }

            foreach (var agent in m_Agents)
            {
                agent.Initialize(cityEditor, m_AgentRoot.transform, this);
            }

            foreach (var point in m_InteractivePoints)
            {
                point.Initialize(this);
            }

            foreach (var point in m_Waypoints)
            {
                point.Initialize(this);
            }

            SubscribeEvents();

            if (m_SelectedRegionIndex < 0 && m_RegionTemplates.Count > 0)
            {
                m_SelectedRegionIndex = 0;
            }

            if (m_SelectedGridLabelIndex < 0 && m_GridLabels.Count > 0)
            {
                m_SelectedGridLabelIndex = 0;
            }

            if (m_SelectedInteractivePointIndex < 0 && m_InteractivePoints.Count > 0)
            {
                m_SelectedInteractivePointIndex = 0;
            }

            if (m_SelectedWaypointIndex < 0 && m_Waypoints.Count > 0)
            {
                m_SelectedWaypointIndex = 0;
            }
        }

        void EnsureLayers(CityEditor cityEditor)
        {
            if (GetLayer<EventLayer>() == null)
            {
                var eventLayer = new EventLayer(cityEditor.World.AllocateObjectID(), "Event Layer", m_HorizontalGridCount, m_VerticalGridCount, m_GridSize);
                m_Layers.Add(eventLayer);
            }

            if (GetLayer<GridLabelLayer>() == null)
            {
                var gridLabelLayer = new GridLabelLayer(cityEditor.World.AllocateObjectID(), "Grid Label Layer", m_HorizontalGridCount, m_VerticalGridCount, m_GridSize);
                m_Layers.Add(gridLabelLayer);
            }
        }

        public void OnDestroy()
        {
            UnsubscribeEvents();

            m_View.OnDestroy();

            foreach (var point in m_InteractivePoints)
            {
                point.OnDestroy();
            }

            foreach (var point in m_Waypoints)
            {
                point.OnDestroy();
            }

            foreach (var temp in m_RegionTemplates)
            {
                temp.OnDestroy();
            }

            foreach (var building in m_Buildings)
            {
                building.OnDestroy();
            }
            m_Buildings.Clear();

            foreach (var agent in m_Agents)
            {
                agent.OnDestroy();
            }
            m_Agents.Clear();

            foreach (var layer in m_Layers)
            {
                layer.OnDestroy();
            }
            m_Layers.Clear();
        }

        public void Update(float dt)
        {
            UpdateDestroyQueue();

            foreach (var building in m_Buildings)
            {
                building.Update(dt);
            }

            foreach (var agent in m_Agents)
            {
                agent.Update(dt);
            }
        }

        public Vector3 CoordinateToGridCenterPosition(int x, int y)
        {
            var localX = m_GridSize * (x + 0.5f) - m_Width * 0.5f;
            var localZ = m_GridSize * (y + 0.5f) - m_Height * 0.5f;
            return Position + Rotation * new Vector3(localX, 0, localZ);
        }

        public Vector3 CoordinateToGridPosition(int x, int y)
        {
            var localX = m_GridSize * x - m_Width * 0.5f;
            var localZ = m_GridSize * y - m_Height * 0.5f;
            return Position + Rotation * new Vector3(localX, 0, localZ);
        }

        public Vector2Int PositionToCoordinate(Vector3 worldPos)
        {
            var localPosition = RotationInverse * (worldPos - Position);

            var xCoord = Mathf.FloorToInt((localPosition.x + m_Width * 0.5f) / m_GridSize);
            var yCoord = Mathf.FloorToInt((localPosition.z + m_Height * 0.5f) / m_GridSize);
            return new Vector2Int(xCoord, yCoord);
        }

        public Vector3 WorldToLocalPosition(Vector3 worldPos)
        {
            return RotationInverse * (worldPos - Position);
        }

        public Vector2Int LocalPositionToCoordinate(float x, float z)
        {
            var xCoord = Mathf.FloorToInt((x + m_GridSize * 0.05f + m_Width * 0.5f) / m_GridSize);
            var yCoord = Mathf.FloorToInt((z + m_GridSize * 0.05f + m_Height * 0.5f) / m_GridSize);
            return new Vector2Int(xCoord, yCoord);
        }

        public bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < m_HorizontalGridCount &&
                y >= 0 && y < m_VerticalGridCount;
        }

        public float GetGridCost(int x, int y, Func<int, float> costOverride = null)
        {
            return 1.0f;
        }

        public void SetBuildState(int x, int y, int width, int height, bool buildable)
        {
            var layer = GetLayer<BuildableLayer>();
            layer.SetBuildableState(x, y, width, height, buildable);
            layer.UpdateColors();
        }

        public void SetWalkableState(int x, int y, int width, int height, bool walkable)
        {
            var layer = GetLayer<WalkableLayer>();
            layer.SetWalkable(x, y, width, height, walkable);
            layer.UpdateColors();
        }

        public int GetRegionID(int x, int y)
        {
            var layer = GetLayer<RegionLayer>();
            if (layer != null)
            {
                return layer.GetRegionID(x, y);
            }
            return 0;
        }

        public void SetRegion(int x, int y, int width, int height, bool set)
        {
            var layer = GetLayer<RegionLayer>();
            if (layer != null)
            {
                var regionTemplate = m_RegionTemplates[m_SelectedRegionIndex];
                if (set)
                {
                    layer.SetRegionID(x, y, width, height, regionTemplate.ObjectID);
                }
                else
                {
                    layer.SetRegionID(x, y, width, height, 0);
                }
                layer.UpdateColors();
            }
        }

        public void SetGridLabel(int x, int y, int width, int height, bool set)
        {
            var layer = GetLayer<GridLabelLayer>();
            if (layer != null)
            {
                var gridLabel = m_GridLabels[m_SelectedGridLabelIndex];
                if (set)
                {
                    layer.SetGridLabelID(x, y, width, height, gridLabel.ObjectID);
                }
                else
                {
                    layer.SetGridLabelID(x, y, width, height, 0);
                }
                layer.UpdateColors();
            }
        }

        public void SetArea(int x, int y, int width, int height, bool set)
        {
            var layer = GetLayer<AreaLayer>();
            if (layer != null)
            {
                var region = m_RegionTemplates[m_SelectedRegionIndex];
                var area = region.AreaTemplates[region.SelectedAreaTemplateIndex];

                area.SetCoordinates(x, y, width, height, set);

                layer.UpdateColors();
            }
        }

        public void SetLand(int x, int y, int width, int height, bool set)
        {
            //var layer = GetLayer<LandLayer>();
            //if (layer != null)
            //{
            //    var region = m_RegionTemplates[m_SelectedRegionIndex];
            //    var land = region.LandTemplates[region.SelectedLandTemplateIndex];

            //    if (set)
            //    {
            //        layer.SetLandID(x, y, width, height, land.ObjectID, region.ObjectID);
            //    }
            //    else
            //    {
            //        layer.SetLandID(x, y, width, height, 0, region.ObjectID);
            //    }
            //    layer.UpdateColors();
            //}
        }

        public void SetEvent(int x, int y, int width, int height, bool set)
        {
            var layer = GetLayer<EventLayer>();
            if (layer != null)
            {
                var region = m_RegionTemplates[m_SelectedRegionIndex];
                var eventTemplate = region.EventTemplates[region.SelectedEventTemplateIndex];
                var regionLayer = GetLayer<RegionLayer>();

                for (var i = y; i < y + height; ++i)
                {
                    for (var j = x; j < x + width; ++j)
                    {
                        if (regionLayer.GetRegionID(j, i) == region.ObjectID)
                        {
                            eventTemplate.SetCoordinates(j, i, set);
                        }
                    }
                }

                layer.UpdateColors();
            }
        }

        public int GetObjectID(int x, int y)
        {
            return GetLayer<ObjectLayer>().GetObjectID(x, y);
        }

        public bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= m_HorizontalGridCount || y < 0 || y >= m_VerticalGridCount)
            {
                return false;
            }
            return GetLayer<GridLabelLayer>().IsWalkable(x, y);
        }

        public bool IsWalkableNoCheck(int x, int y)
        {
            return IsWalkable(x, y);
        }

        public Vector2Int GetNeighbourCoordinate(int x, int y, int index)
        {
            return new Vector2Int(m_NeighbourCoordinateOffsets[index].x + x, m_NeighbourCoordinateOffsets[index].y + y);
        }

        public Vector2Int FindNearestWalkableCoordinate(Vector2Int coord)
        {
            Debug.LogWarning("todo");
            return coord;
        }

        public void Occupy(int minX, int minY, int maxX, int maxY, int objectID)
        {
            Debug.Assert(objectID != 0);
            GetLayer<OccupyStateLayer>().SetEmpty(minX, minY, maxX - minX + 1, maxY - minY + 1, empty:false);
            GetLayer<ObjectLayer>().SetObjectID(minX, minY, maxX - minX + 1, maxY - minY + 1, objectID);
        }

        public void Free(int minX, int minY, int maxX, int maxY)
        {
            GetLayer<OccupyStateLayer>().SetEmpty(minX, minY, maxX - minX + 1, maxY - minY + 1, empty: true);
            GetLayer<ObjectLayer>().SetObjectID(minX, minY, maxX - minX + 1, maxY - minY + 1, 0);
        }

        public bool CanPlaceGridObjectAt(float posX, float posZ, Vector2Int objectSize)
        {
            CalculateCoordinateBoundsByCenterPosition(new Vector3(posX, 0, posZ), objectSize, out var min, out var max);

            return CanPlaceGridObjectAt(min, max, Vector2Int.one, Vector2Int.zero);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="excludedMin">判断时忽略范围的min</param>
        /// <param name="excludedMax">判断时忽略范围的max</param>
        /// <returns></returns>
        public bool CanPlaceGridObjectAt(Vector2Int min, Vector2Int max, Vector2Int excludedMin, Vector2Int excludedMax)
        {
            for (var y = min.y; y <= max.y; ++y)
            {
                for (var x = min.x; x <= max.x; ++x)
                {
                    if (x >= excludedMin.x && x <= excludedMax.x &&
                        y >= excludedMin.y && y <= excludedMax.y)
                    {
                        continue;
                    }
                    var ok = GetLayer<BuildableLayer>().IsBuildable(x, y, 1, 1) &&
                        GetLayer<OccupyStateLayer>().IsEmpty(x, y, 1, 1);
                    if (!ok)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool PlaceBuildingInstance(int objectID, float posX, float posZ, BuildingTemplate template, CityEditor cityEditor)
        {
            if (!CanPlaceGridObjectAt(posX, posZ, template.Size))
            {
                return false;
            }

            var isInRange = CalculateCoordinateBoundsByCenterPosition(new Vector3(posX, 0, posZ), template.Size, out var min, out var max);
            if (!isInRange)
            {
                return false;
            }

            var buildingInstance = new BuildingInstance(objectID, template.Size, min, max, Rotation, template.ID);
            buildingInstance.Initialize(cityEditor, m_BuildingRoot.transform, this, cityEditor.World);
            m_Buildings.Add(buildingInstance);

            return true;
        }

        public void RemoveBuildingInstanceOfType(int buildingTemplateID)
        {
            for (var i = m_Buildings.Count - 1; i >= 0; --i)
            {
                if (m_Buildings[i].Template.ID == buildingTemplateID)
                {
                    RemoveBuildingInstance(m_Buildings[i]);
                }
            }
        }

        public bool RemoveBuildingInstance(float posX, float posZ)
        {
            var coord = PositionToCoordinate(new Vector3(posX, 0, posZ));
            if (coord.x < 0 || coord.x >= m_HorizontalGridCount ||
                coord.y < 0 || coord.y >= m_VerticalGridCount)
            {
                return false;
            }

            var objectID = GetLayer<ObjectLayer>().GetObjectID(coord.x, coord.y);
            if (objectID != 0)
            {
                var existedBuilding = GetBuildingInstance(objectID);
                if (existedBuilding != null)
                {
                    var behaviour = existedBuilding.GameObject.GetComponent<BuildingBehaviour>();
                    behaviour.ProcessDestroyEvent = false;
                    RemoveBuildingInstance(existedBuilding);
                    return true;
                }
            }

            return false;
        }

        void RemoveBuildingInstance(BuildingInstance buildingInstance)
        {
            buildingInstance.OnDestroy();
            m_Buildings.Remove(buildingInstance);
        }

        public BuildingInstance GetBuildingInstance(int id)
        {
            foreach (var building in m_Buildings)
            {
                if (building.ID == id)
                {
                    return building;
                }
            }
            return null;
        }

        public BuildingInstance GetBuildingInstanceOfType(int buildingTemplateConfigID)
        {
            foreach (var building in m_Buildings)
            {
                if (building.Template.ConfigID == buildingTemplateConfigID)
                {
                    return building;
                }
            }
            return null;
        }

        public void SetDefaultInteractivePointStartPrefab(GameObject prefab, bool forceSet)
        {
            if (m_DefaultInteractivePointStartPrefab != prefab || forceSet)
            {
                m_DefaultInteractivePointStartPrefab = prefab;
                foreach (var point in m_InteractivePoints)
                {
                    if (point.Start.Prefab == null || forceSet)
                    {
                        point.Start.Prefab = prefab;
                    }
                }
            }
        }

        public void SetDefaultInteractivePointEndPrefab(GameObject prefab, bool forceSet)
        {
            if (m_DefaultInteractivePointEndPrefab != prefab || forceSet)
            {
                m_DefaultInteractivePointEndPrefab = prefab;
                foreach (var point in m_InteractivePoints)
                {
                    if (point.End.Prefab == null || forceSet)
                    {
                        point.End.Prefab = prefab;
                    }
                }
            }
        }

        public void CalculateCoordinateBoundsByLowerLeftPosition(Vector3 cursorPosition, Vector2Int size, out Vector2Int min, out Vector2Int max)
        {
            var localPosition = WorldToLocalPosition(cursorPosition);

            var minX = localPosition.x - size.x * GridSize * 0.5f;
            var minZ = localPosition.z - size.y * GridSize * 0.5f;
            min = LocalPositionToCoordinate(minX, minZ);
            min.x = Mathf.Clamp(min.x, 0, HorizontalGridCount - size.x);
            min.y = Mathf.Clamp(min.y, 0, VerticalGridCount - size.y);
            max = min + size - Vector2Int.one;
        }

        public bool CalculateCoordinateBoundsByCenterPosition(Vector3 cursorPosition, Vector2Int size, out Vector2Int min, out Vector2Int max)
        {
            var coord = PositionToCoordinate(cursorPosition);
            min = new Vector2Int(
                coord.x - (size.x - 1) / 2,
                coord.y - (size.y - 1) / 2
                );
            max = min + size - Vector2Int.one;
            if (min.x < 0 || min.y < 0 || max.x >= HorizontalGridCount || max.y >= VerticalGridCount)
            {
                return false;
            }

            min.x = Mathf.Clamp(min.x, 0, HorizontalGridCount - size.x);
            min.y = Mathf.Clamp(min.y, 0, VerticalGridCount - size.y);
            max = min + size - Vector2Int.one;

            return true;
        }

        public Vector3 CalculateObjectCenterPosition(float posX, float posZ, Vector2Int size, out bool isValidCoord)
        {
            var coord = PositionToCoordinate(new Vector3(posX, 0, posZ));
            isValidCoord = IsValidCoordinate(coord.x, coord.y);
            CalculateCoordinateBoundsByCenterPosition(new Vector3(posX, 0, posZ), size, out var min, out var max);
            return (CoordinateToGridPosition(min.x, min.y) + CoordinateToGridPosition(max.x + 1, max.y + 1)) * 0.5f;
        }

        public bool PlaceAgentInstance(int objectID, float posX, float posZ, AgentTemplate template, CityEditor cityEditor)
        {
            if (!CanPlaceGridObjectAt(posX, posZ, template.Size))
            {
                return false;
            }

            var isInRange = CalculateCoordinateBoundsByCenterPosition(new Vector3(posX, 0, posZ), template.Size, out var min, out var max);
            if (!isInRange)
            {
                return false;
            }

            var agentInstance = new AgentInstance(objectID, template.Size, min, max, Rotation, template.ID);
            agentInstance.Initialize(cityEditor, m_AgentRoot.transform, this);
            m_Agents.Add(agentInstance);

            return true;
        }

        void RemoveAgentInstance(AgentInstance agent)
        {
            agent.OnDestroy();
            m_Agents.Remove(agent);
        }

        public AgentInstance GetAgentInstance(int id)
        {
            foreach (var agent in m_Agents)
            {
                if (agent.ID == id)
                {
                    return agent;
                }
            }
            return null;
        }

        public void CreateNavigationData(ITaskSystem taskSystem)
        {
            var pathFinder = IGridBasedPathFinder.Create(taskSystem, this, m_NeighbourCoordinateOffsets.Length);
            foreach (var agent in m_Agents)
            {
                agent.SetNavigationData(pathFinder);
            }
        }

        public void ShowOccupyState(bool visible)
        {
            GetLayer<OccupyStateLayer>().IsActive = visible;
            UpdateOccupyState();
        }

        public void ShowGridCost(bool visible)
        {
            m_DisplayGridCost.IsActive = visible;
        }

        public void RenderGridCost()
        {
            m_DisplayGridCost.Render(this);
        }

        public void DrawSceneGUI()
        {
            foreach (var building in m_Buildings)
            {
                building.DrawHandle();
            }

            m_RoomEditor.DrawSceneGUI();
        }

        void UpdateOccupyState()
        {
            var layer = GetLayer<OccupyStateLayer>();
            layer.UpdateColors();
        }

        public void Save(ISerializer writer, IObjectIDConverter translator)
        {
            writer.WriteInt32(m_Version, "Grid.Version");

            writer.WriteObjectID(m_ID, "ID", translator);
            writer.WriteString(m_Name, "Name");
            writer.WriteInt32(m_HorizontalGridCount, "Horizontal Grid Count");
            writer.WriteInt32(m_VerticalGridCount, "Vertical Grid Count");
            writer.WriteSingle(m_GridSize, "Grid Size");
            writer.WriteBoolean(m_ShowInInspector, "Show In Inspector");
            writer.WriteVector3(Position, "Position");
            writer.WriteQuaternion(Rotation, "Rotation");
            writer.WriteInt32(m_SelectedRegionIndex, "Selected Big Region Index");
            writer.WriteInt32(m_SelectedGridLabelIndex, "Selected Grid Label Index");
            writer.WriteInt32(m_SelectedInteractivePointIndex, "Selected Interactive Point Index");
            writer.WriteInt32(m_SelectedWaypointIndex, "Selected Way Point Index");
            writer.WriteBoolean(m_View.IsLineActive, "Grid Visible");
            writer.WriteString(EditorHelper.GetObjectGUID(m_DefaultInteractivePointStartPrefab), "Default Interactive Point Start Prefab");
            writer.WriteString(EditorHelper.GetObjectGUID(m_DefaultInteractivePointEndPrefab), "Default Interactive Point End Prefab");

            writer.WriteList(m_RegionTemplates, "Big Regions", (RegionTemplate region, int index) =>
            {
                writer.WriteStructure($"Big Region {index}", () =>
                {
                    region.Save(writer, translator);
                });
            });

            writer.WriteList(m_Buildings, "Buildings", (BuildingInstance building, int index) =>
            {
                writer.WriteStructure($"Building {index}", () =>
                {
                    building.Save(writer, translator);
                });
            });

            writer.WriteList(m_Agents, "Agents", (AgentInstance agent, int index) =>
            {
                writer.WriteStructure($"Agent {index}", () =>
                {
                    agent.Save(writer, translator);
                });
            });

            writer.WriteList(m_GridLabels, "Grid Labels", (GridLabelTemplate gridLabel, int index) =>
            {
                writer.WriteStructure($"Grid Label {index}", () =>
                {
                    gridLabel.Save(writer, translator);
                });
            });

            writer.WriteList(m_InteractivePoints, "Interactive Points", (point, index) =>
            {
                writer.WriteStructure($"Interactive Point {index}", () =>
                {
                    point.Save(writer);
                });
            });

            writer.WriteList(m_Waypoints, "Way Points", (point, index) =>
            {
                writer.WriteStructure($"Way Point {index}", () =>
                {
                    point.Save(writer);
                });
            });

            writer.WriteList(m_Layers, "Layers", (BaseLayer layer, int index) =>
            {
                writer.WriteSerializable(layer, $"Layer {index}", translator, false);
            });

            writer.WriteStructure("Building Editor", () =>
            {
                m_RoomEditor.Save(writer, translator);
            });
        }

        public void Load(IDeserializer reader)
        {
            reader.ReadInt32("Grid.Version");

            m_ID = reader.ReadInt32("ID");
            m_Name = reader.ReadString("Name");
            m_HorizontalGridCount = reader.ReadInt32("Horizontal Grid Count");
            m_VerticalGridCount = reader.ReadInt32("Vertical Grid Count");
            m_GridSize = reader.ReadSingle("Grid Size");
            m_ShowInInspector = reader.ReadBoolean("Show In Inspector");
            m_Position = reader.ReadVector3("Position");
            m_Rotation = reader.ReadQuaternion("Rotation");
            m_SelectedRegionIndex = reader.ReadInt32("Selected Big Region Index");
            m_SelectedGridLabelIndex = reader.ReadInt32("Selected Grid Label Index");
            m_SelectedInteractivePointIndex = reader.ReadInt32("Selected Interactive Point Index");
            m_SelectedWaypointIndex = reader.ReadInt32("Selected Way Point Index");
            m_GridLineVisible = reader.ReadBoolean("Grid Visible");
            m_StartPrefabGUID = reader.ReadString("Default Interactive Point Start Prefab");
            m_EndPrefabGUID = reader.ReadString("Default Interactive Point End Prefab");

            m_RegionTemplates = reader.ReadList("Big Regions", (int index) =>
            {
                var region = new RegionTemplate();
                reader.ReadStructure($"Big Region {index}", () =>
                {
                    region.Load(reader);
                });
                return region;
            });

            m_Buildings = reader.ReadList("Buildings", (int index) =>
            {
                var building = new BuildingInstance();
                reader.ReadStructure($"Building {index}", () =>
                {
                    building.Load(reader);
                });
                return building;
            });

            m_Agents = reader.ReadList("Agents", (int index) =>
            {
                var agent = new AgentInstance();
                reader.ReadStructure($"Agent {index}", () =>
                {
                    agent.Load(reader);
                });
                return agent;
            });
            
            m_GridLabels = reader.ReadList("Grid Labels", (int index) =>
            {
                var gridLabel = new GridLabelTemplate();
                reader.ReadStructure($"Grid Label {index}", () =>
                {
                    gridLabel.Load(reader);
                });
                return gridLabel;
            });

            m_InteractivePoints = reader.ReadList("Interactive Points", (int index) =>
            {
                var point = new InteractivePoint();
                reader.ReadStructure($"Interactive Point {index}", () =>
                {
                    point.Load(reader);
                });
                return point;
            });
            
            m_Waypoints = reader.ReadList("Way Points", (int index) =>
            {
                var point = new Waypoint();
                reader.ReadStructure($"Way Point {index}", () =>
                {
                    point.Load(reader);
                });
                return point;
            });

            m_Layers = reader.ReadList("Layers", (int index) =>
            {
                return reader.ReadSerializable<BaseLayer>($"Layer {index}", false);
            });
            
            reader.ReadStructure("Building Editor", () =>
            {
                m_RoomEditor = new RoomEditor();
                m_RoomEditor.Load(reader);
            });
        }

        public void AddToDestroyQueue(GridObject obj)
        {
            if (obj != null)
            {
                if (!m_DestroyQueue.Contains(obj))
                {
                    m_DestroyQueue.Add(obj);
                }
            }
        }

        public void UpdateDestroyQueue()
        {
            foreach (var obj in m_DestroyQueue)
            {
                if (obj is AgentInstance)
                {
                    RemoveAgentInstance(obj as AgentInstance);
                }
                else if (obj is BuildingInstance)
                {
                    RemoveBuildingInstance(obj as BuildingInstance);
                }
                else
                {
                    Debug.Assert(false, $"todo {obj.GetType()}");
                }
            }
            m_DestroyQueue.Clear();
        }

        void SubscribeEvents()
        {
            WorldEditor.EventSystem.Register(this, (BuildingPositionChangeEvent e) => {
                if (m_ID == e.GridID)
                {
                    var building = GetBuildingInstance(e.BuildingID);
                    building?.OnPositionChanged();
                }
            });
			WorldEditor.EventSystem.Register(this, (UpdateBuildingMovementEvent e) => {
                if (m_ID == e.GridID)
                {
                    var building = GetBuildingInstance(e.BuildingID);
                    building?.UpdateMovement(changePosition: true, forceUpdate: false, e.IsSelectionValid);
                }
            });
			WorldEditor.EventSystem.Register(this, (DrawGridEvent e) => {
                if (m_ID == e.GridID)
                {
                    var building = GetBuildingInstance(e.BuildingID);
                    building?.DrawHandle();
                }
            });
			WorldEditor.EventSystem.Register(this, (AgentPositionChangeEvent e) => {
                if (m_ID == e.GridID)
                {
                    var agent = GetAgentInstance(e.AgentID);
                    agent.OnPositionChanged();
                }
            });
			WorldEditor.EventSystem.Register(this, (UpdateAgentMovementEvent e) => {
                if (m_ID == e.GridID)
                {
                    var agent = GetAgentInstance(e.AgentID);
                    agent.UpdateMovement(changePosition: true, forceUpdate: false, true);
                }
            });
			WorldEditor.EventSystem.Register(this, (SetAgentDestinationEvent e) => {
                if (m_ID == e.GridID)
                {
                    var agent = GetAgentInstance(e.AgentID);
                    agent.MoveTo(e.GuiScreenPoint);
                }
            });
			WorldEditor.EventSystem.Register(this, (UpdateLocatorMovementEvent e) => {
                if (m_ID == e.GridID)
                {
                    var locator = GetLocator(e.LocatorGameObject);
                    var coord = PositionToCoordinate(locator.Position);
                    locator.Position = CoordinateToGridCenterPosition(coord.x, coord.y);
                }
            });
        }

        void UnsubscribeEvents()
        {
            WorldEditor.EventSystem.Unregister(this);
        }

        public List<T> GetLayers<T>() where T : BaseLayer
        {
            var layers = new List<T>();
            foreach (var layer in m_Layers)
            {
                if (layer.GetType() == typeof(T))
                {
                    layers.Add(layer as T);
                }
            }
            return layers;
        }

        public T GetLayer<T>(int layerID) where T : BaseLayer
        {
            foreach (var layer in m_Layers)
            {
                if (layer.ID == layerID && layer.GetType() == typeof(T))
                {
                    return layer as T;
                }
            }
            return null;
        }

        public T GetLayer<T>() where T : BaseLayer
        {
            foreach (var layer in m_Layers)
            {
                if (layer.GetType() == typeof(T))
                {
                    return layer as T;
                }
            }
            return null;
        }

        public void HideLayers()
        {
            foreach (var layer in m_Layers)
            {
                layer.IsActive = false;
            }
        }

        public void SetLayerVisibility<T>(bool visible) where T : BaseLayer
        {
            GetLayer<T>().IsActive = visible;
        }

        public void AddLayer(BaseLayer layer)
        {
            var localPosition = new Vector3(0, m_LayerOffset, 0);
            layer.Initialize(m_View.RootGameObject.transform, localPosition, this);
            m_Layers.Add(layer);
        }

        public RegionTemplate GetRegionTemplate(int objectID)
        {
            foreach (var region in m_RegionTemplates)
            {
                if (region.ObjectID == objectID)
                {
                    return region;
                }
            }

            return null;
        }

        public bool IsRegionLocked(int objectID)
        {
            var regionRegion = GetRegionTemplate(objectID);
            if (regionRegion == null)
            {
                return false;
            }
            return regionRegion.Lock;
        }

        public AreaTemplate GetAreaTemplate(int objectID)
        {
            foreach (var region in m_RegionTemplates)
            {
                var area = region.GetAreaTemplate(objectID);
                if (area != null)
                {
                    return area;
                }
            }

            return null;
        }

        public LandTemplate GetLandTemplate(int objectID)
        {
            foreach (var region in m_RegionTemplates)
            {
                var land = region.GetLandTemplate(objectID);
                if (land != null)
                {
                    return land;
                }
            }

            return null;
        }

        public void AddGridLabel(GridLabelTemplate gridLabel)
        {
            m_GridLabels.Add(gridLabel);

            m_SelectedGridLabelIndex = m_GridLabels.Count - 1;
        }

        public GridLabelTemplate GetGridLabel(int objectID)
        {
            foreach (var gridLabel in m_GridLabels)
            {
                if (gridLabel.ObjectID == objectID)
                {
                    return gridLabel;
                }
            }

            return null;
        }

        public void RemoveGridLabel(int index)
        {
            if (index >= 0 && index < m_GridLabels.Count)
            {
                var layer = GetLayer<GridLabelLayer>();
                layer.Clear(m_GridLabels[index].ObjectID);
                layer.UpdateColors();

                m_GridLabels.RemoveAt(index);

                --m_SelectedGridLabelIndex;
                if (m_GridLabels.Count > 0 && m_SelectedGridLabelIndex < 0)
                {
                    m_SelectedGridLabelIndex = 0;
                }
            }
        }

        public void AddRegionTemplate(RegionTemplate template)
        {
            template.Initialize(this);

            m_RegionTemplates.Add(template);

            m_SelectedRegionIndex = m_RegionTemplates.Count - 1;
        }

        public void RemoveRegionTemplate(int index)
        {
            if (index >= 0 && index < m_RegionTemplates.Count) 
            {
                var regionLayer = GetLayer<RegionLayer>();
                regionLayer.Clear(m_RegionTemplates[index].ObjectID);
                regionLayer.UpdateColors();

                var areaLayer = GetLayer<AreaLayer>();
                foreach (var area in m_RegionTemplates[index].AreaTemplates)
                {
                    areaLayer.Clear(area.ObjectID);
                }
                areaLayer.UpdateColors();

                //var landLayer = GetLayer<LandLayer>();
                //foreach (var land in m_RegionTemplates[index].LandTemplates)
                //{
                //    landLayer.Clear(land.ObjectID);
                //}
                //landLayer.UpdateColors();

                m_RegionTemplates.RemoveAt(index);

                --m_SelectedRegionIndex;
                if (m_RegionTemplates.Count > 0 && m_SelectedRegionIndex < 0)
                {
                    m_SelectedRegionIndex = 0;
                }
            }
        }

        public void UpdateRegionNames()
        {
            if (m_RegionNames == null || m_RegionNames.Length != m_RegionTemplates.Count)
            {
                m_RegionNames = new string[m_RegionTemplates.Count];
            }

            for (var i = 0; i < m_RegionNames.Length; ++i)
            {
                m_RegionNames[i] = m_RegionTemplates[i].Name;
            }
        }

        public void ShowGridLine(bool show)
        {
            m_View.IsLineActive = show;
        }

        public void SetInteractivePointVisibility(bool visible)
        {
            foreach (var point in m_InteractivePoints)
            {
                point.Visible = visible;
            }
        }

        public void SetWayPointVisibility(bool visible)
        {
            foreach (var point in m_Waypoints)
            {
                point.Visible = visible;
            }
        }

        public void StartBuildingEditor()
        {
            m_RoomEditor.Start();
        }

        public void StopBuildingEditor()
        {
            m_RoomEditor.Stop();
        }

        public Vector2Int FindNearestWalkableCoordinate(int x, int y, int maxTryDistance)
        {
            for (var distance = 1; distance <= maxTryDistance; ++distance)
            {
                var minX = x - distance;
                var maxX = x + distance;
                var minY = y - distance;
                var maxY = y + distance;
                for (var yy = minY; yy <= maxY; ++yy)
                {
                    for (var xx = minX; xx <= maxX; ++xx)
                    {
                        var dx = Mathf.Abs(xx - x);
                        var dy = Mathf.Abs(yy - y);
                        if (dx != distance && dy != distance)
                        {
                            continue;
                        }

                        if (IsWalkable(xx, yy))
                        {
                            return new Vector2Int(xx, yy);
                        }
                    }
                }
            }

            Debug.LogWarning($"FindNearestWalkableCoordinate failed! {x}_{y}");
            return new Vector2Int(x, y);
        }

        public List<Vector2Int> GetRegionCoordinates(int regionID)
        {
            var layer = GetLayer<RegionLayer>();
            return layer.GetRegionCoordinates(regionID);
        }

        public List<Vector2Int> GetLandCoordinates(int landID)
        {
            return new();
            //var layer = GetLayer<LandLayer>();
            //return layer.GetLandCoordinates(landID);
        }

        public void UpdateLocatorSize(float size)
        {
            foreach (var region in m_RegionTemplates)
            {
                region.UpdateLocatorSize(size);
            }
        }

        public int GetBuildingInstanceCount(int buildingTemplateID)
        {
            var n = 0;
            foreach (var building in m_Buildings)
            {
                if (building.Template.ConfigID == buildingTemplateID)
                {
                    ++n;
                }
            }
            return n;
        }

        Locator GetLocator(GameObject locatorGameObject)
        {
            foreach (var region in m_RegionTemplates)
            {
                foreach (var area in region.AreaTemplates)
                {
                    foreach (var locator in area.Locators)
                    {
                        if (locator.GameObject == locatorGameObject)
                        {
                            return locator;
                        }
                    }
                }

                foreach (var land in region.LandTemplates)
                {
                    foreach (var locator in land.Locators)
                    {
                        if (locator.GameObject == locatorGameObject)
                        {
                            return locator;
                        }
                    }
                }
            }

            return null;
        }

        public Vector2Int FindNearestWalkableCoordinate(int x, int y, Vector2Int referencePoint, int maxTryDistance)
        {
            for (var distance = 1; distance <= maxTryDistance; ++distance)
            {
                var minX = x - distance;
                var maxX = x + distance;
                var minY = y - distance;
                var maxY = y + distance;
                for (var yy = minY; yy <= maxY; ++yy)
                {
                    for (var xx = minX; xx <= maxX; ++xx)
                    {
                        var dx = Mathf.Abs(xx - x);
                        var dy = Mathf.Abs(yy - y);
                        if (dx != distance && dy != distance)
                        {
                            continue;
                        }

                        if (IsWalkable(xx, yy))
                        {
                            return new Vector2Int(xx, yy);
                        }
                    }
                }
            }

            Debug.LogWarning($"FindNearestWalkableCoordinate failed! {x}_{y}");
            return new Vector2Int(x, y);
        }

        public void OnBuildTemplatePrefabChange(BuildingTemplate building)
        {
            foreach (var buildingInstance in m_Buildings)
            {
                if (buildingInstance.Template == building)
                {
                    buildingInstance.ChangeModel();
                }
            }
        }

        public void AddInteractivePoint(InteractivePoint point)
        {
            point.Initialize(this);

            m_InteractivePoints.Add(point);

            m_SelectedInteractivePointIndex = m_InteractivePoints.Count - 1;
        }

        public void RemoveInteractivePoint(int index)
        {
            if (index >= 0 && index < m_InteractivePoints.Count)
            {
                m_InteractivePoints[index].OnDestroy();

                m_InteractivePoints.RemoveAt(index);

                --m_SelectedInteractivePointIndex;
                if (m_InteractivePoints.Count > 0 && m_SelectedInteractivePointIndex < 0)
                {
                    m_SelectedInteractivePointIndex = 0;
                }
            }
        }

        public void AddWaypoint(Waypoint point)
        {
            point.Initialize(this);

            m_Waypoints.Add(point);

            m_SelectedWaypointIndex = m_Waypoints.Count - 1;
        }

        public void RemoveWaypoint(int index)
        {
            if (index >= 0 && index < m_Waypoints.Count)
            {
                m_Waypoints[index].OnDestroy();

                m_Waypoints.RemoveAt(index);

                --m_SelectedWaypointIndex;
                if (m_Waypoints.Count > 0 && m_SelectedWaypointIndex < 0)
                {
                    m_SelectedWaypointIndex = 0;
                }
            }
        }

        public void ShowRoots(bool visible)
        {
            m_BuildingRoot.SetActive(visible);
            m_AgentRoot.SetActive(visible);
            m_ScenePrefabRoot.SetActive(visible);
            m_EventPrefabRoot.SetActive(visible);
            m_InteractivePointRoot.SetActive(visible);
            m_WaypointRoot.SetActive(visible);
        }

        private GameObject CreateItemRoot(string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(m_View.RootGameObject.transform, worldPositionStays: false);
            root.AddComponent<NoKeyDeletion>();
            return root;
        }

        public void MoveBuildings(Vector2Int offset)
        {
            if (offset == Vector2Int.zero)
            {
                return;
            }

            var ox = offset.x * m_GridSize;
            var oz = offset.y * m_GridSize;

            var sortedBuildings = new List<BuildingInstance>(m_Buildings);
            sortedBuildings.Sort((BuildingInstance a, BuildingInstance b) => {
                var posA = a.Position;
                var posB = b.Position;
                if (Mathf.Abs(ox) > Mathf.Abs(oz))
                {
                    if (ox > 0)
                    {
                        if (posA.x < posB.x)
                        {
                            return -1;
                        }
                        return 1;
                    }
                    else
                    {
                        if (posA.x < posB.x)
                        {
                            return 1;
                        }
                        return -1;
                    }
                }
                else
                {
                    if (oz > 0)
                    {
                        if (posA.z < posB.z)
                        {
                            return -1;
                        }
                        return 1;
                    }
                    else
                    {
                        if (posA.z < posB.z)
                        {
                            return 1;
                        }
                        return -1;
                    }
                }
            });

            foreach (var building in sortedBuildings)
            {
                building.Position += new Vector3(ox, 0, oz);
                building.OnPositionChanged();
                building.UpdateMovement(true, true, true);
            }
        }

        public void MoveInteractivePoints(Vector2Int offset)
        {
            if (offset == Vector2Int.zero)
            {
                return;
            }

            var ox = offset.x * m_GridSize;
            var oz = offset.y * m_GridSize;

            foreach (var point in m_InteractivePoints)
            {
                point.Position += new Vector3(ox, 0, oz);
            }
        }

        public bool IsTeleporter(int x, int y)
        {
            foreach (var pt in m_Waypoints)
            {
                if (pt.Enabled)
                {
                    var pos = pt.GameObject.transform.position;
                    var coord = PositionToCoordinate(pos);
                    if (coord.x == x &&
                        coord.y == y)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public Vector2Int GetConnectedTeleporterCoordinate(int x, int y)
        {
            foreach (var pt in m_Waypoints)
            {
                if (pt.Enabled)
                {
                    var pos = pt.GameObject.transform.position;
                    var coord = PositionToCoordinate(pos);
                    if (coord.x == x &&
                        coord.y == y)
                    {
                        var connectPt = GetConnectedWaypoint(pt);
                        if (connectPt != null && connectPt.Enabled)
                        {
                            pos = connectPt.GameObject.transform.position;
                            return PositionToCoordinate(pos);
                        }
                    }
                }
            }
            return new Vector2Int(x, y);
        }

        public Waypoint GetConnectedWaypoint(Waypoint waypoint)
        {
            if (waypoint.ConnectedID == 0)
            {
                return FindNearestWaypoint(waypoint);
            }
            else
            {
                foreach (var pt in m_Waypoints)
                {
                    if (pt.ID == waypoint.ConnectedID && pt.Enabled)
                    {
                        return pt;
                    }
                }
            }

            return null;
        }

        private Waypoint FindNearestWaypoint(Waypoint waypoint)
        {
            Waypoint nearestPt = null;
            var minDistance = float.MaxValue;
            foreach (var pt in m_Waypoints)
            {
                if (pt.Enabled && pt != waypoint)
                {
                    var dis2 = (pt.GameObject.transform.position - waypoint.GameObject.transform.position).sqrMagnitude;
                    if (dis2 < minDistance)
                    {
                        minDistance = dis2;
                        nearestPt = pt;
                    }
                }
            }

            return nearestPt;
        }

        public void SetTeleporterState(int id, bool enable)
        {
        }

        public bool GetTeleporterState(int id)
        {
            return true;
        }

        int m_ID;

        [SerializeField]
        string m_Name;

        [SerializeField]
        int m_HorizontalGridCount;

        [SerializeField]
        int m_VerticalGridCount;

        [SerializeField]
        float m_GridSize;

        float m_Width;
        float m_Height;

        [SerializeField]
        Vector3 m_Position;

        [SerializeField]
        Quaternion m_Rotation;

        [SerializeField]
        List<BaseLayer> m_Layers = new();

        [SerializeField]
        List<RegionTemplate> m_RegionTemplates = new();

        [SerializeField]
        List<BuildingInstance> m_Buildings = new();

        [SerializeField]
        List<AgentInstance> m_Agents = new();

        [SerializeField]
        List<GridLabelTemplate> m_GridLabels = new();

        [SerializeField]
        List<InteractivePoint> m_InteractivePoints = new();

        [SerializeField]
        List<Waypoint> m_Waypoints = new();

        [SerializeField]
        bool m_GridLineVisible = true;

        readonly List<GridObject> m_DestroyQueue = new();

        [SerializeField]
        bool m_ShowInInspector = true;

        [SerializeField]
        int m_SelectedRegionIndex = -1;

        [SerializeField]
        int m_SelectedGridLabelIndex = -1;

        [SerializeField]
        int m_SelectedInteractivePointIndex = -1;

        [SerializeField]
        int m_SelectedWaypointIndex = -1;

        string[] m_RegionNames;

        GridView m_View;

        GameObject m_BuildingRoot;
        GameObject m_AgentRoot;
        GameObject m_ScenePrefabRoot;
        GameObject m_EventPrefabRoot;
        GameObject m_InteractivePointRoot;
        GameObject m_WaypointRoot;

        [SerializeField]
        GameObject m_DefaultInteractivePointStartPrefab;
        [SerializeField]
        GameObject m_DefaultInteractivePointEndPrefab;

        string m_StartPrefabGUID;
        string m_EndPrefabGUID;

        DisplayGridCost m_DisplayGridCost;

        CityEditor m_CityEditor;

        RoomEditor m_RoomEditor = new();

        static readonly Vector2Int[] m_NeighbourCoordinateOffsets = new Vector2Int[]
        {
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(0, 1),

            new Vector2Int(1, 1),
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(1, -1),
        };

        const int m_Version = 1;
        const float m_LayerOffset = 0.2f;
    }
}