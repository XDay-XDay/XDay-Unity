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
    internal partial class PinchMotion
    {
        class Zoom : Phase
        {
            public float ScrollRate => m_LastZoomRate;
            public Vector2 ZoomCenter => m_ZoomCenter;

            public Zoom(PinchMotion motion) 
                : base(motion)
            {
            }

            public override void OnActivate(ITouch touch0, ITouch touch1)
            {
                var distance = Vector2.Distance(touch0.Current, touch1.Current);
                m_StartDistance = distance;
                m_LastDistance = distance;
                m_ZoomCenter = Vector2.zero;
                m_LastZoomRate = 1;
                m_StartDirection = Vector2.zero;
            }

            public override void OnDeactivate()
            {
                m_StartDirection = Vector2.zero;
            }

            public override bool Match(ITouch touch0, ITouch touch1)
            {
                var isFinished = (touch0.State == TouchState.Finish || touch1.State == TouchState.Finish);
                var isTouching = (touch0.State == TouchState.Touching || touch1.State == TouchState.Touching);

                var match = false;
                var curDistance = Vector2.Distance(touch0.Current, touch1.Current);
                var movedDistance = m_LastDistance - curDistance;

                if (isTouching &&
                    !isFinished &&
                    m_StartDistance > 0 &&
                    m_LastDistance > 0 &&
                    Mathf.Abs(movedDistance) > m_MoveThreshold)
                {
                    match = true;
                    m_ZoomCenter = (touch0.Current + touch1.Current) * 0.5f;
                    m_LastZoomRate = m_StartDistance / m_LastDistance;
                    m_LastDistance = curDistance;
                }

                if (m_StartDirection == Vector2.zero)
                {
                    m_StartDirection = touch0.Current - touch1.Current;
                    m_StartDirection.Normalize();
                }

                m_Motion.CheckPhaseChange(touch0, touch1, m_StartDirection);

                return match;
            }

            private Vector2 m_ZoomCenter;
            private float m_StartDistance = 0;
            private Vector2 m_StartDirection;
            private float m_MoveThreshold = 10.0f;
            private float m_LastZoomRate;
            private float m_LastDistance = 0;
        }
    }
}

//XDay