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
using XDay.SerializationAPI;

namespace XDay.Terrain.Editor
{
    /// <summary>
    /// height map resolution must equal terrain resolution
    /// </summary>
    class HeightMap : TerrainModifier
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
            public List<float> heightMap;

            public void Save(ISerializer writer)
            {
                writer.WriteInt32(m_Version, "Setting.Version");
                writer.WriteEnum(heightMode, "Height Mode");
                writer.WriteSingleList(heightMap, "Height Data");
            }

            public void Load(IDeserializer reader)
            {
                reader.ReadInt32("Setting.Version");
                heightMode = reader.ReadEnum<HeightMode>("Height Mode");
                heightMap = reader.ReadSingleList("Height Data");
            }

            private const int m_Version = 1;
        };

        public HeightMap(int id) : base(id)
        {
        }

        public HeightMap()
        {
        }

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

            if (setting.heightMap.Count != size.Resolution * size.Resolution)
            {
                Debug.LogError("height map size not match terrain resolution");
                return;
            }

            for (var i = 0; i < size.Resolution; ++i)
            {
                for (var j = 0; j < size.Resolution; ++j)
                {
                    var idx = i * size.Resolution + j;
                    if (setting.heightMode == HeightMode.Set)
                    {
                        mHeightData[idx] = setting.heightMap[idx];
                    }
                    else if (setting.heightMode == HeightMode.Add)
                    {
                        mHeightData[idx] += setting.heightMap[idx];
                    }
                    else if (setting.heightMode == HeightMode.Multiply)
                    {
                        mHeightData[idx] *= setting.heightMap[idx];
                    }
                    else if (setting.heightMode == HeightMode.Subtract)
                    {
                        mHeightData[idx] -= setting.heightMap[idx];
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

            EditorGUILayout.IntField("ID", GetID());
            setting.heightMode = (HeightMode)EditorGUILayout.EnumPopup("Height Mode", setting.heightMode);
        }

        public override ITerrainModifierSetting CreateSetting()
        {
            return new Setting();
        }

        public override string GetName()
        {
            return "HeightMap";
        }
    };
}