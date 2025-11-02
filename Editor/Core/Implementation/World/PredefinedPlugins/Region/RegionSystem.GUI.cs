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
using System.Linq;
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
                });
                EditorGUILayout.EndScrollView();
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
                    ChangeOperation(Operation.Edit);
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.Alpha3 && evt.control)
                {
                    ChangeOperation(Operation.Create);
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
            }
            EditorGUILayout.EndHorizontal();

            DrawDescription();
        }

        private void DrawLayerButtons()
        {
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

            var regionCount = 0;
            foreach (var kv in m_Layers)
            {
                regionCount += kv.Regions.Count;
            }
            EditorGUILayout.LabelField($"物体总数: {regionCount}, 范围: {m_Bounds.min:F0}到{m_Bounds.max:F0}米");
            EditorGUILayout.LabelField("Ctrl+1/Ctrl+2/Ctrl+3切换操作");
        }

        //private void DrawRenameObjects()
        //{
        //    var region = GetFirstActiveRegion();
        //    if (region != null)
        //    {
        //        if (m_ButtonRenameObjects.Render(Inited))
        //        {
        //            var parameters = new List<ParameterWindow.Parameter>()
        //            {
        //                new ParameterWindow.StringParameter("名称", "", region.Name),
        //            };

        //            ParameterWindow.Open("修改名称", parameters, (p) =>
        //            {
        //                ParameterWindow.GetString(p[0], out var name);
        //                if (!string.IsNullOrEmpty(name))
        //                {
        //                    UndoSystem.SetAspect(region, RegionDefine.REGION_NAME, IAspect.FromString(name), "Rename Region", ID, UndoActionJoinMode.Both);
        //                    return true;
        //                }
        //                return false;
        //            });
        //        }
        //    }
        //}

        protected override void SceneGUISelectedInternal()
        {
            DrawBounds();

            SceneView.RepaintAll();

            foreach (var layer in m_Layers)
            {
                layer.Update();
            }
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

        private RegionObject GetRegionObjectFromGameObject(GameObject gameObject)
        {
            foreach (var layer in m_Layers)
            {
                var id = layer.Renderer.QueryObjectID(gameObject);
                if (id != 0)
                {
                    return QueryObjectUndo(id) as RegionObject;
                }
            }
            return null;
        }

        private void OnSelectionChanged()
        {
            EditorApplication.delayCall += () =>
            {
                var gameObject = Selection.activeGameObject;
                if (gameObject == null)
                {
                    return;
                }

                //if (m_PickedRegions.Count <= 1)
                //{
                //    var regionObject = GetRegionObjectFromGameObject(gameObject);
                //    if (regionObject != null)
                //    {
                //        SetActiveRegions(new List<RegionPickInfo>() { new RegionPickInfo(regionObject.ID, -1) });
                //    }
                //}
            };
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
            var input = new List<ParameterWindow.Parameter>()
            {
                new ParameterWindow.StringParameter("名称", "", "Layer"),
            };
            ParameterWindow.Open("新建子层", input, (items) =>
            {
                var ok = ParameterWindow.GetString(items[0], out var name);
                if (ok)
                {
                    if (GetLayer(name) == null)
                    {
                        var layer = new RegionSystemLayer(World.AllocateObjectID(), m_Layers.Count, name, ID);

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
            Edit,
            Create,
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
        private GUIStyle m_LabelStyle;
        private List<UIControl> m_Controls;
        private Vector2 m_ScrollPos;
        private bool m_Show = true;
        private Popup m_PopupOperation;
        private Operation m_Action = Operation.Select;
        private AspectContainerEditor m_AspectContainerEditor = new();
        private string[] m_LayerNames;
        private string[] m_ActionNames = new string[]
        {
            "选择",
            "编辑",
            "创建",
        };
    }
}
