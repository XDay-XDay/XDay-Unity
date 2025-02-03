
using UnityEditor;
using UnityEngine;
using XDay.SerializationAPI;
using XDay.UtilityAPI;

namespace XDay.Terrain.Editor
{
    class RGBA : TerrainModifier
	{
		public class Setting : ITerrainModifierSetting
		{
            public void Save(ISerializer writer)
            {
                writer.WriteInt32(m_Version, "Setting.Version");
            }

            public void Load(IDeserializer reader)
            {
                reader.ReadInt32("Setting.Version");
            }

            private const int m_Version = 1;
        };

		public RGBA(int id) : base(id) { }
		public RGBA() { }

		public override void Execute()
		{
            var inputModifier = GetAnyValidInputModifier();
            var maskData = inputModifier.GetMaskData();
            if (maskData == null)
            {
                Debug.LogError("RGBA::Execute failed, no mask data!");
                return;
            }

            var size = inputModifier.GetSize();

            mHeightData.Clear();
            mHeightData.AddRange(inputModifier.GetHeightData());
            Debug.Assert(mHeightData.Count > 0);

            Helper.Resize(mMaskData, maskData.Count);

            float r, g, b, a;
            for (var i = 0; i < maskData.Count; ++i)
            {
                float totalValue = 0;
                if (GetChannel(0, i, out r))
                {
                    totalValue += r;
                }

                if (GetChannel(1, i, out g))
                {
                    totalValue += g;
                }

                if (GetChannel(2, i, out b))
                {
                    totalValue += b;
                }

                if (GetChannel(3, i, out a))
                {
                    totalValue += a;
                }

                r /= totalValue;
                g /= totalValue;
                b /= totalValue;
                a /= totalValue;

                mMaskData.Add(new Color(r, g, b, a));
            }
        }

		public override string GetName()
        {
            return "RGBA";
        }

		public override ITerrainModifierSetting CreateSetting()
        {
            return new Setting();
        }

		public override void DrawInspector()
        {
            EditorGUILayout.IntField("ID", GetID());
        }

		public override int GetMaxInputCount()
        {
            return 4;
        }

		public override bool HasMaskData() { return true; }

		private bool GetChannel(int channel, int index, out float value)
        {
            value = 0;
            var input = GetPureInputModifier(channel);
            if (input == null)
            {
                return false;
            }
            
            var maskData = input.GetMaskData();
            value = maskData[index][channel];
            return true;
        }
	};
}