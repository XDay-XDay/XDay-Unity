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

namespace XDay.UtilityAPI.Shape.Editor
{
    /// <summary>
    /// 编辑Shape
    /// </summary>
    public class ShapeBuilder
    {
        public class ShapeBuilderCreateInfo
        {
            public DelegateOnCreateShape CreateShape { get; set; }
            public DelegateSnapVertexLocal SnapVertexLocal { get; set; }
            public DelegateSnapVertexWorld SnapVertexWorld { get; set; }
            public DelegateGetVertexLocalPosition GetVertexLocalPosition { get; set; }
            public DelegateGetVertexCount GetVertexCount { get; set; }
            public DelegateRepaintInspector RepaintInspector { get; set; }
            //null时表示使用世界坐标
            public DelegateConvertWorldToLocal ConvertWorldToLocal { get; set; }
            public DelegateMoveVertex MoveVertex { get; set; }
            public DelegateMoveShape MoveShape { get; set; }
            public DelegateInsertVertex InsertVertex { get; set; }
            public DelegateDeleteVertex DeleteVertex { get; set; }
            public DelegatePickShape PickShape { get; set; }
        }

        public enum Operation
        {
            Create,
            Edit,
        }

        public bool IsCreatingShape => m_VertexOfCreatingShape.Count > 0;
        public List<int> PickedVertexIndices
        { 
            get => m_PickedVertexIndices;
        }

        public ShapeBuilder(ShapeBuilderCreateInfo createInfo)
        {
            m_OnCreateShape = createInfo.CreateShape;
            m_SnapVertexLocal = createInfo.SnapVertexLocal;
            m_SnapVertexWorld = createInfo.SnapVertexWorld;
            m_RepaintInspector = createInfo.RepaintInspector;
            m_WorldToLocal = createInfo.ConvertWorldToLocal;
            m_WorldToLocal ??= (Vector3 pos) => { return pos; };
            m_GetVertexLocalPosition = createInfo.GetVertexLocalPosition;
            m_GetVertexCount = createInfo.GetVertexCount;
            m_DeleteVertex = createInfo.DeleteVertex;
            m_InsertVertex = createInfo.InsertVertex;
            m_MoveVertex = createInfo.MoveVertex;
            m_MoveShape = createInfo.MoveShape;
            m_PickShape = createInfo.PickShape;
        }

        public void Clear()
        {
            m_IsValidCreateState = true;
            m_GizmoVertices = new Vector3[0];
            m_VertexOfCreatingShape.Clear();
            m_PickedVertexIndices.Clear();
        }

        public void DrawSceneGUI(float vertexDisplaySize, float planeHeight, Operation operation)
        {
            if (operation == Operation.Create)
            {
                DrawCreateModeSceneGUI(vertexDisplaySize, planeHeight);
            }
            else
            {
                DrawEditModeSceneGUI(vertexDisplaySize, planeHeight);
            }

            m_RepaintInspector?.Invoke();
            SceneView.RepaintAll();
        }

        #region Shape Operation
        private void CreateShape()
        {
            if (m_VertexOfCreatingShape.Count > 3)
            {
                m_VertexOfCreatingShape.RemoveAt(m_VertexOfCreatingShape.Count - 1);
                m_OnCreateShape.Invoke(m_VertexOfCreatingShape);
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
                m_MoveShape?.Invoke(startMoving, offset);
            }
        }
        #endregion

        #region Vertex Operation
        private void VertexHitTest(Vector3 worldPosition, float boxColliderSize)
        {
            m_PickedVertexIndices.Clear();
            if (m_PickShape != null)
            {
                bool picked = m_PickShape.Invoke(worldPosition);
                if (!picked)
                {
                    return;
                }
            }
            var localPosition = m_WorldToLocal(worldPosition);
            var vertexCount = m_GetVertexCount.Invoke();
            for (var i = 0; i < vertexCount; ++i)
            {
                if (IsHitOneVertex(localPosition, m_GetVertexLocalPosition.Invoke(i), boxColliderSize))
                {
                    m_PickedVertexIndices.Add(i);
                }
            }
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
            if (m_PickedVertexIndices.Count > 0)
            {
                foreach (var vertexIndex in m_PickedVertexIndices)
                {
                    var localPosition = m_WorldToLocal.Invoke(worldPosition);
                    m_MovementUpdater.Update(localPosition.ToVector2());

                    var movedOffset = m_MovementUpdater.GetMovement().ToVector3XZ();
                    var originalPosition = m_GetVertexLocalPosition(vertexIndex);
                    var newPosition = movedOffset + originalPosition;

                    if (m_SnapVertexLocal != null)
                    {
                        newPosition = m_SnapVertexLocal.Invoke(newPosition);
                    }

                    if (startMoving || newPosition != originalPosition)
                    {
                        m_MoveVertex?.Invoke(startMoving, vertexIndex, movedOffset);
                    }
                }
            }
        }

