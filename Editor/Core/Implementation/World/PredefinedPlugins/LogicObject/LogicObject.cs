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
using XDay.UtilityAPI;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.LogicObject.Editor
{
    internal class LogicObject : WorldObject
    {
        public IEditorResourceDescriptor ResourceDescriptor => m_ResourceDescriptor.ToObject<IEditorResourceDescriptor>();
        public override Vector3 Scale => m_EditScale.Mult(PrefabScale);
        public override Quaternion Rotation => m_EditRotation * PrefabRotation;
        public override Vector3 Position => m_EditPosition + PrefabPosition;
        public Vector3 PrefabScale => (ResourceDescriptor != null && ResourceDescriptor.Prefab != null) ? ResourceDescriptor.Prefab.transform.localScale : Vector3.one;
        public Quaternion PrefabRotation => (ResourceDescriptor != null && ResourceDescriptor.Prefab != null) ? ResourceDescriptor.Prefab.transform.rotation : Quaternion.identity;
        public Vector3 PrefabPosition => (ResourceDescriptor != null && ResourceDescriptor.Prefab != null) ? ResourceDescriptor.Prefab.transform.position : Vector3.zero;
        public override string TypeName => "EditorLogicObject";
        public string Name { get => m_Name; set => m_Name = value; }
        public LogicObjectGroup Group => World.QueryObject<LogicObjectGroup>(m_Group.ObjectID);

        public LogicObject()
        {
        }

        public LogicObject(int objectID, int objectIndex, Vector3 editPosition, Quaternion editRotation, Vector3 editScale, IResourceDescriptor descriptor, LogicObjectGroup group)
            : base(objectID, objectIndex)
        {
            m_EditPosition = editPosition;
            m_EditRotation = editRotation;
            m_EditScale = editScale;
            m_ResourceDescriptor = new WorldObjectWeakRef(descriptor);
            m_Name = descriptor != null ? Helper.GetPathName(descriptor.GetPath(0),false) : "";
            m_Group = new(group);
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
            m_Group = null;
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            base.EditorDeserialize(deserializer, label);
            var version = deserializer.ReadInt32("LogicObject.Version");
            m_Enabled = deserializer.ReadBoolean("Is Enabled");
            m_EditPosition = deserializer.ReadVector3("Edit Position");
            m_EditRotation = deserializer.ReadQuaternion("Edit Rotation");
            m_EditScale = deserializer.ReadVector3("Edit Scale");
            m_ResourceDescriptor = new WorldObjectWeakRef(deserializer.ReadInt32("Resource Descriptor"));
            m_Name = deserializer.ReadString("Name");
            if (version >= 2)
            {
                m_Group = new WorldObjectWeakRef(deserializer.ReadInt32("Group"));
            }
        }

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            base.GameSerialize(serializer, label, converter);
            serializer.WriteInt32(m_Version, "LogicObject.Version");
            serializer.WriteBoolean(m_Enabled, "Is Enabled");
            serializer.WriteVector3(m_EditPosition, "Edit Position");
            serializer.WriteQuaternion(m_EditRotation, "Edit Rotation");
            serializer.WriteVector3(m_EditScale, "Edit Scale");
            serializer.WriteObjectID(m_ResourceDescriptor.ObjectID, "Resource Descriptor", converter);
            serializer.WriteString(m_Name, "Name");
            serializer.WriteObjectID(m_Group.ObjectID, "Group", converter);
        }

        public override bool SetAspect(int objectID, string name, IAspect aspect)
        {
            if (base.SetAspect(objectID, name, aspect))
            {
                return true;
            }

            if (name == LogicObjectDefine.ENABLE_LOGIC_OBJECT_NAME)
            {
                SetEnabled(aspect.GetBoolean());
                return true;
            }

            if (name == LogicObjectDefine.ROTATION_NAME)
            {
                m_EditRotation = aspect.GetQuaternion() * Quaternion.Inverse(PrefabRotation);
                return true;
            }

            if (name == LogicObjectDefine.SCALE_NAME)
            {
                m_EditScale = aspect.GetVector3().Div(PrefabScale);
                return true;
            }

            if (name == LogicObjectDefine.POSITION_NAME)
            {
                m_EditPosition = aspect.GetVector3() - PrefabPosition;
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

            if (name == LogicObjectDefine.ENABLE_LOGIC_OBJECT_NAME)
            {
                return IAspect.FromBoolean(m_Enabled);
            }

            if (name == LogicObjectDefine.ROTATION_NAME)
            {
                return IAspect.FromQuaternion(Rotation);
            }

            if (name == LogicObjectDefine.SCALE_NAME)
            {
                return IAspect.FromVector3(Scale);
            }

            if (name == LogicObjectDefine.POSITION_NAME)
            {
                return IAspect.FromVector3(Position);
            }

            Debug.Assert(false, $"Unknown aspect {name}");
            return null;
        }

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

        private bool m_Enabled = true;
        private WorldObjectWeakRef m_ResourceDescriptor;
        private Vector3 m_EditPosition;
        private Quaternion m_EditRotation;
        private Vector3 m_EditScale;
        private string m_Name;
        private const int m_Version = 2;
        private WorldObjectWeakRef m_Group;
    }
}
