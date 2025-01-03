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



namespace XDay.CameraAPI
{
    public partial class CameraSetup
    {
        public class OrbitSetup
        {
            public float Range => m_Range;
            public float ResetSpeed => m_ResetSpeed;
            public bool EnableTouchOrbit => m_EnableTouchOrbit;
            public float Yaw => m_Yaw;
            public float Pitch => m_Pitch;
            public bool EnableUnrestrictedOrbit => m_EnableUnrestrictedOrbit;
            public float MinAltitude => m_MinAltitude;
            public float MaxAltitude => m_MaxAltitude;

            public OrbitSetup()
            {
            }

            public OrbitSetup(float range, float resetSpeed, bool enableTouchOrbit, float yaw, float pitch, bool enableUnrestrictedOrbit, float minAltitude, float maxAltitude)
            {
                m_Range = range;
                m_ResetSpeed = resetSpeed;
                m_EnableTouchOrbit = enableTouchOrbit;
                m_Yaw = yaw;
                m_Pitch = pitch;
                m_EnableUnrestrictedOrbit = enableUnrestrictedOrbit;
                m_MinAltitude = minAltitude;
                m_MaxAltitude = maxAltitude;
            }

            private float m_Range = 0;
            private float m_ResetSpeed = 180f;
            private bool m_EnableTouchOrbit = false;
            private float m_Yaw = 0;
            private float m_Pitch = 45;
            private bool m_EnableUnrestrictedOrbit = false;
            private float m_MinAltitude;
            private float m_MaxAltitude;
        }
    }
}


//XDay