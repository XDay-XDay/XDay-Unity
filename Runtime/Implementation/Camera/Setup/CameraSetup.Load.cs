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

using MiniJSON;
using System;
using System.Collections.Generic;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.CameraAPI
{
    public partial class CameraSetup
    {
        public void Load(string text)
        {
            var root = Json.Deserialize(text) as Dictionary<string, object>;

            var direction = root.TryGetValue("Direction", out var directionObj);
            if (directionObj != null)
            {
                var dir = directionObj as string;
                bool ok = Enum.TryParse(dir, out m_Direction);
                Debug.Assert(ok, $"Invalid direction: {dir}");
            }

            //load bounds
            {
                if (root.ContainsKey("Bounds"))
                {
                    var boundsConfig = root["Bounds"] as Dictionary<string, object>;
                    var boundsMin = ToVector2(boundsConfig["Min"] as string);
                    var boundsMax = ToVector2(boundsConfig["Max"] as string);
                    m_FocusPointBounds = new Rect(boundsMin, boundsMax - boundsMin);
                }
            }

            float pitch;
            {
                //load orbit
                var rotationConfig = root["Orbit"] as Dictionary<string, object>;
                pitch = System.Convert.ToSingle(rotationConfig["Pitch"]);
                var yaw = System.Convert.ToSingle(rotationConfig["Yaw"]);
                var range = System.Convert.ToSingle(rotationConfig["Range"]);
                var minAltitude = System.Convert.ToSingle(rotationConfig["Min Altitude"]);
                var maxAltitude = System.Convert.ToSingle(rotationConfig["Max Altitude"]);
                var restoreSpeed = System.Convert.ToSingle(rotationConfig["Restore Speed"]);
                var enableZoomRotation = System.Convert.ToBoolean(rotationConfig["Zoom"]);
                var enableFreeRotation = System.Convert.ToBoolean(rotationConfig["Free"]);
                m_Orbit = new OrbitSetup(range, restoreSpeed, enableZoomRotation, yaw, pitch, enableFreeRotation, minAltitude, maxAltitude);
            }

            {
                //load restore
                var restoreConfig = root["Restore"] as Dictionary<string, object>;
                var duration = System.Convert.ToSingle(restoreConfig["Duration"]);
                var distance = System.Convert.ToSingle(restoreConfig["Distance"]);
                m_Restore = new RestoreSetup(distance, duration);
            }

            {
                //load altitude
                m_AltitudeManager = new();
                var altitudeConfig = root["Altitude"] as List<object>;
                for (var i = 0; i < altitudeConfig.Count; i++)
                {
                    var config = altitudeConfig[i] as Dictionary<string, object>;
                    var name = System.Convert.ToString(config["Name"]);
                    var altitude = System.Convert.ToSingle(config["Altitude"]);
                    var fov = System.Convert.ToSingle(config["FOV"]);
                    float focalLength;
                    if (m_Direction == CameraDirection.XZ)
                    {
                        focalLength = Helper.FocalLengthFromAltitudeXZ(pitch, altitude);
                    }
                    else
                    {
                        focalLength = Helper.FocalLengthFromAltitudeXY(pitch, altitude);
                    }
                    m_AltitudeManager.AddSetup(new AltitudeSetup(name, altitude, fov, focalLength));
                }
                m_AltitudeManager.PostLoad();
            }
        }

        private Vector2 ToVector2(string v)
        {
            v = v.Trim();
            var tokens = v.Split(',');
            float x = float.Parse(tokens[0]);
            float y = float.Parse(tokens[1]);
            return new Vector2(x, y);
        }
    }
}

//XDay