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


using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using XDay.UtilityAPI;
using XDay.SerializationAPI;

namespace XDay.Terrain.Editor
{
    //generate mask by slope
    class Slope : TerrainModifier
    {
        public enum Mode
        {
            Average,
            Center,
        };

        public class Setting : ITerrainModifierSetting
        {
            public float MinAngle = 0;
            public float MaxAngle = 45;
            public Mode Mode = Mode.Center;

            public void Save(ISerializer writer)
            {
                writer.WriteInt32(m_Version, "Setting.Version");
                writer.WriteSingle(MinAngle, "Min Angle");
                writer.WriteSingle(MaxAngle, "Max Angle");
                writer.WriteEnum(Mode, "Mode");
            }

            public void Load(IDeserializer reader)
            {
                reader.ReadInt32("Setting.Version");
                MinAngle = reader.ReadSingle("Min Angle");
                MaxAngle = reader.ReadSingle("Max Angle");
                Mode = reader.ReadEnum<Mode>("Mode");
            }

            private const int m_Version = 1;
        };

        public Slope(int id)
        {
        }

        public Slope()
        {
        }

        public override void Execute()
        {
            var setting = GetSetting() as Setting;

            var inputModifier0 = GetInputModifier(0);
            var heightData = inputModifier0.GetHeightData();
            if (heightData == null)
            {
                Debug.LogError("Slope::Execute failed, no height data!");
                return;
            }

            var size = inputModifier0.GetSize();

            mHeightData.Clear();
            mHeightData.AddRange(heightData);
            Helper.Resize(mMaskData, size.Resolution * size.Resolution);

            for (var i = 0; i < size.Resolution; ++i)
            {
                for (var j = 0; j < size.Resolution; ++j)
                {
                    float slope;
                    if (setting.Mode == Mode.Center)
                    {
                        slope = GetSlope(heightData, j, i, size.Resolution, size.Width / size.Resolution, mGenerator.GetMaxHeight());
                    }
                    else
                    {
                        slope = GetSlopeAverage(heightData, j, i, size.Resolution, size.Width / size.Resolution, mGenerator.GetMaxHeight());
                    }
                    float angle = Mathf.Atan(slope) * Mathf.Rad2Deg;
                    if (angle >= setting.MinAngle && angle <= setting.MaxAngle)
                    {
                        //slope is atmost 90 degree
                        float v = Mathf.Clamp01(angle / 90.0f);
                        mMaskData.Add(new Color(v, v, v, 1.0f));
                    }
                    else
                    {
                        mMaskData.Add(new Color(0, 0, 0, 1.0f));
                    }
                }
            }
        }

        public override string GetName()
        {
            return "Slope";
        }

        public override ITerrainModifierSetting CreateSetting()
        {
            return new Setting();
        }

        public override void DrawInspector()
        {
            var setting = GetSetting() as Setting;

            EditorGUILayout.IntField("ID", GetID());
            setting.MinAngle = EditorGUILayout.FloatField("Min Angle", setting.MinAngle);
            setting.MaxAngle = EditorGUILayout.FloatField("Max Angle", setting.MaxAngle);
            setting.Mode = (Mode)EditorGUILayout.EnumPopup("Mode", setting.Mode);
        }

        public override bool HasMaskData() { return true; }

        private float GetData(List<float> data, int x, int y, int resolution)
        {
            return data[y * resolution + x];
        }

        //https://surferhelp.goldensoftware.com/gridops/terrain_slope.htm
        private float GetSlope(List<float> data, int x, int y, int resolution, float gridSize, float maxHeight)
        {
            int left = x - 1;
            int right = x + 1;
            int top = y + 1;
            int bottom = y - 1;

            if (left >= 0 && right < resolution && bottom >= 0 && top < resolution)
            {
                float leftHeight = GetData(data, left, y, resolution) * maxHeight;
                float rightHeight = GetData(data, right, y, resolution) * maxHeight;
                float topHeight = GetData(data, x, top, resolution) * maxHeight;
                float bottomHeight = GetData(data, x, bottom, resolution) * maxHeight;

                float dzdx = (rightHeight - leftHeight) / (gridSize * 2);
                float dzdy = (topHeight - bottomHeight) / (gridSize * 2);
                float slope = Mathf.Sqrt(dzdx * dzdx + dzdy * dzdy);
                return slope;
            }

            //no slope at edge!
            return 0;
        }

        private float GetSlopeAverage(List<float> data, int x, int y, int resolution, float gridSize, float maxHeight)
        {
            int left = x - 1;
            int right = x + 1;
            int top = y + 1;
            int bottom = y - 1;

            if (left >= 0 && right < resolution && bottom >= 0 && top < resolution)
            {
                float leftHeight = GetData(data, left, y, resolution) * maxHeight;
                float leftTopHeight = GetData(data, left, top, resolution) * maxHeight;
                float leftBottomHeight = GetData(data, left, bottom, resolution) * maxHeight;
                float rightHeight = GetData(data, right, y, resolution) * maxHeight;
                float rightTopHeight = GetData(data, right, top, resolution) * maxHeight;
                float rightBottomHeight = GetData(data, right, bottom, resolution) * maxHeight;
                float topHeight = GetData(data, x, top, resolution) * maxHeight;
                float bottomHeight = GetData(data, x, bottom, resolution) * maxHeight;

                float dew = ((rightTopHeight + rightHeight * 2 + rightBottomHeight) - (leftTopHeight + leftHeight * 2 + leftBottomHeight)) / (gridSize * 8);
                float dns = ((rightTopHeight + topHeight * 2 + leftTopHeight) - (rightBottomHeight + bottomHeight * 2 + leftBottomHeight)) / (gridSize * 8);
                float slope = Mathf.Sqrt(dew * dew + dns * dns);
                return slope;
            }

            //no slope at edge!
            return 0;
        }
    };
}