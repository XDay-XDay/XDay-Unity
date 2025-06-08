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

namespace XDay.WorldAPI.Tile.Editor
{
    internal sealed partial class TileObject : WorldObject
    {
        public int MaterialConfigID { get => m_MaterialConfigID; set => m_MaterialConfigID = value; }
        public override Vector3 Position => m_Position;
        public override Quaternion Rotation => m_Rotation;
        public override Vector3 Scale => Vector3.one;
        public ResourceDescriptor ResourceDescriptor => m_ResourceDescriptor.ToObject<ResourceDescriptor>();
        public string AssetPath => m_ResourceDescriptor == null ? null : m_ResourceDescriptor.ToObject<ResourceDescriptor>()?.GetPath(0);
        public override string TypeName => "EditorTileObject";
        public override bool EnablePostInit => true;

        public TileObject() 
        {
        }

        public TileObject(int id, 
            int objectIndex, 
            bool isEnabled, 
            IResourceDescriptor descriptor, 
            WorldObjectVisibility visibility, 
            Vector3 position, 
            Quaternion rotation,
            float[] heights, bool clipped)
            : base(id, objectIndex)
        {
            m_IsEnabled = isEnabled;
            m_ResourceDescriptor = new WorldObjectWeakRef(descriptor);
            m_Visibility = visibility;
            m_Position = position;
            m_Rotation = rotation;

#if ENABLE_CLIP_MASK
            m_Clipped = clipped;
#endif
            m_VertexHeights = heights;
            if (heights != null)
            {
                m_MeshResolution = Mathf.FloorToInt(Mathf.Sqrt(m_VertexHeights.Length)) - 1;
            }
        }

        protected override void OnInit()
        {
            m_ResourceDescriptor.Init(World);
        }

        protected override void OnUninit()
        {
        }

        public override IAspect GetAspect(int objectID, string name)
        {
            var aspect = base.GetAspect(objectID, name);
            if (aspect != null)
            {
                return aspect;
            }

            if (name == TileDefine.TILE_MATERIAL_ID_NAME)
            {
                return IAspect.FromInt32(m_MaterialConfigID);
            }

            if (name.StartsWith(TileDefine.SHADER_PROPERTY_ASPECT_NAME))
            {
                return null;
            }
            else if (name == TileDefine.ENABLE_ASPECT_NAME)
            {
                return IAspect.FromBoolean(EnabledInternal);
            }

            Debug.Assert(false, $"Unknown aspect {name}");
            return null;
        }

        public override bool SetAspect(int objectID, string name, IAspect aspect)
        {
            if (base.SetAspect(objectID, name, aspect))
            {
                return true;
            }
            
            if (name.StartsWith(TileDefine.SHADER_PROPERTY_ASPECT_NAME))
            {
                return true;
            }

            if (name == TileDefine.ENABLE_ASPECT_NAME)
            {
                SetEnabled(aspect.GetBoolean());
                return true;
            }

            if (name == TileDefine.TILE_MATERIAL_ID_NAME)
            {
                MaterialConfigID = aspect.GetInt32();
                return true;
            }

            Debug.Assert(false, $"Unknown aspect {name}");
            return false;
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            var version = deserializer.ReadInt32("TileObject.Version");

            base.EditorDeserialize(deserializer, label);

            m_MaterialConfigID = deserializer.ReadInt32("Material Config ID");
            m_ResourceDescriptor = new WorldObjectWeakRef(deserializer.ReadInt32("Resource Descriptor"));
            m_IsEnabled = deserializer.ReadBoolean("Enabled");
            m_Position = deserializer.ReadVector3("Position");
            m_Rotation = deserializer.ReadQuaternion("Rotation");
            if (version >= 2)
            {
                m_VertexHeights = deserializer.ReadSingleArray("Vertex Heights");
                m_MeshResolution = deserializer.ReadInt32("Mesh Resolution");
            }
        }

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_Version, "TileObject.Version");

            base.EditorSerialize(serializer, label, converter);

            serializer.WriteObjectID(m_MaterialConfigID, "Material Config ID", converter);
            serializer.WriteObjectID(m_ResourceDescriptor.ObjectID, "Resource Descriptor", converter);
            serializer.WriteBoolean(m_IsEnabled, "Enabled");
            serializer.WriteVector3(m_Position, "Position");
            serializer.WriteQuaternion(m_Rotation, "Rotation");
            serializer.WriteSingleArray(m_VertexHeights, "Vertex Heights");
            serializer.WriteInt32(m_MeshResolution, "Mesh Resolution");
        }

        protected override WorldObjectVisibility VisibilityInternal
        {
            set => m_Visibility = value;
            get => m_Visibility;
        }
        protected override bool EnabledInternal
        {
            set => m_IsEnabled = value;
            get => m_IsEnabled;
        }

        private int m_MaterialConfigID = 0;
        private WorldObjectVisibility m_Visibility = WorldObjectVisibility.Undefined;
        private bool m_IsEnabled = true;
        private WorldObjectWeakRef m_ResourceDescriptor;
        private Vector3 m_Position;
        private Quaternion m_Rotation;
        private const int m_Version = 2;
    }
}

//XDay
