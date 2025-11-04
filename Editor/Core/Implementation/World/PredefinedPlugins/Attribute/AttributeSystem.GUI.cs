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
using XDay.UtilityAPI.Editor;
using XDay.UtilityAPI.Math;

namespace XDay.WorldAPI.Attribute.Editor
{
    public partial class AttributeSystem
    {
        protected override void SceneViewControlInternal(Rect sceneViewRect)
        {
            CreateControls();

            EditorGUILayout.BeginHorizontal();

            var operation = (Operation)m_OperationPopup.Render((int)m_Operation, m_OperationNames, 60);
            SetOperation(operation);

            GUILayout.Space(20);

            UpdateLayerNames();
            var layerIndex = GetLayerIndex(m_ActiveLayerID);
            var newLayerIndex = m_LayersPopup.Render(layerIndex, m_LayerNames, 40);
            SetCurrentLayer(newLayerIndex);

            var layer = QueryLayer(m_ActiveLayerID);
            if (layer != null)
            {
                m_LayerVisibilityButton.Active = layer.IsEnabled();
            }
            if (m_LayerVisibilityButton.Render(true, GUI.enabled && layer != null))
            {
                SetLayerVisibility(m_ActiveLayerID, m_LayerVisibilityButton.Active);
            }

            if (layer != null)
            {
                m_ShowGrid.Active = layer.GridVisible;
            }
            if (m_ShowGrid.Render(true, GUI.enabled && layer != null))
            {
                SetGridVisibility(m_ShowGrid.Active);
            }

            if (m_AddLayerButton.Render(Inited))
            {
                CommandAddLayer();
            }

            if (m_RemoveLayerButton.Render(Inited))
            {
                CommandDeleteLayer();
            }

            if (m_EditLayerNameButton.Render(Inited))
            {
                CommandEditLayerName();
            }

            if (m_ActiveLayerID != 0)
            {
                layer.Type = (LayerType)m_TypePopup.Render((int)layer.Type, m_LayerTypeNames, 30);
            }

            GUILayout.Space(20);

            var brushSize = m_BrushSizeControl.Render(m_BrushSize, 50);
            SetBrushSize(brushSize);

            if (m_ActiveLayerID != 0)
            {
                var newColor = m_ColorField.Render(layer.Color, 50);
                if (newColor != layer.Color)
                {
                    SetColor(newColor);
                }
            }

            EditorGUILayout.EndHorizontal();

            DrawHooks();

            DrawText();
        }

        private void DrawHooks()
        {
            foreach (var hook in m_Hooks)
            {
                if (GUILayout.Button(new GUIContent(hook.DisplayName, hook.Tooltip), GUILayout.MaxWidth(hook.ButtonWidth)))
                {
                    hook.Run();
                }
            }
        }

        private void DrawText()
        {
            var layer = QueryLayer(m_ActiveLayerID);
            if (layer != null)
            {
                if (m_TipsStyle == null)
                {
                    m_TipsStyle = new GUIStyle(GUI.skin.label);
                    m_TipsStyle.normal.textColor = Color.cyan;
                }

                EditorGUILayout.LabelField($"横向格子数: {layer.HorizontalGridCount}个", m_TipsStyle);
                EditorGUILayout.LabelField($"纵向格子数: {layer.VerticalGridCount}个", m_TipsStyle);
                EditorGUILayout.LabelField($"格子宽: {layer.GridWidth}米", m_TipsStyle);
                EditorGUILayout.LabelField($"格子高: {layer.GridHeight}米", m_TipsStyle);
                EditorGUILayout.LabelField($"Ctrl+1: 选择模式", m_TipsStyle);
                EditorGUILayout.LabelField($"Ctrl+2: 绘制模式", m_TipsStyle);
                EditorGUILayout.LabelField($"Ctrl+鼠标左键: 擦除", m_TipsStyle);
                EditorGUILayout.LabelField($"Q: 增大笔刷", m_TipsStyle);
                EditorGUILayout.LabelField($"W: 缩小笔刷", m_TipsStyle);
            }
        }

