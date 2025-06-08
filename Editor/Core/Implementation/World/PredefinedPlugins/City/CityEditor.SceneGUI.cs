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
using UnityEditor;
using System.Collections.Generic;
using XDay.UtilityAPI;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.City.Editor
{
    partial class CityEditor
    {
        public enum OperationType
        {
            Select,
            SetBuildState,
            SetGridLabel,
            SetRegion,
            SetArea,
            SetEvent,
            SetInteractivePoint,
            SetWayPoint,
            Building,
            RoomEdit,
            Agent,
        }

        protected override void SceneGUISelectedInternal()
        {
            var e = Event.current;

            if (m_Operation == OperationType.Select)
            {
                CommandSelect(e, World);
            }
            else if (m_Operation == OperationType.SetBuildState)
            {
                CommandSetBuildState(e, World);

                HandleUtility.AddDefaultControl(0);
            }
            else if (m_Operation == OperationType.SetGridLabel)
            {
                CommandSetGridLabel(e, World);

                HandleUtility.AddDefaultControl(0);
            }
            else if (m_Operation == OperationType.Building)
            {
                if (e.control)
                {
                    CommandRemoveBuildingInstance(e, World);
                }
                else
                {
                    CommandAddBuildingInstance(e, World);
                }

                HandleUtility.AddDefaultControl(0);
            }
            else if (m_Operation == OperationType.RoomEdit)
            {
                var grid = GetSelectedGrid();
                if (grid != null)
                {
                    grid.RoomEditor.DrawSceneGUI(e, World);
                }
            }
            else if (m_Operation == OperationType.SetRegion)
            {
                CommandSetRegion(e, World);
                HandleUtility.AddDefaultControl(0);
            }
            else if (m_Operation == OperationType.SetArea)
            {
                CommandSetArea(e, World);
                HandleUtility.AddDefaultControl(0);
            }
            else if (m_Operation == OperationType.SetEvent)
            {
                CommandSetEvent(e, World);
                HandleUtility.AddDefaultControl(0);
            }
            else if (m_Operation == OperationType.SetInteractivePoint)
            {
            }
            else if (m_Operation == OperationType.SetWayPoint)
            {
            }
            else if (m_Operation == OperationType.Agent)
            {
                CommandAddAgent(e, World);

                HandleUtility.AddDefaultControl(0);
            }

            foreach (var grid in m_Grids)
            {
                grid.RenderGridCost();
                grid.DrawSceneGUI();
            }

            m_TileIndicator?.Draw(CityEditorDefine.Red, centerAlignment:true);
            SceneView.RepaintAll();
        }

        protected override void SceneGUIInternal()
        {
            m_TileIndicator.Visible = false;
        }

        void CommandSetBuildState(Event e, IWorld world)
        {
            var worldPosition = Helper.GUIRayCastWithXZPlane(e.mousePosition, world.CameraManipulator.Camera);

            m_TileIndicator.Visible = true;
            UpdateTileCursor(worldPosition);

            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && e.alt == false)
            {
                for (var i = 0; i < m_Grids.Count; ++i)
                {
                    var coord = m_Grids[i].PositionToCoordinate(worldPosition);
                    if (m_Grids[i].IsValidCoordinate(coord.x, coord.y))
                    {
                        var minX = coord.x - m_BrushSize / 2;
                        var minY = coord.y - m_BrushSize / 2;
                        m_Grids[i].SetBuildState(minX, minY, m_BrushSize, m_BrushSize, !e.control);
                        break;
                    }
                }
            }
        }

        void CommandSelect(Event e, IWorld world)
        {
            var worldPosition = Helper.GUIRayCastWithXZPlane(e.mousePosition, world.CameraManipulator.Camera);
            if ((e.type == EventType.MouseDown) && e.button == 0 && e.alt == false)
            {
                for (var i = 0; i < m_Grids.Count; ++i)
                {
                    var coord = m_Grids[i].PositionToCoordinate(worldPosition);
                    Debug.Log($"{coord}");
                }
            }
        }

        void CommandSetWalkableState(Event e, IWorld world)
        {
            var worldPosition = Helper.GUIRayCastWithXZPlane(e.mousePosition, world.CameraManipulator.Camera);

            m_TileIndicator.Visible = true;

            UpdateTileCursor(worldPosition);

            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && e.alt == false)
            {
                for (var i = 0; i < m_Grids.Count; ++i)
                {
                    var coord = m_Grids[i].PositionToCoordinate(worldPosition);
                    if (m_Grids[i].IsValidCoordinate(coord.x, coord.y))
                    {
                        var minX = coord.x - m_BrushSize / 2;
                        var minY = coord.y - m_BrushSize / 2;
                        m_Grids[i].SetWalkableState(minX, minY, m_BrushSize, m_BrushSize, !e.control);
                        break;
                    }
                }
            }
        }

        void CommandSetRegion(Event e, IWorld world)
        {
            if (m_SelectedGridIndex >= 0)
            {
                var worldPosition = Helper.GUIRayCastWithXZPlane(e.mousePosition, world.CameraManipulator.Camera);

                m_TileIndicator.Visible = true;
                UpdateTileCursor(worldPosition);

                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && e.alt == false)
                {
                    var grid = GetSelectedGrid();
                    
                    if (grid.SelectedRegionIndex >= 0)
                    {
                        var coord = grid.PositionToCoordinate(worldPosition);
                        if (grid.IsValidCoordinate(coord.x, coord.y))
                        {
                            var minX = coord.x - m_BrushSize / 2;
                            var minY = coord.y - m_BrushSize / 2;
                            grid.SetRegion(minX, minY, m_BrushSize, m_BrushSize, !e.control);
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("错误", "没有创建区域", "确定");
                    }
                }
            }
        }

        void CommandSetGridLabel(Event e, IWorld world)
        {
            if (m_SelectedGridIndex >= 0)
            {
                var worldPosition = Helper.GUIRayCastWithXZPlane(e.mousePosition, world.CameraManipulator.Camera);

                m_TileIndicator.Visible = true;
                UpdateTileCursor(worldPosition);

                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && e.alt == false)
                {
                    var grid = GetSelectedGrid();

                    if (grid.SelectedGridLabelIndex >= 0)
                    {
                        var coord = grid.PositionToCoordinate(worldPosition);
                        if (grid.IsValidCoordinate(coord.x, coord.y))
                        {
                            var minX = coord.x - m_BrushSize / 2;
                            var minY = coord.y - m_BrushSize / 2;
                            grid.SetGridLabel(minX, minY, m_BrushSize, m_BrushSize, !e.control);
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("错误", "没有创建地面类型", "确定");
                    }
                }
            }
        }

        void CommandSetArea(Event e, IWorld world)
        {
            if (m_SelectedGridIndex >= 0)
            {
                var grid = GetSelectedGrid();
                var worldPosition = Helper.GUIRayCastWithXZPlane(e.mousePosition, world.CameraManipulator.Camera);

                m_TileIndicator.Visible = true;
                UpdateTileCursor(worldPosition);

                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && e.alt == false)
                {
                    if (GetSelectedAreaTemplate() != null)
                    {
                        var coord = grid.PositionToCoordinate(worldPosition);
                        if (grid.IsValidCoordinate(coord.x, coord.y))
                        {
                            var minX = coord.x - m_BrushSize / 2;
                            var minY = coord.y - m_BrushSize / 2;
                            grid.SetArea(minX, minY, m_BrushSize, m_BrushSize, !e.control);
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("错误", "没有创建地块", "确定");
                        e.Use();
                    }
                }
            }
        }

        void CommandSetLand(Event e, IWorld world)
        {
            if (m_SelectedGridIndex >= 0)
            {
                var grid = GetSelectedGrid();
                var worldPosition = Helper.GUIRayCastWithXZPlane(e.mousePosition, world.CameraManipulator.Camera);

                m_TileIndicator.Visible = true;
                UpdateTileCursor(worldPosition);

                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && e.alt == false)
                {
                    if (GetSelectedLandTemplate() != null)
                    {
                        var coord = grid.PositionToCoordinate(worldPosition);
                        if (grid.IsValidCoordinate(coord.x, coord.y))
                        {
                            var minX = coord.x - m_BrushSize / 2;
                            var minY = coord.y - m_BrushSize / 2;
                            grid.SetLand(minX, minY, m_BrushSize, m_BrushSize, !e.control);
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("错误", "没有创建绿地", "确定");
                        e.Use();
                    }
                }
            }
        }

        void CommandSetEvent(Event e, IWorld world)
        {
            if (m_SelectedGridIndex >= 0)
            {
                var grid = GetSelectedGrid();
                var worldPosition = Helper.GUIRayCastWithXZPlane(e.mousePosition, world.CameraManipulator.Camera);

                m_TileIndicator.Visible = true;
                UpdateTileCursor(worldPosition);

                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && e.alt == false)
                {
                    var eventTemplate = GetSelectedEventTemplate();
                    if (eventTemplate != null)
                    {
                        var coord = grid.PositionToCoordinate(worldPosition);
                        if (grid.IsValidCoordinate(coord.x, coord.y))
                        {
                            var minX = coord.x - m_BrushSize / 2;
                            var minY = coord.y - m_BrushSize / 2;
                            grid.SetEvent(minX, minY, m_BrushSize, m_BrushSize, !e.control);
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("错误", "没有创建事件", "确定");
                        e.Use();
                    }
                }
            }
        }

        void CommandAddBuildingInstance(Event e, IWorld world)
        {
            if (m_SelectedBuildingTemplateIndex >= 0)
            {
                var worldPosition = Helper.GUIRayCastWithXZPlane(e.mousePosition, world.CameraManipulator.Camera);
                var template = m_BuildingTemplates[m_SelectedBuildingTemplateIndex];
                UpdateModelIndicator(worldPosition, template.Size, template.Prefab);

                if (e.type == EventType.MouseDown && e.button == 0 && e.alt == false)
                {
                    if (template.Prefab == null)
                    {
                        EditorUtility.DisplayDialog("错误", "建筑模型未设置", "确定");
                        e.Use();
                    }
                    else
                    {
                        for (var i = 0; i < m_Grids.Count; ++i)
                        {
                            if (m_Grids[i].GetBuildingInstanceCount(template.ConfigID) == 0)
                            {
                                var ok = m_Grids[i].PlaceBuildingInstance(NextObjectID, worldPosition.x, worldPosition.z, template, this);
                                if (ok)
                                {
                                    break;
                                }
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("错误", "只能放置一个同类型建筑", "确定");
                                e.Use();
                            }
                        }
                    }
                }
            }
        }

        void CommandRemoveBuildingInstance(Event e, IWorld world)
        {
            m_MeshIndicator.Visible = false;
            if (e.type == EventType.MouseDown && e.button == 0 && e.alt == false)
            {
                var worldPosition = Helper.GUIRayCastWithXZPlane(e.mousePosition, world.CameraManipulator.Camera);
                for (var i = 0; i < m_Grids.Count; ++i)
                {
                    var ok = m_Grids[i].RemoveBuildingInstance(worldPosition.x, worldPosition.z);
                    if (ok)
                    {
                        break;
                    }
                }
            }
        }

        void CommandAddAgent(Event e, IWorld world)
        {
            if (m_SelectedAgentTemplateIndex >= 0)
            {
                var worldPosition = Helper.GUIRayCastWithXZPlane(e.mousePosition, world.CameraManipulator.Camera);
                var template = m_AgentTemplates[m_SelectedAgentTemplateIndex];
                UpdateModelIndicator(worldPosition, template.Size, template.Prefab);

                if (e.type == EventType.MouseDown && e.button == 0 && e.alt == false)
                {
                    if (template.Prefab == null)
                    {
                        EditorUtility.DisplayDialog("错误", "人物模型未设置", "确定");
                    }
                    else
                    {
                        for (var i = 0; i < m_Grids.Count; ++i)
                        {
                            var ok = m_Grids[i].PlaceAgentInstance(NextObjectID, worldPosition.x, worldPosition.z, template, this);
                            if (ok)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void HideModelIndicator()
        {
            m_MeshIndicator.Visible = false;
        }

        public Vector3 UpdateModelIndicator(Vector3 worldPosition, Vector2Int size, GameObject model)
        {
            var pos = new Vector3();
            if (model != null)
            {
                var visible = false;
                for (var i = 0; i < m_Grids.Count; ++i)
                {
                    pos = m_Grids[i].CalculateObjectCenterPosition(worldPosition.x, worldPosition.z, size, out var isValidCoordinate);
                    if (isValidCoordinate)
                    {
                        visible = true;
                        m_MeshIndicator.Prefab = AssetDatabase.GetAssetPath(model);
                        m_MeshIndicator.Position = pos;
                        m_MeshIndicator.Rotation = m_Grids[i].Rotation;
                        break;
                    }
                }
                m_MeshIndicator.Visible = visible;
            }
            else
            {
                m_MeshIndicator.Visible = false;
            }
            return pos;
        }

        void UpdateTileCursor(Vector3 worldPosition)
        {            
            for (var i = 0; i < m_Grids.Count; ++i)
            {
                var coord = m_Grids[i].PositionToCoordinate(worldPosition);
                if (m_Grids.Count == 1 || m_Grids[i].IsValidCoordinate(coord.x, coord.y))
                {
                    var minX = coord.x - m_BrushSize / 2;
                    var minY = coord.y - m_BrushSize / 2;
                    var pos = (m_Grids[i].CoordinateToGridPosition(minX, minY) + m_Grids[i].CoordinateToGridPosition(minX + m_BrushSize, minY + m_BrushSize)) * 0.5f;
                    pos.y = 0.05f;
                    m_TileIndicator.Position = pos;
                    m_TileIndicator.Rotation = m_Grids[i].Rotation;
                    m_TileIndicator.Size = m_BrushSize * m_Grids[i].GridSize;
                    break;
                }
            }            
        }

        protected override void SceneViewControlInternal(Rect sceneViewRect)
        {
            CreateControls();

            EditorGUILayout.BeginHorizontal();
            var operation = (OperationType)m_OperationList.Render((int)m_Operation, m_OperationNames, 40);
            SetOperation(operation);

            if (m_CreateNavigationData.Render(enabled: true))
            {
                var jobSystem = WorldEditor.WorldManager.TaskSystem;
                foreach (var grid in m_Grids)
                {
                    grid.CreateNavigationData(jobSystem);
                }
            }

            if (m_ShowOccupyState.Render(changeColor: true))
            {
                CommandShowGridOccupyState(m_ShowOccupyState.Active);
            }

            if (m_ShowGrid.Render(changeColor: true))
            {
                CommandShowGridLine(m_ShowGrid.Active);
            }

            //if (m_ShowGridCost.Draw(changeColor: true))
            //{
            //    CommandShowGridCost(m_ShowGridCost.IsActive);
            //}

            var selectedGrid = GetSelectedGrid();
            var enabled = selectedGrid != null && m_Operation == OperationType.SetBuildState;
            if (m_SetAll.Render(enabled))
            {
                SetAllGrid(selectedGrid, set: true);
            }

            if (m_ClearAll.Render(enabled))
            {
                SetAllGrid(selectedGrid, set: false);
            }

            //if (m_UpdateLandBounds.Draw(enabled: selectedGrid != null && m_Operation == OperationType.SetLand))
            //{
            //    var template = GetSelectedRegionTemplate();
            //    template.CalculateLandBounds();
            //}

            GUILayout.Space(20);

            m_BrushSize = m_BrushSizeField.Render(m_BrushSize, 50);

            GUILayout.Space(20);

            var newSize = m_LocatorSizeField.Render(m_LocatorSize, 40);
            if (!Mathf.Approximately(newSize, m_LocatorSize))
            {
                SetLocatorSize(newSize);
            }

            EditorGUILayout.EndHorizontal();

            DrawText();
        }

        void SetLocatorSize(float size)
        {
            m_LocatorSize = size;
            foreach (var grid in m_Grids)
            {
                grid.UpdateLocatorSize(size);
            }
        }

        void SetAllGrid(Grid grid, bool set)
        {
            if (EditorUtility.DisplayDialog("注意", "确定设置?", "确定", "取消"))
            {
                switch (m_Operation)
                {
                    case OperationType.SetBuildState:
                        grid.GetLayer<BuildableLayer>().SetAll(set);
                        break;
                }
            }
        }

        void DrawText()
        {
            if (m_Operation == OperationType.SetBuildState)
            {
                EditorGUILayout.LabelField("绿色可建造,黑色不可建造");
                EditorGUILayout.LabelField("按住Ctrl+鼠标左键擦除");
            }
            else if (m_Operation == OperationType.SetGridLabel)
            {
                EditorGUILayout.LabelField("按住Ctrl+鼠标左键擦除");
                EditorGUILayout.LabelField("没有绘制的格子不可走");
            }
            else if (m_Operation == OperationType.SetRegion)
            {
                EditorGUILayout.LabelField("黑色表示未设置区域,其他颜色表示有效区域");
                EditorGUILayout.LabelField("按住Ctrl+鼠标左键擦除");
            }
            else if (m_Operation == OperationType.SetArea)
            {
                EditorGUILayout.LabelField("黑色表示未设置地块,灰色表示其他区域,其他颜色表示有效地块");
                EditorGUILayout.LabelField("按住Ctrl+鼠标左键擦除");
            }
            else if (m_Operation == OperationType.SetEvent)
            {
                EditorGUILayout.LabelField("黑色表示未设置事件,灰色表示其他区域,其他颜色表示有效事件范围");
                EditorGUILayout.LabelField("按住Ctrl+鼠标左键擦除");
            }
            else if (m_Operation == OperationType.SetWayPoint)
            {
            }
            else if (m_Operation == OperationType.SetInteractivePoint)
            {
            }
            else if (m_Operation == OperationType.Building)
            {
                EditorGUILayout.LabelField("列表选中要摆放的建筑，鼠标左键放置");
                EditorGUILayout.LabelField("按住Ctrl+鼠标左键删除建筑");
            }
        }

        public override List<UIControl> GetSceneViewControls()
        {
            return m_AllControls;
        }

        void CreateControls()
        {
            if (m_AllControls == null)
            {
                m_AllControls = new List<UIControl>();

                m_OperationList = new Popup("操作", "", 200);
                m_AllControls.Add(m_OperationList);

                m_BrushSizeField = new IntField("笔刷大小", "", 80);
                m_AllControls.Add(m_BrushSizeField);

                m_LocatorSizeField = new FloatField("点大小", "", 70);
                m_AllControls.Add(m_LocatorSizeField);

                m_CreateNavigationData = EditorWorldHelper.CreateImageButton("build.png", "生成寻路数据");
                m_ShowOccupyState = EditorWorldHelper.CreateToggleImageButton(false, "show.png", "显示格子是否被占领");
                m_ShowGridCost = EditorWorldHelper.CreateToggleImageButton(false, "cost.png", "显示格子的消耗");
                m_ShowGrid = EditorWorldHelper.CreateToggleImageButton(true, "grid.png", "显示格子");
                m_SetAll = EditorWorldHelper.CreateImageButton("set.png", "涂上所有格子");
                m_ClearAll = EditorWorldHelper.CreateImageButton("clear.png", "清除所有格子");
                m_UpdateLandBounds = EditorWorldHelper.CreateImageButton("refresh.png", "更新绿地序号位置");
            }
        }

        Popup m_OperationList;
        ImageButton m_CreateNavigationData;
        ImageButton m_SetAll;
        ImageButton m_ClearAll;
        ImageButton m_UpdateLandBounds;
        ToggleImageButton m_ShowOccupyState;
        ToggleImageButton m_ShowGridCost;
        ToggleImageButton m_ShowGrid;
        IntField m_BrushSizeField;
        FloatField m_LocatorSizeField;
        List<UIControl> m_AllControls;

        [SerializeField]
        int m_BrushSize = 1;

        [SerializeField]
        float m_LocatorSize = 5.0f;

        [SerializeField]
        OperationType m_Operation = OperationType.Select;

        static readonly string[] m_OperationNames = new string[]
        {
            "选择物体",
            "建造区域",
            "寻路地面类型",
            "区域",
            "地块",
            "事件",
            "交互点",
            "捷径点",
            "建筑",
            "房间编辑",
            "NPC"
        };
    }
}