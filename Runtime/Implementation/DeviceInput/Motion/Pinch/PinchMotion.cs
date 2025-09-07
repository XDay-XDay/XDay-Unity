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
    internal partial class PinchMotion : Motion, IPinchMotion
    {
        public Vector2 Center => m_Zoom.ZoomCenter;
        public bool EnableRotate => m_EnableRotate;
        public float SlideDirection => m_Rotate.SlideDirection;
        public float DragDistance => m_Rotate.DraggedOffset;
        public bool IsRotating => m_CurrentPhase == m_Rotate;
        public float ZoomRate => m_Zoom.ScrollRate;
        public float MinAltitude => m_MinAltitude;
        public float MaxAltitude => m_MaxAltitude;
        public float Range => m_Range;
        public Camera Camera => m_Camera;
        public override MotionType Type => MotionType.Pinch;

        public PinchMotion(int id, float minAltitude, float maxAltitude, float range, Camera camera, bool enableRotate, IDeviceInput device, DeviceTouchType touchType) : base(id, device, touchType)
        {
            m_Idle = new(this);
            m_Rotate = new(this);
            m_Zoom = new(this);

            m_Camera = camera;
            m_MinAltitude = minAltitude;
            m_MaxAltitude = maxAltitude;
            m_Range = range;
            m_EnableRotate = enableRotate;

            ChangePhase(m_Idle);
        }

        protected override void OnReset()
        {
            m_Zoom.Reset();
            m_Rotate.Reset();
            ChangePhase(m_Idle);
        }

        protected override bool Match()
        {
            if (GetTouchCount() != 2)
            {
                Reset(true);
                return false;
            }

            var touch0 = GetTouch(0);
            var touch1 = GetTouch(1);

            return m_CurrentPhase.Match(touch0, touch1);
        }

        public void CheckPhaseChange(ITouch touch0, ITouch touch1, Vector2 direction)
        {
            var moved0 = IsTouchMoved(touch0, out var dir0);
            var moved1 = IsTouchMoved(touch1, out var dir1);
            if (moved0 || moved1)
            {
                var isZoom = true;
                if (moved0 && !IsDirectionColinear(direction, dir0, m_ZoomAngleThreshold))
                {
                    isZoom = false;   
                }
                if (moved1 && !IsDirectionColinear(direction, dir1, m_ZoomAngleThreshold))
                {
                    isZoom = false;
                }

                if (!isZoom)
                {
                    ChangePhase(m_Rotate, touch0, touch1);
                }
                else
                {
                    ChangePhase(m_Zoom, touch0, touch1);
                }
            }
        }

        private bool IsTouchMoved(ITouch touch, out Vector2 delta)
        {
            delta = touch.Current - touch.Previous;
            var valid = false;
            if (delta.magnitude >= m_MoveThreshold)
            {
                valid = true;
            }
            delta.Normalize();
            return valid;
        }

        private bool IsDirectionColinear(Vector2 dir0, Vector2 dir1, float threshold)
        {
            var angle = Vector2.Angle(dir0, dir1);
            if (angle >= 90)
            {
                angle = 180 - angle;
            }
            return angle <= threshold;
        }

        private void ChangePhase(Phase phase, ITouch touch0 = null, ITouch touch1 = null)
        {
            if (m_CurrentPhase != phase)
            {
                m_CurrentPhase?.OnDeactivate();
                m_CurrentPhase = phase;
                m_CurrentPhase?.OnActivate(touch0, touch1);
            }
        }

        private Phase m_CurrentPhase;
        private Idle m_Idle;
        private Rotate m_Rotate;
        private Zoom m_Zoom;
        private bool m_EnableRotate = true;
        private float m_MoveThreshold = 3.0f;
        private float m_ZoomAngleThreshold = 45.0f;
        private float m_Range;
        private float m_MinAltitude;
        private float m_MaxAltitude;
        private Camera m_Camera;
    }
}

//XDay
