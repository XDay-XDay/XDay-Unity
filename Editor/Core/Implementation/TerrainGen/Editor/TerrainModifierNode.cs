using UnityEngine;
using XDay.Node.Editor;
using XDay.SerializationAPI;

namespace XDay.Terrain.Editor
{
    class TerrainModifierNode : XNode
    {
        public TerrainModifierNode(int modifierID)
        {
            mModifierID = modifierID;
        }

        public TerrainModifierNode()
        {
        }

        public override void OnDestroy()
        {
        }

        public override int GetID()
        {
            return GetModifier().GetID();
        }

        public TerrainModifier GetModifier()
        {
            var terrainGenerator = mUserData as TerrainGenerator;
            return terrainGenerator.GetModifier(mModifierID);
        }

        public override void OnDraw()
        {
        }

        public override string GetTitle()
        {
            return GetModifier().GetName();
        }

        public override int GetRequiredInputCount()
        {
            return GetModifier().GetMaxInputCount();
        }

        public override int GetRequiredOutputCount()
        {
            return GetModifier().GetMaxOutputCount();
        }

        public override bool CanDelete()
        {
            return GetModifier().CanDelete();
        }

        public override bool IsVisible()
        {
            return GetModifier().IsVisible();
        }

        public override Color GetColor()
        {
            return GetModifier().GetDisplayColor();
        }

        public override void Save(ISerializer writer, IObjectIDConverter translator)
        {
            base.Save(writer, translator);

            writer.WriteInt32(mVersion, "TerrainModifierNode.Version");
            writer.WriteInt32(translator.Convert(mModifierID), "ID");
        }

        public override void Load(IDeserializer reader)
        {
            base.Load(reader);

            reader.ReadInt32("TerrainModifierNode.Version");
            mModifierID = reader.ReadInt32("ID");
        }

        private int mModifierID = 0;
        private const int mVersion = 1;
    };
}
