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

using System;
using System.IO;

namespace XDay.WorldAPI.Editor
{
    internal class UndoSerializer
    {
        public UndoSerializer(ISerializableFactory serializableFactory, EditorWorldManager worldSystem)
        {
            m_SerializableFactory = serializableFactory;
            m_WorldSystem = worldSystem;
        }

        public byte[] Serialize(IWorldObject obj)
        {
            var serializer = ISerializer.CreateBinary();
            var worldObj = obj as WorldObject;
            worldObj.EditorSerialize(serializer, "", new PassID());
            serializer.Uninit();
            return serializer.Data;
        }

        public IWorldObject Deserialize(int worldID, string type, byte[] data, int id, int objectIndex, bool callInit)
        {
            var deserializer = IDeserializer.CreateBinary(new MemoryStream(data), m_SerializableFactory);
            var obj = Activator.CreateInstance(Type.GetType(type)) as WorldObject;
            obj.EditorDeserialize(deserializer, "");
            if (id != 0)
            {
                obj.ID = id;
                obj.ObjectIndex = objectIndex;
            }
            if (callInit)
            {
                var world = m_WorldSystem.QueryWorld(worldID);
                obj.Init(world);
            }
            return obj;
        }

        private EditorWorldManager m_WorldSystem;
        private ISerializableFactory m_SerializableFactory;
    }
}

//XDay