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

using XDay.UtilityAPI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace XDay.InputAPI
{
    internal class InertialDragMotion : Motion, IInertialDragMotion
    {
        public Vector3 SlideDirection => m_SlideDirection;
        public Vector3 DraggedOffset => m_DragOffset;
        public float DragTime => m_DragTime;
        public float MovedDistance => m_DragDistance;
        public int TouchCount => m_TrackedTouches.Count;
        public override MotionType Type => MotionType.InertialDrag;

        public InertialDragMotion(int id, TouchID touchID, Camera camera, IDeviceInput device)
            : base(id, device)
        {
            m_TouchID = touchID;
            m_Camera = camera;
            m_TouchPool = new ObjectPool<TouchData>(() => { return new TouchData(); }, null, null, null, true, 100);
        }

        protected override void OnReset()
        {
            m_Start = Vector3.zero;
            m_DragTime = 0;
            m_DragDistance = 0;
            m_DragOffset = Vector3.zero;
            m_SlideDirection = Vector3.zero;
            m_TouchStartCaptured = false;

            ClearTouches();
        }

        protected override bool Match()
        {
            if (m_Device.TouchCountNotStartFromUI != 1)
            {
                Reset(m_Device.TouchCountNotStartFromUI == 0);
                return false;
            }

            var touch = m_Device.GetTouchNotStartFromUI(0);
            if (m_Device.QueryTouchID(touch.ID) != m_TouchID)
            {
                Reset(false);
                return false;
            }

            var lastPos = Helper.RayCastWithXZPlane(touch.Previous, m_Camera);
            var curPos = Helper.RayCastWithXZPlane(touch.Current, m_Camera);
            m_DragOffset = lastPos - curPos;
            var distance = m_DragOffset.magnitude;
            if (touch.State == TouchState.Start)
            {
                ClearTouches();
                m_Start = curPos;
                m_TouchStartCaptured = true;
            }

            if (m_TouchStartCaptured)
            {
                if (touch.State == TouchState.Finish)
                {
                    var currentTime = Time.time;
                    while (m_TrackedTouches.Count > 0)
                    {
                        if (m_TrackedTouches[0].Time >= currentTime - m_MaxTimeInterval)
                        {
                            break;
                        }

                        var touchData = m_TrackedTouches[0];
                        m_TouchPool.Release(touchData);
                        m_TrackedTouches.RemoveAt(0);
                    }

                    m_DragTime = 0;
                    m_DragDistance = 0;
                    if (m_TrackedTouches.Count > 0)
                    {
                        m_DragTime = currentTime - m_TrackedTouches[0].Time;
                        for (var i = 0; i < m_TrackedTouches.Count; i++)
                        {
                            m_DragDistance += m_TrackedTouches[i].Movement;
                        }
                    }
                }
                else if (touch.State == TouchState.Touching)
                {
                    var delta = touch.Current - touch.Previous;
                    if (Mathf.Abs(delta.x) >= m_MoveThreshold ||
                        Mathf.Abs(delta.y) >= m_MoveThreshold)
                    {
                        m_SlideDirection = m_Start - curPos;
                        m_SlideDirection.Normalize();

                        var touchData = m_TouchPool.Get();
                        touchData.Movement = distance;
                        touchData.Position = touch.Current;
                        touchData.Time = Time.time;
                        m_TrackedTouches.Add(touchData);
                    }
                }

                return true;
            }
            return false;
        }

        private void ClearTouches()
        {
            for (var i = 0; i < m_TrackedTouches.Count; ++i)
            {
                m_TouchPool.Release(m_TrackedTouches[i]);
            }
            m_TrackedTouches.Clear();
        }

        private TouchID m_TouchID;
        private float m_DragTime = 0;
        private Vector3 m_DragOffset;
        private Vector3 m_SlideDirection;
        private float m_MoveThreshold = 5.0f;
        private Vector3 m_Start;
        private Camera m_Camera;
        private float m_DragDistance = 0;
        private bool m_TouchStartCaptured = false;
        private List<TouchData> m_TrackedTouches = new(90);
        private ObjectPool<TouchData> m_TouchPool;
        private float m_MaxTimeInterval = 0.1f;
    }
}

//XDay