        protected override void InspectorGUIInternal()
        {
            m_ShowInspectorUI = EditorGUILayout.Foldout(m_ShowInspectorUI, "属性");
            if (!m_ShowInspectorUI)
            {
                return;
            }

            m_ViewScrollPosition = EditorGUILayout.BeginScrollView(m_ViewScrollPosition);
            EditorHelper.IndentLayout(() =>
            {
                m_EnableAutoObstacleGeneration = EditorGUILayout.ToggleLeft("开启自动障碍物计算", m_EnableAutoObstacleGeneration);

                if (GUILayout.Button("计算障碍物"))
                {
                    CalculateIfGridHasObstacles(true);
                }
            });

            EditorGUILayout.EndScrollView();
        }

        protected override void SceneGUISelectedInternal()
        {
            var evt = Event.current;
            if (evt.type == EventType.KeyDown && evt.shift == false)
            {
                if (evt.keyCode == KeyCode.Alpha1 && evt.control)
                {
                    SetOperation(Operation.Select);
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.Alpha2 && evt.control)
                {
                    SetOperation(Operation.SetGrid);
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.Q)
                {
                    SetBrushSize(m_BrushSize + 1);
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.W)
                {
                    SetBrushSize(m_BrushSize - 1);
                    evt.Use();
                }
            }

            var worldPosition = Helper.GUIRayCastWithXZPlane(evt.mousePosition, World.CameraManipulator.Camera);

            if (m_Operation == Operation.Select)
            {
            }
            else if (m_Operation == Operation.SetGrid)
            {
                HandleSetGrid(evt, worldPosition);

                HandleUtility.AddDefaultControl(0);
            }
            else
            {
                Debug.Assert(false, $"todo {m_Operation}");
            }
        }

        void HandleSetGrid(Event e, Vector3 worldPosition)
        {
            if (m_ActiveLayerID == 0)
            {
                return;
            }

            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                    && e.button == 0
                    && e.alt == false)
            {
                if (e.type == EventType.MouseDown)
                {
                    UndoSystem.NextGroup();
                }

                var layer = QueryLayer(m_ActiveLayerID);

                var coord = layer.PositionToCoordinate(worldPosition.x, worldPosition.z);
                var minX = coord.x - m_BrushSize / 2;
                var minY = coord.y - m_BrushSize / 2;
                var maxX = minX + m_BrushSize - 1;
                var maxY = minY + m_BrushSize - 1;

                var valid = Helper.GetIntersection(minX, minY, maxX, maxY, 0, 0, layer.HorizontalGridCount - 1, layer.VerticalGridCount - 1, 
                    out var intersectionMinX, out var intersectionMinY, out var intersectionMaxX, out var intersectionMaxY);
                if (valid)
                {
                    //使用Undo/Redo方式
                    var data = new uint[(intersectionMaxX - intersectionMinX + 1) * (intersectionMaxY - intersectionMinY + 1)];
                    var idx = 0;
                    var value = e.control ? 0 : 1u;
                    for (var y = intersectionMinY; y <= intersectionMaxY; ++y)
                    {
                        for (var x = intersectionMinX; x <= intersectionMaxX; ++x)
                        {
                            data[idx++] = value;
                        }
                    }
                    UndoSystem.SetAspect(this, 
                        $"{GridAttributeName}[{m_ActiveLayerID}, {intersectionMinX},{intersectionMinY},{intersectionMaxX - intersectionMinX + 1},{intersectionMaxY - intersectionMinY + 1}]", 
                        IAspect.FromArray(data, makeCopy: true), 
                        "Set Grid Attribute", 
                        0, UndoActionJoinMode.None);
                }
            }

            DrawHandle(worldPosition);

            SceneView.RepaintAll();
        }

        void DrawHandle(Vector3 worldPosition)
        {
            var layer = QueryLayer(m_ActiveLayerID);
            var coord = layer.PositionToCoordinate(worldPosition.x, worldPosition.z);
            var minX = coord.x - m_BrushSize / 2;
            var minY = coord.y - m_BrushSize / 2;
            var maxX = minX + m_BrushSize;
            var maxY = minY + m_BrushSize;
            var minPos = layer.CoordinateToPosition(minX, minY);
            var maxPos = layer.CoordinateToPosition(maxX, maxY);
            var oldColor = Handles.color;
            Handles.color = Color.red;
            Handles.DrawWireCube((minPos + maxPos) * 0.5f, maxPos - minPos);
            Handles.color = oldColor;
        }

