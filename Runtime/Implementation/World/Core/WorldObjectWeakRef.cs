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

namespace XDay.WorldAPI
{
    [XDaySerializableClass("World Object Weak Ref")]
    public partial class WorldObjectWeakRef : ISerializable
    {
        public int ObjectID => m_ObjectID;
        public string TypeName => "WorldObjectWeakRef";

        public WorldObjectWeakRef()
        {
        }

        public WorldObjectWeakRef(IWorldObject obj)
        {
            m_World = obj.World;
            m_ObjectID = obj.ID;
        }

        public WorldObjectWeakRef(int objectID)
        {
            m_ObjectID = objectID;
        }

        public void Init(IWorld world)
        {
            m_World = world;
        }

        public void GameDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("WorldObjectWeakRef.Version");

            m_ObjectID = deserializer.ReadInt32("Object ID");
        }

        public void GameSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_RuntimeVersion, "WorldObjectWeakRef.Version");

            serializer.WriteObjectID(m_ObjectID, "Object ID", converter);
        }

        public void EditorDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("WorldObjectWeakRef.Version");

            m_ObjectID = deserializer.ReadInt32("Object ID");
        }

        public void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_RuntimeVersion, "WorldObjectWeakRef.Version");

            serializer.WriteObjectID(m_ObjectID, "Object ID", converter);
        }

        public T ToObject<T>() where T : class, IWorldObject
        {
            var obj = m_World.QueryObject<T>(m_ObjectID);
            if (obj == null)
            {
                Debug.LogError("Invalid ref");
            }
            return obj;
        }

        public void FromObject<T>(T obj) where T : IWorldObject
        {
            m_ObjectID = obj != null ? obj.ID : 0;
        }

        [XDaySerializableField(1, "Object ID")]
        private int m_ObjectID;
        private IWorld m_World;
        private const int m_Version = 1;
        private const int m_RuntimeVersion = 1;
    }
}

//XDay