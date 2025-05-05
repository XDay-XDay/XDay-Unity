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
    internal class ZoomFactorUpdater : ZoomFactorUpdaterBase
    {
        public ZoomFactorUpdater(CameraManipulator manipulator) 
            : base(manipulator)
        { 
        }

        public void Start()
        {
            m_StartZoomFactor = m_Manipulator.ZoomFactor;
            m_CurrentZoomFactor = m_StartZoomFactor;
        }

        public void AbsoluteZoom(float centerX, float centerY, float rate, ref Vector3 newCameraPos)
        {
            if (m_Manipulator.Setup.DecomposeZoomFactor(m_StartZoomFactor * rate, out var focalLength, out _))
            {
                newCameraPos = CalculateCameraPos(m_Manipulator.Camera, focalLength, centerX, centerY, m_Manipulator.Direction);
            }
        }

        public void DeltaZoom(float centerX, float centerY, float deltaRate, ref Vector3 newCameraPos)
        {
            m_CurrentZoomFactor += deltaRate;
            if (m_Manipulator.Setup.DecomposeZoomFactor(m_CurrentZoomFactor, out var focalLength, out _))
            {
                newCameraPos = CalculateCameraPos(m_Manipulator.Camera, focalLength, centerX, centerY, m_Manipulator.Direction);
            }   
        }

        private float m_StartZoomFactor;
        private float m_CurrentZoomFactor;
    }
}

//XDay