        private void InsertVertex(Vector3 worldPosition)
        {
            m_TempContainer.Clear();
            var vertexCount = m_GetVertexCount();
            if (vertexCount == 0)
            {
                return;
            }
            for (var i = 0; i < vertexCount; ++i)
            {
                m_TempContainer.Add(m_GetVertexLocalPosition(i));
            }

            var localPosition = m_WorldToLocal(worldPosition);
            if (m_InsertVertex != null)
            {
                var index = Helper.FindClosestEdgeOnProjection(localPosition, m_TempContainer);
                if (m_InsertVertex(index, localPosition))
                {
                    m_PickedVertexIndices.Clear();
                    m_PickedVertexIndices.Add(index);
                }
            }
        }

        private void DeleteVertex()
        {
            if (m_PickedVertexIndices.Count > 0)
            {
                if (m_GetVertexCount.Invoke() > 3)
                {
                    if (m_DeleteVertex != null)
                    {
                        for (var i = m_PickedVertexIndices.Count - 1; i >= 0; --i)
                        {
                            if (m_DeleteVertex.Invoke(m_PickedVertexIndices[i]))
                            {
                                m_PickedVertexIndices.RemoveAt(i);
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Draw GUI
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
                    VertexHitTest(worldPosition, vertexDisplaySize);
                }
                else
                {
                    if (!e.shift)
                    {
                        InsertVertex(worldPosition);
                    }
                    else
                    {
                        VertexHitTest(worldPosition, vertexDisplaySize);
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
                if (m_SnapVertexWorld != null)
                {
                    worldPosition = m_SnapVertexWorld(worldPosition);
                }

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
#endregion

        private readonly IMover m_MovementUpdater = IMover.Create();
        private bool m_IsLeftButtonPressed;
        private bool m_IsValidCreateState = true;
        private List<int> m_PickedVertexIndices = new();
        private readonly List<Vector3> m_VertexOfCreatingShape = new();
        private Vector3[] m_GizmoVertices = new Vector3[0];
        private readonly List<Vector3> m_TempContainer = new();
        private readonly DelegateSnapVertexLocal m_SnapVertexLocal;
        private readonly DelegateSnapVertexWorld m_SnapVertexWorld;
        private readonly DelegateRepaintInspector m_RepaintInspector;
        private readonly DelegateConvertWorldToLocal m_WorldToLocal;
        private readonly DelegateOnCreateShape m_OnCreateShape;
        private readonly DelegateGetVertexLocalPosition m_GetVertexLocalPosition;
        private readonly DelegateGetVertexCount m_GetVertexCount;
        private readonly DelegateMoveVertex m_MoveVertex;
        private readonly DelegateMoveShape m_MoveShape;
        private readonly DelegateInsertVertex m_InsertVertex;
        private readonly DelegateDeleteVertex m_DeleteVertex;
        private readonly DelegatePickShape m_PickShape;
    }

    public delegate void DelegateOnCreateShape(List<Vector3> vertices);
    public delegate void DelegateMoveShape(bool startMoving, Vector3 moveOffset);
    public delegate void DelegateMoveVertex(bool startMoving, int index, Vector3 moveOffset);
    public delegate bool DelegateInsertVertex(int index, Vector3 localPosition);
    public delegate bool DelegateDeleteVertex(int index);
    public delegate Vector3 DelegateSnapVertexLocal(Vector3 localPosition);
    public delegate Vector3 DelegateSnapVertexWorld(Vector3 worldPosition);
    public delegate Vector3 DelegateGetVertexLocalPosition(int index);
    public delegate int DelegateGetVertexCount();
    public delegate void DelegateRepaintInspector();
    public delegate Vector3 DelegateConvertWorldToLocal(Vector3 worldPosition);
    public delegate bool DelegatePickShape(Vector3 worldPosition);
}
