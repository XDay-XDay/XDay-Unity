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

namespace XDay.WorldAPI.Shape.Editor
{
    internal class ShapeRenderer
    {
        public GameObject Root => m_Root;

        public ShapeRenderer(ShapeObject shape, Transform parent)
        {
            m_ShapeObject = shape;
            m_Root = new GameObject(shape.Name);
            m_Root.AddOrGetComponent<NoKeyDeletion>();

            var transform = m_Root.transform;
            transform.localScale = shape.Scale;
            transform.SetLocalPositionAndRotation(shape.Position, shape.Rotation);
            transform.SetParent(parent, false);

            var listener = m_Root.AddOrGetComponent<ShapeObjectBehaviour>();
            listener.Init(shape.ID, (objectID) => {
                m_Dirty = true;
            });
        }

        public void OnDestroy()
        {
            Helper.DestroyUnityObject(m_Root);
        }

        public void SetDirty()
        {
            m_Dirty = true;
        }

        public void SetActive(bool active)
        {
            m_Root.SetActive(active);
        }

        public void Draw(bool isEditing)
        {
            if (!m_ShapeObject.IsActive)
            {
                return;
            }

            if (m_ShapeObject == null)
            {
                return;
            }

            if (m_Dirty)
            {
                m_Dirty = false;

                m_GizmoVertices.Clear();
                var vertexCount = m_ShapeObject.VertexCount;
                for (var i = 0; i < vertexCount; ++i)
                {
                    m_GizmoVertices.Add(m_ShapeObject.TransformToWorldPosition(m_ShapeObject.GetVertexPosition(i)));
                }
            }

            Handles.color = m_ShapeObject.RenderColor;

            //draw outline
            for (var i = 0; i < m_GizmoVertices.Count; ++i)
            {
                Handles.DrawLine(m_GizmoVertices[i], m_GizmoVertices[(i + 1) % m_GizmoVertices.Count]);
            }

            if (isEditing)
            {
                for (var i = 0; i < m_GizmoVertices.Count; ++i)
                {
                    Handles.SphereHandleCap(0, m_GizmoVertices[i], Quaternion.identity, m_ShapeObject.VertexDisplaySize, EventType.Repaint);

                    if (m_ShapeObject.ShowVertexIndex)
                    {
                        var color = Handles.color;
                        GUI.skin.label.normal.textColor = Color.red;
                        GUI.skin.label.fontSize = 20;
                        Handles.Label(m_GizmoVertices[i], i.ToString());
                        GUI.skin.label.normal.textColor = color;
                    }
                }
            }
        }

        private GameObject m_Root;
        private readonly ShapeObject m_ShapeObject;
        private readonly List<Vector3> m_GizmoVertices = new();
        private bool m_Dirty = true;
    }
}
