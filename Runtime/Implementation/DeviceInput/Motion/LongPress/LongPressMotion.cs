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
    internal class LongPressMotion : Motion, ILongPressMotion
    {
        public float Duration => m_Duration;
        public Vector2 Start => m_Start;
        public override MotionType Type => MotionType.LongPress;

        public LongPressMotion(int id, float duration, float moveThreshold, IDeviceInput device, DeviceTouchType touchType)
            : base(id, device, touchType)
        {
            m_Duration = duration;
            m_MoveThreshold = moveThreshold;
        }

        protected override void OnReset()
        {
            m_Timer = 0;
            m_Start = Vector2.zero;
            m_TouchStartCaptured = false;
        }

        protected override bool Match()
        {
            var touchCount = GetTouchCount();
            if (touchCount != 1)
            {
                Reset(touchCount == 0);
                return false;
            }

            var touch = GetTouch(0);
            if (touch.State == TouchState.Start)
            {
                m_Start = touch.Current;
                m_TouchStartCaptured = true;
            }

            if (m_TouchStartCaptured)
            {
                if (touch.State == TouchState.Touching)
                {
                    m_Timer += Time.deltaTime;
                    if (m_Timer >= m_Duration)
                    {
                        if ((touch.Current - m_Start).sqrMagnitude <= m_MoveThreshold * m_MoveThreshold)
                        {
                            m_TouchStartCaptured = false;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private Vector2 m_Start;
        private float m_Duration = 0;
        private float m_Timer = 0;
        private float m_MoveThreshold;
        private bool m_TouchStartCaptured = false;
    }
}

//XDay