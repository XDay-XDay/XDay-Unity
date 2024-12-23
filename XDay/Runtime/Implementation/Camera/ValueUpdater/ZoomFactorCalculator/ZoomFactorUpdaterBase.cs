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
using UnityEngine;

namespace XDay.CameraAPI
{
    internal class ZoomFactorUpdaterBase
    {
        public ZoomFactorUpdaterBase(CameraManipulator manipulator)
        {
            m_Manipulator = manipulator;
        }

        protected Vector3 CalculateCameraPos(Camera camera, float focalLength, float screenX, float screenY)
        {
            var min = Helper.FocalLengthFromAltitude(m_Manipulator.Setup.Orbit.Pitch, m_Manipulator.MinAltitude);
            var max = Helper.FocalLengthFromAltitude(m_Manipulator.Setup.Orbit.Pitch, m_Manipulator.MaxAltitude);
            focalLength = Mathf.Clamp(focalLength, min, max);
            var centerPos = Helper.RayCastWithXZPlane(new Vector3(screenX, screenY, 0), camera);

            var focusPoint = m_Manipulator.FocusPoint;
            var originalPos = camera.transform.position;
            camera.transform.position = PositionFromFocusPoint(camera, focalLength, focusPoint);
            var zoomCenter = Helper.RayCastWithXZPlane(new Vector3(screenX, screenY, 0), camera);
            camera.transform.position = originalPos;

            var delta = zoomCenter - centerPos;
            var newPos = PositionFromFocusPoint(camera, focalLength, new Vector3(focusPoint.x - delta.x, 0, focusPoint.z - delta.z));
            return newPos;
        }

        protected Vector3 PositionFromFocusPoint(Camera camera, float focalLength, Vector3 focusPoint)
        {
            return focusPoint - camera.transform.forward * focalLength;
        }

        protected CameraManipulator m_Manipulator;
    }
}


//XDay