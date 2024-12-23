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
    internal class DoubleClickMotion : Motion, IDoubleClickMotion
    {
        public Vector2 Start => m_Start;
        public override MotionType Type => MotionType.DoubleClick;

        public DoubleClickMotion(int id, float maxClickInterval, IDeviceInput device)
            : base(id, device)
        {
            m_MaxClickThreshold = maxClickInterval;
        }

        protected override void OnReset()
        {
            m_State = State.Idle;
            m_Start = Vector2.zero;
            m_ClickTime = 0;
            m_Timer = 0;
            m_TouchStartCaptured = false;
        }

        protected override bool Match()
        {
            var touchCount = m_Device.SceneTouchCount;
            if (touchCount != 1 && m_State == State.Idle)
            {
                Reset(touchCount == 0);
                return false;
            }

            if (touchCount == 1)
            {
                var touch = m_Device.GetSceneTouch(0);
                if (touch.State == TouchState.Start)
                {
                    if (m_State == State.Idle)
                    {
                        m_Start = touch.Current;
                        m_ClickTime = Time.time;
                        m_TouchStartCaptured = true;
                    }
                }

                if (m_TouchStartCaptured)
                {
                    if (touch.State == TouchState.Finish)
                    {
                        if (m_State == State.Idle)
                        {
                            m_State = State.ClickedOnce;
                        }
                        else if (m_State == State.ClickedOnce)
                        {
                            if (Time.time - m_ClickTime <= m_MaxClickThreshold &&
                                (m_Start - touch.Current).sqrMagnitude <= m_MoveThreshold * m_MoveThreshold)
                            {
                                return true;   
                            }
                            m_State = State.Idle;
                        }
                    }
                }
            }
            else
            {
                m_Timer += Time.deltaTime;
                if (m_Timer >= m_MaxClickThreshold)
                {
                    Reset(false);
                }
            }

            return false;
        }

        private enum State
        {
            Idle,
            ClickedOnce,
        }

        private float m_ClickTime = 0;
        private float m_MoveThreshold = 10.0f;
        private float m_Timer = 0;
        private float m_MaxClickThreshold = 0.25f;
        private bool m_TouchStartCaptured = false;
        private State m_State = State.Idle;
        private Vector2 m_Start;
    }
}


//XDay