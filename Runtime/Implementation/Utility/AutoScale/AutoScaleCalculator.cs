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
        }

        public void Update(bool forceUpdate)
        {
            if (m_Camera == null)
            {
                return;
            }

            var currentAltitude = m_Camera.CurrentAltitude;
            var lastAltitude = m_Camera.LastAltitude;

            //if (!forceUpdate && Mathf.Approximately(lastAltitude, currentAltitude))
            //{
            //    return;
            //}

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

        private ICameraManipulator m_Camera;
        private AutoScaleConfig[] m_ScaleConfigs;
        private float m_CurrentScale = 0;
    }
}
