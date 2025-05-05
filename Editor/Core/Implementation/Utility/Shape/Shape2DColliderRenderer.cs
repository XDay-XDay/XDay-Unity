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

namespace XDay.UtilityAPI.Shape.Editor
{
    class Shape2DColliderRenderer
    {
        public Shape2DColliderRenderer(Shape2DCollider collider)
        {
            m_Collider = collider;
        }

        public void SetDirty()
        {
            m_Dirty = true;
        }

        public bool Draw(bool isEditing)
        {
            if (m_Collider == null)
            {
                return false;
            }

            if (m_Dirty)
            {
                m_Dirty = false;

                m_GizmoVertices.Clear();
                var vertexCount = m_Collider.VertexCount;
                for (var i = 0; i < vertexCount; ++i)
                {
                    m_GizmoVertices.Add(m_Collider.TransformToWorldPosition(m_Collider.GetVertexPosition(i)));
                }
            }

            if (isEditing)
            {
                Handles.color = Color.green;
            }
            else
            {
                Handles.color = Color.white;
            }

            //draw outline
            for (var i = 0; i < m_GizmoVertices.Count; ++i)
            {
                Handles.DrawLine(m_GizmoVertices[i], m_GizmoVertices[(i + 1) % m_GizmoVertices.Count]);
            }

            if (isEditing)
            {
                for (var i = 0; i < m_GizmoVertices.Count; ++i)
                {
                    Handles.SphereHandleCap(0, m_GizmoVertices[i], Quaternion.identity, m_Collider.VertexDisplaySize, EventType.Repaint);

                    if (m_Collider.ShowVertexIndex)
                    {
                        var color = Handles.color;
                        GUI.skin.label.normal.textColor = Color.red;
                        Handles.Label(m_GizmoVertices[i], i.ToString());
                        GUI.skin.label.normal.textColor = color;
                    }
                }
            }

            return true;
        }

        bool m_Dirty = true;
        readonly Shape2DCollider m_Collider;
        readonly List<Vector3> m_GizmoVertices = new();
    }
}
