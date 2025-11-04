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
using UnityEditor;
using XDay.WorldAPI.Editor;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.City.Editor
{
    [WorldPluginMetadata("城市", "city_editor_data", typeof(CityEditorCreateWindow), true)]
    internal partial class CityEditor : EditorWorldPlugin
    {
        public override string Name { get => m_Name; set { m_Name = value; m_Root.name = value; } }
        public override GameObject Root => m_Root;
        public override List<string> GameFileNames => new() { "city" };
        public override string TypeName => "CityEditor";
        public int NextObjectID => --m_NextObjectID;
        public Grid FirstGrid
        {
            get
            {
                if (m_Grids.Count == 0)
                {
                    return null;
                }
                return m_Grids[0];
            }
        }
        public override int FileIDOffset => WorldDefine.CITY_SYSTEM_FILE_ID_OFFSET;

        public CityEditor()
        {
        }

        public CityEditor(int id, int objectIndex, string name)
            : base(id, objectIndex)
        {
            m_Name = name;

            var defaultTile = new GridTileTemplate(NextObjectID, "障碍")
            {
                Cost = CityEditorDefine.BLOCK_COST,
                Color = Color.black,
                ReadOnly = true
            };
            m_Tiles.Add(defaultTile);
        }

        protected override void InitInternal()
        {
            m_Root = new GameObject(m_Name);
            m_GridRoot = new GameObject("格子");

            m_Root.transform.SetParent(World.Root.transform);
            m_Root.transform.position = new Vector3(0, 0.1f, 0);
            m_GridRoot.transform.SetParent(m_Root.transform);

            m_Root.AddComponent<NoKeyDeletion>();
            m_GridRoot.AddComponent<NoKeyDeletion>();

            m_MeshIndicator = IMeshIndicator.Create(World);

            foreach (var building in m_BuildingTemplates)
            {
                building.Initialize(this);
            }

            foreach (var grid in m_Grids)
            {
                grid.Initialize(this, m_GridRoot.transform, World);
                grid.SetWayPointVisibility(m_ShowAllWaypoints);
                grid.SetInteractivePointVisibility(m_ShowAllInteractivePoints);
            }

            Refresh();

            SubscribeEvents();

            if (m_SelectedGridIndex < 0 && m_Grids.Count > 0)
            {
                m_SelectedGridIndex = 0;
            }

            m_TileIndicator = IGizmoCubeIndicator.Create();

            SetLocatorSize(m_LocatorSize);

            Selection.selectionChanged += OnSelectionChanged;

            var oldOperation = m_Operation;
            SetOperation(OperationType.Building);
            SetOperation(oldOperation);
        }

        protected override void UninitInternal()
        {
            Selection.selectionChanged -= OnSelectionChanged;

            UnsubscribeEvents();

            foreach (var grid in m_Grids)
            {
                grid.OnDestroy();
            }
            m_Grids.Clear();

            m_MeshIndicator.OnDestroy();

            Helper.DestroyUnityObject(m_Root);
        }

        public GridTileTemplate GetGridTileTemplate(int id)
        {
            if (id == 0)
            {
                return null;
            }

            foreach (var tile in m_Tiles)
            {
                if (tile.ID == id)
                {
                    return tile;
                }
            }

            return null;
        }

        public BuildingTemplate GetBuildingTemplate(int id)
        {
            if (id == 0)
            {
                return null;
            }

            foreach (var building in m_BuildingTemplates)
            {
                if (building.ID == id)
                {
                    return building;
                }
            }

            return null;
        }

        public AgentTemplate GetAgentTemplate(int id)
        {
            if (id == 0)
            {
                return null;
            }

            foreach (var agent in m_AgentTemplates)
            {
                if (agent.ID == id)
                {
                    return agent;
                }
            }

            return null;
        }

        Grid AddGrid(string name, int horizontalGridCount, int verticalGridCount, float gridSize, Quaternion rotation, GridTileInstance[] tiles)
        {
            var layers = CreateLayers(horizontalGridCount, verticalGridCount, gridSize, rotation);

            var pos = new Vector3(World.Width * 0.5f, 0, World.Height * 0.5f);
            var grid = new Grid(NextObjectID, name, horizontalGridCount, verticalGridCount, gridSize, pos, rotation, layers);
            grid.Initialize(this, m_GridRoot.transform, World);
            m_Grids.Add(grid);

            if (m_SelectedGridIndex < 0)
            {
                m_SelectedGridIndex = 0;
            }

            Refresh();

            return grid;
        }

        void RemoveGrid(Grid grid)
        {
            grid.OnDestroy();
            m_Grids.Remove(grid);
            --m_SelectedGridIndex;
            if (m_Grids.Count > 0 && m_SelectedGridIndex < 0)
            {
                m_SelectedGridIndex = 0;
            }
        }

        Grid GetGrid(int id)
        {
            foreach (var grid in m_Grids)
            {
                if (grid.ID == id)
                {
                    return grid;
                }
            }
            return null;
        }

        protected override void UpdateInternal(float dt)
        {
            foreach (var grid in m_GridDestroyQueue)
            {
                RemoveGrid(grid);
            }
            m_GridDestroyQueue.Clear();

            foreach (var grid in m_Grids)
            {
                grid.Update(dt);
            }
        }

        public void AddToDestroyQueue(Grid grid)
        {
            if (!m_GridDestroyQueue.Contains(grid))
            {
                m_GridDestroyQueue.Add(grid);
            }
        }

        public void SetOperation(OperationType operation)
        {
            if (m_Operation != operation)
            {
                m_Operation = operation;
                Refresh();
            }
        }

        void Refresh()
        {
            if (m_Operation == OperationType.Select)
            {
                Tools.hidden = false;
                return;
            }
            else if (m_Operation == OperationType.SetWayPoint)
            {
                Tools.hidden = false;
            }
            else if (m_Operation != OperationType.SetInteractivePoint)
            {
                Tools.hidden = true;
            }

            foreach (var grid in m_Grids)
            {
                grid.HideLayers();
                grid.GetLayer<AreaLayer>().DisableGrayDisplay();
                grid.GetLayer<EventLayer>().DisableGrayDisplay();
            }

            switch (m_Operation)
            {
                case OperationType.SetBuildState:
                    foreach (var grid in m_Grids)
                    {
                        grid.SetLayerVisibility<BuildableLayer>(true);
                    }
                    break;
                case OperationType.SetGridLabel:
                    foreach (var grid in m_Grids)
                    {
                        grid.SetLayerVisibility<GridLabelLayer>(true);
                    }
                    break;
                case OperationType.SetRegion:
                    foreach (var grid in m_Grids)
                    {
                        grid.SetLayerVisibility<RegionLayer>(true);
                    }
                    break;
                case OperationType.SetArea:
                    foreach (var grid in m_Grids)
                    {
                        Grayout(grid);
                        grid.SetLayerVisibility<AreaLayer>(true);
                    }
                    break;
                case OperationType.SetEvent:
                    foreach (var grid in m_Grids)
                    {
                        Grayout(grid);
                        grid.SetLayerVisibility<EventLayer>(true);
                    }
                    break;
                case OperationType.SetInteractivePoint:
                    break;
                case OperationType.SetWayPoint:
                    break;
                default:
                    break;
            }

            if (m_Operation == OperationType.RoomEdit)
            {
                foreach (var grid in m_Grids)
                {
                    grid.StartBuildingEditor();
                }

                CommandShowGridLine(true);
            }
            else
            {
                foreach (var grid in m_Grids)
                {
                    grid.StopBuildingEditor();
                }
            }
        }

        protected override void SelectionChangeInternal(bool selected)
        {
            if (!selected)
            {
                Tools.hidden = false;
            }
            else
            {
                SetOperation(m_Operation);
            }
        }

        void SubscribeEvents()
        {
            WorldEditor.EventSystem.Register(this, (DestroyGridEvent e) => {
                var grid = GetGrid(e.ID);
                if (grid != null)
                {
                    AddToDestroyQueue(grid);
                }
            });
            WorldEditor.EventSystem.Register(this, (DestroyBuildingEvent e) => {
                var grid = GetGrid(e.GridID);
                if (grid != null)
                {
                    grid.AddToDestroyQueue(grid.GetBuildingInstance(e.BuildingID));
                }
            });
            WorldEditor.EventSystem.Register(this, (DestroyAgentEvent e) => {
                var grid = GetGrid(e.GridID);
                if (grid != null)
                {
                    grid.AddToDestroyQueue(grid.GetAgentInstance(e.AgentID));
                }
            });
        }

        void UnsubscribeEvents()
        {
            WorldEditor.EventSystem.Unregister(this);
        }

        List<BaseLayer> CreateLayers(int horizontalGridCount, int verticalGridCount, float gridSize, Quaternion rotation)
        {
            var layers = new List<BaseLayer>();

            var objectLayer = new ObjectLayer(World.AllocateObjectID(), "Object Layer", horizontalGridCount, verticalGridCount, gridSize);
            layers.Add(objectLayer);

            var buildStateLayer = new BuildableLayer(World.AllocateObjectID(), "Build State Layer", horizontalGridCount, verticalGridCount, gridSize);
            layers.Add(buildStateLayer);

            var occupyStateLayer = new OccupyStateLayer(World.AllocateObjectID(), "Occupy State Layer", horizontalGridCount, verticalGridCount, gridSize);
            layers.Add(occupyStateLayer);

            var walkableLayer = new WalkableLayer(World.AllocateObjectID(), "Walkable Layer", horizontalGridCount, verticalGridCount, gridSize);
            layers.Add(walkableLayer);

            var regionLayer = new RegionLayer(World.AllocateObjectID(), "Region Layer", horizontalGridCount, verticalGridCount, gridSize);
            layers.Add(regionLayer);

            var gridLabelLayer = new GridLabelLayer(World.AllocateObjectID(), "Grid Label Layer", horizontalGridCount, verticalGridCount, gridSize);
            layers.Add(gridLabelLayer);

            var areaLayer = new AreaLayer(World.AllocateObjectID(), "Area Layer", horizontalGridCount, verticalGridCount, gridSize);
            layers.Add(areaLayer);

            return layers;
        }

        void Grayout(Grid grid)
        {
            var areaLayer = grid.GetLayer<AreaLayer>();
            var region = GetSelectedRegionTemplate();
            if (region != null)
            {
                areaLayer.Grayout(region.ObjectID);
            }

            var eventLayer = grid.GetLayer<EventLayer>();
            if (region != null)
            {
                eventLayer.Grayout(region.ObjectID);
            }
        }

        void OnSelectionChanged()
        {
            //Tools.hidden = !IsScenePrefabInstance(Selection.activeGameObject);
        }

        bool IsScenePrefabInstance(GameObject gameObject)
        {
            var grid = GetSelectedGrid();
            if (grid != null)
            {
                foreach (var temp in grid.RegionTemplates)
                {
                    if (temp.PrefabInstance == gameObject)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void OnBuildTemplatePrefabChange(BuildingTemplate building)
        {
            foreach (var grid in m_Grids)
            {
                grid.OnBuildTemplatePrefabChange(building);
            }
        }

        public override void AddObjectUndo(IWorldObject obj, int lod, int objectIndex)
        {
            throw new System.NotImplementedException();
        }

        public override void DestroyObjectUndo(int objectID)
        {
            throw new System.NotImplementedException();
        }

        public override IWorldObject QueryObjectUndo(int objectID)
        {
            throw new System.NotImplementedException();
        }

        private int m_NextObjectID;
        [SerializeField]
        private string m_Name;
        private GameObject m_Root;
        private GameObject m_GridRoot;
        [SerializeField]
        private List<Grid> m_Grids = new();
        [SerializeField]
        private List<GridTileTemplate> m_Tiles = new();
        [SerializeField]
        private List<BuildingTemplate> m_BuildingTemplates = new();
        [SerializeField]
        private List<AgentTemplate> m_AgentTemplates = new();
        private List<Grid> m_GridDestroyQueue = new();
        private IMeshIndicator m_MeshIndicator;
        private IGizmoCubeIndicator m_TileIndicator;
    }
}

