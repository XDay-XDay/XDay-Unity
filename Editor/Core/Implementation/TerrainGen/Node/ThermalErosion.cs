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

namespace XDay.Terrain.Editor
{
    class ThermalErosion : TerrainModifier
    {
        public class Setting : ITerrainModifierSetting
        {
            public int iterateCount = 500;
            public float slopeThreshold = 0.02f;
            public float erosionRate = 0.5f;

            public void Save(ISerializer writer)
            {
                writer.WriteInt32(m_Version, "Setting.Version");
                writer.WriteInt32(iterateCount, "Iterate Count");
                writer.WriteSingle(slopeThreshold, "Slope Threshold");
                writer.WriteSingle(erosionRate, "Erosion Rate");
            }

            public void Load(IDeserializer reader)
            {
                reader.ReadInt32("Setting.Version");
                iterateCount = reader.ReadInt32("Iterate Count");
                slopeThreshold = reader.ReadSingle("Slope Threshold");
                erosionRate = reader.ReadSingle("Erosion Rate");
            }

            private const int m_Version = 1;
        };

        public ThermalErosion(int id) : base(id) { }
        public ThermalErosion() { }

        public override void Execute()
        {
            mHeightData.Clear();
            var heightData = GetInputModifier(0).GetHeightData();

            if (heightData != null)
            {
                ApplyThermalErosion(heightData);
                mHeightData.AddRange(heightData);
            }

            SetMaskDataToHeightMap();
        }

        public override ITerrainModifierSetting CreateSetting()
        {
            return new Setting();
        }

        public override string GetName()
        {
            return "ThermalErosion";
        }

        public override void DrawInspector()
        {
            var setting = GetSetting() as Setting;

            EditorGUILayout.IntField("ID", GetID());
            setting.iterateCount = EditorGUILayout.IntField("Iterate Count", setting.iterateCount);
            setting.slopeThreshold = EditorGUILayout.FloatField("Slope Threshold", setting.slopeThreshold);
            setting.erosionRate = EditorGUILayout.FloatField("Erosion Rate", setting.erosionRate);
        }

        private void ApplyThermalErosion(List<float> heightmap)
        {
            var size = GetSize();
            var setting = GetSetting() as Setting;
            int iterations = setting.iterateCount;

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 1; x < size.Resolution - 1; x++)
                {
                    for (int y = 1; y < size.Resolution - 1; y++)
                    {
                        var idx = y * size.Resolution + x;
                        float currentHeight = heightmap[idx];
                        float maxDelta = 0;
                        int targetX = x, targetY = y;

                        // 寻找坡度最大的相邻点
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                if (dx == 0 && dy == 0) continue;

                                var k = (y + dy) * size.Resolution + x + dx;
                                float neighborHeight = heightmap[k];
                                float delta = currentHeight - neighborHeight;
                                if (delta > maxDelta)
                                {
                                    maxDelta = delta;
                                    targetX = x + dx;
                                    targetY = y + dy;
                                }
                            }
                        }

                        // 若坡度超过阈值，转移高度差
                        if (maxDelta > setting.slopeThreshold)
                        {
                            float transfer = maxDelta * setting.erosionRate;
                            heightmap[y * size.Resolution + x] -= transfer;
                            heightmap[targetY * size.Resolution + targetX] += transfer;
                        }
                    }
                }
            }
        }
    };
}
