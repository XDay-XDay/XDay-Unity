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

namespace XDay.WorldAPI.Decoration.Editor
{
    internal partial class DecorationSystem
    {
        private void ActionCreateObject()
        {
            var evt = Event.current;

            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.T && evt.shift == false)
            {
                m_CreateMode = (ObjectCreateMode)(((int)m_CreateMode + 1) % 2);
                evt.Use();
            }

            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Q && evt.shift == false)
            {
                m_Rotation += m_RotationDelta;
                evt.Use();
            }

            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.W && evt.shift == false)
            {
                m_Rotation -= m_RotationDelta;
                evt.Use();
            }

            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.A && evt.shift == false)
            {
                m_Scale += m_ScaleDelta;
                evt.Use();
            }

            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.S && evt.shift == false)
            {
                m_Scale -= m_ScaleDelta;
                if (m_Scale < m_MinScale)
                {
                    m_Scale = m_MinScale;
                }
                evt.Use();
            }

            if (m_CreateMode == ObjectCreateMode.Multiple)
            {
                ActionCreateMultipleObjects();
            }
            else
            {
                ActionCreateSingleObject();
            }
        }

        private void ActionCreateObjectFromPattern()
        {
            var pattern = GetPattern(m_ActivePatternIndex);
            if (pattern == null)
            {
                return;
            }

            var evt = Event.current;

            var cursorPos = Helper.GUIRayCastWithXZPlane(evt.mousePosition, World.CameraManipulator.Camera);
            pattern.SetPosition(cursorPos);

            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Q && evt.shift == false)
            {
                pattern.Rotate(m_RotationDelta);
                evt.Use();
            }

            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.W && evt.shift == false)
            {
                pattern.Rotate(-m_RotationDelta);
                evt.Use();
            }

            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.A && evt.shift == false)
            {
                pattern.Scale(m_ScaleDelta);
                evt.Use();
            }

            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.S && evt.shift == false)
            {
                pattern.Scale(-m_ScaleDelta);
                evt.Use();
            }

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                for (var i = 0; i < pattern.ItemCount; i++)
                {
                    var ok = pattern.GetItemInfo(i, out var prefabPath, out var worldPosition, out var worldScale, out var worldRotation);
                    if (ok)
                    {
                        var decoration = CreateObject(World.AllocateObjectID(), prefabPath, worldPosition, worldRotation, worldScale);
                        UndoSystem.CreateObject(decoration, World.ID, DecorationDefine.ADD_DECORATION_NAME, ID, CurrentLOD);
                    }
                }
            }
        }

        private void ActionCloneObject(Vector3 offset)
        {
            UndoSystem.NextGroupAndJoin();

            var objectIDs = QueryRootObjectSelectionIDs();
            CloneObjects(new List<int>(objectIDs), offset);
        }

        protected override void InspectorGUIInternal()
        {
            m_Show = EditorGUILayout.Foldout(m_Show, "Decoration");
            if (m_Show)
            {
                m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
                EditorHelper.IndentLayout(() =>
                {
                    m_PluginLODSystemEditor.InspectorGUI(LODSystem, null, "LOD", (lodCount) =>
                    {
                        return lodCount <= 8;
                    });

                    var newBounds = EditorGUILayout.RectField("范围", m_Bounds.ToRect());
                    SetBounds(newBounds.ToBounds());

                    m_ResourceGroupSystem.InspectorGUI();
                });
                EditorGUILayout.EndScrollView();
            }
        }

        private void RedoLastGeneration()
        {
            if (m_LastOperation != null)
            {
                List<Vector3> points = null;
                if (UndoSystem.LastActionName == DecorationDefine.ADD_DECORATION_NAME)
                {
                    m_ClearOperation = false;
                    UndoSystem.Undo();
                    m_ClearOperation = true;

                    if (m_LastOperation.Shape == GeometryType.Line)
                    {
                        points = GenerateLineCoordinates(m_LastOperation.LineStart, m_LastOperation.LineEnd, false);
                    }
                    else if (m_LastOperation.Shape == GeometryType.Polygon)
                    {
                        points = GeneratePolygonCoordinates(m_LastOperation.Polygon, false);
                    }
                    else if (m_LastOperation.Shape == GeometryType.Circle)
                    {
                        points = GenerateCircleCoordinates(m_LastOperation.Center, false);
                    }
                    else if (m_LastOperation.Shape == GeometryType.Rectangle)
                    {
                        points = GenerateRectangleCoordinates(m_LastOperation.Center, false);
                    }
                    else
                    {
                        Debug.Assert(false, "todo");
                    }

                    CreateObjects(points, m_Rotation, m_Scale, m_CoordinateGenerateSetting.Random);
                }
            }
        }

        private void ActionDeleteObject()
        {
            DrawRemoveRange();

            var evt = Event.current;

            if (evt.button == 0 && (evt.type == EventType.MouseDown || evt.type == EventType.MouseDrag))
            {
                if (evt.type == EventType.MouseDown)
                {
                    UndoSystem.NextGroup();
                }

                var worldPosition = Helper.GUIRayCastWithXZPlane(evt.mousePosition, World.CameraManipulator.Camera);
                foreach (var obj in QueryObjectsInRectangle(worldPosition, m_RemoveRange * 2, m_RemoveRange * 2))
                {
                    UndoSystem.DestroyObject(obj, DecorationDefine.REMOVE_DECORATION_NAME, ID);
                }
            }
        }

        private List<DecorationObject> QueryRootObjectSelection()
        {
            List<DecorationObject> ret = new();
            var ids = QueryRootObjectSelectionIDs();
            foreach (var id in ids)
            {
                var obj = QueryObjectUndo(id) as DecorationObject;
                Debug.Assert(obj != null);
                ret.Add(obj);
            }
            return ret;
        }

        private HashSet<int> QueryRootObjectSelectionIDs()
        {
            var rootObjectIDs = new HashSet<int>();
            foreach (var gameObject in Selection.gameObjects)
            {
                var behaviour = gameObject.GetComponentInParent<DecorationObjectBehaviour>(true);
                if (behaviour != null)
                {
                    rootObjectIDs.Add(behaviour.ObjectID);
                }
            }
            return rootObjectIDs;
        }

        protected override void SceneViewControlInternal(Rect sceneViewRect)
        {
            var evt = Event.current;
            if (evt.type == EventType.KeyDown && evt.shift == false)
            {
                if (evt.keyCode == KeyCode.Alpha1 && evt.control)
                {
                    ChangeOperation(Action.Select);
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.Alpha2 && evt.control)
                {
                    ChangeOperation(Action.EditLOD);
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.Alpha3 && evt.control)
                {
                    ChangeOperation(Action.CreateObject);
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.Alpha4 && evt.control)
                {
                    ChangeOperation(Action.UsePattern);
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.Alpha5 && evt.control)
                {
                    ChangeOperation(Action.DeleteObject);
                    evt.Use();
                }
            }

            CreateUIControls();

            EditorGUILayout.BeginHorizontal();
            {
                DrawLODSelection();

                DrawOperation();

                DrawObjectCountCalculation();

                DrawObjectTransformSync();

                DrawDeleteObjects();

                DrawCloneObjects();

                DrawAdjustObjectHeight();

                DrawCreatePattern();

                GUILayout.Space(30);

                switch (m_Action)
                {
                    case Action.Select:
                    case Action.EditLOD:
                        {
                            DrawShowObjectNotInDisplayLOD();
                            DrawHideObjectNotInDisplayLOD();
                            DrawAddObjectToLOD();
                            DrawRemoveObjectFromLOD();
                        }
                        break;
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            switch (m_Action)
            {
                case Action.DeleteObject:
                    {
                        m_RemoveRange = m_RadiusField.Render(m_RemoveRange, 25);
                        break;
                    }
                case Action.CreateObject:
                    {
                        DrawAddObjectGUI();
                        break;
                    }
                case Action.UsePattern:
                    {
                        DrawUsePatternGUI();
                        break;
                    }
            }
            EditorGUILayout.EndHorizontal();

            DrawDescription();
        }

        private void CreateUIControls()
        {
            if (m_Controls == null)
            {
                m_Controls = new();

                m_PopupOperation = new Popup("操作", "", 130);
                m_Controls.Add(m_PopupOperation);

                m_ObjectCountField = new IntField("个数", "", 70);
                m_Controls.Add(m_ObjectCountField);

                m_PopupActiveLOD = new Popup("LOD", "", 80);
                m_Controls.Add(m_PopupActiveLOD);

                m_PopupPattern = new Popup("集合", "", 130);
                m_Controls.Add(m_PopupPattern);

                m_HeightField = new FloatField("高", "", 60);
                m_Controls.Add(m_HeightField);

                m_RotationField = new FloatField("旋转角度", "", 100);
                m_Controls.Add(m_RotationField);

                m_ScaleField = new FloatField("缩放", "", 60);
                m_Controls.Add(m_ScaleField);

                m_SpaceField = new FloatField("间隔", "物体之间的最小间隔", 60);
                m_Controls.Add(m_SpaceField);

                m_BorderSizeField = new FloatField("边界", "沿形状边界生成物体", 60);
                m_Controls.Add(m_BorderSizeField);

                m_ButtonQueryObjectCountInActiveLOD = EditorWorldHelper.CreateImageButton("number.png", "显示当前LOD中的物体数量");
                m_Controls.Add(m_ButtonQueryObjectCountInActiveLOD);

                m_ButtonRandom = EditorWorldHelper.CreateToggleImageButton(false, "random.png", "使用所选模型组中的随机模型");
                m_ButtonRandom.Active = m_CoordinateGenerateSetting.Random;
                m_Controls.Add(m_ButtonRandom);

                m_ButtonShowObjectsNotInActiveLOD = EditorWorldHelper.CreateImageButton("show.png", "显示不在当前LOD中的物体");
                m_Controls.Add(m_ButtonShowObjectsNotInActiveLOD);

                m_ButtonAddObjectsToActiveLOD = EditorWorldHelper.CreateImageButton("add.png", "将选中物体加入到当前LOD中");
                m_Controls.Add(m_ButtonAddObjectsToActiveLOD);

                m_ButtonAdjustObjectHeight = EditorWorldHelper.CreateImageButton("adjust.png", "物体自适应地形高度");
                m_Controls.Add(m_ButtonAdjustObjectHeight);

                m_ButtonCreatePattern = EditorWorldHelper.CreateImageButton("pattern.png", "创建集合");
                m_Controls.Add(m_ButtonCreatePattern);

                m_WidthField = new FloatField("宽", "", 60);
                m_Controls.Add(m_WidthField);

                m_ButtonRemoveObjectFromActiveLOD = EditorWorldHelper.CreateImageButton("remove.png", "将选中物体从当前LOD中删除");
                m_Controls.Add(m_ButtonRemoveObjectFromActiveLOD);

                m_GeometryPopup = new Popup("形状", "", 90);
                m_Controls.Add(m_GeometryPopup);

                m_ButtonSycnObjectTransforms = EditorWorldHelper.CreateImageButton("refresh.png", "同步物体坐标");
                m_Controls.Add(m_ButtonSycnObjectTransforms);

                m_ButtonDeleteObjects = EditorWorldHelper.CreateImageButton("delete.png", "删除物体");
                m_Controls.Add(m_ButtonDeleteObjects);

                m_RenamePatternButton = EditorWorldHelper.CreateImageButton("rename.png", "修改集合名称");
                m_Controls.Add(m_RenamePatternButton);

                m_DeletePatternButton = EditorWorldHelper.CreateImageButton("remove.png", "删除集合");
                m_Controls.Add(m_DeletePatternButton);

                m_ButtonCloneObjects = EditorWorldHelper.CreateImageButton("clone.png", "复制物体");
                m_Controls.Add(m_ButtonCloneObjects);

                m_ButtonHideObjectsNotInActiveLOD = EditorWorldHelper.CreateImageButton("hide.png", "隐藏不在当前LOD的物体");
                m_Controls.Add(m_ButtonHideObjectsNotInActiveLOD);

                m_ButtonEqualSpace = EditorWorldHelper.CreateToggleImageButton(false, "equal.png", "生成等距的物体");
                m_ButtonEqualSpace.Active = m_CoordinateGenerateSetting.LineEquidistant;
                m_Controls.Add(m_ButtonEqualSpace);

                m_RadiusField = new FloatField("半径", "", 60);
                m_Controls.Add(m_RadiusField);
            }
        }

        private void ActionSetObjectLOD()
        {
            m_SceneSelectionTool.SceneGUI(World.CameraManipulator.Camera, (min, max) =>
            {
                var size = max - min;
                var center = (min + max) * 0.5f;
                var decorations = QueryObjectsInRectangle(center, size.x, size.z);
                var gameObjects = new GameObject[decorations.Count];
                for (var i = 0; i < decorations.Count; ++i)
                {
                    gameObjects[i] = m_Renderer.QueryGameObject(decorations[i].ID);
                }
                Selection.objects = gameObjects;
            });
        }

        private void ChangeActiveLOD(int lod, bool alwaysChange)
        {
            if (lod != m_ActiveLOD)
            {
                var oldLOD = m_ActiveLOD;
                m_ActiveLOD = lod;
                foreach (var decoration in m_Decorations.Values)
                {
                    if (decoration.ResourceDescriptor != null)
                    {
                        var oldLODPath = decoration.ResourceDescriptor.GetPath(oldLOD);
                        var newLODPath = decoration.ResourceDescriptor.GetPath(lod);
                        if (newLODPath == oldLODPath)
                        {
                            var existInNewLOD = decoration.ExistsInLOD(lod);
                            var existInOldLOD = decoration.ExistsInLOD(oldLOD);
                            if (!existInOldLOD && existInNewLOD)
                            {
                                UpdateObjectLOD(decoration.ID, lod);
                                SetEnabled(decoration.ID, true, lod, false);
                            }
                            else if (!existInNewLOD && existInOldLOD)
                            {
                                UpdateObjectLOD(decoration.ID, lod);
                            }
                        }
                        else
                        {
                            SetEnabled(decoration.ID, false, oldLOD, alwaysChange);
                            SetEnabled(decoration.ID, true, lod, alwaysChange);
                        }
                    }
                }
            }
        }

        private void SetObjectLODLayerMask(int lod, LayerMaskChangeMode changeMode)
        {
            if (lod < 0)
            {
                return;
            }

            UndoSystem.NextGroup();

            foreach (var objectID in QueryRootObjectSelectionIDs())
            {
                var decoration = World.QueryObject<DecorationObject>(objectID);
                LODLayerMask layerMask = 0;
                if (changeMode == LayerMaskChangeMode.DeleteFromActiveLODOnly)
                {
                    layerMask = decoration.RemoveLODLayer(lod);
                }
                else if (changeMode == LayerMaskChangeMode.AddToActiveLODOnly)
                {
                    layerMask = decoration.AddLODLayer(lod);
                }
                else if (changeMode == LayerMaskChangeMode.DeleteFromActiveAndHigherLOD)
                {
                    layerMask = decoration.LODLayerMask;
                    for (var k = lod; k < LODCount; ++k)
                    {
                        layerMask &= (LODLayerMask)~(1 << k);
                    }
                }
                else if (changeMode == LayerMaskChangeMode.AddToActiveAndHigherLOD)
                {
                    layerMask = decoration.LODLayerMask;
                    for (var k = lod; k < LODCount; ++k)
                    {
                        layerMask |= (LODLayerMask)(1 << k);
                    }
                }
                else
                {
                    Debug.Assert(false, $"Unknown change mode: {changeMode}");
                }
                UndoSystem.SetAspect(decoration, DecorationDefine.LOD_LAYER_MASK_NAME, IAspect.FromEnum(layerMask), changeMode.ToString(), ID, UndoActionJoinMode.None);
            }
        }

        private bool IsObjectTransformChanged(DecorationObject obj, GameObject gameObject)
        {
            var transform = gameObject.transform;
            return obj.Position != transform.position ||
                obj.Scale != transform.localScale ||
                obj.Rotation != transform.rotation;
        }

        private void QueryLODNames()
        {
            if (m_LODNames == null ||
                m_LODNames.Length != LODCount)
            {
                m_LODNames = new string[LODCount];
                for (var i = 0; i < LODCount; i++)
                {
                    m_LODNames[i] = $"{i}";
                }
            }
        }

        private void QueryPatternNames()
        {
            if (m_PatternNames == null ||
                m_PatternNames.Length != m_Patterns.Count)
            {
                m_PatternNames = new string[m_Patterns.Count];
            }

            for (var i = 0; i < m_Patterns.Count; i++)
            {
                m_PatternNames[i] = m_Patterns[i].Name;
            }

            if(m_Patterns.Count > 0 && m_ActivePatternIndex < 0)
            {
                SetActivePattern(0);
            }
        }

        private void ShowObjects()
        {
            foreach (var decoration in m_Decorations.Values)
            {
                if (decoration.SetVisibility(WorldObjectVisibility.Visible))
                {
                    m_Renderer.ToggleVisibility(decoration, 0);
                }
            }

            ChangeActiveLOD(0, true);
        }

        private void ShowObjectsNotInActiveLOD(bool show)
        {
            UndoSystem.NextGroupAndJoin();

            var aspect = IAspect.FromBoolean(show);
            foreach (var decoration in m_Decorations.Values)
            {
                if (!decoration.ExistsInLOD(m_ActiveLOD))
                {
                    UndoSystem.SetAspect(decoration, DecorationDefine.ENABLE_DECORATION_NAME, aspect, "Enable/Disable Decoration Object", ID, UndoActionJoinMode.None);
                }
            }
        }

        private void UpdateIndicator(GameObject gameObject, Vector3 worldPosition)
        {
            if (gameObject == null)
            {
                m_Indicator.Visible = false;
            }
            else
            {
                m_Indicator.Prefab = AssetDatabase.GetAssetPath(gameObject);
                m_Indicator.Visible = true;
                m_Indicator.Rotation = Quaternion.Euler(0, m_Rotation, 0) * gameObject.transform.rotation;
                m_Indicator.Position = worldPosition;
                m_Indicator.Scale = m_Scale * gameObject.transform.localScale;
            }
        }

        public override List<UIControl> GetSceneViewControls()
        {
            return m_Controls;
        }

        protected override void SelectionChangeInternal(bool selected)
        {
            m_LastOperation = null;
            if (selected)
            {
                ChangeOperation(m_Action);
            }
            else
            {
                Tools.hidden = false;
                m_Indicator.Visible = false;
            }
        }

        private void ChangeOperation(Action operation)
        {
            m_Action = operation;
            if (m_Action != Action.Select)
            {
                Tools.hidden = true;
            }
            else
            {
                Tools.hidden = false;
            }
        }

        private void SetGeometryType(GeometryType type)
        {
            if (type != m_GeometryType)
            {
                m_GeometryType = type;
                m_ButtonRandom.Active = m_CoordinateGenerateSetting.Random;
            }
        }

        private void DrawDescription()
        {
            if (m_LabelStyle == null)
            {
                m_LabelStyle = new GUIStyle(GUI.skin.label);
            }

            EditorGUILayout.LabelField($"物体总数: {m_Decorations.Count}, 范围: {m_Bounds.min:F0}到{m_Bounds.max:F0}米");
            if (m_Action == Action.CreateObject)
            {
                EditorGUILayout.LabelField("R键重做上次生成");
                EditorGUILayout.LabelField("T键切换单/多物体模式");
                EditorGUILayout.LabelField("Q和W键旋转物体");
                EditorGUILayout.LabelField("A和S键缩放物体");
            }
            EditorGUILayout.LabelField("Ctrl+1/Ctrl+2/Ctrl+3/Ctrl+4切换操作");

            if (GetDirtyObjectIDs().Count == 0)
            {
                m_LabelStyle.normal.textColor = new Color(0, 0, 0, 0);
            }
            else
            {
                m_LabelStyle.normal.textColor = Color.magenta;
            }
            EditorGUILayout.LabelField("物体坐标改变,需要同步!", m_LabelStyle);
        }

        private void UndoRedo(UndoAction action, bool undo)
        {
            if (m_ClearOperation)
            {
                m_LastOperation = null;
            }
        }

        private void DrawCreateRange()
        {
            var worldPosition = Helper.GUIRayCastWithXZPlane(Event.current.mousePosition, World.CameraManipulator.Camera);
            var oldColor = Handles.color;
            Handles.color = Color.green;
            if (m_GeometryType == GeometryType.Rectangle)
            {
                Handles.DrawWireCube(worldPosition, new Vector3(m_CoordinateGenerateSetting.RectWidth, 0, m_CoordinateGenerateSetting.RectHeight));
            }
            else if (m_GeometryType == GeometryType.Circle)
            {
                Handles.DrawWireDisc(worldPosition, Vector3.up, m_CoordinateGenerateSetting.CircleRadius);
            }
            Handles.color = oldColor;
        }

        private void DrawRemoveRange()
        {
            var worldPosition = Helper.GUIRayCastWithXZPlane(Event.current.mousePosition, World.CameraManipulator.Camera);
            var oldColor = Handles.color;
            Handles.color = Color.red;
            Handles.DrawWireDisc(worldPosition, Vector3.up, m_RemoveRange);
            Handles.color = oldColor;
        }

        private void DrawUsePatternGUI()
        {
            EditorGUILayout.BeginHorizontal();
            DrawPatternSelection();
            DrawRenamePattern();
            DrawDeletePattern();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAddObjectGUI()
        {
            if (m_CreateMode == ObjectCreateMode.Single)
            {
                m_Rotation = m_RotationField.Render(m_Rotation, 50);
                m_Scale = m_ScaleField.Render(m_Scale, 25);
            }

            GUI.enabled = m_CreateMode == ObjectCreateMode.Multiple;

            var shape = (GeometryType)m_GeometryPopup.Render((int)m_GeometryType, m_ShapeNames, 25);
            SetGeometryType(shape);

            var enabled = true;
            if (m_GeometryType == GeometryType.Line && m_CoordinateGenerateSetting.LineEquidistant)
            {
                enabled = false;
            }

            var old = GUI.enabled;
            GUI.enabled = enabled;
            m_CoordinateGenerateSetting.Count = m_ObjectCountField.Render(m_CoordinateGenerateSetting.Count, 25);
            GUI.enabled = old;

            if (m_GeometryType == GeometryType.Circle)
            {
                m_CoordinateGenerateSetting.CircleRadius = m_RadiusField.Render(m_CoordinateGenerateSetting.CircleRadius, 25);
            }
            else if (m_GeometryType == GeometryType.Rectangle)
            {
                m_CoordinateGenerateSetting.RectWidth = m_WidthField.Render(m_CoordinateGenerateSetting.RectWidth, 15);
                m_CoordinateGenerateSetting.RectHeight = m_HeightField.Render(m_CoordinateGenerateSetting.RectHeight, 15);
            }
            else if (m_GeometryType == GeometryType.Line)
            {
                if (m_ButtonEqualSpace.Render(true, GUI.enabled))
                {
                    m_CoordinateGenerateSetting.LineEquidistant = m_ButtonEqualSpace.Active;
                }
            }
            m_CoordinateGenerateSetting.Space = m_SpaceField.Render(m_CoordinateGenerateSetting.Space, 25);
            m_CoordinateGenerateSetting.BorderSize = m_BorderSizeField.Render(m_CoordinateGenerateSetting.BorderSize, 25);

            if (m_ButtonRandom.Render(true, GUI.enabled))
            {
                m_CoordinateGenerateSetting.Random = m_ButtonRandom.Active;
            }

            GUI.enabled = true;
        }

        private void DrawDeleteObjects()
        {
            if (m_ButtonDeleteObjects.Render(Inited))
            {
                UndoSystem.NextGroupAndJoin();
                var objectIDs = QueryRootObjectSelectionIDs();
                foreach (var objectID in objectIDs)
                {
                    UndoSystem.DestroyObject(QueryObjectUndo(objectID), DecorationDefine.REMOVE_DECORATION_NAME, ID);
                }
            }
        }

        private void DrawCloneObjects()
        {
            if (m_ButtonCloneObjects.Render(Inited))
            {
                var parameters = new List<ParameterWindow.Parameter>
                {
                    new ParameterWindow.Vector3Parameter("复制体坐标偏移", "", new Vector3(10, 0, 10)),
                };

                ParameterWindow.Open("复制物体", parameters, (p) =>
                {
                    bool ok = ParameterWindow.GetVector3(p[0], out var offset);
                    if (ok)
                    {
                        ActionCloneObject(offset);
                        return true;
                    }
                    return false;
                });
            }
        }

        private void DrawObjectTransformSync()
        {
            if (m_ButtonSycnObjectTransforms.Render(Inited))
            {
                SyncObjectTransforms();
            }
        }

        private void DrawObjectCountCalculation()
        {
            if (m_ButtonQueryObjectCountInActiveLOD.Render(Inited))
            {
                var count = QueryObjectCountInLOD(m_ActiveLOD);
                EditorUtility.DisplayDialog("", $"当前LOD物体数量: {count}", "确定");
            }
        }

        private void DrawOperation()
        {
            ChangeOperation((Action)m_PopupOperation.Render((int)m_Action, m_ActionNames, 35));
        }

        private void DrawPatternSelection()
        {
            QueryPatternNames();

            var newIndex = m_PopupPattern.Render(m_ActivePatternIndex, m_PatternNames, 30);
            if (newIndex != m_ActivePatternIndex)
            {
                SetActivePattern(newIndex);
            }
        }

        private void SetActivePattern(int index)
        {
            if (m_ActivePatternIndex == index)
            {
                return;
            }

            var pattern = GetPattern(m_ActivePatternIndex);
            if (pattern != null)
            {
                pattern.SetActive(false);
            }
            m_ActivePatternIndex = index;
            pattern = GetPattern(m_ActivePatternIndex);
            if (pattern != null)
            {
                pattern.SetActive(true);
            }
        }

        private Pattern GetPattern(int index)
        {
            if (index >= 0 && index < m_Patterns.Count)
            {
                return m_Patterns[index];
            }
            return null;
        }

        private Pattern FindPattern(string name)
        {
            foreach (var pattern in m_Patterns)
            {
                if (pattern.Name == name)
                {
                    return pattern;
                }
            }
            return null;
        }

        private void DrawLODSelection()
        {
            QueryLODNames();

            var newLOD = m_PopupActiveLOD.Render(m_ActiveLOD, m_LODNames, 30);
            if (newLOD != m_ActiveLOD)
            {
                UndoSystem.SetAspect(this, DecorationDefine.LOD_NAME, IAspect.FromInt32(newLOD), "Set LOD", ID, UndoActionJoinMode.Both);
            }
        }

        private void DrawAddObjectToLOD()
        {
            if (m_ButtonAddObjectsToActiveLOD.Render(Inited))
            {
                var parameters = new List<ParameterWindow.Parameter>()
            {
                new ParameterWindow.BoolParameter("只添加到当前LOD", "只将选中的物体加入到当前编辑的LOD中", true),
            };
                ParameterWindow.Open("添加物体到LOD", parameters, (p) =>
                {
                    var ok = ParameterWindow.GetBool(p[0], out var onlyAddToActiveLOD);
                    if (ok)
                    {
                        SetObjectLODLayerMask(m_ActiveLOD, onlyAddToActiveLOD ? LayerMaskChangeMode.AddToActiveLODOnly : LayerMaskChangeMode.AddToActiveAndHigherLOD);
                        return true;
                    }
                    return false;
                });
            }
        }

        private void DrawCreatePattern()
        {
            if (m_ButtonCreatePattern.Render(Inited && (m_Action == Action.Select || m_Action == Action.EditLOD)))
            {
                var decorations = QueryRootObjectSelection();
                if (decorations.Count > 0)
                {
                    UndoSystem.NextGroupAndJoin();
                    var pattern = new Pattern(World.AllocateObjectID(), m_Patterns.Count, GetUniquePatternName("New Pattern"), decorations);
                    UndoSystem.CreateObject(pattern, World.ID, "Create Decoration Pattern", ID);
                }
            }
        }

        private string GetUniquePatternName(string name)
        {
            var idx = 0;
            while (true)
            {
                var newName = $"{name}_{idx}";
                if (FindPattern(newName) == null)
                {
                    return newName;
                }
                ++idx;
            }
        }

        private void DrawRenamePattern()
        {
            var pattern = GetPattern(m_ActivePatternIndex);
            if (m_RenamePatternButton.Render(Inited && pattern != null))
            {
                RenamePattern(pattern);
            }
        }

        private void DrawDeletePattern()
        {
            var activePattern = GetPattern(m_ActivePatternIndex);
            if (m_DeletePatternButton.Render(Inited && activePattern != null))
            {
                UndoSystem.DestroyObject(activePattern, "Destroy Pattern", ID, 0);
            }
        }

        private int GetPatternIndex(Pattern pattern)
        {
            for (var i = 0; i < m_Patterns.Count; ++i)
            {
                if (m_Patterns[i] == pattern)
                {
                    return i;
                }
            }
            return -1;
        }

        private void RenamePattern(Pattern pattern)
        {
            var parameters = new List<ParameterWindow.Parameter>()
                {
                    new ParameterWindow.StringParameter("新名称", "", pattern.Name),
                };
            ParameterWindow.Open("修改集合名称", parameters, (p) =>
            {
                var ok = ParameterWindow.GetString(p[0], out var name);
                if (ok)
                {
                    if (FindPattern(name) == null)
                    {
                        UndoSystem.SetAspect(pattern, DecorationDefine.PATTERN_NAME, IAspect.FromString(name), "Set Pattern Name", ID, UndoActionJoinMode.None);
                        return true;
                    }
                }
                return false;
            });
        }

        private void DrawAdjustObjectHeight()
        {
            if (m_ButtonAdjustObjectHeight.Render(Inited))
            {
                if (EditorUtility.DisplayDialog("注意", "确定修改高度?", "确定", "取消"))
                {
                    IHeightDataSource heightSource = null;
                    for (var i = 0; i < World.PluginCount; ++i)
                    {
                        var plugin = World.GetPlugin(i);
                        if (plugin is IHeightDataSource heightDataSource)
                        {
                            heightSource = heightDataSource;
                        }
                    }

                    if (heightSource != null)
                    {
                        UndoSystem.NextGroupAndJoin();
                        foreach (var dec in m_Decorations.Values)
                        {
                            if (dec.EnableHeightAdjust)
                            {
                                var pos = dec.Position;
                                var height = heightSource.GetHeightAtPos(pos.x, pos.z);
                                if (!Mathf.Approximately(height, 0))
                                {
                                    pos.y = height;
                                    UndoSystem.SetAspect(dec, DecorationDefine.POSITION_NAME, IAspect.FromVector3(pos), "Adjust Decoration Position", ID, UndoActionJoinMode.None);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DrawRemoveObjectFromLOD()
        {
            if (m_ButtonRemoveObjectFromActiveLOD.Render(Inited))
            {
                var parameters = new List<ParameterWindow.Parameter>()
                {
                    new ParameterWindow.BoolParameter("只从当前LOD中删除", "勾选后只会从当前LOD中过滤掉所选装饰物,理论上一个物体如果在当前LOD不显示,在更高级别的LOD也不会显示,所以一般不用勾选该选项", false),
                };
                ParameterWindow.Open("从LOD删除物体", parameters, (p) =>
                {
                    var ok = ParameterWindow.GetBool(p[0], out var onlyRemoveFromActiveLOD);
                    if (ok)
                    {
                        SetObjectLODLayerMask(m_ActiveLOD, onlyRemoveFromActiveLOD ? LayerMaskChangeMode.DeleteFromActiveLODOnly : LayerMaskChangeMode.DeleteFromActiveAndHigherLOD);
                        return true;
                    }
                    return false;
                });
            }
        }

        private void DrawShowObjectNotInDisplayLOD()
        {
            if (m_ButtonShowObjectsNotInActiveLOD.Render(Inited))
            {
                ShowObjectsNotInActiveLOD(true);
            }
        }

        private void DrawHideObjectNotInDisplayLOD()
        {
            if (m_ButtonHideObjectsNotInActiveLOD.Render(Inited))
            {
                ShowObjectsNotInActiveLOD(false);
            }
        }

        protected override void SceneGUISelectedInternal()
        {
            m_Indicator.Visible = false;
            if (m_Action == Action.EditLOD)
            {
                ActionSetObjectLOD();
            }
            else if (m_Action == Action.DeleteObject)
            {
                ActionDeleteObject();
            }
            else if (m_Action == Action.CloneObject)
            {
            }
            else if (m_Action == Action.CreateObject)
            {
                ActionCreateObject();

                if (m_CreateMode == ObjectCreateMode.Multiple)
                {
                    DrawCreateRange();
                }
            }
            else if (m_Action == Action.UsePattern)
            {
                ActionCreateObjectFromPattern();
            }
            else
            {
                if (m_Action != Action.Select)
                {
                    Debug.Assert(false, $"todo {m_Action}");
                }
            }

            if (m_Action != Action.Select)
            {
                HandleUtility.AddDefaultControl(0);
            }

            if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                SyncObjectTransforms();
            }

            DrawBounds();

            SceneView.RepaintAll();
        }

        private void DrawBounds()
        {
            var oldColor = Handles.color;
            Handles.color = Color.white;
            Handles.DrawWireCube(m_Bounds.center, m_Bounds.size);
            Handles.color = oldColor;
        }

        private void ActionCreateSingleObject()
        {
            var evt = Event.current;
            var worldPosition = Helper.GUIRayCastWithXZPlane(evt.mousePosition, World.CameraManipulator.Camera);

            UpdateIndicator(m_ResourceGroupSystem.SelectedPrefab, worldPosition);

            if (evt.type == EventType.MouseDown && evt.alt == false && evt.button == 0)
            {
                if (Vector3.Distance(m_LastGenerationCenter, worldPosition) > m_MinSpace)
                {
                    m_LastGenerationCenter = worldPosition;

                    if (evt.type == EventType.MouseDown)
                    {
                        UndoSystem.NextGroup();
                    }

                    CreateObjects(new List<Vector3>() { worldPosition }, m_Rotation, m_Scale, false);
                }
            }
        }

        private List<Vector3> GenerateLineCoordinates(Vector3 start, Vector3 end, bool nextGroup = true)
        {
            if (nextGroup)
            {
                UndoSystem.NextGroup();
            }
            return GeometryCoordinateGenerator.GenerateInLine(m_CoordinateGenerateSetting.Count, start, end, m_CoordinateGenerateSetting.BorderSize, m_CoordinateGenerateSetting.Space, m_CoordinateGenerateSetting.LineEquidistant);
        }

        private List<Vector3> GeneratePolygonCoordinates(List<Vector3> polygon, bool nextGroup = true)
        {
            if (nextGroup)
            {
                UndoSystem.NextGroup();
            }
            return GeometryCoordinateGenerator.GenerateInPolygon(m_CoordinateGenerateSetting.Count, polygon, m_CoordinateGenerateSetting.BorderSize, m_CoordinateGenerateSetting.Space);
        }

        private List<Vector3> GenerateCircleCoordinates(Vector3 center, bool nextGroup = true)
        {
            if (nextGroup)
            {
                UndoSystem.NextGroup();
            }
            return GeometryCoordinateGenerator.GenerateInCircle(m_CoordinateGenerateSetting.Count, m_CoordinateGenerateSetting.CircleRadius, center, m_CoordinateGenerateSetting.BorderSize, m_CoordinateGenerateSetting.Space);
        }

        private List<Vector3> GenerateRectangleCoordinates(Vector3 center, bool nextGroup = true)
        {
            if (nextGroup)
            {
                UndoSystem.NextGroup();
            }
            return GeometryCoordinateGenerator.GenerateInRectangle(m_CoordinateGenerateSetting.Count, center, m_CoordinateGenerateSetting.RectWidth, m_CoordinateGenerateSetting.RectHeight, m_CoordinateGenerateSetting.BorderSize, m_CoordinateGenerateSetting.Space);
        }

        private void CreateObjects(List<Vector3> coordinates, float rotation, float scale, bool random)
        {
            if (coordinates != null)
            {
                foreach (var position in coordinates)
                {
                    var prefabPath = m_ResourceGroupSystem.SelectedResourcePath;
                    if (random)
                    {
                        prefabPath = m_ResourceGroupSystem.RandomResourcePath;
                    }

                    if (!string.IsNullOrEmpty(prefabPath))
                    {
                        var decoration = CreateObject(World.AllocateObjectID(), prefabPath, position, Quaternion.Euler(0, rotation, 0), Vector3.one * scale);
                        UndoSystem.CreateObject(decoration, World.ID, DecorationDefine.ADD_DECORATION_NAME, ID, CurrentLOD);
                    }
                }
            }
        }

        private void CloneObjects(List<int> objects, Vector3 offset)
        {
            List<DecorationObject> newObjects = new();
            foreach (var objID in objects)
            {
                var obj = QueryObjectUndo(objID) as DecorationObject;
                var decoration = CloneObject(World.AllocateObjectID(), m_Decorations.Count, obj);
                if (decoration != null)
                {
                    UndoSystem.CreateObject(decoration, World.ID, DecorationDefine.ADD_DECORATION_NAME, ID, CurrentLOD);
                    var newDecoration = World.QueryObject<DecorationObject>(decoration.ID);
                    newObjects.Add(newDecoration);
                    UndoSystem.SetAspect(newDecoration, DecorationDefine.POSITION_NAME, IAspect.FromVector3(obj.Position + offset), "Set Decoration Position", ID, UndoActionJoinMode.None);
                }
            }

            List<Object> gameObjects = new();
            foreach (var dec in newObjects)
            {
                gameObjects.Add(m_Renderer.QueryGameObject(dec.ID));
            }
            Selection.objects = gameObjects.ToArray();
        }

        private List<Vector3> ActionClickCreate()
        {
            var evt = Event.current;
            var center = Helper.GUIRayCastWithXZPlane(evt.mousePosition, World.CameraManipulator.Camera);

            List<Vector3> coordinates = null;
            if (evt.type == EventType.MouseDown &&
                evt.alt == false &&
                evt.button == 0)
            {
                if (Vector3.Distance(m_LastGenerationCenter, center) > m_MinSpace)
                {
                    m_LastGenerationCenter = center;

                    if (m_GeometryType == GeometryType.Rectangle)
                    {
                        m_LastOperation = new CoordinateGenerateOperation(center, m_CoordinateGenerateSetting.RectWidth, m_CoordinateGenerateSetting.RectHeight);
                        coordinates = GenerateRectangleCoordinates(center);
                    }
                    else if (m_GeometryType == GeometryType.Circle)
                    {
                        m_LastOperation = new CoordinateGenerateOperation(center, m_CoordinateGenerateSetting.CircleRadius);
                        coordinates = GenerateCircleCoordinates(center);
                    }
                }
            }
            return coordinates;
        }

        private void ActionCreateMultipleObjects()
        {
            var evt = Event.current;
            var worldPosition = Helper.GUIRayCastWithXZPlane(evt.mousePosition, World.CameraManipulator.Camera);

            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.R)
            {
                RedoLastGeneration();
            }

            List<Vector3> coordinates = null;
            if (m_GeometryType == GeometryType.Polygon)
            {
                m_PolygonTool.SceneUpdate(Color.green, (polygon) =>
                {
                    m_LastOperation = new CoordinateGenerateOperation(polygon);

                    coordinates = GeneratePolygonCoordinates(polygon);

                });
            }
            else if (m_GeometryType == GeometryType.Line)
            {
                m_LineTool.SceneUpdate(Color.green, (start, end) =>
                {
                    m_LastOperation = new CoordinateGenerateOperation(start, end);

                    coordinates = GenerateLineCoordinates(start, end);
                });
            }
            else
            {
                coordinates = ActionClickCreate();
            }

            CreateObjects(coordinates, m_Rotation, m_Scale, m_CoordinateGenerateSetting.Random);
        }

        internal void SyncObjectTransforms()
        {
            var dirtyObjects = GetDirtyObjectIDs();
            if (dirtyObjects.Count > 0)
            {
                UndoSystem.NextGroupAndJoin();

                foreach (var objID in dirtyObjects)
                {
                    var decoration = World.QueryObject<DecorationObject>(objID);
                    var gameObject = m_Renderer.QueryGameObject(objID);

                    if (gameObject == null || decoration == null)
                    {
                        continue;
                    }

                    var transform = gameObject.transform;
                    if (IsObjectTransformChanged(decoration, gameObject))
                    {
                        UndoSystem.SetAspect(decoration, DecorationDefine.SCALE_NAME, IAspect.FromVector3(transform.localScale), "Set Decoration Scale", ID, UndoActionJoinMode.None);
                        UndoSystem.SetAspect(decoration, DecorationDefine.ROTATION_NAME, IAspect.FromQuaternion(transform.rotation), "Set Decoration Rotation", ID, UndoActionJoinMode.None);
                        UndoSystem.SetAspect(decoration, DecorationDefine.POSITION_NAME, IAspect.FromVector3(transform.position), "Set Decoration Position", ID, UndoActionJoinMode.None);
                    }
                }
                ClearDirtyObjects();
            }
        }

        private enum LayerMaskChangeMode
        {
            AddToActiveLODOnly,
            DeleteFromActiveLODOnly,
            AddToActiveAndHigherLOD,
            DeleteFromActiveAndHigherLOD,
        }

        private enum Action
        {
            Select,
            EditLOD,
            CreateObject,
            UsePattern,
            DeleteObject,
            CloneObject,
        }

        private CoordinateGenerateOperation m_LastOperation;
        private ImageButton m_ButtonShowObjectsNotInActiveLOD;
        private bool m_ClearOperation = false;
        private ImageButton m_ButtonRemoveObjectFromActiveLOD;
        private Popup m_PopupActiveLOD;
        private Popup m_PopupPattern;
        private FloatField m_BorderSizeField;
        private ImageButton m_ButtonDeleteObjects;
        private ImageButton m_RenamePatternButton;
        private ImageButton m_DeletePatternButton;
        private ImageButton m_ButtonCloneObjects;
        private FloatField m_RadiusField;
        private Popup m_PopupOperation;
        private IntField m_ObjectCountField;
        private ImageButton m_ButtonSycnObjectTransforms;
        private IResourceGroupSystem m_ResourceGroupSystem;
        private ImageButton m_ButtonQueryObjectCountInActiveLOD;
        private ToggleImageButton m_ButtonRandom;
        private FloatField m_SpaceField;
        private ToggleImageButton m_ButtonEqualSpace;
        private ImageButton m_ButtonHideObjectsNotInActiveLOD;
        private Popup m_GeometryPopup;
        private FloatField m_WidthField;
        private PluginLODSystemEditor m_PluginLODSystemEditor = new();
        private FloatField m_HeightField;
        private FloatField m_RotationField;
        private FloatField m_ScaleField;
        private Action m_Action = Action.Select;
        private GUIStyle m_LabelStyle;
        private ImageButton m_ButtonAddObjectsToActiveLOD;
        private ImageButton m_ButtonAdjustObjectHeight;
        private ImageButton m_ButtonCreatePattern;
        private List<UIControl> m_Controls;
        private SceneSelectionTool m_SceneSelectionTool = new();
        private string[] m_LODNames;
        private string[] m_PatternNames;
        private Vector2 m_ScrollPos;
        private PolygonTool m_PolygonTool = new();
        private GeometryType m_GeometryType = GeometryType.Circle;
        private LineTool m_LineTool = new();
        private float m_Rotation = 0;
        private float m_Scale = 1;
        private bool m_Show = true;
        private Vector3 m_LastGenerationCenter = new(-1000, -1000, -1000);
        private const float m_MinSpace = 0.05f;
        private float m_RotationDelta = 10f;
        private float m_ScaleDelta = 0.2f;
        private const float m_MinScale = 0.1f;
        private string[] m_ShapeNames = new string[]
        {
            "多边形",
            "直线",
            "矩形",
            "圆形",
        };
        private string[] m_ActionNames = new string[]
        {
            "选择",
            "编辑LOD",
            "创建物体",
            "使用集合",
            "删除物体",
        };
    }
}

