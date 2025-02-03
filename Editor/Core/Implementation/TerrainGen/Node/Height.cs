

using UnityEditor;
using UnityEngine;
using XDay.SerializationAPI;
using XDay.UtilityAPI;

namespace XDay.Terrain.Editor
{
    //generate mask by height
    class Height : TerrainModifier
    {
        public class Setting : ITerrainModifierSetting
        {
            public float MinHeightRatio = 0;
            public float MaxHeightRatio = 0.5f;

            public void Save(ISerializer writer)
            {
                writer.WriteInt32(m_Version, "Setting.Version");
                writer.WriteSingle(MinHeightRatio, "Min Height");
                writer.WriteSingle(MaxHeightRatio, "Max Height");
            }

            public void Load(IDeserializer reader)
            {
                reader.ReadInt32("Setting.Version");
                MinHeightRatio = reader.ReadSingle("Min Height");
                MaxHeightRatio = reader.ReadSingle("Max Height");
            }

            private const int m_Version = 1;
        };

        public Height(int id) : base(id) { }    
        public Height() { }

        public override void Execute()
        {
            var setting = GetSetting() as Setting;

            var inputModifier0 = GetInputModifier(0);
            var heightData = inputModifier0.GetHeightData();
            if (heightData == null)
            {
                Debug.LogError("Height::Execute failed, no height data!");
                return;
            }

            var size = inputModifier0.GetSize();

            mHeightData.Clear();
            mHeightData.AddRange(heightData);
            Helper.Resize(mMaskData, size.Resolution * size.Resolution);

            Vector2 minMaxHeight = GetMaxNormalizedHeight();
            float minHeightSelect = minMaxHeight.x + setting.MinHeightRatio * (minMaxHeight.y - minMaxHeight.x);
            float maxHeightSelect = minMaxHeight.x + setting.MaxHeightRatio * (minMaxHeight.y - minMaxHeight.x);

            for (var i = 0; i < size.Resolution; ++i)
            {
                for (var j = 0; j < size.Resolution; ++j)
                {
                    var idx = i * size.Resolution + j;
                    float height = mHeightData[idx];
                    float value = Mathf.Clamp01((height - minHeightSelect) / (maxHeightSelect - minHeightSelect));
                    mMaskData.Add(new Color(value, value, value, 1.0f));
                }
            }
        }

        public override string GetName()
        {
            return "Height";
        }

        public override ITerrainModifierSetting CreateSetting()
        {
            return new Setting();
        }

        public override void DrawInspector()
        {
            var setting = GetSetting() as Setting;

            EditorGUILayout.IntField("ID", GetID());
            setting.MinHeightRatio = Mathf.Clamp(EditorGUILayout.FloatField("Min Height", setting.MinHeightRatio), -1, 1);
            setting.MaxHeightRatio = Mathf.Clamp(EditorGUILayout.FloatField("Max Height", setting.MaxHeightRatio), -1, 1);
        }

        public override bool HasMaskData() { return true; }

        public Vector2 GetMaxNormalizedHeight()
        {
            Vector2 minMax = new(float.MaxValue, float.MinValue);
            for (var i = 0; i < mHeightData.Count; ++i)
            {
                float height = mHeightData[i];
                if (height < minMax.x)
                {
                    minMax.x = height;
                }
                if (height > minMax.y)
                {
                    minMax.y = height;
                }
            }
            return minMax;
        }
    };
}