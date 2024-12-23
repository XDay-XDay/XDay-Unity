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



using UnityEditor;
using UnityEngine;

namespace XDay.WorldAPI.Editor
{
    internal class GizmoCubeIndicator : IGizmoCubeIndicator
    {
        public bool Visible { get => m_Visible; set => m_Visible = value; }
        public Quaternion Rotation { get => m_Rotation; set => m_Rotation = value; }
        public Vector3 Position { get => m_Position; set => m_Position = value; }
        public float Size { get => m_Size; set => m_Size = value; }

        public void Draw(Color color, bool matchCenter)
        {
            if (Visible)
            {
                var oldColor = Handles.color;
                Handles.color = color;

                Vector3 position;
                if (matchCenter)
                {
                    position = m_Position;
                }
                else
                {
                    var offset = new Vector3(m_Size, 0, m_Size) * 0.5f;
                    position = m_Position + m_Rotation * offset;
                }

                Handles.matrix = Matrix4x4.TRS(position, m_Rotation, new Vector3(m_Size, 0, m_Size));
                Handles.DrawWireCube(Vector3.zero, Vector3.one);
                Handles.color = oldColor;
            }
        }

        private Vector3 m_Position;
        private Quaternion m_Rotation;
        private float m_Size;
        private bool m_Visible;
    }
}


//XDay