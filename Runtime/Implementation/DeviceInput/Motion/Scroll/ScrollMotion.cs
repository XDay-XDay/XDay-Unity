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
using UnityEngine;
using UnityEngine.Pool;

namespace XDay.InputAPI
{
    internal class ScrollMotion : Motion, IScrollMotion
    {
        public Vector2 MoveDirection => m_Direction;
        public float MoveDistance => m_Distance;
        public override MotionType Type => MotionType.MouseScroll;

        public ScrollMotion(int id, float maxAngle, float interval, TouchID touchID, IDeviceInput device)
            : base(id, device)
        {
            m_MaxAngle = maxAngle;
            m_Interval = interval;
            m_TouchID = touchID;
            m_Pool = new ObjectPool<TouchData>(() => { return new TouchData(); }, null, null, null, true, 100);
        }

        protected override void OnReset()
        {
            m_TouchTime = 0;
            m_Distance = 0;
            m_Direction = Vector2.zero;
            m_TouchStartCaptured = false;
            m_MovedDistance = 0;

            ClearTouches();
        }

        protected override bool Match()
        {
            var touchCount = m_Device.UITouchCount;
            if (touchCount != 1)
            {
                Reset(touchCount == 0);
                return false;
            }

            var touch = m_Device.GetUITouch(0);
            if (m_Device.QueryTouchID(touch.ID) != m_TouchID)
            {
                Reset(false);
                return false;
            }

            if (touch.State == TouchState.Start)
            {
                m_TouchStartCaptured = true;
                ClearTouches();
            }

            if (m_TouchStartCaptured)
            {
                var match = false;
                if (touch.State == TouchState.Touching)
                {
                    OnTouching(touch);
                }
                else if (touch.State == TouchState.Finish)
                {
                    match = OnFinish();
                }

                return match;
            }

            return false;
        }

        private bool OnFinish()
        {
            var match = false;
            var currentTime = Time.time;
            while (m_TouchData.Count > 0)
            {
                if (m_TouchData[0].Time >= currentTime - m_Interval)
                {
                    break;
                }

                var data = m_TouchData[0];
                m_Pool.Release(data);
                m_TouchData.RemoveAt(0);
            }

            m_TouchTime = 0;
            m_MovedDistance = 0;
            if (m_TouchData.Count > 1)
            {
                match = true;
                var dir = Vector2.zero;
                m_TouchTime = currentTime - m_TouchData[0].Time;
                for (var i = 0; i < m_TouchData.Count; i++)
                {
                    if (i != 0)
                    {
                        var curDir = m_TouchData[i].Position - m_TouchData[i - 1].Position;
                        if (Vector2.Angle(curDir, dir) > m_MaxAngle)
                        {
                            match = false;
                            break;
                        }
                        dir = curDir;
                    }
                    m_MovedDistance += m_TouchData[i].Movement;
                }

                m_Direction = (m_TouchData[^1].Position - m_TouchData[0].Position);
                m_Distance = m_Direction.magnitude;
                m_Direction /= m_Distance;
            }

            return match;
        }

        private void OnTouching(ITouch touch)
        {
            var delta = touch.Current - touch.Previous;
            if (Mathf.Abs(delta.x) >= m_MoveThreshold ||
                Mathf.Abs(delta.y) >= m_MoveThreshold)
            {
                var touchData = m_Pool.Get();
                touchData.Movement = Vector2.Distance(touch.Current, touch.Previous);
                touchData.Position = touch.Current;
                touchData.Time = Time.time;
                m_TouchData.Add(touchData);
            }
        }

        private void ClearTouches()
        {
            for (var i = 0; i < m_TouchData.Count; ++i)
            {
                m_Pool.Release(m_TouchData[i]);
            }
            m_TouchData.Clear();
        }

        private TouchID m_TouchID;
        private float m_TouchTime = 0;
        private float m_Distance;
        private float m_MaxAngle;
        private float m_MovedDistance = 0;
        private float m_Interval;
        private Vector2 m_Direction;
        private float m_MoveThreshold = 5.0f;
        private List<TouchData> m_TouchData = new(100);
        private ObjectPool<TouchData> m_Pool;
        private bool m_TouchStartCaptured = false;
    }
}

//XDay