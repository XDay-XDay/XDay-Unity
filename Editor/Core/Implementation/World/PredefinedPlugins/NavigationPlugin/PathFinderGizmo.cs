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

using UnityEditor;
using UnityEngine;

namespace XDay.WorldAPI.Navigation.Editor
{
    internal class PathFinderGizmo
    {
        public Vector3 Start => m_Start;
        public Vector3 End => m_End;

        public bool Set(Vector3 position)
        {
            if (m_Step == 0)
            {
                SetStart(position);
            }
            else if (m_Step == 1)
            {
                SetEnd(position);
                return true;
            }
            else
            {
                Reset();
            }
            return false;
        }

        public void Reset()
        {
            m_Step = 0;
            m_Start = Vector3.zero;
            m_End = Vector3.zero;
        }

        public void Draw(float size)
        {
            if (m_Step != 0)
            {
                var color = Handles.color;
                Handles.color = Color.red;
                Handles.SphereHandleCap(0, m_Start, Quaternion.identity, size, EventType.Repaint);
                Handles.color = Color.green;
                Handles.SphereHandleCap(0, m_End, Quaternion.identity, size, EventType.Repaint);
                Handles.color = color;
            }
        }

        private void SetStart(Vector3 position)
        {
            m_Start = position;
            m_Step = 1;
        }

        private void SetEnd(Vector3 position)
        {
            m_End = position;
            m_Step = 2;
        }

        private int m_Step = 0;
        private Vector3 m_Start;
        private Vector3 m_End;
    }
}