        void CreateControls()
        {
            if (m_Controls == null)
            {
                m_Controls = new List<UIControl>();

                m_OperationPopup = new Popup("当前操作", "", 160);
                m_Controls.Add(m_OperationPopup);

                m_TypePopup = new Popup("类型", "", 120);
                m_Controls.Add(m_TypePopup);

                m_BrushSizeControl = new IntField("笔刷大小", "", 100);
                m_Controls.Add(m_BrushSizeControl);

                m_ColorField = new ColorField("颜色", "", 100);
                m_Controls.Add(m_ColorField);

                m_LayersPopup = new Popup("当前层", "", 170);
                m_Controls.Add(m_LayersPopup);
                m_AddLayerButton = CreateIconButton("add.png", "新建层");
                m_RemoveLayerButton = CreateIconButton("remove.png", "删除当前层");
                m_EditLayerNameButton = CreateIconButton("edit.png", "编辑层名称");
                m_LayerVisibilityButton = EditorWorldHelper.CreateToggleImageButton(false, "show.png", "显隐组");
                m_Controls.Add(m_LayerVisibilityButton);

                m_ShowGrid = EditorWorldHelper.CreateToggleImageButton(false, "grid.png", "显隐格子");
                m_Controls.Add(m_ShowGrid);
            }
        }

        private ImageButton CreateIconButton(string textureName, string tooltip)
        {
            var button = EditorWorldHelper.CreateImageButton(textureName, tooltip);
            m_Controls.Add(button);
            return button;
        }

        public override bool SetAspect(int objectID, string name, IAspect property)
        {
            if (!base.SetAspect(objectID, name, property))
            {
                if (name == "Layer Name")
                {
                    var layer = QueryObjectUndo(objectID) as LayerBase;
                    layer.Name = property.GetString();
                    m_Renderer.SetAspect(objectID, name);
                }
                else if (name == "Layer Visibility")
                {
                    var layer = QueryObjectUndo(objectID) as LayerBase;
                    layer.SetEnabled(property.GetBoolean());
                    m_Renderer.SetAspect(objectID, name);
                }
                else if (name == "Grid Visible")
                {
                    var layer = QueryObjectUndo(objectID) as LayerBase;
                    layer.GridVisible = property.GetBoolean();
                    m_Renderer.SetAspect(objectID, name);
                }
                else if (name == "Layer Color")
                {
                    var layer = QueryObjectUndo(objectID) as LayerBase;
                    layer.Color = property.GetColor();
                    m_Renderer.SetAspect(objectID, name);
                }
                else if (name.StartsWith(GridAttributeName))
                {
                    ParseGridAttribute(name, out var range, out var layerID);
                    var layer = QueryLayer(layerID) as Layer;
                    var layerIndex = GetLayerIndex(layerID);
                    var set = property.GetArray<uint>();
                    for (var y = range.Min.y; y <= range.Max.y; ++y)
                    {
                        for (var x = range.Min.x; x <= range.Max.x; ++x)
                        {
                            layer.Set(x, y, set[(y - range.Min.y) * range.Size.x + x - range.Min.x]);
                            m_Renderer.UpdateGrid(layerIndex, x, y);
                        }
                    }
                    
                }
            }
            return true;
        }

        public override IAspect GetAspect(int objectID, string name)
        {
            var prop = base.GetAspect(objectID, name);
            if (prop != null)
            {
                return prop;
            }

            if (name == "Layer Name")
            {
                var layer = QueryObjectUndo(objectID) as LayerBase;
                return IAspect.FromString(layer.Name);
            }

            if (name == "Layer Visibility")
            {
                var layer = QueryObjectUndo(objectID) as LayerBase;
                return IAspect.FromBoolean(layer.IsEnabled());
            }

            if (name == "Grid Visible")
            {
                var layer = QueryObjectUndo(objectID) as LayerBase;
                return IAspect.FromBoolean(layer.GridVisible);
            }

            if (name == "Layer Color")
            {
                var layer = QueryObjectUndo(objectID) as LayerBase;
                return IAspect.FromColor(layer.Color);
            }

            if (name.StartsWith(GridAttributeName))
            {
                ParseGridAttribute(name, out var range, out var layerID);

                var layer = QueryLayer(layerID) as Layer;
                var array = new uint[range.Size.x * range.Size.y];
                for (var y = range.Min.y; y <= range.Max.y; ++y)
                {
                    for (var x = range.Min.x; x <= range.Max.x; ++x)
                    {
                        array[(y - range.Min.y) * range.Size.x + x - range.Min.x] = layer.Get(x, y);
                    }
                }
                return IAspect.FromArray(array, makeCopy: false);
            }

            return null;
        }

