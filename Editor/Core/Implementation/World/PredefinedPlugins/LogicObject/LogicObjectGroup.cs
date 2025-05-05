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


using System.Collections.Generic;
using UnityEngine;

namespace XDay.WorldAPI.LogicObject.Editor
{
    public class LogicObjectGroup : WorldObject
    {
        protected override WorldObjectVisibility VisibilityInternal
        {
            set { }
            get => WorldObjectVisibility.Visible;
        }
        protected override bool EnabledInternal { get => m_Visible; set => m_Visible = value; }
        public bool Visible { get => m_Visible; set => m_Visible = value; }
        public override string TypeName => "EditorLogicObjectGroup";
        public int ObjectCount => m_Objects.Count;
        public List<LogicObject> Objects => m_Objects;
        public string Name { get => m_Name; set => m_Name = value; }
        public IAspectContainer AspectContainer => m_AspectContainer;

        public LogicObjectGroup() { }

        public LogicObjectGroup(int id, int index, string name) 
            : base(id, index)
        {
            m_ID = id;
            m_Index = index;
            m_Name = name;
        }

        protected override void OnInit()
        {
            m_AspectContainer ??= IAspectContainer.Create();

            foreach (var obj in m_Objects)
            {
                obj.Init(World);
            }
        }

        protected override void OnUninit()
        {
            foreach (var obj in m_Objects)
            {
                obj.Uninit();
            }
        }

        internal void AddObject(LogicObject obj, int objectIndex)
        {
            m_Objects.Add(obj);
        }

        internal void RemoveObject(LogicObject obj)
        {
            m_Objects.Remove(obj);
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

            serializer.WriteStructure("Aspect Container", () =>
            {
                m_AspectContainer.Serialize(serializer);
            });
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            var version = deserializer.ReadInt32("LogicObjectGroup.Version");

            base.EditorDeserialize(deserializer, label);

            m_Objects = deserializer.ReadList("Objects", (index) =>
            {
                return deserializer.ReadSerializable<LogicObject>($"Object {index}", false);
            });

            m_Name = deserializer.ReadString("Name");
            m_Visible = deserializer.ReadBoolean("Visible");

            if (version >= 2)
            {
                deserializer.ReadStructure("Aspect Container", () =>
                {
                    m_AspectContainer = IAspectContainer.Create();
                    m_AspectContainer.Deserialize(deserializer);
                });
            }
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
        private const int m_EditorVersion = 2;
        private string m_Name;
        private bool m_Visible = true;
        private IAspectContainer m_AspectContainer = IAspectContainer.Create();
    }
}
