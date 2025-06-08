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
using XDay.UtilityAPI.Shape.Editor;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Shape.Editor
{
    internal partial class ShapeSystem
    {
        private void InitBuilder()
        {
            var createInfo = new ShapeBuilder.ShapeBuilderCreateInfo
            {
                CreateShape = (worldVertices) => 
                {
                    var center = Helper.CalculateCenterAndLocalVertices(worldVertices, out var localVertices);
                    var shape = new ShapeObject(World.AllocateObjectID(), m_Shapes.Count, localVertices, center);
                    UndoSystem.CreateObject(shape, World.ID, ShapeDefine.ADD_SHAPE_NAME, ID, 0);
                },
                SnapVertex = null,
                PickShape = (worldPosition) =>
                {
                    var shapeID = FindShape(worldPosition);
                    if (shapeID != 0)
                    {
                        SetActiveShape(shapeID);
                    }
                    return m_ActiveShapeID != 0;
                },
                GetVertexLocalPosition = (index) => 
                {
                    var shape = GetActiveShape();
                    var localPos = shape.GetVertexPosition(index);
                    return localPos;
                },
                GetVertexCount = () => 
                {
                    var shape = GetActiveShape();
                    if (shape == null)
                    {
                        return 0;
                    }
                    return shape.VertexCount; 
                },
                ConvertWorldToLocal = (worldPos) => 
                {
                    var shape = GetActiveShape();
                    return shape.TransformToLocalPosition(worldPos); 
                },
                RepaintInspector = null,

                MoveVertex = (startMoving, index, moveOffset) =>
                {
                    var shape = GetActiveShape();
                    var action = new UndoActionMoveShapeVertex("Move Shape Vertex", UndoSystem.Group, ID, shape.ID, index, moveOffset);
                    UndoSystem.PerformCustomAction(action, true);
                },
                MoveShape = (startMoving, moveOffset) =>
                {
                    var shape = GetActiveShape();
                    var action = new UndoActionMoveShape("Move Shape", UndoSystem.Group, ID, shape.ID, moveOffset);
                    UndoSystem.PerformCustomAction(action, true);
                },
                InsertVertex = (index, localPosition) =>
                {
                    var shape = GetActiveShape();
                    var action = new UndoActionInsertShapeVertex("Insert Shape Vertex", UndoSystem.Group, ID, shape.ID, index, localPosition);
                    UndoSystem.PerformCustomAction(action, true);
                    return true;
                },
                DeleteVertex = (index) =>
                {
                    var shape = GetActiveShape();
                    var action = new UndoActionDeleteShapeVertex("Delete Shape Vertex", UndoSystem.Group, ID, shape.ID, index);
                    UndoSystem.PerformCustomAction(action, true);
                    return true;
                }
            };

            m_Builder = new ShapeBuilder(createInfo);
        }

        protected override void InspectorGUIInternal()
        {
            m_Show = EditorGUILayout.Foldout(m_Show, "Shape");
            if (m_Show)
            {
                m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
                EditorHelper.IndentLayout(() =>
                {
                    DrawProperties();
                });
                EditorGUILayout.EndScrollView();
            }
        }

        private HashSet<int> QueryRootObjectSelection()
        {
            var rootObjectIDs = new HashSet<int>();
            foreach (var gameObject in Selection.gameObjects)
            {
                var behaviour = gameObject.GetComponentInParent<ShapeObjectBehaviour>(true);
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

                if (m_Action == Operation.Edit)
                {
                    DrawColor();
                    DrawRenameObjects();
                }
                DrawDeleteObjects();
                DrawCloneObjects();
                DrawShowVertexIndex();
                DrawVertexDisplaySize();
                GUILayout.Space(30);
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

                m_ColorField = new ColorField("颜色", "", 100);
                m_Controls.Add(m_ColorField);

                m_ShowVertexIndex = EditorWorldHelper.CreateToggleImageButton(false, "show.png", "显隐顶点序号");
                m_Controls.Add(m_ShowVertexIndex);

                m_ButtonDeleteObjects = EditorWorldHelper.CreateImageButton("delete.png", "删除物体");
                m_Controls.Add(m_ButtonDeleteObjects);

                m_ButtonCloneObjects = EditorWorldHelper.CreateImageButton("clone.png", "复制物体");
                m_Controls.Add(m_ButtonCloneObjects);

                m_ButtonRenameObjects = EditorWorldHelper.CreateImageButton("rename.png", "修改物体名称");
                m_Controls.Add(m_ButtonRenameObjects);

                m_VertexSizeField = new FloatField("顶点大小", "", 100);
                m_Controls.Add(m_VertexSizeField);
            }
        }

        private void ShowObjects()
        {
            foreach (var kv in m_Shapes)
            {
                m_Renderer.ToggleVisibility(kv.Value);
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

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            base.EditorSerialize(serializer, label, converter);

            serializer.WriteInt32(m_Version, "ShapeSystem.Version");
            serializer.WriteString(m_Name, "Name");
            serializer.WriteBounds(m_Bounds, "Bounds");

            var allObjects = new List<ShapeObject>();
            foreach (var p in m_Shapes)
            {
                allObjects.Add(p.Value);
            }

            serializer.WriteList(allObjects, "Objects", (obj, index) =>
            {
                serializer.WriteSerializable(obj, $"Object {index}", converter, false);
            });
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            base.EditorDeserialize(deserializer, label);

            deserializer.ReadInt32("ShapeSystem.Version");

            m_Name = deserializer.ReadString("Name");
            m_Bounds = deserializer.ReadBounds("Bounds");

            var allObjects = deserializer.ReadList("Objects", (index) =>
            {
                return deserializer.ReadSerializable<ShapeObject>($"Object {index}", false);
            });
            foreach (var obj in allObjects)
            {
                m_Shapes.Add(obj.ID, obj);
            }
        }

        private void DrawDescription()
        {
            if (m_LabelStyle == null)
            {
                m_LabelStyle = new GUIStyle(GUI.skin.label);
            }

            EditorGUILayout.LabelField($"物体总数: {m_Shapes.Count}, 范围: {m_Bounds.min:F0}到{m_Bounds.max:F0}米");
            EditorGUILayout.LabelField("Ctrl+1/Ctrl+2/Ctrl+3切换操作");
        }

        private void DrawDeleteObjects()
        {
            if (m_ButtonDeleteObjects.Render(Inited))
            {
                UndoSystem.NextGroup();
                var objectIDs = QueryRootObjectSelection();
                foreach (var objectID in objectIDs)
                {
                    UndoSystem.DestroyObject(QueryObjectUndo(objectID), ShapeDefine.REMOVE_SHAPE_NAME, ID);
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

                ParameterWindow.Open("复制物体", parameters, (p) => {
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

        private void DrawRenameObjects()
        {
            var shape = GetActiveShape();
            if (shape != null)
            {
                if (m_ButtonRenameObjects.Render(Inited))
                {
                    var parameters = new List<ParameterWindow.Parameter>()
                    {
                        new ParameterWindow.StringParameter("名称", "", shape.Name),
                    };

                    ParameterWindow.Open("修改名称", parameters, (p) =>
                    {
                        ParameterWindow.GetString(p[0], out var name);
                        if (!string.IsNullOrEmpty(name))
                        {
                            UndoSystem.SetAspect(shape, ShapeDefine.SHAPE_NAME, IAspect.FromString(name), "Rename Shape", ID, UndoActionJoinMode.Both);
                            return true;
                        }
                        return false;
                    });
                }
            }
        }

        protected override void SceneGUISelectedInternal()
        {
            if (m_Action != Operation.Select)
            {
                var shape = GetActiveShape();
                m_Builder.DrawSceneGUI(
                    vertexDisplaySize: shape == null ? 1 : shape.VertexDisplaySize,
                    shape == null ? 0 : shape.Position.y,
                    m_Action == Operation.Create ? ShapeBuilder.Operation.Create : ShapeBuilder.Operation.Edit);
            }

            DrawBounds();

            SceneView.RepaintAll();

            m_Renderer.Update();
        }

        private void DrawBounds()
        {
            var oldColor = Handles.color;
            Handles.color = Color.white;
            Handles.DrawWireCube(m_Bounds.center, m_Bounds.size);
            Handles.color = oldColor;
        }

        private void ActionCloneObject(Vector3 offset)
        {
            UndoSystem.NextGroupAndJoin();

            var objectIDs = QueryRootObjectSelection();
            CloneObjects(new List<int>(objectIDs), offset);
        }

        private void CloneObjects(List<int> objects, Vector3 offset)
        {
            List<ShapeObject> newObjects = new();
            foreach (var objID in objects)
            {
                var obj = QueryObjectUndo(objID) as ShapeObject;
                var shape = CloneObject(World.AllocateObjectID(), m_Shapes.Count, obj);
                if (shape != null)
                {
                    UndoSystem.CreateObject(shape, World.ID, ShapeDefine.ADD_SHAPE_NAME, ID, 0);
                    var newShape = World.QueryObject<ShapeObject>(shape.ID);
                    newObjects.Add(newShape);
                    UndoSystem.SetAspect(newShape, ShapeDefine.POSITION_NAME, IAspect.FromVector3(obj.Position + offset), "Set Shape Position", ID, UndoActionJoinMode.None);
                }
            }

            List<Object> gameObjects = new();
            foreach (var dec in newObjects)
            {
                gameObjects.Add(m_Renderer.QueryGameObject(dec.ID));
            }
            Selection.objects = gameObjects.ToArray();
        }

        private ShapeObject GetActiveShape()
        {
            return QueryObjectUndo(m_ActiveShapeID) as ShapeObject;
        }

        private int FindShape(Vector3 worldPosition)
        {
            int shapeID = 0;
            foreach (var shape in m_Shapes.Values)
            {
                if (shape.Hit(worldPosition))
                {
                    shapeID = shape.ID;
                    break;
                }
            }
            return shapeID;
        }

        public void UpdateRenderer(int objectID)
        {
            m_Renderer.SetDirty(objectID);
        }

        private void DrawOperation()
        {
            ChangeOperation((Operation)m_PopupOperation.Render((int)m_Action, m_ActionNames, 35));
        }

        private void DrawShowVertexIndex()
        {
            var shape = GetActiveShape();
            if (shape != null)
            {
                m_ShowVertexIndex.Active = shape.ShowVertexIndex;
                if (m_ShowVertexIndex.Render(true, Inited))
                {
                    UndoSystem.SetAspect(shape, ShapeDefine.SHAPE_VERTEX_INDEX_NAME, IAspect.FromBoolean(m_ShowVertexIndex.Active), "Show Shape Vertex Index", 0, UndoActionJoinMode.Both);
                }
            }
        }

        private void DrawColor()
        {
            var shape = GetActiveShape();
            if (shape != null)
            {
                var newColor = m_ColorField.Render(shape.Color, 50);
                if (newColor != shape.Color)
                {
                    UndoSystem.SetAspect(shape, ShapeDefine.COLOR_NAME, IAspect.FromColor(newColor), "Set Shape Color", ID, UndoActionJoinMode.None);
                }
            }
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

        private void SetActiveShape(int shapeID)
        {
            if (shapeID != m_ActiveShapeID)
            {
                var oldShape = QueryObjectUndo(m_ActiveShapeID) as ShapeObject;
                if (oldShape != null)
                {
                    oldShape.UseOverriddenColor(false);
                }
                m_ActiveShapeID = shapeID;
                var shape = GetActiveShape();
                if (shape != null)
                {
                    shape.UseOverriddenColor(true);
                    shape.SetOverriddenColor(Color.green);
                    var gameObject = m_Renderer.QueryGameObject(shape.ID);
                    if (gameObject != null)
                    {
                        Selection.activeGameObject = gameObject;
                    }
                }

                EditorWindow.GetWindow<WorldEditorEntrance>().Repaint();
            }
        }

        private void DrawProperties()
        {
            var shape = GetActiveShape();
            if (shape != null)
            {
                shape.AreaID = EditorGUILayout.IntField("Area ID", shape.AreaID);
                shape.Attribute = (ObstacleAttribute)EditorGUILayout.EnumFlagsField("Attribute", shape.Attribute);
                shape.Height = EditorGUILayout.FloatField("Height", shape.Height);

                m_AspectContainerEditor.Draw(canEdit:true, shape.AspectContainer);
            }
        }

        private void DrawVertexDisplaySize()
        {
            var shape = GetActiveShape();
            if (shape != null) 
            {
                float value = m_VertexSizeField.Render(shape.VertexDisplaySize, 60);
                if (!Mathf.Approximately(value, shape.VertexDisplaySize))
                {
                    UndoSystem.SetAspect(shape, ShapeDefine.SHAPE_VERTEX_DISPLAY_SIZE, IAspect.FromSingle(value), "Shape Vertex Display Size", 0, UndoActionJoinMode.None);
                }
            }
        }

        private ShapeObject GetShapeObjectFromGameObject(GameObject gameObject)
        {
            var objectID = m_Renderer.QueryObjectID(gameObject);
            if (objectID == 0)
            {
                return null;
            }
            return QueryObjectUndo(objectID) as ShapeObject;
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

                var shapeObject = GetShapeObjectFromGameObject(gameObject);
                if (shapeObject != null)
                {
                    SetActiveShape(shapeObject.ID);
                }
            };
        }

        private enum Operation
        {
            Select,
            Edit,
            Create,
        }

        private int m_ActiveShapeID = 0;
        private ImageButton m_ButtonDeleteObjects;
        private ImageButton m_ButtonCloneObjects;
        private ImageButton m_ButtonRenameObjects;
        private FloatField m_VertexSizeField;
        private ColorField m_ColorField;
        private ToggleImageButton m_ShowVertexIndex;
        private GUIStyle m_LabelStyle;
        private List<UIControl> m_Controls;
        private Vector2 m_ScrollPos;
        private bool m_Show = true;
        private ShapeBuilder m_Builder;
        private Popup m_PopupOperation;
        private Operation m_Action = Operation.Select;
        private AspectContainerEditor m_AspectContainerEditor = new();
        private string[] m_ActionNames = new string[]
        {
            "选择",
            "编辑",
            "创建",
        };
    }
}