        public override List<UIControl> GetSceneViewControls()
        {
            return m_Controls;
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

        private void SetOperation(Operation operation)
        {
            m_Operation = operation;
            if (m_Operation == Operation.Select)
            {
                Tools.hidden = false;
            }
            else
            {
                Tools.hidden = true;
            }
        }

        private Vector3 SnapPosition(Vector3 worldPosition)
        {
            var layer = QueryLayer(m_ActiveLayerID);
            var coord = layer.PositionToCoordinate(worldPosition.x, worldPosition.z);
            return layer.CoordinateToPosition(coord.x, coord.y);
        }

        private void ParseGridAttribute(string text, out IntBounds2D bounds, out int layerID)
        {
            var index = text.IndexOf(GridAttributeName);
            var startIndex = index + 1 + GridAttributeName.Length;
            var endIndex = text.Length - 1;
            var val = text[startIndex..endIndex];
            var tokens = val.Split(",");

            int.TryParse(tokens[0], out layerID);
            int.TryParse(tokens[1], out var minX);
            int.TryParse(tokens[2], out var minY);
            int.TryParse(tokens[3], out var width);
            int.TryParse(tokens[4], out var height);
            bounds = new IntBounds2D(new Vector2Int(minX, minY), new Vector2Int(minX + width - 1, minY + height - 1));
        }

        private void UpdateLayerNames()
        {
            if (m_LayerNames == null || m_LayerNames.Length != m_Layers.Count)
            {
                m_LayerNames = new string[m_Layers.Count];
            }

            var idx = 0;
            foreach (var layer in m_Layers)
            {
                m_LayerNames[idx] = layer.Name;
                ++idx;
            }
        }

        private void SetLayerVisibility(int layerID, bool show)
        {
            var layer = QueryLayer(layerID);
            if (layer != null)
            {
                UndoSystem.SetAspect(layer, "Layer Visibility", IAspect.FromBoolean(show), "Set Layer Visibility", ID, UndoActionJoinMode.Both);
            }
        }

        private void SetGridVisibility(bool show)
        {
            UndoSystem.NextGroupAndJoin();
            foreach (var layer in m_Layers)
            {
                if (layer != null)
                {
                    UndoSystem.SetAspect(layer, "Grid Visible", IAspect.FromBoolean(show), $"Set Grid {layer.ID} Visibility", ID, UndoActionJoinMode.None);
                }
            }
        }

        private void CommandAddLayer()
        {
            var width = World.Width;
            var height = World.Height;
            var horizontalGridCount = 100;
            var verticalGridCount = 100;
            var origin = Vector2.zero;
            var horizontalBlockCount = 4;
            var verticalBlockCount = 4;
            if (m_Layers.Count > 0)
            {
                width = m_Layers.Max.Width;
                height = m_Layers.Max.Height;
                horizontalGridCount = m_Layers.Max.HorizontalGridCount;
                verticalGridCount = m_Layers.Max.VerticalGridCount;
                origin = m_Layers.Max.Origin;
                horizontalBlockCount = m_Layers.Max.HorizontalBlockCount;
                verticalBlockCount = m_Layers.Max.VerticalBlockCount;
            }

            var input = new List<ParameterWindow.Parameter>()
            {
                new ParameterWindow.StringParameter("名称", "", "Layer"),
                new ParameterWindow.FloatParameter("层宽(米)", "", width),
                new ParameterWindow.FloatParameter("层高(米)", "", height),
                new ParameterWindow.IntParameter("横向格子数", "", horizontalGridCount),
                new ParameterWindow.IntParameter("纵向格子数", "", verticalGridCount),
                new ParameterWindow.StringArrayParameter("类型", "", m_LayerTypeNames),
            };
            ParameterWindow.Open("新建子层", input, (items) =>
            {
                var ok = ParameterWindow.GetString(items[0], out var name);
                ok &= ParameterWindow.GetFloat(items[1], out var width);
                ok &= ParameterWindow.GetFloat(items[2], out var height);
                ok &= ParameterWindow.GetInt(items[3], out var horizontalGridCount);
                ok &= ParameterWindow.GetInt(items[4], out var verticalGridCount);
                ok &= ParameterWindow.GetStringArraySelection(items[5], out var layerType);
                if (ok)
                {
                    if (QueryLayer(name) == null)
                    {
                        var layer = new Layer(World.AllocateObjectID(), objectIndex: m_Layers.Count, name, horizontalGridCount, verticalGridCount, width / horizontalGridCount, height / verticalGridCount, origin, horizontalBlockCount, verticalBlockCount, (LayerType)layerType, Color.white);

                        UndoSystem.CreateObject(layer, World.ID, "Add Attribute Layer", ID, lod: 0);

                        m_ActiveLayerID = m_Layers.Max.ID;

                        return true;
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", $"layer {name} already exists", "OK");
                    }
                }
                return false;
            });
        }

        private void CommandDeleteLayer()
        {
            if (m_ActiveLayerID != 0)
            {
                var layer = QueryLayer(m_ActiveLayerID);
                UndoSystem.DestroyObject(layer, "Delete Attribute Layer", ID, lod: 0);
                if (m_Layers.Count > 0)
                {
                    m_ActiveLayerID = m_Layers.Min.ID;
                }
                else
                {
                    m_ActiveLayerID = 0;
                }
            }
        }

        private void CommandEditLayerName()
        {
            if (m_ActiveLayerID == 0)
            {
                return;
            }

            var layer = QueryLayer(m_ActiveLayerID);
            var input = new List<ParameterWindow.Parameter>()
            {
                new ParameterWindow.StringParameter("名称", "", layer.Name),
            };
            ParameterWindow.Open("修改层名", input, (items) =>
            {
                var ok = ParameterWindow.GetString(items[0], out var name);
                if (ok)
                {
                    if (QueryLayer(name) == null)
                    {
                        UndoSystem.SetAspect(layer, "Layer Name", IAspect.FromString(name), "Set Layer Name", ID, UndoActionJoinMode.NextJoin);

                        return true;
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("出错了", $"层名{name}已经存在", "确定");
                    }
                }
                return false;
            });
        }

        private void SetBrushSize(int brushSize)
        {
            m_BrushSize = brushSize;
            m_BrushSize = Mathf.Clamp(m_BrushSize, 1, 500);
        }

        private void SetColor(Color color)
        {
            var layer = QueryLayer(m_ActiveLayerID);
            if (layer != null)
            {
                UndoSystem.SetAspect(layer, "Layer Color", IAspect.FromColor(color), "Set Attribute System Layer Color", ID, UndoActionJoinMode.None);
            }
        }

        private void SetCurrentLayer(int layerIndex)
        {
            if (layerIndex >= 0 && layerIndex < m_Layers.Count)
            {
                var idx = 0;
                foreach (var layer in m_Layers)
                {
                    if (idx == layerIndex)
                    {
                        m_ActiveLayerID = layer.ID;
                        return;
                    }
                    ++idx;
                }
            }
            else
            {
                m_ActiveLayerID = 0;
            }
        }

        public int GetLayerIndex(int layerID)
        {
            var idx = 0;
            foreach (var layer in m_Layers)
            {
                if (layer.ID == layerID)
                {
                    return idx;
                }
                ++idx;
            }
            return -1;
        }

        private void SearchHooks()
        {
            m_Hooks = EditorHelper.QueryAssets<AttributeSystemHook>();
        }

        private enum Operation
        {
            Select,
            SetGrid,
        }

        private Operation m_Operation = Operation.Select;
        private readonly string[] m_OperationNames = new string[]
        {
            "选择",
            "绘制",
        };
        private readonly string[] m_LayerTypeNames = new string[]
        {
            "自动碰撞",
            "手绘碰撞",
            "自定义",
        };

        private string[] m_LayerNames;
        private Vector2 m_ViewScrollPosition;
        private bool m_ShowInspectorUI = true;
        private Popup m_OperationPopup;
        private Popup m_TypePopup;
        private IntField m_BrushSizeControl;
        private ColorField m_ColorField;
        private Popup m_LayersPopup;
        private ImageButton m_AddLayerButton;
        private ImageButton m_RemoveLayerButton;
        private ImageButton m_EditLayerNameButton;
        private ToggleImageButton m_LayerVisibilityButton;
        private ToggleImageButton m_ShowGrid;
        private List<UIControl> m_Controls;
        private GUIStyle m_TipsStyle;
        private List<AttributeSystemHook> m_Hooks;
        public const string GridAttributeName = "GridAttribute";
    }
}