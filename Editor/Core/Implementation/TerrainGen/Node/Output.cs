

using UnityEditor;
using UnityEngine;
using XDay.SerializationAPI;

namespace XDay.Terrain.Editor
{
    /// <summary>
    /// default output
    /// </summary>
    class Output : OutputBase
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

        public Output(int id) : base(id) { }
        public Output() { }

        public override Material GetMaterial()
        {
            return mGenerator.GetDefaultOutputMaterial();
        }

        public override string GetName()
        {
            return "Output";
        }

        public override ITerrainModifierSetting CreateSetting()
        {
            return new Setting();
        }

        public override void DrawInspector()
        {
            EditorGUILayout.IntField("ID", GetID());
        }

        public override TerrainModifier GetInputModifier(int index)
        {
            Debug.Assert(false, "can't be here!");
            return null;
        }
    };
}