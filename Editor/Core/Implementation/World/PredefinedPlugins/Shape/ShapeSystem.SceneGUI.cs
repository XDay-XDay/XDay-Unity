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
using XDay.UtilityAPI;
using XDay.WorldAPI.Editor;
using System;

namespace XDay.WorldAPI.Shape.Editor
{
    /// <summary>
    /// 编辑Shape
    /// </summary>
    internal partial class ShapeSystem
    {
        public void Clear()
        {
            m_IsValidCreateState = true;
            m_GizmoVertices = new Vector3[0];
            m_VertexOfCreatingShape.Clear();
            m_PickedShapes.Clear();
        }

        private void CreateShape()
        {
            if (m_VertexOfCreatingShape.Count > 3)
            {
                m_VertexOfCreatingShape.RemoveAt(m_VertexOfCreatingShape.Count - 1);
                var center = Helper.CalculateCenterAndLocalVertices(m_VertexOfCreatingShape, out var localVertices);
                var layer = GetCurrentLayer();
                var shape = new ShapeObject(World.AllocateObjectID(), layer.ShapeCount, ID, localVertices, center);
                UndoSystem.CreateObject(shape, World.ID, ShapeDefine.ADD_SHAPE_NAME, ID, 0);
            }

            m_VertexOfCreatingShape.Clear();
            m_GizmoVertices = new Vector3[0];
            m_IsValidCreateState = true;
        }

        private void MoveShape(bool startMoving, Vector3 worldPos)
        {
            m_MovementUpdater.Update(worldPos.ToVector2());

            var offset = m_MovementUpdater.GetMovement().ToVector3XZ();

            if (startMoving || offset != Vector3.zero)
            {
                foreach (var info in m_PickedShapes)
                {
                    var shape = QueryObjectUndo(info.ShapeID);
                    var action = new UndoActionMoveShape("Move Shape", UndoSystem.Group, ID, shape.ID, offset);
                    UndoSystem.PerformCustomAction(action, true);
                }
            }
        }

        private void HitTest(Vector3 worldPosition)
        {
            List<ShapePickInfo> pickedShapes = new();

            foreach (var layer in m_Layers)
            {
                foreach (var shape in layer.Shapes.Values)
                {
                    if (shape.Hit(worldPosition, m_VertexDisplaySize * 0.5f, out var index))
                    {
                        pickedShapes.Add(new ShapePickInfo(shape.ID, index));
                    }
                }
            }

            SetActiveShapes(pickedShapes);
        }

        private bool IsHitOneVertex(Vector3 pos, Vector3 vertexPos, float boxColliderSize)
        {
            var halfSize = boxColliderSize * 0.5f;
            var delta = pos - vertexPos;
            delta.y = 0;
            return delta.sqrMagnitude <= halfSize * halfSize;
        }

        private void MoveVertex(bool startMoving, Vector3 worldPosition)
        {
            m_MovementUpdater.Update(worldPosition.ToVector2());
            var movedOffset = m_MovementUpdater.GetMovement().ToVector3XZ();

            if (m_PickedShapes.Count > 0)
            {
                foreach (var pickInfo in m_PickedShapes)
                {
                    var shape = QueryObjectUndo(pickInfo.ShapeID) as ShapeObject;

                    var oldWorldPos = shape.GetVertexWorldPosition(pickInfo.VertexIndex);
                    var newWorldPosition = movedOffset + oldWorldPos;

                    if (startMoving || newWorldPosition != oldWorldPos)
                    {
                        var action = new UndoActionMoveShapeVertex("Move Shape Vertex", UndoSystem.Group, ID, shape.ID, pickInfo.VertexIndex, movedOffset);
                        UndoSystem.PerformCustomAction(action, true);
                    }
                }
            }
        }

        private void InsertVertex(Vector3 worldPosition)
        {
            foreach (var info in m_PickedShapes)
            {
                m_TempContainer.Clear();
                var shape = QueryObjectUndo(info.ShapeID) as ShapeObject;
                var vertexCount = shape.VertexCount;
                if (vertexCount > 0)
                {
                    for (var i = 0; i < vertexCount; ++i)
                    {
                        var localPos = shape.GetVertexPosition(i);
                        m_TempContainer.Add(localPos);
                    }
                    var localPosition = shape.TransformToLocalPosition(worldPosition);
                    var index = Helper.FindClosestEdgeOnProjection(localPosition, m_TempContainer);
                    var action = new UndoActionInsertShapeVertex("Insert Shape Vertex", UndoSystem.Group, ID, shape.ID, index, localPosition);
                    UndoSystem.PerformCustomAction(action, true);
                    info.VertexIndex = index;
                }
            }
        }

