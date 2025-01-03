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
using XDay.SerializationAPI;
using XDay.UtilityAPI;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Decoration.Editor
{
    internal class DecorationObject : WorldObject
    {
        public LODLayerMask LODLayerMask => m_LODLayerMask;
        public int RuntimeObjectIndex { get; set; }
        public IEditorResourceDescriptor ResourceDescriptor => m_ResourceDescriptor.ToObject<IEditorResourceDescriptor>();
        public override Vector3 Scale => m_OverridePrefabTransform ? m_EditScale : m_EditScale.Mult(PrefabScale);
        public override Quaternion Rotation => m_OverridePrefabTransform ? m_EditRotation : m_EditRotation * PrefabRotation;
        public override Vector3 Position => m_OverridePrefabTransform ? m_EditPosition : m_EditPosition + PrefabPosition;
        public Vector3 PrefabScale => ResourceDescriptor.Prefab != null ? ResourceDescriptor.Prefab.transform.localScale : Vector3.one;
        public Quaternion PrefabRotation => ResourceDescriptor.Prefab != null ? ResourceDescriptor.Prefab.transform.rotation : Quaternion.identity;
        public Vector3 PrefabPosition => ResourceDescriptor.Prefab != null ? ResourceDescriptor.Prefab.transform.position : Vector3.zero;
        public override string TypeName => "EditorDecorationObject";

        public DecorationObject()
        {
        }

        public DecorationObject(int objectID, int objectIndex, bool overridePrefabTransform, LODLayerMask lodLayerMask, Vector3 editPosition, Quaternion editRotation, Vector3 editScale, IResourceDescriptor descriptor)
            : base(objectID, objectIndex)
        {
            m_OverridePrefabTransform = overridePrefabTransform;
            m_LODLayerMask = lodLayerMask;
            m_EditPosition = editPosition;
            m_EditRotation = editRotation;
            m_EditScale = editScale;
            m_ResourceDescriptor = new WorldObjectWeakRef(descriptor);
        }

        public override void Init(IWorld world)
        {
            Debug.Assert(ID != 0);
            
            base.Init(world);

            m_ResourceDescriptor.Init(world);
        }

        public override void Uninit()
        {
            Debug.Assert(ID != 0);
            
            base.Uninit();
            
            m_ResourceDescriptor = null;
        }

        public Rect QueryWorldBounds()
        {
            if (m_BoundsDirty)
            {
                m_WorldBounds = ResourceDescriptor.Prefab.QueryRectWithLocalTransform(Position, Rotation, Scale);
                m_BoundsDirty = false;
            }
            return m_WorldBounds;   
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            base.EditorDeserialize(deserializer, label);
            deserializer.ReadInt32("DecorationObject.Version");
            m_Enabled = deserializer.ReadBoolean("Is Enabled");
            m_LODLayerMask = (LODLayerMask)deserializer.ReadUInt32("LOD Layer Mask");
            m_EditPosition = deserializer.ReadVector3("Edit Position");
            m_EditRotation = deserializer.ReadQuaternion("Edit Rotation");
            m_EditScale = deserializer.ReadVector3("Edit Scale");
            m_ResourceDescriptor = new WorldObjectWeakRef(deserializer.ReadInt32("Resource Descriptor"));
        }

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            base.GameSerialize(serializer, label, converter);
            serializer.WriteInt32(m_Version, "DecorationObject.Version");
            serializer.WriteBoolean(m_Enabled, "Is Enabled");
            serializer.WriteUInt32((uint)m_LODLayerMask, "LOD Layer Mask");
            serializer.WriteVector3(m_EditPosition, "Edit Position");
            serializer.WriteQuaternion(m_EditRotation, "Edit Rotation");
            serializer.WriteVector3(m_EditScale, "Edit Scale");
            serializer.WriteObjectID(m_ResourceDescriptor.ObjectID, "Resource Descriptor", converter);
        }

        public override bool SetAspect(int objectID, string name, IAspect aspect)
        {
            if (base.SetAspect(objectID, name, aspect))
            {
                return true;
            }

            if (name == DecorationDefine.ENABLE_DECORATION_NAME)
            {
                SetEnabled(aspect.GetBoolean());
                return true;
            }

            if (name == DecorationDefine.ROTATION_NAME)
            {
                m_BoundsDirty = true;
                m_EditRotation = aspect.GetQuaternion() * Quaternion.Inverse(PrefabRotation);
                return true;
            }

            if (name == DecorationDefine.SCALE_NAME)
            {
                m_BoundsDirty = true;
                m_EditScale = aspect.GetVector3().Div(PrefabScale);
                return true;
            }

            if (name == DecorationDefine.POSITION_NAME)
            {
                m_BoundsDirty = true;
                m_EditPosition = aspect.GetVector3() - PrefabPosition;
                return true;
            }

            if (name == DecorationDefine.LOD_LAYER_MASK_NAME)
            {
                m_LODLayerMask = aspect.GetEnum<LODLayerMask>();
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

            if (name == DecorationDefine.ENABLE_DECORATION_NAME)
            {
                return IAspect.FromBoolean(m_Enabled);
            }

            if (name == DecorationDefine.ROTATION_NAME)
            {
                return IAspect.FromQuaternion(Rotation);
            }

            if (name == DecorationDefine.SCALE_NAME)
            {
                return IAspect.FromVector3(Scale);
            }

            if (name == DecorationDefine.POSITION_NAME)
            {
                return IAspect.FromVector3(Position);
            }

            if (name == DecorationDefine.LOD_LAYER_MASK_NAME)
            {
                return IAspect.FromEnum(m_LODLayerMask);
            }

            Debug.Assert(false, $"Unknown aspect {name}");
            return null;
        }

        internal bool ExistsInLOD(int lod)
        {
            if (lod < 0)
            {
                return false;
            }
            return (m_LODLayerMask & (LODLayerMask)(1 << lod)) != LODLayerMask.None;
        }

        internal LODLayerMask AddLODLayer(int lod)
        {
            return m_LODLayerMask | (LODLayerMask)(1 << lod);
        }

        internal LODLayerMask RemoveLODLayer(int lod)
        {
            return m_LODLayerMask & (LODLayerMask)~(1 << lod);
        }

        protected override WorldObjectVisibility VisibilityInternal
        {
            set => m_Visibility = value;
            get => m_Visibility;
        }

        protected override bool EnabledInternal
        {
            set => m_Enabled = value;
            get => m_Enabled;
        }

        private bool m_BoundsDirty = true;
        private Rect m_WorldBounds = new();
        private LODLayerMask m_LODLayerMask = LODLayerMask.AllLOD;
        private bool m_Enabled = true;
        private WorldObjectVisibility m_Visibility = WorldObjectVisibility.Undefined;
        private bool m_OverridePrefabTransform = false;
        private WorldObjectWeakRef m_ResourceDescriptor;
        private Vector3 m_EditPosition;
        private Quaternion m_EditRotation;
        private Vector3 m_EditScale;
        private const int m_Version = 1;
    }
    
}

//XDay