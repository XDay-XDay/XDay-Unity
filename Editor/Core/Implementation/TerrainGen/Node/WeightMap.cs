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
using UnityEditor;
using UnityEngine;
using XDay.SerializationAPI;

namespace XDay.Terrain.Editor
{
    /// <summary>
    /// mask can be any size
    /// </summary>
    class WeightMap : TerrainModifier
    {
        public enum HeightMode
        {
            Set,
            Add,
            Subtract,
            Multiply,
        };

        public class Setting : ITerrainModifierSetting
        {
            public HeightMode heightMode = HeightMode.Set;
            public List<float> maskData;
            public int maskResolution;

            public float GetWeight(int x, int y)
            {
                return maskData[y * maskResolution + x];
            }

            public void Save(ISerializer writer)
            {
                writer.WriteInt32(m_Version, "Setting.Version");
                writer.WriteEnum(heightMode, "Height Mode");
                writer.WriteInt32(maskResolution, "Mask Resolution");
                writer.WriteSingleList(maskData, "Mask Data");
            }

            public void Load(IDeserializer reader)
            {
                reader.ReadInt32("Setting.Version");
                heightMode = reader.ReadEnum<HeightMode>("Height Mode");
                maskResolution = reader.ReadInt32("Mask Resolution");
                maskData = reader.ReadSingleList("Mask Data");
            }

            private const int m_Version = 1;
        };

        public WeightMap(int id) : base(id) { }
        public WeightMap() { }

        public override void Execute()
        {
            var setting = GetSetting() as Setting;

            mHeightData.Clear();
            var heightData = GetInputModifier(0).GetHeightData();
            if (heightData != null)
            {
                mHeightData.AddRange(heightData);
            }

            var size = GetSize();

            float stepX = size.GetStepX();
            float stepY = size.GetStepY();
            for (var i = 0; i < size.Resolution; ++i)
            {
                for (var j = 0; j < size.Resolution; ++j)
                {
                    var idx = i * size.Resolution + j;
                    float weight = GetWeight(j * stepX / size.Width, i * stepY / size.Height);

                    if (setting.heightMode == HeightMode.Set)
                    {
                        mHeightData[idx] = weight;
                    }
                    else if (setting.heightMode == HeightMode.Add)
                    {
                        mHeightData[idx] += weight;
                    }
                    else if (setting.heightMode == HeightMode.Multiply)
                    {
                        mHeightData[idx] *= weight;
                    }
                    else if (setting.heightMode == HeightMode.Subtract)
                    {
                        mHeightData[idx] -= weight;
                    }
                    else
                    {
                        Debug.Assert(false);
                    }
                }
            }

            SetMaskDataToHeightMap();
        }

        public override void DrawInspector()
        {
            var setting = GetSetting() as Setting;

            setting.heightMode = (HeightMode)EditorGUILayout.EnumPopup("Height Mode", setting.heightMode);
        }

        public override ITerrainModifierSetting CreateSetting()
        {
            return new Setting();
        }

        public override string GetName()
        {
            return "WeightMap";
        }

        private float GetWeight(float x, float y)
        {
            var setting = GetSetting() as Setting;

            var maskResolution = Mathf.FloorToInt(Mathf.Sqrt(setting.maskData.Count));
            Debug.Assert(maskResolution == setting.maskResolution);

            float mx = (maskResolution - 1) * x;
            float my = (maskResolution - 1) * y;

            int px0 = Mathf.FloorToInt(mx);
            int py0 = Mathf.FloorToInt(my);
            int px1 = Mathf.Min(maskResolution - 1, px0 + 1);
            int py1 = Mathf.Min(maskResolution - 1, py0 + 1);
            float rx = mx - px0;
            float ry = my - py0;
            float lx0 = Mathf.Lerp(setting.GetWeight(px0, py0), setting.GetWeight(px1, py0), rx);
            float lx1 = Mathf.Lerp(setting.GetWeight(px0, py1), setting.GetWeight(px1, py1), rx);
            float weight = Mathf.Lerp(lx0, lx1, ry);
            return weight;
        }
    };
}