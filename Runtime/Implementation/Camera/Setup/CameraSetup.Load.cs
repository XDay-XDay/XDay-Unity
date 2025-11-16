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
using System.Text;
using UnityEditor;
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

            if (root.ContainsKey("Default Altitude"))
            {
                m_DefaultAltitude = System.Convert.ToSingle(root["Default Altitude"]);
            }

            if (root.ContainsKey("Fixed FOV"))
            {
                m_FixedFOV = System.Convert.ToSingle(root["Fixed FOV"]);
            }

            if (root.ContainsKey("Change FOV"))
            {
                m_ChangeFOV = System.Convert.ToBoolean(root["Change FOV"]);
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

        public void Save(string path)
        {
            var error = Validate();
            if (!string.IsNullOrEmpty(error))
            {
                EditorUtility.DisplayDialog("出错了,无法保存相机配置", error, "确定");
                return;
            }

            Dictionary<string, object> root = new();

            root["Direction"] = m_Direction.ToString();
            root["Default Altitude"] = m_DefaultAltitude;
            root["Change FOV"] = m_ChangeFOV;
            root["Fixed FOV"] = m_FixedFOV;

            var boundsConfig = new Dictionary<string, object>
            {
                ["Min"] = FromVector2(m_FocusPointBounds.min),
                ["Max"] = FromVector2(m_FocusPointBounds.max)
            };
            root["Bounds"] = boundsConfig;

            var rotationConfig = new Dictionary<string, object>
            {
                ["Pitch"] = m_Orbit.Pitch,
                ["Yaw"] = m_Orbit.Yaw,
                ["Range"] = m_Orbit.Range,
                ["Min Altitude"] = m_Orbit.MinAltitude,
                ["Max Altitude"] = m_Orbit.MaxAltitude,
                ["Restore Speed"] = m_Orbit.RestoreSpeed,
                ["Zoom"] = m_Orbit.EnableTouchOrbit,
                ["Free"] = m_Orbit.EnableUnrestrictedOrbit
            };
            root["Orbit"] = rotationConfig;

            var restoreConfig = new Dictionary<string, object>
            {
                ["Duration"] = m_Restore.Duration,
                ["Distance"] = m_Restore.Distance
            };
            root["Restore"] = restoreConfig;

            List<object> altitudes = new();
            foreach (var altitude in m_AltitudeManager.AltitudeSetups)
            {
                var config = new Dictionary<string, object>
                {
                    ["Name"] = altitude.Name,
                    ["Altitude"] = altitude.Altitude,
                    ["FOV"] = altitude.FOV
                };
                altitudes.Add(config);
            }
            root["Altitude"] = altitudes;

            var text = Json.Serialize(root);

            EditorHelper.WriteFile(text, path);

            AssetDatabase.Refresh();
        }

        private string Validate()
        {
            StringBuilder error = new();
            HashSet<string> names = new();
            foreach (var altitude in m_AltitudeManager.AltitudeSetups)
            {
                if (!names.Contains(altitude.Name))
                {
                    names.Add(altitude.Name);
                }
                else
                {
                    error.AppendLine($"重复的名字: {altitude.Name}");
                }
            }

            if (m_AltitudeManager.AltitudeSetups[0].Name != "min")
            {
                error.AppendLine("min必须是第一个");
            }

            if (m_AltitudeManager.AltitudeSetups[^1].Name != "max")
            {
                error.AppendLine("max必须是最后一个");
            }

            return error.ToString();
        }

        private Vector2 ToVector2(string v)
        {
            v = v.Trim();
            var tokens = v.Split(',');
            float x = float.Parse(tokens[0]);
            float y = float.Parse(tokens[1]);
            return new Vector2(x, y);
        }

        private string FromVector2(Vector2 v)
        {
            return $"{v.x},{v.y}";
        }
    }
}

//XDay