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



using UnityEngine;
using UnityEditor;
using XDay.UtilityAPI;
using XDay.SerializationAPI;

namespace XDay.Terrain.Editor
{
    class Start : TerrainModifier
    {
        public class Setting : ITerrainModifierSetting
        {
            public TerrainSize size = new();
            public Material defaultOutputMaterial;

            public void Save(ISerializer writer)
            {
                writer.WriteInt32(m_Version, "Setting.Version");
                writer.WriteSingle(size.Width, "Width");
                writer.WriteSingle(size.Height, "Height");
                writer.WriteInt32(size.Resolution, "Resolution");
                writer.WriteVector3(size.Position, "Position");
                writer.WriteSingle(size.MaxHeight, "Max Height");
                writer.WriteString(EditorHelper.GetObjectGUID(defaultOutputMaterial), "Material File ID");
            }

            public void Load(IDeserializer reader)
            {
                reader.ReadInt32("Setting.Version");
                size.Width = reader.ReadSingle("Width");
                size.Height = reader.ReadSingle("Height");
                size.Resolution = reader.ReadInt32("Resolution");
                size.Position = reader.ReadVector3("Position");
                size.MaxHeight = reader.ReadSingle("Max Height");
                string materialGUID = reader.ReadString("Material File ID");
                defaultOutputMaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(materialGUID));
            }

            private const int m_Version = 1;
        };

        public Start(int id) : base(id)
        {
        }

        public Start()
        {
        }

        public override void Execute()
        {
            var setting = GetSetting() as Setting;

            var n = setting.size.Resolution * setting.size.Resolution;
            Helper.Resize(mHeightData, n);
            for (var i = 0; i < n; ++i)
            {
                mHeightData.Add(0);
            }

            SetMaskDataToHeightMap();
        }
        public override TerrainSize GetSize()
        {
            var setting = GetSetting() as Setting;
            return setting.size;
        }

        public override string GetName()
        {
            return "Start";
        }

        public override ITerrainModifierSetting CreateSetting()
        {
            return new Setting();
        }
        public override int GetMaxInputCount()
        {
            return 0;
        }

        public override void DrawInspector()
        {
            var setting = GetSetting() as Setting;

            EditorGUILayout.IntField("ID", GetID());
            setting.size.Height = EditorGUILayout.FloatField("Height", setting.size.Height);
            setting.size.Width = EditorGUILayout.FloatField("Width", setting.size.Width);
            setting.size.Resolution = EditorGUILayout.IntField("Resolution", setting.size.Resolution);
            setting.size.MaxHeight = EditorGUILayout.FloatField("Max Height", setting.size.MaxHeight);

            bool show = EditorGUILayout.Toggle("Show Mask", mGenerator.GetShowMask());
            mGenerator.ShowMask(show);

            setting.defaultOutputMaterial = EditorGUILayout.ObjectField("Material", setting.defaultOutputMaterial, typeof(Material), false) as Material;
        }

        public override bool CanDelete()
        {
            return false;
        }

        public override Color GetDisplayColor()
        {
            return new Color(0, 1, 0, 0.5f);
        }
    };
}