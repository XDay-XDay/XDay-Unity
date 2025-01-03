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



using XDay.SerializationAPI;
using XDay.UtilityAPI;
using UnityEngine;

namespace XDay.WorldAPI
{
    [XDaySerializableClass("World Object")]
    public abstract partial class WorldObject : IWorldObject
    {
        public virtual bool IsActive => EnabledInternal && VisibilityInternal == WorldObjectVisibility.Visible;
        public int ID => m_ID;
        public int ObjectIndex => m_Index;
        public int WorldID => m_World.ID;
        public IWorld World => m_World;
        public virtual Vector3 Position { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public virtual Vector3 Scale { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public virtual Quaternion Rotation { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public abstract string TypeName { get; }
        public virtual string GameTypeName => TypeName;

        public WorldObject()
        {
        }

        public WorldObject(int id, int index)
        {
            m_ID = id;
            m_Index = index;
        }

        public virtual void Init(IWorld world)
        {
            if (m_ID != 0)
            {
                m_World = world as World;
                m_World.RegisterObject(this);
            }
        }

        public virtual void InitNewID(IWorld world, int id, int index)
        {
            m_ID = id;
            m_Index = index;
            Init(world);
        }

        public virtual void Uninit()
        {
            if (m_ID != 0)
            {
                m_World?.UnregisterObject(m_ID);
            }
        }

        public virtual bool SetAspect(int objectID, string name, IAspect aspect)
        {
            return false;
        }

        public virtual IAspect GetAspect(int objectID, string name)
        {
            return null;
        }

        public virtual void GameSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_RuntimeVersion, "WorldObject.Version");

            serializer.WriteObjectID(m_ID, "ID", converter);
            serializer.WriteInt32(m_Index, "Index");
        }

        public virtual void GameDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("WorldObject.Version");

            m_ID = deserializer.ReadInt32("ID");
            m_Index = deserializer.ReadInt32("Index");
        }

        public virtual void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_Version, "WorldObject.Version");

            serializer.WriteObjectID(m_ID, "ID", converter);
            serializer.WriteInt32(m_Index, "Index");
        }

        public virtual void EditorDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("WorldObject.Version");

            m_ID = deserializer.ReadInt32("ID");
            m_Index = deserializer.ReadInt32("Index");
        }

        public bool SetVisibility(WorldObjectVisibility visibility)
        {
            if (VisibilityInternal != visibility)
            {
                var oldState = IsActive;
                VisibilityInternal = visibility;
                return oldState != IsActive;
            }

            return false;
        }

        public WorldObjectVisibility GetVisibility()
        {
            return VisibilityInternal;
        }

        public bool SetEnabled(bool enabled)
        {
            if (EnabledInternal != enabled)
            {
                var oldState = IsActive;
                EnabledInternal = enabled;
                return oldState != IsActive;
            }

            return false;
        }

        public bool IsEnabled()
        {
            return EnabledInternal;
        }

        protected virtual WorldObjectVisibility VisibilityInternal
        {
            set => throw new System.NotImplementedException();
            get => throw new System.NotImplementedException();
        }
        protected virtual bool EnabledInternal
        {
            set => throw new System.NotImplementedException();
            get => throw new System.NotImplementedException();
        }

        [XDaySerializableField(1, "ID")]
        protected int m_ID;
        [XDaySerializableField(1, "Index")]
        protected int m_Index;
        private World m_World;
        private const int m_Version = 1;
        private const int m_RuntimeVersion = 1;
    }
}

//XDay