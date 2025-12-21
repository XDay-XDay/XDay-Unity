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
using XDay.CameraAPI;

namespace XDay.UtilityAPI
{
    public class AutoScaleCalculator
    {
        public float CurrentScale => m_CurrentScale;

        public AutoScaleCalculator(ICameraManipulator camera, AutoScaleConfig[] scaleConfigs)
        {
            m_Camera = camera;
            m_ScaleConfigs = scaleConfigs;

            Update(true);
        }

        public void Update(bool forceUpdate)
        {
            if (m_Camera == null)
            {
                return;
            }

            var currentAltitude = m_Camera.CurrentAltitude;
            var lastAltitude = m_Camera.LastAltitude;

            if (!forceUpdate && Mathf.Approximately(lastAltitude, currentAltitude))
            {
                return;
            }

            foreach (var scaleConfig in m_ScaleConfigs)
            {
                if (scaleConfig != null)
                {
                    if (!((lastAltitude > scaleConfig.EndScaleCameraAltitude && currentAltitude > scaleConfig.EndScaleCameraAltitude) ||
                       (lastAltitude < scaleConfig.StartScaleCameraAltitude && currentAltitude < scaleConfig.StartScaleCameraAltitude)))
                    {
                        currentAltitude = Mathf.Clamp(currentAltitude, scaleConfig.StartScaleCameraAltitude, scaleConfig.EndScaleCameraAltitude);

                        m_CurrentScale = CalculateScaleAtHeight(scaleConfig, currentAltitude);
                    }
                }
            }
        }

        private float CalculateScaleAtHeight(AutoScaleConfig scaleConfig, float cameraHeight)
        {
            float oldWidth = CalculateViewportWidth(scaleConfig.CameraFOVWhenScaleIsBaseScale);
            m_Camera.Setup.FOVAtAltitude(cameraHeight, out var fov);
            float newWidth = CalculateViewportWidth(fov);
            float ratio = newWidth / oldWidth;

            float scaleFactor = scaleConfig.BaseScale / scaleConfig.CameraAltitudeWhenScaleIsBaseScale;

            var extraScale = 0f;
            if (scaleConfig.ExtraScaleMultiplier != 0)
            {
                float t = (cameraHeight - scaleConfig.StartScaleCameraAltitude) / (scaleConfig.EndScaleCameraAltitude - scaleConfig.StartScaleCameraAltitude);
                extraScale = Mathf.Clamp01(t) * scaleConfig.ExtraScaleMultiplier;
            }

            return scaleFactor * cameraHeight * ratio * (1 + extraScale);
        }

        private float CalculateViewportWidth(float fov)
        {
            float height = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * m_Camera.Camera.nearClipPlane * 2.0f;
            return height * m_Camera.Camera.aspect;
        }

        private readonly ICameraManipulator m_Camera;
        private readonly AutoScaleConfig[] m_ScaleConfigs;
        private float m_CurrentScale = 0;
    }
}
