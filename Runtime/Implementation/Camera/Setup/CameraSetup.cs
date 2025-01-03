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

using XDay.UtilityAPI;
using UnityEngine;


namespace XDay.CameraAPI
{
    public partial class CameraSetup
    {
        public OrbitSetup Orbit { get => m_Orbit; set => m_Orbit = value; }
        public RestoreSetup Restore => m_Restore;
        public string Name => m_Name;
        public bool ChangeFOV { get => m_ChangeFOV; set => m_ChangeFOV = value; }
        public float MaxAltitude
        {
            get => m_AltitudeManager.Max.Altitude;
            set => m_AltitudeManager.Max.Altitude = value;
        }
        public float MinAltitude => m_AltitudeManager.Min.Altitude;

        public CameraSetup(string name)
        {
            m_Name = name;
        }

        public void AddAltitudeSetup(AltitudeSetup setup)
        {
            m_AltitudeManager.AddSetup(setup);
        }

        public AltitudeSetup QueryAltitudeSetup(float height)
        {
            var setup = m_AltitudeManager.QuerySetup(height);
            if (setup == null)
            {
                Debug.LogError($"setup at height {height} not found!");
            }
            return setup;
        }

        public bool DecomposeZoomFactor(float zoomFactor, out float focalLength, out float fov)
        {
            return m_AltitudeManager.DecomposeZoomFactor(zoomFactor, out focalLength, out fov);
        }

        public float QueryZoomFactor(string name)
        {
            return m_AltitudeManager.QueryZoomFactor(name);
        }

        public float ZoomFactorAtAltitude(float height)
        {
            var setup = m_AltitudeManager.QuerySetup(height);
            if (setup != null)
            {
                return setup.ZoomFactor;
            }

            FOVAtAltitude(height, out var fov);
            return fov * Helper.FocalLengthFromAltitude(m_Orbit.Pitch, height);
        }

        public void FOVAtAltitude(float height, out float fov)
        {
            m_AltitudeManager.FOVAtAltitude(height, out fov);
        }

        private string m_Name;
        private bool m_ChangeFOV = true;
        private OrbitSetup m_Orbit = new();
        private AltitudeSetupManager m_AltitudeManager = new();
        private RestoreSetup m_Restore = new();
    }
}


//XDay