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
    internal class ColorGradient : TerrainModifier
    {
		public class Setting : ITerrainModifierSetting
        {
            public Gradient gradient;

            public void Save(ISerializer writer)
            {
                writer.WriteInt32(mVersion, "Setting.Version");
            }

            public void Load(IDeserializer reader)
            {
                reader.ReadInt32("Setting.Version");
            }

            private const int mVersion = 1;
        };

        public ColorGradient(int id) : base(id)
        {
        }

        public ColorGradient()
        {
        }

        public override void Execute()
        {
            var setting = GetSetting() as Setting;

            var inputModifier0 = GetInputModifier(0);
            var maskData = inputModifier0.GetMaskData();
            if (maskData == null)
            {
                Debug.LogError("ColorGradient::Execute failed, no mask data!");
                return;
            }

            var size = inputModifier0.GetSize();

            mHeightData = inputModifier0.GetHeightData();
            Debug.Assert(mHeightData.Count > 0);
            mMaskData.Clear();
            mMaskData.Capacity = size.Resolution * size.Resolution;

            for (var i = 0; i < size.Resolution; ++i)
            {
                for (var j = 0; j < size.Resolution; ++j)
                {
                    var idx = i * size.Resolution + j;
                    float value = maskData[idx].r;
                    mMaskData.Add(setting.gradient.Evaluate(value));
                }
            }
        }

        public override string GetName()
        {
            return "ColorGradient";
        }

        public override ITerrainModifierSetting CreateSetting()
        {
            var setting = new Setting();

            var colorKeys = new GradientColorKey[]
            {
                new(Color.black, 0),
                new(Color.white, 1),
            };

            setting.gradient.colorKeys = colorKeys;
            return setting;
        }

        public override void DrawInspector()
        {
            var setting = GetSetting() as Setting;

            EditorGUILayout.IntField("ID", GetID());
            EditorGUILayout.GradientField("Gradient", setting.gradient);
        }

        public override bool HasMaskData() 
        { 
            return true; 
        }
    };
}