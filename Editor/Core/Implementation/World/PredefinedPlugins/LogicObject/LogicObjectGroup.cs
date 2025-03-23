


using System.Collections.Generic;
using UnityEngine;

namespace XDay.WorldAPI.LogicObject.Editor
{
    internal class LogicObjectGroup : WorldObject
    {
        protected override WorldObjectVisibility VisibilityInternal
        {
            set { }
            get => WorldObjectVisibility.Visible;
        }
        protected override bool EnabledInternal { get => m_IsActiveGroup && m_Visible; set => m_IsActiveGroup = value; }
        public bool Visible { get => m_Visible; set => m_Visible = value; }
        public override string TypeName => "EditorLogicObjectGroup";
        public int ObjectCount => m_Objects.Count;
        public List<LogicObject> Objects => m_Objects;
        public string Name { get => m_Name; set => m_Name = value; }

        public LogicObjectGroup() { }

        public LogicObjectGroup(int id, int index, string name) 
            : base(id, index)
        {
            m_ID = id;
            m_Index = index;
            m_Name = name;
        }

        public override void Init(IWorld world)
        {
            base.Init(world);

            foreach (var obj in m_Objects)
            {
                obj.Init(World);
            }
        }

        public override void Uninit()
        {
            base.Uninit();

            foreach (var obj in m_Objects)
            {
                obj.Uninit();
            }
        }

        public void AddObject(LogicObject obj, int objectIndex)
        {
            m_Objects.Add(obj);
        }

        public bool DestroyObject(int objectID)
        {
            for (var i = 0; i < m_Objects.Count; ++i)
            {
                if (m_Objects[i].ID == objectID)
                {
                    m_Objects[i].Uninit();
                    m_Objects.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_EditorVersion, "LogicObjectGroup.Version");

            base.EditorSerialize(serializer, label, converter);

            serializer.WriteList(m_Objects, "Objects", (obj, index) =>
            {
                serializer.WriteSerializable(obj, $"Object {index}", converter, false);
            });

            serializer.WriteString(m_Name, "Name");
            serializer.WriteBoolean(m_Visible, "Visible");
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("LogicObjectGroup.Version");

            base.EditorDeserialize(deserializer, label);

            m_Objects = deserializer.ReadList("Objects", (index) =>
            {
                return deserializer.ReadSerializable<LogicObject>($"Object {index}", false);
            });

            m_Name = deserializer.ReadString("Name");
            m_Visible = deserializer.ReadBoolean("Visible");
        }

        public override bool SetAspect(int objectID, string name, IAspect aspect)
        {
            if (base.SetAspect(objectID, name, aspect))
            {
                return true;
            }

            if (name == LogicObjectDefine.CHANGE_LOGIC_OBJECT_GROUP_NAME)
            {
                m_Name = aspect.GetString();
                return true;
            }

            if (name == LogicObjectDefine.CHANGE_LOGIC_OBJECT_GROUP_VISIBILITY)
            {
                m_Visible = aspect.GetBoolean();
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

            if (name == LogicObjectDefine.CHANGE_LOGIC_OBJECT_GROUP_NAME)
            {
                return IAspect.FromString(m_Name);
            }

            if (name == LogicObjectDefine.CHANGE_LOGIC_OBJECT_GROUP_VISIBILITY)
            {
                return IAspect.FromBoolean(m_Visible);
            }

            Debug.Assert(false, $"Unknown aspect {name}");
            return null;
        }

        private List<LogicObject> m_Objects = new();
        private const int m_EditorVersion = 1;
        private string m_Name;
        private bool m_IsActiveGroup = false;
        private bool m_Visible = true;
    }
}
