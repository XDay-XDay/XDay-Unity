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
using System.Collections.Generic;
using UnityEngine;

namespace XDay.CameraAPI
{ 
    public partial class CameraSetup
    {
        public class AltitudeSetupManager
        {
            public AltitudeSetup Min => m_UserMin != null ? m_UserMin : m_Min;
            public AltitudeSetup Max => m_UserMax != null ? m_UserMax : m_Max;

            public AltitudeSetup QuerySetup(string name)
            {
                var setup = QuerySetupInternal(name);
                if (setup == null)
                {
                    Debug.Assert(false, $"camera setup {name} not found!");
                }
                return setup;
            }

            public void FOVAtAltitude(float altitude, out float fov)
            {
                fov = 0;
                if (m_Setups.Count == 0)
                {
                    return;
                }

                foreach (var setup in m_Setups)
                {
                    if (Mathf.Approximately(setup.Altitude, altitude))
                    {
                        fov = setup.FOV;
                        return;
                    }
                }

                var end = -1;
                var start = -1;
                for (var i = m_Setups.Count - 1; i >= 0; i--)
                {
                    if (altitude >= m_Setups[i].Altitude)
                    {
                        end = i + 1;
                        start = i;
                        break;
                    }
                }
                start = Mathf.Clamp(start, 0, m_Setups.Count - 1);
                end = Mathf.Clamp(end, 0, m_Setups.Count - 1);
                var delta = m_Setups[end].Altitude - m_Setups[start].Altitude;
                if (Mathf.Approximately(delta, 0))
                {
                    fov = m_Setups[end].FOV;
                }
                else
                {
                    var t = (altitude - m_Setups[start].Altitude) / delta;
                    fov = Mathf.Lerp(m_Setups[start].FOV, m_Setups[end].FOV, t);
                }
            }

            public void AddSetup(AltitudeSetup setup)
            {
                m_Setups.Add(setup);

                Assign();
            }

            public AltitudeSetup QuerySetup(float altitude)
            {
                for (var i = 0; i < m_Setups.Count; i++)
                {
                    if (Mathf.Approximately(altitude, m_Setups[i].Altitude))
                    {
                        return m_Setups[i];
                    }
                }
                return null;
            }

            public bool DecomposeZoomFactor(float zoomFactor, out float focalLength, out float fov)
            {
                focalLength = 0;
                fov = 0;

                var min = m_Setups[0];
                var max = Max;

                if (Helper.LE(zoomFactor, min.ZoomFactor))
                {
                    fov = min.FOV;
                    focalLength = min.FocalLength;
                    return true;
                }

                if (Helper.GE(zoomFactor, max.ZoomFactor))
                {
                    fov = max.FOV;
                    focalLength = max.FocalLength;
                    return true;
                }

                for (var i = 0; i < m_Setups.Count - 1; i++)
                {
                    var cur = m_Setups[i];
                    var next = m_Setups[i + 1];
                    if (Mathf.Approximately(zoomFactor, next.ZoomFactor))
                    {
                        fov = next.FOV;
                        focalLength = next.FocalLength;
                        return true;
                    }

                    if (zoomFactor > cur.ZoomFactor && zoomFactor < next.ZoomFactor)
                    {
                        if (ZoomFactorLerp(cur, next, zoomFactor, out focalLength, out fov))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            public float QueryZoomFactor(string name)
            {
                for (var i = 0; i > m_Setups.Count; i++)
                {
                    if (name.CompareTo(m_Setups[i].Name) == 0)
                    {
                        return m_Setups[i].ZoomFactor;
                    }
                }

                Debug.Assert(false, $"Invalid setup {name}");
                return 0;
            }

            internal void PostLoad()
            {
                m_Setups.Sort((a,b) => { 
                    return a.Altitude.CompareTo(b.Altitude);
                });

                Assign();
            }

            private void Assign()
            {
                m_Min = QuerySetupInternal("min");
                m_Max = QuerySetupInternal("max");
            }

            private bool ZoomFactorLerp(AltitudeSetup min,
                AltitudeSetup max, float zoomFactor, 
                out float focalLength, out float fov)
            {
                var startZoomFactor = min.FocalLength * min.FOV;
                var endZoomFactor = max.FocalLength * max.FOV;
                var fovDelta = max.FOV - min.FOV;
                var focalLengthDelta = max.FocalLength - min.FocalLength;
                var lower = 0.0f;
                var higher = 1.0f;
                var t = -1.0f;

                for (var i = 0; i < 30; i++)
                {
                    if (Mathf.Approximately(zoomFactor, startZoomFactor))
                    {
                        t = lower;
                        break;
                    }

                    if (Mathf.Approximately(zoomFactor, endZoomFactor))
                    {
                        t = higher;
                        break;
                    }

                    var half = (higher + lower) / 2.0f;
                    var middle = (min.FocalLength + half * focalLengthDelta) * (min.FOV + half * fovDelta);

                    if (Mathf.Approximately(zoomFactor, middle))
                    {
                        t = half;
                        break;
                    }

                    if (zoomFactor < middle)
                    {
                        higher = half;
                        endZoomFactor = middle;
                    }
                    else if (zoomFactor > middle)
                    {
                        lower = half;
                        startZoomFactor = middle;
                    }
                }

                if (Mathf.Approximately(t, -1.0f))
                {
                    t = lower;
                }

                fov = min.FOV + t * (max.FOV - min.FOV);
                focalLength = min.FocalLength + t * (max.FocalLength - min.FocalLength);

                return true;
            }

            private AltitudeSetup QuerySetupInternal(string name)
            {
                for (var i = 0; i < m_Setups.Count; i++)
                {
                    if (name.CompareTo(m_Setups[i].Name) == 0)
                    {
                        return m_Setups[i];
                    }
                }
                return null;
            }

            private AltitudeSetup m_UserMin;
            private AltitudeSetup m_UserMax;
            private AltitudeSetup m_Min;
            private AltitudeSetup m_Max;
            private List<AltitudeSetup> m_Setups = new();
        }

        public class AltitudeSetup
        {
            public string Name => m_Name;
            public float FOV => m_FOV;
            public float FocalLength => m_FocalLength;
            public float ZoomFactor => m_ZoomFactor;
            public float Altitude { get => m_Altitude; set => m_Altitude = value; }

            public AltitudeSetup()
            {
            }

            public AltitudeSetup(string name, float altitude, float fov, float focalLength)
            {
                m_Name = name;
                m_Altitude = altitude;
                m_FOV = fov;
                m_FocalLength = focalLength;
                m_ZoomFactor = focalLength * fov;
            }

            private string m_Name;
            private float m_FOV;
            private float m_Altitude;
            private float m_ZoomFactor;
            private float m_FocalLength;
        }
    }
}

//XDay