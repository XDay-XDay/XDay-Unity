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
using System;

namespace XDay.WorldAPI.House.Editor
{
    partial class HouseEditor
    {
        public enum EditMode
        {
            House,
            HouseInstance,
        }

        public enum OperationType
        {
            Select,
            SetWalkable,
            Agent,
        }

        protected override void SceneGUISelectedInternal()
        {
            var e = Event.current;

            if (m_Operation == OperationType.Select)
            {
                CommandSelect(e, World);
            }
            else if (m_Operation == OperationType.SetWalkable)
            {
                if (m_EditMode == EditMode.House)
                {
                    CommandSetWalkable(e, World);
                    HandleUtility.AddDefaultControl(0);
                }
            }
            else if (m_Operation == OperationType.Agent)
            {
                CommandAddAgent(e, World);

                HandleUtility.AddDefaultControl(0);
            }

            m_TileIndicator?.Draw(new Color32(255, 0, 0, 255), centerAlignment: true);
            SceneView.RepaintAll();

            DrawTeleporterInstanceConnections();
        }

        protected override void SceneGUIInternal()
        {
            m_TileIndicator.Visible = false;
        }

        private void DrawTeleporterInstanceConnections()
        {
            if (m_EditMode == EditMode.HouseInstance)
            {
                var color = Handles.color;
                Handles.color = Color.magenta;
                foreach (var houseInstance in m_HouseInstances)
                {
                    foreach (var teleporterInstance in houseInstance.TeleporterInstances)
                    {
                        var connectedTeleporterInstance = GetTeleporterInstance(teleporterInstance.ConnectedID);
                        if (connectedTeleporterInstance != null)
                        {
                            Handles.DrawAAPolyLine(6, teleporterInstance.WorldPosition, connectedTeleporterInstance.WorldPosition);
                        }
                    }
                }
                Handles.color = color;
            }
        }

        private HouseTeleporterInstance GetTeleporterInstance(GameObject gameObject)
        {
            foreach (var house in m_HouseInstances)
            {
                foreach (var teleporter in house.TeleporterInstances)
                {
                    if (teleporter.GameObject == gameObject)
                    {
                        return teleporter;
                    }
                }
            }
            return null;
        }

        private HouseTeleporterInstance GetTeleporterInstance(int id)
        {
            if (id == 0)
            {
                return null;
            }

            foreach (var house in m_HouseInstances)
            {
                foreach (var teleporter in house.TeleporterInstances)
                {
                    if (teleporter.ConfigID == id)
                    {
                        return teleporter;
                    }
                }
            }
            return null;
        }

        private void CommandSetWalkable(Event e, IWorld world)
        {
            var house = GetActiveHouse();
            if (house == null)
            {
                return;
            }
            var worldPosition = Helper.GUIRayCastWithXZPlane(e.mousePosition, world.CameraManipulator.Camera, house.WorldHeight);

            m_TileIndicator.Visible = true;
            UpdateTileCursor(worldPosition);

            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && e.alt == false)
            {
                var coord = house.PositionToCoordinate(worldPosition);
                if (house.IsValidCoordinate(coord.x, coord.y))
                {
                    var minX = coord.x - m_BrushSize / 2;
                    var minY = coord.y - m_BrushSize / 2;
                    house.SetWalkableState(minX, minY, m_BrushSize, m_BrushSize, !e.control);
                }
            }
        }

