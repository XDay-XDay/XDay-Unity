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
        public override Vector3 Scale => m_Scale;
        public override Quaternion Rotation => m_Rotation;
        public override Vector3 Position => m_Position;
        public override string TypeName => "EditorLogicObject";
        public string Name { get => m_Name; set => m_Name = value; }

        public LogicObject()
        {
        }

        public LogicObject(int objectID, int objectIndex, Vector3 position, Quaternion rotation, Vector3 scale, IResourceDescriptor descriptor)
            : base(objectID, objectIndex)
        {
            m_Position = position;
            m_Rotation = rotation;
            m_Scale = scale;
            m_ResourceDescriptor = new WorldObjectWeakRef(descriptor);
            m_Name = descriptor != null ? Helper.GetPathName(descriptor.GetPath(0),false) : "";
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

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            base.EditorDeserialize(deserializer, label);
            var version = deserializer.ReadInt32("LogicObject.Version");
            m_Enabled = deserializer.ReadBoolean("Is Enabled");
            m_Position = deserializer.ReadVector3("Position");
            m_Rotation = deserializer.ReadQuaternion("Rotation");
            m_Scale = deserializer.ReadVector3("Scale");
            m_ResourceDescriptor = new WorldObjectWeakRef(deserializer.ReadInt32("Resource Descriptor"));
            m_Name = deserializer.ReadString("Name");
        }

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            base.GameSerialize(serializer, label, converter);
            serializer.WriteInt32(m_Version, "LogicObject.Version");
            serializer.WriteBoolean(m_Enabled, "Is Enabled");
            serializer.WriteVector3(m_Position, "Position");
            serializer.WriteQuaternion(m_Rotation, "Rotation");
            serializer.WriteVector3(m_Scale, "Scale");
            serializer.WriteObjectID(m_ResourceDescriptor.ObjectID, "Resource Descriptor", converter);
            serializer.WriteString(m_Name, "Name");
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
                m_Rotation = aspect.GetQuaternion();
                return true;
            }

            if (name == LogicObjectDefine.SCALE_NAME)
            {
                m_Scale = aspect.GetVector3();
                return true;
            }

            if (name == LogicObjectDefine.POSITION_NAME)
            {
                m_Position = aspect.GetVector3();
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
        private Vector3 m_Position;
        private Quaternion m_Rotation;
        private Vector3 m_Scale;
        private string m_Name;
        private const int m_Version = 1;
    }
}
