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

using UnityEngine;

namespace XDay.InputAPI
{
    internal class ClickMotion : Motion, IClickMotion
    {
        public Vector2 Start => m_Start;
        public override MotionType Type => MotionType.Click;

        public ClickMotion(int id, IDeviceInput device)
            : base(id, device)
        {
        }

        protected override void OnReset()
        {
            m_Start = Vector2.zero;
        }

        protected override bool Match()
        {
            var touchCount = m_Device.SceneTouchCount;
            if (touchCount != 1)
            {
                Reset(touchCount == 0);
                return false;
            }

            var touch = m_Device.GetSceneTouch(0);
            if (touch.State == TouchState.Start)
            {
                m_Start = touch.Current;
            }

            if (touch.State == TouchState.Finish &&
                (m_Start - touch.Current).sqrMagnitude <= m_MovingThreshold * m_MovingThreshold)
            {
                return true;   
            }

            return false;
        }

        private Vector2 m_Start;
        private float m_MovingThreshold = 5;
    }
}

//XDay