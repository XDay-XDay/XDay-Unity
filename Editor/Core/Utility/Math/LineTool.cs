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

using System;
using UnityEditor;
using UnityEngine;

namespace XDay.UtilityAPI.Editor
{
    public class LineTool
    {
        public void Reset()
        {
            m_State = State.Idle;
            m_Start = Vector3.zero;
            m_End = Vector3.zero;
        }

        public void SceneUpdate(Color color, Action<Vector3, Vector3> onCreate, float y = 0)
        {
            var evt = Event.current;
            if (evt.alt)
            {
                return;
            }

            var pos = Helper.GUIRayCastWithXZPlane(evt.mousePosition, SceneView.currentDrawingSceneView.camera, y);

            if (evt.type == EventType.MouseDown && 
                evt.button == 0)
            {
                if (m_State == State.Idle)
                {
                    m_Start = pos;
                    m_State = State.Clicked;
                }
                else
                {
                    onCreate(m_Start, pos);
                    Reset();
                }
            }
            else if (evt.type == EventType.MouseMove)
            {
                m_End = pos;
            }
            else if (evt.type == EventType.MouseUp && evt.button == 1)
            {
                Reset();
            }

            DrawLine(color);
        }

        private void DrawLine(Color color)
        {
            if (m_State == State.Clicked)
            {
                var oldColor = Handles.color;
                Handles.color = color;
                Handles.DrawLine(m_Start, m_End);
                Handles.color = oldColor;
            }
        }

        private enum State
        {
            Idle,
            Clicked,
        }

        private State m_State = State.Idle;
        private Vector3 m_Start;
        private Vector3 m_End;
    }
}


//XDay