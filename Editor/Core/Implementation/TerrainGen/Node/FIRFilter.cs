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



using UnityEditor;
using UnityEngine;
using XDay.SerializationAPI;

namespace XDay.Terrain.Editor
{
    class FIRFilter : TerrainModifier
    {
        public class Setting : ITerrainModifierSetting
        {
            //between [0, 1]
            public float blend = 0.5f;

            public void Save(ISerializer writer)
            {
                writer.WriteInt32(m_Version, "Setting.Version");
                writer.WriteSingle(blend, "Blend");
            }

            public void Load(IDeserializer reader)
            {
                reader.ReadInt32("Setting.Version");
                blend = reader.ReadSingle("Blend");
            }

            private const int m_Version = 1;
        };

        public FIRFilter(int id) : base(id) { }
        public FIRFilter() { }

        public override void Execute()
        {
            var setting = GetSetting() as Setting;

            mHeightData.Clear();
            var heightData = GetInputModifier(0).GetHeightData();
            if (heightData != null)
            {
                mHeightData.Clear();
                mHeightData.AddRange(heightData);
            }
            else
            {
                Debug.LogError("no height data");
                return;
            }

            var size = GetSize();

            //horizontal pass
            for (var i = 0; i < size.Resolution; ++i)
            {
                float prev = mHeightData[i * size.Resolution];
                for (var j = 1; j < size.Resolution; ++j)
                {
                    prev = ApplyFilter(j, i, prev, setting.blend, size.Resolution);
                }
            }

            //vertical pass
            for (var j = 0; j < size.Resolution; ++j)
            {
                float prev = mHeightData[j];
                for (var i = 1; i < size.Resolution; ++i)
                {
                    prev = ApplyFilter(j, i, prev, setting.blend, size.Resolution);
                }
            }

            SetMaskDataToHeightMap();
        }

        public override void DrawInspector()
        {
            var setting = GetSetting() as Setting;

            EditorGUILayout.IntField("ID", GetID());
            setting.blend = Mathf.Clamp01(EditorGUILayout.FloatField("Blend", setting.blend));
        }

        public override ITerrainModifierSetting CreateSetting()
        {
            return new Setting();
        }

        public override string GetName()
        {
            return "FIRFilter";
        }

        private float ApplyFilter(int x, int y, float prev, float blend, int resolution)
        {
            var idx = y * resolution + x;
            float cur = mHeightData[idx];
            float newVal = blend * prev + (1 - blend) * cur;
            mHeightData[idx] = newVal;
            return newVal;
        }
    };
}