/*
 * Copyright (c) 2024 XDay
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
using XDay.SerializationAPI;
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

            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.T)
            {
                m_CreateMode = (ObjectCreateMode)(((int)m_CreateMode + 1) % 2);
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

                    m_ResourceGroupSystem.InspectorGUI(ResourceDisplayFlags.AddLOD0Only | ResourceDisplayFlags.CanRemove);
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

                    CreateObjects(points, m_CoordinateGenerateSetting.Random);
                }
            }
        }

        private void ActionDeleteObject()
        {
            var evt = Event.current;

            if (evt.button == 0 && (evt.type == EventType.MouseDown || evt.type == EventType.MouseDrag))
            {
                UndoSystem.NextGroup();
            }

            var worldPosition = Helper.GUIRayCastWithXZPlane(evt.mousePosition, World.CameraManipulator.Camera);
            foreach (var obj in QueryObjectsInRectangle(worldPosition, m_RemoveRange * 2, m_RemoveRange * 2))
            {
                UndoSystem.DestroyObject(obj, DecorationDefine.REMOVE_DECORATION_NAME, ID);
            }
        }

        private HashSet<int> QueryRootObjectSelection()
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
            CreateUIControls();

            EditorGUILayout.BeginHorizontal();
            {
                DrawLODSelection();

                DrawOperation();

                DrawObjectCountCalculation();

                DrawObjectTransformSync();

                DrawDeleteObjects();

                GUILayout.Space(30);

                switch (m_ActiveOperation)
                {
                    case Operation.Select:
                    case Operation.EditLOD:
                        {
                            DrawShowObjectNotInDisplayLOD();
                            DrawHideObjectNotInDisplayLOD();
                            DrawAddObjectToLOD();
                            DrawRemoveObjectFromLOD();
                        }
                        break;
                    case Operation.DeleteObject:
                        {
                            m_RemoveRange = m_RadiusField.Render(m_RemoveRange, 25);
                            break;
                        }
                    case Operation.CreateObject:
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

                m_ObjectCountField = new IntField("Object Count", "", 60);
                m_Controls.Add(m_ObjectCountField);

                m_PopupActiveLOD = new Popup("LOD", "", 80);
                m_Controls.Add(m_PopupActiveLOD);

                m_HeightField = new FloatField("Height", "", 55);
                m_Controls.Add(m_HeightField);

                m_SpaceField = new FloatField("Object Distance", "Minimum distance between objects", 90);
                m_Controls.Add(m_SpaceField);

                m_BorderSizeField = new FloatField("Edge Width", "Generate objects along edge of shape", 90);
                m_Controls.Add(m_BorderSizeField);

                m_ButtonQueryObjectCountInActiveLOD = EditorWorldHelper.CreateImageButton("number.png", "Show object count in current lod");
                m_Controls.Add(m_ButtonQueryObjectCountInActiveLOD);

                m_ButtonRandom = EditorWorldHelper.CreateToggleImageButton(false, "random.png", "Use random object in resource group");
                m_ButtonRandom.Active = m_CoordinateGenerateSetting.Random;
                m_Controls.Add(m_ButtonRandom);

                m_ButtonShowObjectsNotInActiveLOD = EditorWorldHelper.CreateImageButton("show.png", "Show objects not in current lod");
                m_Controls.Add(m_ButtonShowObjectsNotInActiveLOD);

                m_ButtonAddObjectsToActiveLOD = EditorWorldHelper.CreateImageButton("add.png", "Add object to current lod");
                m_Controls.Add(m_ButtonAddObjectsToActiveLOD);

                m_WidthField = new FloatField("Width", "", 55);
                m_Controls.Add(m_WidthField);

                m_ButtonRemoveObjectFromActiveLOD = EditorWorldHelper.CreateImageButton("remove.png", "Remove object from current lod");
                m_Controls.Add(m_ButtonRemoveObjectFromActiveLOD);

                m_GeometryPopup = new EnumPopup("Shape", "", 130);
                m_Controls.Add(m_GeometryPopup);

                m_ButtonSycnObjectTransforms = EditorWorldHelper.CreateImageButton("refresh.png", "Sync object transform");
                m_Controls.Add(m_ButtonSycnObjectTransforms);

                m_ButtonDeleteObjects = EditorWorldHelper.CreateImageButton("delete.png", "Delete objects");
                m_Controls.Add(m_ButtonDeleteObjects);

                m_ButtonHideObjectsNotInActiveLOD = EditorWorldHelper.CreateImageButton("hide.png", "Hide objects not in current lod");
                m_Controls.Add(m_ButtonHideObjectsNotInActiveLOD);

                m_ButtonEqualSpace = EditorWorldHelper.CreateToggleImageButton(false, "equal.png", "Generate equal distance objects");
                m_ButtonEqualSpace.Active = m_CoordinateGenerateSetting.LineEquidistant;
                m_Controls.Add(m_ButtonEqualSpace);

                m_RadiusField = new FloatField("Radius", "", 65);
                m_Controls.Add(m_RadiusField);
            }
        }

        private void ActionSetObjectLOD()
        {
            m_SceneSelectionTool.SceneGUI(World.CameraManipulator.Camera, (min, max) => {
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

        private void SetObjectLODLayerMask(int lod, LayerMaskChangeMode changeMode)
        {
            if (lod < 0)
            {
                return;
            }

            UndoSystem.NextGroup();

            foreach (var objectID in QueryRootObjectSelection())
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
                m_Indicator.Rotation = gameObject.transform.rotation;
                m_Indicator.Position = worldPosition;
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
                ChangeOperation(m_ActiveOperation);
            }
            else
            {
                Tools.hidden = false;
                m_Indicator.Visible = false;
            }
        }

        private void ChangeOperation(Operation operation)
        {
            m_ActiveOperation = operation;
            if (m_ActiveOperation != Operation.Select)
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

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            SyncObjectTransforms();

            base.EditorSerialize(serializer, label, converter);

            serializer.WriteInt32(m_Version, "DecorationSystem.Version");

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

            var allObjects = new List<DecorationObject>();
            foreach (var p in m_Decorations)
            {
                allObjects.Add(p.Value);
            }

            serializer.WriteList(allObjects, "Objects", (obj, index) =>
            {
                serializer.WriteSerializable(obj, $"Object {index}", converter, false);
            });

            serializer.WriteVector2(m_GameGridSize, "Game Grid Size");
            serializer.WriteSerializable(m_PluginLODSystem, "LOD System", converter, false);
            serializer.WriteSerializable(m_ResourceDescriptorSystem, "Resource Descriptor System", converter, false);

            serializer.WriteSerializable(m_ResourceGroupSystem, "Resource Group System", converter, false);
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            base.EditorDeserialize(deserializer, label);

            deserializer.ReadInt32("DecorationSystem.Version");

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

            var allObjects = deserializer.ReadList("Objects", (index) =>
            {
                return deserializer.ReadSerializable<DecorationObject>($"Object {index}", false);
            });
            foreach (var obj in allObjects)
            {
                m_Decorations.Add(obj.ID, obj);
            }

            m_GameGridSize = deserializer.ReadVector2("Game Grid Size");
            m_PluginLODSystem = deserializer.ReadSerializable<IPluginLODSystem>("LOD System", false);
            m_ResourceDescriptorSystem = deserializer.ReadSerializable<EditorResourceDescriptorSystem>("Resource Descriptor System", false);

            m_ResourceGroupSystem = deserializer.ReadSerializable<IResourceGroupSystem>("Resource Group System", false);
        }

        private void DrawDescription()
        {
            if (m_LabelStyle == null)
            {
                m_LabelStyle = new GUIStyle(GUI.skin.label);
            }

            EditorGUILayout.LabelField($"Object Count: {m_Decorations.Count}, Range: {m_Bounds.min:F0} to {m_Bounds.max:F0} meters");
            if (m_ActiveOperation == Operation.CreateObject)
            {
                EditorGUILayout.LabelField("Use R key to redo last generation");
                EditorGUILayout.LabelField("Use T key to toggle Multiple/Single mode");
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

        private void ActionAddObject()
        {
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
            m_CoordinateGenerateSetting.Count = m_ObjectCountField.Render(m_CoordinateGenerateSetting.Count, 25);
            GUI.enabled = old;

            if (m_GeometryType == GeometryType.Circle)
            {
                m_CoordinateGenerateSetting.CircleRadius = m_RadiusField.Render(m_CoordinateGenerateSetting.CircleRadius, 25);
            }
            else if (m_GeometryType == GeometryType.Rectangle)
            {
                m_CoordinateGenerateSetting.RectWidth = m_WidthField.Render(m_CoordinateGenerateSetting.RectWidth, 15);
                m_CoordinateGenerateSetting.RectHeight = m_WidthField.Render(m_CoordinateGenerateSetting.RectHeight, 15);
            }
            else if (m_GeometryType == GeometryType.Line)
            {
                if (m_ButtonEqualSpace.Render(true, GUI.enabled))
                {
                    m_CoordinateGenerateSetting.LineEquidistant = m_ButtonEqualSpace.Active;
                }
            }
            m_CoordinateGenerateSetting.Space = m_SpaceField.Render(m_CoordinateGenerateSetting.Space, 50);
            m_CoordinateGenerateSetting.BorderSize = m_BorderSizeField.Render(m_CoordinateGenerateSetting.BorderSize, 50);

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
                    UndoSystem.DestroyObject(QueryObjectUndo(objectID), DecorationDefine.REMOVE_DECORATION_NAME, ID);
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
            if (m_ButtonQueryObjectCountInActiveLOD.Render(Inited))
            {
                var count = QueryObjectCountInLOD(m_ActiveLOD);
                EditorUtility.DisplayDialog("", $"Object count in current lod: {count}", "OK");
            }
        }

        private void DrawOperation()
        {
            ChangeOperation((Operation)m_PopupOperation.Render(m_ActiveOperation, 60));
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
                new ParameterWindow.BoolParameter("Only Add To Active LOD", "", true),
            };
                ParameterWindow.Open("Add object to LOD", parameters, (p) =>
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

        private void DrawRemoveObjectFromLOD()
        {
            if (m_ButtonRemoveObjectFromActiveLOD.Render(Inited))
            {
                var parameters = new List<ParameterWindow.Parameter>()
                {
                    new ParameterWindow.BoolParameter("Only remove from active lod", "", false),
                };
                ParameterWindow.Open("Remove object from LOD", parameters, (p) =>
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
            if (m_ActiveOperation == Operation.EditLOD)
            {
                ActionSetObjectLOD();
            }
            else if (m_ActiveOperation == Operation.DeleteObject)
            {
                ActionDeleteObject();
            }
            else if (m_ActiveOperation == Operation.CreateObject)
            {
                ActionCreateObject();

                if (m_CreateMode == ObjectCreateMode.Multiple)
                {
                    DrawCreateRange();
                }
            }
            else
            {
                if (m_ActiveOperation != Operation.Select)
                {
                    Debug.Assert(false, $"todo {m_ActiveOperation}");
                }
            }

            if (m_ActiveOperation != Operation.Select)
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

                    CreateObjects(new List<Vector3>() { worldPosition}, false);
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

        private void CreateObjects(List<Vector3> coordinates, bool random)
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
                        var decoration = CreateObject(World.AllocateObjectID(), prefabPath, position, Quaternion.identity, Vector3.one);
                        UndoSystem.CreateObject(decoration, World.ID, DecorationDefine.ADD_DECORATION_NAME, ID, CurrentLOD);
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

            CreateObjects(coordinates, m_CoordinateGenerateSetting.Random);
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

        private enum Operation
        {
            Select,
            EditLOD,
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
        private IntField m_ObjectCountField;
        private ImageButton m_ButtonSycnObjectTransforms;
        private IResourceGroupSystem m_ResourceGroupSystem = IResourceGroupSystem.Create(false);
        private ImageButton m_ButtonQueryObjectCountInActiveLOD;
        private ToggleImageButton m_ButtonRandom;
        private FloatField m_SpaceField;
        private ToggleImageButton m_ButtonEqualSpace;
        private ImageButton m_ButtonHideObjectsNotInActiveLOD;
        private EnumPopup m_GeometryPopup;
        private FloatField m_WidthField;
        private PluginLODSystemEditor m_PluginLODSystemEditor = new();
        private FloatField m_HeightField;
        private Operation m_ActiveOperation = Operation.Select;
        private GUIStyle m_LabelStyle;
        private ImageButton m_ButtonAddObjectsToActiveLOD;
        private List<UIControl> m_Controls;
        private SceneSelectionTool m_SceneSelectionTool = new();
        private string[] m_LODNames;
        private Vector2 m_ScrollPos;
        private PolygonTool m_PolygonTool = new();
        private GeometryType m_GeometryType = GeometryType.Circle;
        private LineTool m_LineTool = new();
        private bool m_Show = true;
        private Vector3 m_LastGenerationCenter = new(-1000, -1000, -1000);
        private const float m_MinSpace = 0.05f;
    }
}


//XDay