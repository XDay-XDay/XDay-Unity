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
