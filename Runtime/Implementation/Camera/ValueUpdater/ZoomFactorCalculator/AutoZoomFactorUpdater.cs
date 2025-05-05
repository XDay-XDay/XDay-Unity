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

namespace XDay.CameraAPI
{
    internal class AutoZoomFactorUpdater : ZoomFactorUpdaterBase
    {
        public AutoZoomFactorUpdater(CameraManipulator manipulator)
            : base(manipulator)
        {
        }

        public void Reset()
        {
            m_StartTime = 0;
            m_EndTime = 0;
            m_StartZoomFactor = 0;
            m_EndZoomFactor = 0;
        }

        public void Start(float targetVDF, float duration)
        {   
            m_StartZoomFactor = m_Manipulator.ZoomFactor;
            m_EndZoomFactor = targetVDF;
            m_StartTime = Time.realtimeSinceStartup;
            m_EndTime = m_StartTime + duration;
        }

        public bool Update(AnimationCurve speedCurve, ref Vector3 newPos)
        {
            var setup = m_Manipulator.Setup;
            var camera = m_Manipulator.Camera;

            var currentTime = Time.realtimeSinceStartup;
            if (currentTime >= m_EndTime)
            {
                if (setup.DecomposeZoomFactor(m_EndZoomFactor, out var fl, out _))
                {
                    newPos = PositionFromFocusPoint(camera, fl, m_Manipulator.FocusPoint);
                }
                return true;
            }
            
            var t = (currentTime - m_StartTime) / (m_EndTime - m_StartTime);
            if (speedCurve != null)
            {
                t = Mathf.Clamp01(speedCurve.Evaluate(t));
            }

            var zoomFactor = m_StartZoomFactor + (m_EndZoomFactor - m_StartZoomFactor) * t;
            if (setup.DecomposeZoomFactor(zoomFactor, out var focalLength, out _))
            {
                var focusPoint = m_Manipulator.FocusPoint;
                if (m_Manipulator.Direction == CameraDirection.XZ)
                {
                    newPos = PositionFromFocusPoint(camera, focalLength, new Vector3(focusPoint.x, 0, focusPoint.z));
                }
                else
                {
                    newPos = PositionFromFocusPoint(camera, focalLength, new Vector3(focusPoint.x, focusPoint.y, 0));
                }
            }
            
            return false;
        }

        private float m_StartTime;
        private float m_EndTime;
        private float m_StartZoomFactor;
        private float m_EndZoomFactor;
    }
}

//XDay
