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
    public enum CameraDirection
    {
        XZ,
        XY,
    }

    public partial class CameraSetup
    {
        public OrbitSetup Orbit { get => m_Orbit; set => m_Orbit = value; }
        public RestoreSetup Restore => m_Restore;
        public string Name => m_Name;
        public bool ChangeFOV { get => m_ChangeFOV; set => m_ChangeFOV = value; }
        public bool UseNarrowView { get => m_UseNarrowView; set => m_UseNarrowView = value; }
        public float FixedFOV { get => m_FixedFOV; set => m_FixedFOV = value; }
        public float DefaultAltitude { get => m_DefaultAltitude; set => m_DefaultAltitude = Mathf.Max(0, value); }
        public CameraDirection Direction { get => m_Direction; internal set => m_Direction = value; }
        public float MouseZoomSpeed { get => m_MouseZoomSpeed; set => m_MouseZoomSpeed = value; }
        public Rect FocusPointBounds { get => m_FocusPointBounds; internal set => m_FocusPointBounds = value; }
        public float MaxAltitude
        {
            get => m_AltitudeManager.Max == null ? 1000 : m_AltitudeManager.Max.Altitude;
            set => m_AltitudeManager.Max.Altitude = value;
        }
        public float MinAltitude => m_AltitudeManager.Min == null ? 0 : m_AltitudeManager.Min.Altitude;
        public float MaxAltitudeFOV => m_AltitudeManager.Max.FOV;
        internal AltitudeSetupManager AltitudeManager => m_AltitudeManager;

        public CameraSetup(string name)
        {
            m_Name = name;
        }

        public void AddAltitudeSetup(AltitudeSetup setup)
        {
            m_AltitudeManager.AddSetup(setup);
        }

        public float QueryAltitude(string name)
        {
            var setup = m_AltitudeManager.QuerySetup(name);
            if (setup != null)
            {
                return setup.Altitude;
            }
            Debug.LogError($"Invalid camera height setup {name}");
            return 0;
        }

        public AltitudeSetup QueryAltitudeSetup(float height)
        {
            var setup = m_AltitudeManager.QuerySetup(height);
            if (setup == null && Application.isPlaying)
            {
                Debug.LogError($"setup at height {height} not found!");
            }
            return setup;
        }

        public void OnFOVChange(float fov)
        {
            m_AltitudeManager.OnFOVChange(fov);
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
            if (m_Direction == CameraDirection.XZ)
            {
                return fov * Helper.FocalLengthFromAltitudeXZ(m_Orbit.Pitch, height);
            }
            return fov * Helper.FocalLengthFromAltitudeXY(m_Orbit.Pitch, height);
        }

        public void FOVAtAltitude(float height, out float fov)
        {
            m_AltitudeManager.FOVAtAltitude(height, out fov);
        }

        private string m_Name;
        private bool m_ChangeFOV = true;
        private bool m_UseNarrowView = true;
        private float m_FixedFOV = 0;
        //相机默认高度
        private float m_DefaultAltitude = 0;
        private CameraDirection m_Direction = CameraDirection.XZ;
        private OrbitSetup m_Orbit = new();
        private AltitudeSetupManager m_AltitudeManager = new();
        private RestoreSetup m_Restore = new();
        private float m_MouseZoomSpeed = 100;
        private Rect m_FocusPointBounds;
    }
}


//XDay