        private void CommandSelect(Event e, IWorld world)
        {
            var worldPosition = Helper.GUIRayCastWithXZPlane(e.mousePosition, world.CameraManipulator.Camera);
            if ((e.type == EventType.MouseDown) && e.button == 0 && e.alt == false)
            {
                for (var i = 0; i < m_Houses.Count; ++i)
                {
                    var coord = m_Houses[i].PositionToCoordinate(worldPosition);
                    Debug.Log($"{coord}");
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

                if (m_EditMode == EditMode.House)
                {
                    for (var i = 0; i < m_Houses.Count; ++i)
                    {
                        pos = m_Houses[i].CalculateObjectCenterPosition(worldPosition.x, worldPosition.z, size, out var isValidCoordinate);
                        if (isValidCoordinate)
                        {
                            visible = true;
                            m_MeshIndicator.Prefab = AssetDatabase.GetAssetPath(model);
                            m_MeshIndicator.Position = pos;
                            break;
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < m_HouseInstances.Count; ++i)
                    {
                        pos = m_HouseInstances[i].CalculateObjectCenterPosition(worldPosition.x, worldPosition.z, size, out var isValidCoordinate);
                        pos.y = worldPosition.y;
                        if (isValidCoordinate)
                        {
                            visible = true;
                            m_MeshIndicator.Prefab = AssetDatabase.GetAssetPath(model);
                            m_MeshIndicator.Position = pos;
                            break;
                        }
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

        private void UpdateTileCursor(Vector3 worldPosition)
        {
            var house = GetActiveHouse();
            if (house == null)
            {
                return;
            }
            var coord = house.PositionToCoordinate(worldPosition);
            if (house.IsValidCoordinate(coord.x, coord.y))
            {
                var minX = coord.x - m_BrushSize / 2;
                var minY = coord.y - m_BrushSize / 2;
                var pos = (house.CoordinateToGridPosition(minX, minY) + house.CoordinateToGridPosition(minX + m_BrushSize, minY + m_BrushSize)) * 0.5f;
                pos.y = worldPosition.y;
                m_TileIndicator.Position = pos;
                m_TileIndicator.Size = m_BrushSize * house.GridSize;
            }
        }

        protected override void SceneViewControlInternal(Rect sceneViewRect)
        {
            CreateControls();

            EditorGUILayout.BeginHorizontal();
            var operation = (OperationType)m_OperationList.Render((int)m_Operation, m_OperationNames, 40);
            SetOperation(operation);

            var editMode = (EditMode)m_EditModeList.Render((int)m_EditMode, m_EditModeNames, 60);
            SetEditMode(editMode);

            var activeHouse = GetActiveHouse();
            if (activeHouse != null)
            {
                m_ShowGrid.Active = activeHouse.IsGridActive;
            }
            if (m_ShowGrid.Render(changeColor: true, activeHouse != null && m_EditMode == EditMode.House))
            {
                UndoSystem.SetAspect(activeHouse, "Show Grid", IAspect.FromBoolean(m_ShowGrid.Active), "Show House Grid", 0, UndoActionJoinMode.Both);
            }

            if (activeHouse != null)
            {
                m_ShowWalkableLayer.Active = activeHouse.IsWalkableLayerActive;
            }
            if (m_ShowWalkableLayer.Render(changeColor: true, activeHouse != null && m_EditMode == EditMode.House))
            {
                UndoSystem.SetAspect(activeHouse, "Show Walkable Layer", IAspect.FromBoolean(m_ShowWalkableLayer.Active), "Show House Walkable Layer", 0, UndoActionJoinMode.Both);
            }

            if (m_InteractivePointCopyButton.Render(enabled: true))
            {
                var point = GetSelectedInteractivePoint();
                point?.CopyStartCoordinate();
            }

            if (m_SyncButton.Render(enabled: m_EditMode == EditMode.House))
            {
                foreach (var house in m_Houses)
                {
                    CopyHouseModifications(house);
                }
            }

            if (m_ConnectButton.Render(enabled: true))
            {
                Connect();
            }

            if (m_DisconnectButton.Render(enabled: true))
            {
                Disconnect();
            }

            GUILayout.Space(20);

            if (activeHouse != null) 
            {
                var newHeight = m_GridHeightField.Render(activeHouse.GridHeight, 50);
                if (!Mathf.Approximately(newHeight, activeHouse.GridHeight))
                {
                    activeHouse.SetGridHeight(newHeight);
                }
            }

            if (operation == OperationType.SetWalkable)
            {
                m_BrushSize = m_BrushSizeField.Render(m_BrushSize, 50);
            }

            EditorGUILayout.EndHorizontal();

            DrawText();
        }

        private void Disconnect()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is GameObject gameObject)
                {
                    var teleporter = GetTeleporterInstance(gameObject);
                    if (teleporter != null)
                    {
                        teleporter.ConnectedID = 0;
                    }
                }
            }
        }

        private void Connect()
        {
            List<GameObject> gameObjects = new();
            foreach (var obj in Selection.objects)
            {
                if (obj is GameObject gameObject)
                {
                    gameObjects.Add(gameObject);
                }
            }

            if (gameObjects.Count == 2)
            {
                var teleporterA = GetTeleporterInstance(gameObjects[0]);
                var teleporterB = GetTeleporterInstance(gameObjects[1]);
                if (teleporterA != null &&
                    teleporterB != null)
                {
                    teleporterA.ConnectedID = teleporterB.ConfigID;
                    teleporterB.ConnectedID = teleporterA.ConfigID;
                }
                else
                {
                    EditorUtility.DisplayDialog("出错了", "选中两个传送点再操作", "确定");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("出错了", "选中两个传送点再操作", "确定");
            }
        }

        private void DrawText()
        {
            if (m_Operation == OperationType.SetWalkable)
            {
                EditorGUILayout.LabelField("绿色可行走,黑色不可行走");
                EditorGUILayout.LabelField("按住Ctrl+鼠标左键擦除");
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

                m_OperationList = new Popup("操作", "", 140);
                m_AllControls.Add(m_OperationList);

                m_EditModeList = new Popup("编辑模式", "", 140);
                m_AllControls.Add(m_EditModeList);

                m_BrushSizeField = new IntField("笔刷大小", "", 80);
                m_AllControls.Add(m_BrushSizeField);

                m_PointSizeField = new FloatField("点大小", "", 70);
                m_AllControls.Add(m_PointSizeField);

                m_GridHeightField = new FloatField("格子高度", "", 100);
                m_AllControls.Add(m_GridHeightField);

                m_ShowGrid = EditorWorldHelper.CreateToggleImageButton(true, "grid.png", "显示格子");
                m_AllControls.Add(m_ShowGrid);

                m_ShowWalkableLayer = EditorWorldHelper.CreateToggleImageButton(true, "walkable.png", "显示可行走区域");
                m_AllControls.Add(m_ShowWalkableLayer);

                m_InteractivePointCopyButton = EditorWorldHelper.CreateImageButton("copy.png", "选中交互点,让该交互点终点与起点重合");
                m_AllControls.Add(m_InteractivePointCopyButton);

                m_SyncButton = EditorWorldHelper.CreateImageButton("sync.png", "将房间模板的修改同步到房间中");
                m_AllControls.Add(m_SyncButton);

                m_ConnectButton = EditorWorldHelper.CreateImageButton("connect.png", "连接传送点");
                m_AllControls.Add(m_ConnectButton);

                m_DisconnectButton = EditorWorldHelper.CreateImageButton("disconnect.png", "断开传送点");
                m_AllControls.Add(m_DisconnectButton);
            }
        }

        private void CommandAddAgent(Event e, IWorld world)
        {
            House house = null;
            if (m_EditMode == EditMode.House)
            {
                house = GetActiveHouse();
            }
            else
            {
                house = GetActiveHouseInstance();
            }

            if (house == null)
            {
                return;
            }

            if (m_SelectedAgentTemplateIndex >= 0)
            {
                var worldPosition = Helper.GUIRayCastWithXZPlane(e.mousePosition, world.CameraManipulator.Camera, house.WorldHeight);
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
                        house.PlaceAgentInstance(NextObjectID, worldPosition, template);
                    }
                }
            }
        }

        private HouseInteractivePoint GetSelectedInteractivePoint()
        {
            var selection = Selection.activeGameObject;
            Transform transform = selection.transform;
            while (transform != null)
            {
                var behaviour = transform.GetComponent<HouseInteractivePointBehaviour>();
                if (behaviour != null)
                {
                    return behaviour.Data as HouseInteractivePoint;
                }
                transform = transform.parent;
            }
            return null;
        }

        private Popup m_OperationList;
        private Popup m_EditModeList;
        private ToggleImageButton m_ShowGrid;
        private ToggleImageButton m_ShowWalkableLayer;
        private IntField m_BrushSizeField;
        private FloatField m_PointSizeField;
        private FloatField m_GridHeightField;
        private ImageButton m_InteractivePointCopyButton;
        private ImageButton m_SyncButton;
        private ImageButton m_ConnectButton;
        private ImageButton m_DisconnectButton;
        private List<UIControl> m_AllControls;
        [SerializeField]
        private int m_BrushSize = 1;
        [SerializeField]
        private OperationType m_Operation = OperationType.Select;
        [SerializeField]
        private int m_SelectedAgentTemplateIndex = -1;
        private EditMode m_EditMode = EditMode.House;
        private static readonly string[] m_OperationNames = new string[]
        {
            "选择物体",
            "可行走区域",
            "机器人",
        };
        private static readonly string[] m_EditModeNames = new string[]
        {
            "房间模板",
            "房间",
        };
        private string[] m_TeleporterInstanceNames;
        private readonly List<int> m_TeleporterInstanceIDs = new();
        private readonly List<HouseTeleporterInstance> m_TempList = new();
    }
}
