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

using System;
using UnityEngine;

namespace XDay.CameraAPI
{
    internal class FieldOfViewUpdater
    {
        public float CurrentFOV => m_CurrentFOV;

        public FieldOfViewUpdater(CameraSetup setup, Camera camera, Action onFOVChangedExternally) 
        {
            m_Setup = setup;
            m_CurrentFOV = camera.fieldOfView;
            m_OnFOVChangedExternally = onFOVChangedExternally;

            Update(camera, camera.transform.position.y);
        }

        public void Update(Camera camera, float height)
        {
            //fov被非相机库逻辑修改了
            if (m_CurrentFOV != camera.fieldOfView)
            {
                m_OnFOVChangedExternally?.Invoke();
                m_CurrentFOV = camera.fieldOfView;
            }

            if (m_Setup.ChangeFOV)
            {
                var fov = m_Setup.FixedFOV;
                if (fov <= 0)
                {
                    m_Setup.FOVAtAltitude(height, out fov);
                }
                camera.fieldOfView = fov;
                m_CurrentFOV = fov;
            }
        }

        private CameraSetup m_Setup;
        private float m_CurrentFOV;
        private Action m_OnFOVChangedExternally;
    }
}

//XDay