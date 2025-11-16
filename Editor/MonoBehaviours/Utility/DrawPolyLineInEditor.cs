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

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace XDay.UtilityAPI
{
    public class DrawPolyLineInEditor : MonoBehaviour
    {
        public float Radius = 1.0f;
        public bool DrawVertex = true;
        public bool DrawIndex = true;
        public Color Color = Color.white;

        private void OnDrawGizmos()
        {
            if (m_TextStyle == null)
            {
                m_TextStyle = new GUIStyle(GUI.skin.label);
                m_TextStyle.normal.textColor = Color.blue;
            }

            if (m_Lines != null && m_Lines.Length > 1)
            {
                Color oldColor = Handles.color;
                Handles.color = Color;
                Handles.DrawLines(m_Lines);

                if (DrawVertex)
                {
                    Handles.color = Color.red;
                    Handles.SphereHandleCap(0, m_Lines[0], Quaternion.identity, Radius, EventType.Repaint);

                    Handles.color = Color.green;
                    Handles.SphereHandleCap(0, m_Lines[^1], Quaternion.identity, Radius, EventType.Repaint);

                    for (int i = 1; i < m_Lines.Length - 1; ++i)
                    {
                        Handles.color = Color.white;
                        Handles.SphereHandleCap(0, m_Lines[i], Quaternion.identity, Radius, EventType.Repaint);
                    }
                }

                if (DrawIndex && m_Vertices != null)
                {
                    for (var i = 0; i < m_Vertices.Length; ++i)
                    {
                        Handles.Label(m_Vertices[i], $"{i}", m_TextStyle);
                    }
                }

                Handles.color = oldColor;
            }
        }

        public void SetVertices(List<Vector2> vertices)
        {
            m_Vertices = new Vector3[vertices.Count];
            for (var i = 0; i < m_Vertices.Length; ++i)
            {
                m_Vertices[i] = Helper.ToVector3XZ(vertices[i]);
            }

            int segment = vertices.Count - 1;
            int vertexCount = segment * 2;
            m_Lines = new Vector3[vertexCount];
            for (int i = 0; i < segment; ++i)
            {
                m_Lines[i * 2] = new Vector3(vertices[i].x, 0, vertices[i].y);
                m_Lines[i * 2 + 1] = new Vector3(vertices[i + 1].x, 0, vertices[i + 1].y);
            }
        }

        public void SetVertices(List<Vector3> vertices)
        {
            if (vertices.Count > 1)
            {
                m_Vertices = new Vector3[vertices.Count];
                for (var i = 0; i < m_Vertices.Length; ++i)
                {
                    m_Vertices[i] = vertices[i];
                }

                int segment = vertices.Count - 1;
                int vertexCount = segment * 2;
                m_Lines = new Vector3[vertexCount];
                for (int i = 0; i < segment; ++i)
                {
                    m_Lines[i * 2] = vertices[i];
                    m_Lines[i * 2 + 1] = vertices[i + 1];
                }
            }
        }

        private Vector3[] m_Lines;
        private Vector3[] m_Vertices;
        private GUIStyle m_TextStyle;
    }
}

#endif