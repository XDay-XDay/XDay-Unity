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

namespace XDay.WorldAPI.Region.Editor
{
    public class RegionObject : WorldObject
    {
        internal RegionSystemLayer Layer => World.QueryObject<RegionSystemLayer>(m_RegionLayerID);
        public string Name { get => m_Name; set => m_Name = value; }
        public Color Color => m_Color;
        public IAspectContainer AspectContainer => m_AspectContainer;
        public override string TypeName => "EditorRegionObject";
        protected override WorldObjectVisibility VisibilityInternal
        {
            set { }
            get => WorldObjectVisibility.Visible;
        }
        protected override bool EnabledInternal
        {
            set => m_Enabled = value;
            get => m_Enabled;
        }

        public RegionObject()
        {
        }

        public RegionObject(int id, int index, int regionLayerID)
            : base(id, index)
        {
            m_RegionLayerID = regionLayerID;
        }

        protected override void OnInit()
        {
        }

        protected override void OnUninit()
        {
        }

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            base.GameSerialize(serializer, label, converter);
            serializer.WriteInt32(m_Version, "RegionObject.Version");
            serializer.WriteObjectID(m_RegionLayerID, "Region System Layer ID", converter);
            serializer.WriteBoolean(m_Enabled, "Is Enabled");
            serializer.WriteString(m_Name, "Name");
            serializer.WriteColor(m_Color, "Color");
            serializer.WriteStructure("Aspect Container", () =>
            {
                m_AspectContainer.Serialize(serializer);
            });
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            base.EditorDeserialize(deserializer, label);
            var version = deserializer.ReadInt32("RegionObject.Version");
            m_RegionLayerID = deserializer.ReadInt32("Region System Layer ID");
            m_Enabled = deserializer.ReadBoolean("Is Enabled");
            m_Name = deserializer.ReadString("Name");
            m_Color = deserializer.ReadColor("Color");
            var vertices = deserializer.ReadVector3List("Vertices");
            deserializer.ReadStructure("Aspect Container", () =>
            {
                m_AspectContainer = IAspectContainer.Create();
                m_AspectContainer.Deserialize(deserializer);
            });
        }

        public override bool SetAspect(int objectID, string name, IAspect aspect)
        {
            if (base.SetAspect(objectID, name, aspect))
            {
                return true;
            }

            if (name == RegionDefine.ENABLE_REGION_NAME)
            {
                SetEnabled(aspect.GetBoolean());
                return true;
            }

            if (name == RegionDefine.REGION_NAME)
            {
                m_Name = aspect.GetString();
                return true;
            }

            if (name == RegionDefine.COLOR_NAME)
            {
                m_Color = aspect.GetColor();
                return true;
            }

            return false;
        }

        public override IAspect GetAspect(int objectID, string name)
        {
            var aspect = base.GetAspect(objectID, name);
            if (aspect != null)
            {
                return aspect;
            }

            if (name == RegionDefine.ENABLE_REGION_NAME)
            {
                return IAspect.FromBoolean(m_Enabled);
            }

            if (name == RegionDefine.REGION_NAME)
            {
                return IAspect.FromString(m_Name);
            }

            if (name == RegionDefine.COLOR_NAME)
            {
                return IAspect.FromColor(Color);
            }

            Debug.Assert(false, $"Unknown aspect {name}");
            return null;
        }

        [SerializeField]
        private bool m_Enabled = true;
        [SerializeField]
        private string m_Name = "Region";
        [SerializeField]
        private IAspectContainer m_AspectContainer = IAspectContainer.Create();
        [SerializeField]
        private Color m_Color = Color.white;
        [SerializeField]
        private int m_RegionLayerID;
        private const int m_Version = 1;
    }
}