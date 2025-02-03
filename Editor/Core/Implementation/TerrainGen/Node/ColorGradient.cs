

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