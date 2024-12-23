/*
 * Copyright (c) 2024 XDay
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

namespace XDay.SerializationAPI
{
    public class StringTable : ISerializable
    {
        public string TypeName => "StringTable";

        public void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_Version, "StringTable.Version");
            serializer.WriteInt32(m_StringToID.Count, "String Count");
            int index = 0;
            foreach (var p in m_StringToID)
            {
                serializer.WriteInt32(p.Value, "ID");
                serializer.WriteString(p.Key, "String");
                ++index;
            }
        }

        public void EditorDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("StringTable.Version");
            int maxID = 0;
            int strCount = deserializer.ReadInt32("String Count");
            for (int i = 0; i < strCount; ++i)
            {
                var id = deserializer.ReadInt32("ID");
                var str = deserializer.ReadString("String");
                m_StringToID[str] = id;
                m_IDToString[id] = str;
                if (id > maxID)
                {
                    maxID = id;
                }
            }
            m_NextID = maxID;
        }

        public int GetID(string s)
        {
            bool found = m_StringToID.TryGetValue(s, out var id);
            if (found)
            {
                return id;
            }
            id = ++m_NextID;
            m_StringToID[s] = id;
            m_IDToString[id] = s;
            return id;
        }

        public string GetString(int id)
        {
            m_IDToString.TryGetValue(id, out var str);
            return str;
        }

        private static int m_NextID = 0;
        private Dictionary<string, int> m_StringToID = new();
        private Dictionary<int, string> m_IDToString = new();
        private const int m_Version = 1;
    }
}

//XDay