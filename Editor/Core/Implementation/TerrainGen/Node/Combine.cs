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
using XDay.UtilityAPI;

namespace XDay.Terrain.Editor
{
    class Combine : TerrainModifier
    {
        public enum CombineMode
        {
            Blend,
            Max,
            Min,
            Add,
            Subtract,
        };

        public class Setting : ITerrainModifierSetting
        {
            public float Ratio = 0.5f;
            public CombineMode CombineMode = CombineMode.Blend;
            public bool SwapInput = false;

            public void Save(ISerializer writer)
            {
                writer.WriteInt32(m_Version, "Setting.Version");
                writer.WriteSingle(Ratio, "Ratio");
                writer.WriteBoolean(SwapInput, "Swap Input");
                writer.WriteEnum(CombineMode, "Combine Mode");
            }

            public void Load(IDeserializer reader)
            {
                reader.ReadInt32("Setting.Version");
                Ratio = reader.ReadSingle("Ratio");
                SwapInput = reader.ReadBoolean("Swap Input");
                CombineMode = reader.ReadEnum<CombineMode>("Combine Mode");
            }

            private const int m_Version = 1;
        };

        public Combine(int id) : base(id)
        {
        }

        public Combine() { }

        public override void Execute()
        {
            var setting = GetSetting() as Setting;

            var inputModifier0 = GetInputModifier(0);
            var inputModifier1 = GetInputModifier(1);
            var size0 = inputModifier0.GetSize();
            var size1 = inputModifier1.GetSize();
            Debug.Assert(size0.Resolution == size1.Resolution);

            if (inputModifier0 != null && 
                inputModifier1 != null)
            {
                List<float> data0 = inputModifier0.GetHeightData();
                List<float> data1 = inputModifier1.GetHeightData();
                if (setting.SwapInput)
                {
                    (data0, data1) = (data1, data0);
                }

                Helper.Resize(mHeightData, size0.Resolution * size0.Resolution);

                if (setting.CombineMode == CombineMode.Blend)
                {
                    for (var i = 0; i < data0.Count; ++i)
                    {
                        mHeightData[i] = Mathf.Lerp(data0[i], data1[i], setting.Ratio);
                    }
                }
                else if (setting.CombineMode == CombineMode.Add)
                {
                    for (var i = 0; i < data0.Count; ++i)
                    {
                        mHeightData[i] = data0[i] + data1[i];
                    }
                }
                else if (setting.CombineMode == CombineMode.Subtract)
                {
                    for (var i = 0; i < data0.Count; ++i)
                    {
                        mHeightData[i] = data0[i] - data1[i];
                    }
                }
                else if (setting.CombineMode == CombineMode.Max)
                {
                    for (var i = 0; i < data0.Count; ++i)
                    {
                        mHeightData[i] = Mathf.Max(data0[i], data1[i]);
                    }
                }
                else if (setting.CombineMode == CombineMode.Min)
                {
                    for (var i = 0; i < data0.Count; ++i)
                    {
                        mHeightData[i] = Mathf.Min(data0[i], data1[i]);
                    }
                }
                else
                {
                    Debug.Assert(false, "Todo");
                }
            }
            else if (inputModifier0 != null)
            {
                mHeightData.Clear();
                mHeightData.AddRange(inputModifier0.GetHeightData());
            }
            else if (inputModifier1 != null)
            {
                mHeightData.Clear();
                mHeightData.AddRange(inputModifier1.GetHeightData());
            }

            SetMaskDataToHeightMap();
        }

        public override string GetName()
        {
            return "Combine";
        }

        public override ITerrainModifierSetting CreateSetting()
        {
            return new Setting();
        }

        public override int GetMaxInputCount()
        {
            return 2;
        }

        public override void DrawInspector()
        {
            var setting = GetSetting() as Setting;

            EditorGUILayout.IntField("ID", GetID());
            setting.Ratio = EditorGUILayout.FloatField("Ratio", setting.Ratio);
            setting.SwapInput = EditorGUILayout.Toggle("Swap Input", setting.SwapInput);
            setting.CombineMode = (CombineMode)EditorGUILayout.EnumPopup("Combine Mode", setting.CombineMode);
        }
    };
}