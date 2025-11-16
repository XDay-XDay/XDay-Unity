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
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;
using XDay.UtilityAPI.Editor;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Region.Editor
{
    internal partial class RegionSystem
    {
        protected override void InspectorGUIInternal()
        {
            m_Show = EditorGUILayout.Foldout(m_Show, "Region");
            if (m_Show)
            {
                m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
                EditorHelper.IndentLayout(() =>
                {
                    m_PluginLODSystemEditor.InspectorGUI(LODSystem, null, "LOD", (lodCount) =>
                    {
                        if (lodCount > 8)
                        {
                            EditorUtility.DisplayDialog("出错了", "LOD数不能超过8个", "确定");
                        }
                        return lodCount <= 8;
                    });

                    var layer = GetCurrentLayer();
                    GUI.enabled = layer != null;

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("生成Mesh"))
                    {
                        layer.GenerateMeshes(true);
                    }
                    if (GUILayout.Button("生成预览"))
                    {
                        layer.GenerateMeshes(false);
                    }
                    EditorGUILayout.EndHorizontal();
                    //if (GUILayout.Button("导出"))
                    //{
                    //    layer.Export();
                    //}
                    //if (GUILayout.Button("导入"))
                    //{
                    //    layer.Import();
                    //}
                    GUI.enabled = true;

                    DrawMeshGenerators();

                    DrawRegionSettings();
                });
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawMeshGenerators()
        {
            var layer = GetCurrentLayer();
            if (layer != null)
            {
                foreach (var gen in layer.MeshGenerators)
                {
                    gen.InspectorGUI();
                }
            }
        }

        protected override void SceneViewControlInternal(Rect sceneViewRect)
        {
            var evt = Event.current;
            if ((evt.type == EventType.KeyDown) && evt.shift == false)
            {
                if (evt.keyCode == KeyCode.Alpha1 && evt.control)
                {
                    ChangeOperation(Operation.Select);
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.Alpha2 && evt.control)
                {
                    ChangeOperation(Operation.EditRegion);
                    evt.Use();
                }
            }

            CreateUIControls();

            EditorGUILayout.BeginHorizontal();
            {
                DrawOperation();

                GUILayout.Space(30);

                DrawLayerSelection();
                DrawLayerVisibilityButton();
                DrawLayerButtons();

                GUILayout.Space(20);

                SetBrushSize(m_BrushSizeField.Render(m_BrushSize, 50));

                DrawAlpha();
            }
            EditorGUILayout.EndHorizontal();

            DrawDescription();
        }

        private void DrawAlpha()
        {
            var layer = GetCurrentLayer();
            GUI.enabled = layer != null;
            if (layer != null)
            {
                var newAlpha = m_AlphaField.Render(layer.Alpha, 50);
                if (!Mathf.Approximately(newAlpha, layer.Alpha))
                {
                    layer.Alpha = newAlpha;
                    layer.Renderer.UpdateColors(0, 0, layer.HorizontalGridCount - 1, layer.VerticalGridCount - 1);
                }
            }
            GUI.enabled = true;
        }

        private void DrawLayerButtons()
        {
            var layer = GetCurrentLayer();
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

            DrawShowNameButton();
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

        private void DrawLayerSelection()
        {
            UpdateLayerNames();
            var layerIndex = GetLayerIndex(m_CurrentLayerID);
            var newLayerIndex = m_LayersPopup.Render(layerIndex, m_LayerNames, 40);
            SetCurrentLayer(newLayerIndex);
        }

        private void DrawLayerVisibilityButton()
        {
            var layer = GetCurrentLayer();
            if (layer != null)
            {
                m_LayerVisibilityButton.Active = layer.IsEnabled();
            }
            if (m_LayerVisibilityButton.Render(true, GUI.enabled && layer != null))
            {
                SetLayerVisibility(m_CurrentLayerID, m_LayerVisibilityButton.Active);
            }
        }

        private void CreateUIControls()
        {
            if (m_Controls == null)
            {
                m_Controls = new();

                m_PopupOperation = new Popup("操作", "", 130);
                m_Controls.Add(m_PopupOperation);

                m_ColorField = new ColorField("颜色", "", 100);
                m_Controls.Add(m_ColorField);

                m_LayersPopup = new Popup("当前层", "", 170);
                m_Controls.Add(m_LayersPopup);
                m_AddLayerButton = CreateIconButton("add.png", "新建层");
                m_RemoveLayerButton = CreateIconButton("remove.png", "删除当前层");
                m_EditLayerNameButton = CreateIconButton("edit.png", "编辑层名称");
                m_LayerVisibilityButton = EditorWorldHelper.CreateToggleImageButton(false, "show.png", "显隐组");
                m_Controls.Add(m_LayerVisibilityButton);

                m_ButtonDeleteObjects = EditorWorldHelper.CreateImageButton("delete.png", "删除物体");
                m_Controls.Add(m_ButtonDeleteObjects);

                m_ButtonCloneObjects = EditorWorldHelper.CreateImageButton("clone.png", "复制物体");
                m_Controls.Add(m_ButtonCloneObjects);

                m_ButtonRenameObjects = EditorWorldHelper.CreateImageButton("rename.png", "修改物体名称");
                m_Controls.Add(m_ButtonRenameObjects);

                m_ShowGrid = EditorWorldHelper.CreateToggleImageButton(false, "grid.png", "显隐格子");
                m_Controls.Add(m_ShowGrid);

                m_BrushSizeField = new IntField("笔刷大小", "", 80);
                m_Controls.Add(m_BrushSizeField);

                m_AlphaField = new FloatField("透明度", "", 80);
                m_Controls.Add(m_AlphaField);

                m_ButtonShowName = EditorWorldHelper.CreateToggleImageButton(m_ShowName, "show_material.png", "是否显示区域名称");
                m_Controls.Add(m_ButtonShowName);
            }
        }

        public override List<UIControl> GetSceneViewControls()
        {
            return m_Controls;
        }

        protected override void SelectionChangeInternal(bool selected)
        {
            if (selected)
            {
                ChangeOperation(m_Action);
            }
            else
            {
                Tools.hidden = false;
            }
        }

        private void DrawDescription()
        {
            if (m_LabelStyle == null)
            {
                m_LabelStyle = new GUIStyle(GUI.skin.label);
            }

            var layer = GetCurrentLayer();
            if (layer != null)
            {
                EditorGUILayout.LabelField($"当前层物体总数: {layer.RegionCount}, 范围: {m_Bounds.min:F0}到{m_Bounds.max:F0}米,格子个数:{layer.HorizontalGridCount}X{layer.VerticalGridCount},格子大小:{layer.GridWidth}X{layer.GridHeight}米");
            }
            EditorGUILayout.LabelField($"Q: 增大笔刷", m_LabelStyle);
            EditorGUILayout.LabelField($"W: 缩小笔刷", m_LabelStyle);
            EditorGUILayout.LabelField("Ctrl+1/Ctrl+2/Ctrl+3切换操作", m_LabelStyle);
        }

        private void DrawBounds()
        {
            var oldColor = Handles.color;
            Handles.color = Color.white;
            Handles.DrawWireCube(m_Bounds.center, m_Bounds.size);
            Handles.color = oldColor;
        }

        public void UpdateRenderer(int objectID)
        {
            var region = World.QueryObject<RegionObject>(objectID);
            region.Layer.Renderer.SetDirty(objectID);
        }

        private void DrawOperation()
        {
            ChangeOperation((Operation)m_PopupOperation.Render((int)m_Action, m_ActionNames, 35));
        }

        private void ChangeOperation(Operation operation)
        {
            m_Action = operation;
            if (m_Action != Operation.Select)
            {
                Tools.hidden = true;
            }
            else
            {
                Tools.hidden = false;
            }
        }

        private ImageButton CreateIconButton(string textureName, string tooltip)
        {
            var button = EditorWorldHelper.CreateImageButton(textureName, tooltip);
            m_Controls.Add(button);
            return button;
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
            var layer = GetLayer(layerID);
            if (layer != null)
            {
                UndoSystem.SetAspect(layer, "Layer Visibility", IAspect.FromBoolean(show), "Set Layer Visibility", ID, UndoActionJoinMode.Both);
            }
        }

        private void CommandAddLayer()
        {
            var width = World.Width;
            var height = World.Height;
            var horizontalGridCount = 100;
            var verticalGridCount = 100;
            var origin = Vector2.zero;
            if (m_Layers.Count > 0)
            {
                width = m_Layers[^1].Width;
                height = m_Layers[^1].Height;
                horizontalGridCount = m_Layers[^1].HorizontalGridCount;
                verticalGridCount = m_Layers[^1].VerticalGridCount;
                origin = m_Layers[^1].Origin;
            }

            var input = new List<ParameterWindow.Parameter>()
            {
                new ParameterWindow.StringParameter("名称", "", "Layer"),
                new ParameterWindow.FloatParameter("层宽(米)", "", width),
                new ParameterWindow.FloatParameter("层高(米)", "", height),
                new ParameterWindow.IntParameter("横向格子数", "", horizontalGridCount),
                new ParameterWindow.IntParameter("纵向格子数", "", verticalGridCount),
            };
            ParameterWindow.Open("新建子层", input, (items) =>
            {
                var ok = ParameterWindow.GetString(items[0], out var name);
                ok &= ParameterWindow.GetFloat(items[1], out var width);
                ok &= ParameterWindow.GetFloat(items[2], out var height);
                ok &= ParameterWindow.GetInt(items[3], out var horizontalGridCount);
                ok &= ParameterWindow.GetInt(items[4], out var verticalGridCount);
                if (ok)
                {
                    if (GetLayer(name) == null)
                    {
                        var layer = new RegionSystemLayer(World.AllocateObjectID(), m_Layers.Count, name, ID, horizontalGridCount, verticalGridCount, width / horizontalGridCount, height / verticalGridCount, origin);

                        UndoSystem.CreateObject(layer, World.ID, "Add Region System Layer", ID, lod: 0);

                        m_CurrentLayerID = m_Layers[^1].ID;

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
            if (m_CurrentLayerID != 0)
            {
                if (!EditorUtility.DisplayDialog("注意", "确定删除层?", "确定", "取消"))
                {
                    return;
                }

                var layer = GetCurrentLayer();
                UndoSystem.DestroyObject(layer, "Delete Region System Layer", ID, lod: 0);
                if (m_Layers.Count > 0)
                {
                    m_CurrentLayerID = m_Layers[0].ID;
                }
                else
                {
                    m_CurrentLayerID = 0;
                }
            }
        }

        private void DrawShowNameButton()
        {
            m_ButtonShowName.Active = m_ShowName;
            if (m_ButtonShowName.Render(Inited))
            {
                m_ShowName = m_ButtonShowName.Active;
                SceneView.RepaintAll();
            }
        }

        private void CommandEditLayerName()
        {
            if (m_CurrentLayerID == 0)
            {
                return;
            }

            var layer = GetLayer(m_CurrentLayerID);
            var input = new List<ParameterWindow.Parameter>()
            {
                new ParameterWindow.StringParameter("名称", "", layer.Name),
            };
            ParameterWindow.Open("修改层名", input, (items) =>
            {
                var ok = ParameterWindow.GetString(items[0], out var name);
                if (ok)
                {
                    if (GetLayer(name) == null)
                    {
                        UndoSystem.SetAspect(layer,
                            "Layer Name",
                            IAspect.FromString(name),
                            "Set Region System Layer Name",
                            ID,
                            UndoActionJoinMode.NextJoin);

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

        private enum Operation
        {
            Select,
            EditRegion,
        }

        private ImageButton m_ButtonDeleteObjects;
        private ImageButton m_ButtonCloneObjects;
        private ImageButton m_ButtonRenameObjects;
        private ColorField m_ColorField;
        private Popup m_LayersPopup;
        private ImageButton m_AddLayerButton;
        private ImageButton m_RemoveLayerButton;
        private ImageButton m_EditLayerNameButton;
        private ToggleImageButton m_LayerVisibilityButton;
        private ToggleImageButton m_ShowGrid;
        private ToggleImageButton m_ButtonShowName;
        private IntField m_BrushSizeField;
        private FloatField m_AlphaField;
        private GUIStyle m_LabelStyle;
        private List<UIControl> m_Controls;
        private Vector2 m_ScrollPos;
        private bool m_Show = true;
        private Popup m_PopupOperation;
        private Operation m_Action = Operation.Select;
        private AspectContainerEditor m_AspectContainerEditor = new();
        private PluginLODSystemEditor m_PluginLODSystemEditor = new();
        private string[] m_LayerNames;
        private string[] m_ActionNames = new string[]
        {
            "选择",
            "绘制区域",
        };
    }
}