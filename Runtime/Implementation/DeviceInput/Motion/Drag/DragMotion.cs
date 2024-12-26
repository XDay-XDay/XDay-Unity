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

using UnityEngine;

namespace XDay.InputAPI
{
    internal class DragMotion : Motion, IDragMotion
    {
        public Vector2 Start => m_Start;
        public Vector2 Previous => m_Previous;
        public Vector2 Current => m_Current;
        public override MotionType Type => MotionType.Drag;

        public DragMotion(int id, TouchID touchID, float moveThreshold, IDeviceInput device)
            : base(id, device)
        {
            m_TouchID = touchID;
            m_MoveThreshold = moveThreshold;
        }

        protected override void OnReset()
        {
            m_Start = Vector2.zero;
            m_Current = Vector2.zero;
            m_Previous = Vector2.zero;
            m_TouchStartCaptured = false;
        }

        protected override bool Match()
        {
            var touchCount = m_Device.TouchCountNotStartFromUI;
            if (touchCount != 1)
            {
                Reset(touchCount == 0);
                return false;
            }

            var touch = m_Device.GetTouchNotStartFromUI(0);
            if (m_Device.QueryTouchID(touch.ID) != m_TouchID)
            {
                Reset(false);
                return false;
            }

            if (touch.State == TouchState.Start)
            {
                m_Start = touch.Current;
                m_Current = touch.Current;
                m_Previous = touch.Current;
                m_TouchStartCaptured = true;
            }

            if (m_TouchStartCaptured)
            {
                if (touch.State == TouchState.Touching ||
                    touch.State == TouchState.Finish)
                {
                    m_Previous = m_Current;
                    m_Current = touch.Current;
                    if ((touch.Current - touch.Previous).sqrMagnitude > m_MoveThreshold * m_MoveThreshold)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private Vector2 m_Current;
        private TouchID m_TouchID;
        private Vector2 m_Start;
        private float m_MoveThreshold;
        private Vector2 m_Previous;
        private bool m_TouchStartCaptured = false;
    }
}

//XDay
