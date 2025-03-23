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

namespace XDay.WorldAPI.LogicObject.Editor
{
    internal partial class LogicObjectSystem
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

        protected override void InspectorGUIInternal()
        {
            m_Show = EditorGUILayout.Foldout(m_Show, "Logic Object");
            if (m_Show)
            {
                m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
                EditorHelper.IndentLayout(() =>
                {
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
                if (UndoSystem.LastActionName == LogicObjectDefine.ADD_LOGIC_OBJECT_NAME)
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
                    UndoSystem.DestroyObject(obj, LogicObjectDefine.REMOVE_LOGIC_OBJECT_NAME, ID);
                }
            }
        }

        private HashSet<int> QueryRootObjectSelection()
        {
            var rootObjectIDs = new HashSet<int>();
            foreach (var gameObject in Selection.gameObjects)
            {
                var behaviour = gameObject.GetComponentInParent<LogicObjectBehaviour>(true);
                if (behaviour != null)
                {
                    rootObjectIDs.Add(behaviour.ObjectID);
                }
            }
            return rootObjectIDs;
        }

        protected override void SceneViewControlInternal(Rect sceneViewRect)
        {
            CreateUIControls();

            DrawGroups();

            EditorGUILayout.BeginHorizontal();
            {
                DrawOperation();

                DrawObjectCountCalculation();

                DrawObjectTransformSync();

                DrawDeleteObjects();

                GUILayout.Space(30);

                switch (m_Action)
                {
                    case Action.Select:
                        break;
                    case Action.DeleteObject:
                        {
                            m_RemoveRange = m_RadiusField.Render(m_RemoveRange, 40);
                            break;
                        }
                    case Action.CreateObject:
                        {
                            ActionAddObject();
                            break;
                        }
                    default:
                        Debug.Assert(false, "todo");
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

                m_PopupOperation = new EnumPopup("Operation", "", 160);
                m_Controls.Add(m_PopupOperation);

                m_GroupsPopup = new Popup("Group", "", 160);
                m_Controls.Add(m_GroupsPopup);

                m_ObjectCountField = new IntField("Count", "", 80);
                m_Controls.Add(m_ObjectCountField);

                m_PopupActiveLOD = new Popup("LOD", "", 80);
                m_Controls.Add(m_PopupActiveLOD);

                m_HeightField = new FloatField("Height", "", 80);
                m_Controls.Add(m_HeightField);

                m_RotationField = new FloatField("Rotation", "", 100);
                m_Controls.Add(m_RotationField);

                m_ScaleField = new FloatField("Scale", "", 100);
                m_Controls.Add(m_ScaleField);

                m_SpaceField = new FloatField("Space", "Minimum distance between objects", 90);
                m_Controls.Add(m_SpaceField);

                m_BorderSizeField = new FloatField("Border", "Generate objects along edge of shape", 90);
                m_Controls.Add(m_BorderSizeField);

                m_ButtonQueryObjectCountInActiveGroup = EditorWorldHelper.CreateImageButton("number.png", "Show object count in current group");
                m_Controls.Add(m_ButtonQueryObjectCountInActiveGroup);

                m_AddGroupButton = EditorWorldHelper.CreateImageButton("add.png", "Add Group");
                m_Controls.Add(m_AddGroupButton);

                m_RemoveGroupButton = EditorWorldHelper.CreateImageButton("remove.png", "Remove Group");
                m_Controls.Add(m_RemoveGroupButton);

                m_RenameGroupButton = EditorWorldHelper.CreateImageButton("rename.png", "Rename Group");
                m_Controls.Add(m_RenameGroupButton);

                m_GroupVisibilityButton = EditorWorldHelper.CreateToggleImageButton(false, "show.png", "Show/Hide Group");
                m_Controls.Add(m_GroupVisibilityButton);

                m_ButtonRandom = EditorWorldHelper.CreateToggleImageButton(false, "random.png", "Use random object in resource group");
                m_ButtonRandom.Active = m_CoordinateGenerateSetting.Random;
                m_Controls.Add(m_ButtonRandom);

                m_ButtonShowObjectsNotInActiveLOD = EditorWorldHelper.CreateImageButton("show.png", "Show objects not in current group");
                m_Controls.Add(m_ButtonShowObjectsNotInActiveLOD);

                m_ButtonAddObjectsToActiveLOD = EditorWorldHelper.CreateImageButton("add.png", "Add object to current lod");
                m_Controls.Add(m_ButtonAddObjectsToActiveLOD);

                m_WidthField = new FloatField("Width", "", 80);
                m_Controls.Add(m_WidthField);

                m_ButtonRemoveObjectFromActiveLOD = EditorWorldHelper.CreateImageButton("remove.png", "Remove object from current lod");
                m_Controls.Add(m_ButtonRemoveObjectFromActiveLOD);

                m_GeometryPopup = new EnumPopup("Shape", "", 130);
                m_Controls.Add(m_GeometryPopup);

                m_ButtonSycnObjectTransforms = EditorWorldHelper.CreateImageButton("refresh.png", "Sync object transform");
                m_Controls.Add(m_ButtonSycnObjectTransforms);

                m_ButtonDeleteObjects = EditorWorldHelper.CreateImageButton("delete.png", "Delete objects");
                m_Controls.Add(m_ButtonDeleteObjects);

                m_ButtonEqualSpace = EditorWorldHelper.CreateToggleImageButton(false, "equal.png", "Generate equal distance objects");
                m_ButtonEqualSpace.Active = m_CoordinateGenerateSetting.LineEquidistant;
                m_Controls.Add(m_ButtonEqualSpace);

                m_RadiusField = new FloatField("Radius", "", 80);
                m_Controls.Add(m_RadiusField);
            }
        }

        private bool IsObjectTransformChanged(LogicObject obj, GameObject gameObject)
        {
            var transform = gameObject.transform;
            return obj.Position != transform.position ||
                obj.Scale != transform.localScale ||
                obj.Rotation != transform.rotation;
        }

        private void ShowObjects()
        {
            for (var i = 0; i < m_Groups.Count; i++)
            {
                m_Renderer.Create(m_Groups[i], i);
                foreach (var obj in m_Groups[i].Objects)
                {
                    m_Renderer.ToggleVisibility(obj, 0);
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

        private void SetCurrentGroup(int groupID)
        {
            if (m_CurrentGroupID != groupID)
            {
                SetGroupActive(m_CurrentGroupID, false);
                m_CurrentGroupID = groupID;
                SetGroupActive(m_CurrentGroupID, true);
            }
        }

        private void SetGroupActive(int groupID, bool show)
        {
            var group = GetGroup(groupID);
            if (group != null)
            {
                group.SetEnabled(show);
                m_Renderer?.ToggleVisibility(group);
            }
        }

        private void SetGroupVisibility(int groupID, bool show)
        {
            var group = GetGroup(groupID);
            if (group != null)
            {
                UndoSystem.SetAspect(group, LogicObjectDefine.CHANGE_LOGIC_OBJECT_GROUP_VISIBILITY, IAspect.FromBoolean(show), "Set Group Visibility", ID, UndoActionJoinMode.Both);
            }
        }

        private LogicObjectGroup GetGroup(int groupID)
        {
            foreach (var group in m_Groups)
            {
                if (group.ID == groupID)
                {
                    return group;
                }
            }
            return null;
        }

        private void SetGeometryType(GeometryType type)
        {
            if (type != m_GeometryType)
            {
                m_GeometryType = type;
                m_ButtonRandom.Active = m_CoordinateGenerateSetting.Random;
            }
        }

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            SyncObjectTransforms();

            base.EditorSerialize(serializer, label, converter);

            serializer.WriteInt32(m_Version, "LogicObjectSystem.Version");

            serializer.WriteInt32((int)m_CreateMode, "Create Mode");
            serializer.WriteString(m_Name, "Name");
            serializer.WriteSingle(m_RemoveRange, "Remove Range");
            serializer.WriteBounds(m_Bounds, "Bounds");

            serializer.WriteSingle(m_CoordinateGenerateSetting.CircleRadius, "Circle Radius");
            serializer.WriteSingle(m_CoordinateGenerateSetting.RectWidth, "Rect Width");
            serializer.WriteSingle(m_CoordinateGenerateSetting.RectHeight, "Rect Height");
            serializer.WriteInt32(m_CoordinateGenerateSetting.Count, "Object Count");
            serializer.WriteSingle(m_CoordinateGenerateSetting.Space, "Space");
            serializer.WriteBoolean(m_CoordinateGenerateSetting.Random, "Random");
            serializer.WriteSingle(m_CoordinateGenerateSetting.BorderSize, "Border Size");
            serializer.WriteBoolean(m_CoordinateGenerateSetting.LineEquidistant, "Line Equidistant");

            //sync object names
            foreach (var group in m_Groups)
            {
                foreach (var obj in group.Objects)
                {
                    var gameObject = m_Renderer.QueryObject(obj.ID);
                    obj.Name = gameObject.name;
                }
            }

            serializer.WriteList(m_Groups, "Groups", (group, index) =>
            {
                serializer.WriteSerializable(group, $"Group {index}", converter, false);
            });

            serializer.WriteSerializable(m_ResourceDescriptorSystem, "Resource Descriptor System", converter, false);
            serializer.WriteSerializable(m_ResourceGroupSystem, "Resource Group System", converter, false);
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            base.EditorDeserialize(deserializer, label);

            var version = deserializer.ReadInt32("LogicObjectSystem.Version");

            m_CreateMode = (ObjectCreateMode)deserializer.ReadInt32("Create Mode");
            m_Name = deserializer.ReadString("Name");
            m_RemoveRange = deserializer.ReadSingle("Remove Range");
            m_Bounds = deserializer.ReadBounds("Bounds");

            m_CoordinateGenerateSetting.CircleRadius = deserializer.ReadSingle("Circle Radius");
            m_CoordinateGenerateSetting.RectWidth = deserializer.ReadSingle("Rect Width");
            m_CoordinateGenerateSetting.RectHeight = deserializer.ReadSingle("Rect Height");
            m_CoordinateGenerateSetting.Count = deserializer.ReadInt32("Object Count");
            m_CoordinateGenerateSetting.Space = deserializer.ReadSingle("Space");
            m_CoordinateGenerateSetting.Random = deserializer.ReadBoolean("Random");
            m_CoordinateGenerateSetting.BorderSize = deserializer.ReadSingle("Border Size");
            m_CoordinateGenerateSetting.LineEquidistant = deserializer.ReadBoolean("Line Equidistant");

            m_Groups = deserializer.ReadList("Groups", (index) =>
            {
                return deserializer.ReadSerializable<LogicObjectGroup>($"Group {index}", false);
            });

            m_ResourceDescriptorSystem = deserializer.ReadSerializable<EditorResourceDescriptorSystem>("Resource Descriptor System", false);
            m_ResourceGroupSystem = deserializer.ReadSerializable<IResourceGroupSystem>("Resource Group System", false);
        }

        private void DrawDescription()
        {
            if (m_LabelStyle == null)
            {
                m_LabelStyle = new GUIStyle(GUI.skin.label);
            }

            var group = GetCurrentGroup();
            if (group != null)
            {
                EditorGUILayout.LabelField($"Object Count: {group.ObjectCount}, Range: {m_Bounds.min:F0} to {m_Bounds.max:F0} meters");
            }
            if (m_Action == Action.CreateObject)
            {
                EditorGUILayout.LabelField("Use R key to redo last generation");
                EditorGUILayout.LabelField("Use T key to toggle Multiple/Single mode");
                EditorGUILayout.LabelField("Use Q/W key to rotate object");
                EditorGUILayout.LabelField("Use A/S key to scale object");
            }

            if (GetDirtyObjectIDs().Count == 0)
            {
                m_LabelStyle.normal.textColor = new Color(0, 0, 0, 0);
            }
            else
            {
                m_LabelStyle.normal.textColor = Color.magenta;
            }
            EditorGUILayout.LabelField("Transform sync needed!", m_LabelStyle);
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
            if (m_GeometryType == GeometryType.Circle)
            {
                Handles.DrawWireDisc(worldPosition, Vector3.up, m_RemoveRange);
            }
            Handles.color = oldColor;
        }

        private void ActionAddObject()
        {
            if (m_CreateMode == ObjectCreateMode.Single)
            {
                m_Rotation = m_RotationField.Render(m_Rotation, 50);
                m_Scale = m_ScaleField.Render(m_Scale, 50);
            }

            GUI.enabled = m_CreateMode == ObjectCreateMode.Multiple;

            var shape = (GeometryType)m_GeometryPopup.Render(m_GeometryType, 40);
            SetGeometryType(shape);

            var enabled = true;
            if (m_GeometryType == GeometryType.Line && m_CoordinateGenerateSetting.LineEquidistant)
            {
                enabled = false;
            }

            var old = GUI.enabled;
            GUI.enabled = enabled;
            m_CoordinateGenerateSetting.Count = m_ObjectCountField.Render(m_CoordinateGenerateSetting.Count, 40);
            GUI.enabled = old;

            if (m_GeometryType == GeometryType.Circle)
            {
                m_CoordinateGenerateSetting.CircleRadius = m_RadiusField.Render(m_CoordinateGenerateSetting.CircleRadius, 40);
            }
            else if (m_GeometryType == GeometryType.Rectangle)
            {
                m_CoordinateGenerateSetting.RectWidth = m_WidthField.Render(m_CoordinateGenerateSetting.RectWidth, 40);
                m_CoordinateGenerateSetting.RectHeight = m_HeightField.Render(m_CoordinateGenerateSetting.RectHeight, 40);
            }
            else if (m_GeometryType == GeometryType.Line)
            {
                if (m_ButtonEqualSpace.Render(true, GUI.enabled))
                {
                    m_CoordinateGenerateSetting.LineEquidistant = m_ButtonEqualSpace.Active;
                }
            }
            m_CoordinateGenerateSetting.Space = m_SpaceField.Render(m_CoordinateGenerateSetting.Space, 40);
            m_CoordinateGenerateSetting.BorderSize = m_BorderSizeField.Render(m_CoordinateGenerateSetting.BorderSize, 40);

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
                UndoSystem.NextGroup();
                var objectIDs = QueryRootObjectSelection();
                foreach (var objectID in objectIDs)
                {
                    UndoSystem.DestroyObject(QueryObjectUndo(objectID), LogicObjectDefine.REMOVE_LOGIC_OBJECT_NAME, ID);
                }
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
            if (m_ButtonQueryObjectCountInActiveGroup.Render(Inited))
            {
                var group = GetCurrentGroup();
                if (group != null)
                {
                    EditorUtility.DisplayDialog("", $"Object count in current group: {group.ObjectCount}", "OK");
                }
            }
        }

        private void DrawOperation()
        {
            ChangeOperation((Action)m_PopupOperation.Render(m_Action, 60));
        }

        private void DrawGroups()
        {
            EditorGUILayout.BeginHorizontal();
            if (m_GroupNames == null || m_GroupNames.Length != m_Groups.Count)
            {
                m_GroupNames = new string[m_Groups.Count];
            }
            for (var i = 0; i < m_Groups.Count; ++i)
            {
                m_GroupNames[i] = m_Groups[i].Name;
            }

            var curGroupIndex = GetCurrentGroupIndex();
            var groupIndex = m_GroupsPopup.Render(curGroupIndex, m_GroupNames, 50);
            if (groupIndex != curGroupIndex)
            {
                SetCurrentGroup(m_Groups[groupIndex].ID);
            }

            var curGroup = GetCurrentGroup();
            if (curGroup != null)
            {
                m_GroupVisibilityButton.Active = curGroup.Visible;
            }
            if (m_GroupVisibilityButton.Render(true, GUI.enabled && curGroup != null))
            {
                SetGroupVisibility(curGroup.ID, m_GroupVisibilityButton.Active);
            }

            if (m_AddGroupButton.Render(Inited))
            {
                var parameters = new List<ParameterWindow.Parameter>
                    {
                        new ParameterWindow.StringParameter("Name", "", ""),
                    };

                ParameterWindow.Open("Add Group", parameters, (p) => {
                    bool ok = ParameterWindow.GetString(p[0], out var groupName);
                    if (!ok || HasGroup(groupName))
                    {
                        return false;
                    }
                    var group = new LogicObjectGroup(World.AllocateObjectID(), m_Groups.Count, groupName);
                    UndoSystem.CreateObject(group, World.ID, LogicObjectDefine.ADD_LOGIC_OBJECT_GROUP_NAME, ID);
                    SetCurrentGroup(group.ID);

                    return true;
                });
            }

            if (m_RemoveGroupButton.Render(Inited))
            {
                var group = GetCurrentGroup();
                if (group != null)
                {
                    var objects = new List<LogicObject>(group.Objects);
                    for (var i = objects.Count - 1; i >= 0; --i)
                    {
                        var name = objects[i].Name;
                        UndoSystem.DestroyObject(objects[i], LogicObjectDefine.REMOVE_LOGIC_OBJECT_NAME, ID);
                    }
                    UndoSystem.DestroyObject(group, LogicObjectDefine.REMOVE_LOGIC_OBJECT_GROUP_NAME, ID);
                    if (m_Groups.Count > 0)
                    {
                        SetCurrentGroup(m_Groups[0].ID);
                    }
                    else
                    {
                        SetCurrentGroup(0);
                    }
                }
            }

            if (m_RenameGroupButton.Render(Inited))
            {
                var group = GetCurrentGroup();
                var parameters = new List<ParameterWindow.Parameter>()
                {
                    new ParameterWindow.StringParameter("Name", "", group.Name),
                };
                ParameterWindow.Open("Change Group Name", parameters, (p) =>
                {
                    var ok = ParameterWindow.GetString(p[0], out var name);
                    if (ok && !HasGroup(name))
                    {
                        UndoSystem.SetAspect(group, LogicObjectDefine.CHANGE_LOGIC_OBJECT_GROUP_NAME, IAspect.FromString(name), "Group Name", ID, UndoActionJoinMode.NextGroup);
                        return true;
                    }
                    return false;
                });
            }
            EditorGUILayout.EndHorizontal();
        }

        protected override void SceneGUISelectedInternal()
        {
            var evt = Event.current;
            if (evt.type == EventType.KeyDown && evt.shift == false)
            {
                if (evt.keyCode == KeyCode.Alpha1)
                {
                    ChangeOperation(Action.Select);
                }
                else if (evt.keyCode == KeyCode.Alpha2)
                {
                    ChangeOperation(Action.CreateObject);
                }
                else if (evt.keyCode == KeyCode.Alpha3)
                {
                    ChangeOperation(Action.DeleteObject);
                }
                evt.Use();
            }

            m_Indicator.Visible = false;
            if (m_Action == Action.DeleteObject)
            {
                ActionDeleteObject();
            }
            else if (m_Action == Action.CreateObject)
            {
                ActionCreateObject();

                if (m_CreateMode == ObjectCreateMode.Multiple)
                {
                    DrawCreateRange();
                }
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

            SceneView.RepaintAll();
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

                    CreateObjects(new List<Vector3>() { worldPosition}, m_Rotation, m_Scale, false);
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
                        var group = GetCurrentGroup();
                        var obj = CreateObject(World.AllocateObjectID(), prefabPath, position, Quaternion.Euler(0, rotation, 0), Vector3.one * scale, group);
                        UndoSystem.CreateObject(obj, World.ID, LogicObjectDefine.ADD_LOGIC_OBJECT_NAME, ID, 0);
                    }
                }
            }
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
                m_PolygonTool.SceneUpdate(Color.green, (polygon) => {
                    m_LastOperation = new CoordinateGenerateOperation(polygon);

                    coordinates = GeneratePolygonCoordinates(polygon);

                });
            }
            else if (m_GeometryType == GeometryType.Line)
            {
                m_LineTool.SceneUpdate(Color.green, (start, end) => {
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
                    var obj = World.QueryObject<LogicObject>(objID);
                    var gameObject = m_Renderer.QueryObject(objID);

                    if (gameObject == null || obj == null)
                    {
                        continue;
                    }

                    var transform = gameObject.transform;
                    if (IsObjectTransformChanged(obj, gameObject))
                    {
                        UndoSystem.SetAspect(obj, LogicObjectDefine.SCALE_NAME, IAspect.FromVector3(transform.localScale), "Set Logic Object Scale", ID, UndoActionJoinMode.None);
                        UndoSystem.SetAspect(obj, LogicObjectDefine.ROTATION_NAME, IAspect.FromQuaternion(transform.rotation), "Set Logic Object Rotation", ID, UndoActionJoinMode.None);
                        UndoSystem.SetAspect(obj, LogicObjectDefine.POSITION_NAME, IAspect.FromVector3(transform.position), "Set Logic Object Position", ID, UndoActionJoinMode.None);
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
            CreateObject,
            DeleteObject,
        }

        private CoordinateGenerateOperation m_LastOperation;
        private ImageButton m_ButtonShowObjectsNotInActiveLOD;
        private bool m_ClearOperation = false;
        private ImageButton m_ButtonRemoveObjectFromActiveLOD;
        private Popup m_PopupActiveLOD;
        private FloatField m_BorderSizeField;
        private ImageButton m_ButtonDeleteObjects;
        private FloatField m_RadiusField;
        private EnumPopup m_PopupOperation;
        private Popup m_GroupsPopup;
        private IntField m_ObjectCountField;
        private ImageButton m_ButtonSycnObjectTransforms;
        private IResourceGroupSystem m_ResourceGroupSystem = IResourceGroupSystem.Create(false);
        private ImageButton m_ButtonQueryObjectCountInActiveGroup;
        private ImageButton m_AddGroupButton;
        private ImageButton m_RemoveGroupButton;
        private ImageButton m_RenameGroupButton;
        private ToggleImageButton m_GroupVisibilityButton;
        private ToggleImageButton m_ButtonRandom;
        private FloatField m_SpaceField;
        private ToggleImageButton m_ButtonEqualSpace;
        private EnumPopup m_GeometryPopup;
        private FloatField m_WidthField;
        private FloatField m_HeightField;
        private FloatField m_RotationField;
        private FloatField m_ScaleField;
        private Action m_Action = Action.Select;
        private GUIStyle m_LabelStyle;
        private ImageButton m_ButtonAddObjectsToActiveLOD;
        private List<UIControl> m_Controls;
        private SceneSelectionTool m_SceneSelectionTool = new();
        private string[] m_GroupNames;
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
    }
}

//XDay