        private void DeleteVertex()
        {
            if (m_PickedShapes.Count > 0)
            {
                foreach (var pickInfo in m_PickedShapes)
                {
                    var shape = QueryObjectUndo(pickInfo.ShapeID) as ShapeObject;
                    if (shape.VertexCount > 3)
                    {
                        var action = new UndoActionDeleteShapeVertex("Delete Shape Vertex", UndoSystem.Group, ID, shape.ID, pickInfo.VertexIndex);
                        UndoSystem.PerformCustomAction(action, true);
                        pickInfo.VertexIndex = 0;
                    }
                }
                if (m_PickedShapes.Count > 1)
                {
                    SetActiveShapes(new());
                }
            }
        }

        private void DrawSceneGUI(float vertexDisplaySize, float planeHeight, Operation operation)
        {
            if (GetCurrentLayer() == null)
            {
                return;
            }

            if (operation == Operation.Create)
            {
                DrawCreateModeSceneGUI(vertexDisplaySize, planeHeight);
            }
            else
            {
                DrawEditModeSceneGUI(vertexDisplaySize, planeHeight);
            }

            SceneView.RepaintAll();
        }

        private void DrawEditModeSceneGUI(float vertexDisplaySize, float planeHeight)
        {
            var e = Event.current;

            var worldPosition = Helper.GUIRayCastWithXZPlane(e.mousePosition, SceneView.currentDrawingSceneView.camera, planeHeight);

            if (e.button == 0 && e.type == EventType.MouseDown)
            {
                if (!e.alt)
                {
                    m_IsLeftButtonPressed = true;
                }

                if (!e.control)
                {
                    HitTest(worldPosition);
                }
                else
                {
                    if (!e.shift)
                    {
                        InsertVertex(worldPosition);
                    }
                    else
                    {
                        HitTest(worldPosition);
                        DeleteVertex();
                    }
                }
            }
            else if (e.type == EventType.MouseUp)
            {
                if (e.button == 0)
                {
                    m_IsLeftButtonPressed = false;
                }

                m_MovementUpdater.Reset();
            }

            if (!e.control && m_IsLeftButtonPressed)
            {
                var startMoving = e.type == EventType.MouseDown;
                if (!e.shift)
                {
                    MoveVertex(startMoving, worldPosition);
                }
                else
                {
                    MoveShape(startMoving, worldPosition);
                }
            }

            HandleUtility.AddDefaultControl(0);
        }

        private void DrawCreateModeSceneGUI(float displaySize, float planeHeight)
        {
            var e = Event.current;

            var worldPosition = Helper.GUIRayCastWithXZPlane(e.mousePosition, SceneView.currentDrawingSceneView.camera, planeHeight);

            worldPosition = SnapVertexWorld(worldPosition);

            if (!e.alt && e.button == 0 && e.type == EventType.MouseDown)
            {
                m_IsLeftButtonPressed = true;
            }
            else if (e.button == 1 && (e.type == EventType.MouseDown || e.type == EventType.Used))
            {
                CreateShape();
            }
            else if (e.type == EventType.MouseUp)
            {
                m_MovementUpdater.Reset();

                if (e.button == 0)
                {
                    m_IsValidCreateState = true;
                    m_IsLeftButtonPressed = false;
                }
            }
            else if (e.type == EventType.MouseMove)
            {
                if (m_VertexOfCreatingShape.Count > 0)
                {
                    m_GizmoVertices[^1] = worldPosition;
                    m_VertexOfCreatingShape[^1] = worldPosition;
                }
            }

            if (m_IsValidCreateState && m_IsLeftButtonPressed)
            {
                m_VertexOfCreatingShape.Add(worldPosition);
                if (m_VertexOfCreatingShape.Count == 1)
                {
                    m_VertexOfCreatingShape.Add(worldPosition);
                }

                m_GizmoVertices = m_VertexOfCreatingShape.ToArray();

                m_IsValidCreateState = false;
            }

            DrawGizmo(displaySize);

            HandleUtility.AddDefaultControl(0);
        }

        private void DrawGizmo(float displaySize)
        {
            Handles.color = new Color32(240, 155, 89, 255);

            Handles.DrawPolyLine(m_GizmoVertices);
            for (var i = 0; i < m_GizmoVertices.Length; ++i)
            {
                Handles.SphereHandleCap(0, m_GizmoVertices[i], Quaternion.identity, displaySize, EventType.Repaint);
            }

            Handles.color = Color.white;
        }

        private readonly IMover m_MovementUpdater = IMover.Create();
        private bool m_IsLeftButtonPressed;
        private bool m_IsValidCreateState = true;
        private List<ShapePickInfo> m_PickedShapes = new();
        private readonly List<Vector3> m_VertexOfCreatingShape = new();
        private Vector3[] m_GizmoVertices = new Vector3[0];
        private readonly List<Vector3> m_TempContainer = new();

        private class ShapePickInfo
        {
            public ShapePickInfo(int id, int index)
            {
                ShapeID = id;
                VertexIndex = index;
            }

            public int ShapeID;
            public int VertexIndex;
        }
    }
}