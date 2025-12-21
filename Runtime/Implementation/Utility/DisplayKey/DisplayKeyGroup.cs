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

namespace XDay.DisplayKeyAPI
{
    public class DisplayKeyGroup
    {
        public string Name { get => m_Name; set => m_Name = value; }
        public List<DisplayKey> Keys => m_DisplayKeys;
        public bool Show { get => m_Show; set => m_Show = value; }

        public DisplayKeyGroup(DisplayKeyManager manager, string name = "")
        {
            m_Manager = manager;
            m_Name = name;
        }

        public void AddKey(DisplayKey key)
        {
            m_DisplayKeys.Add(key);

            m_Manager.AddKey(key);
        }

        public DisplayKey GetKey(int id)
        {
            foreach (var key in m_DisplayKeys)
            {
                if (key.ID == id)
                {
                    return key;
                }
            }
            Log.Instance?.Error($"Display key id {id} not found!");
            return null;
        }

#if UNITY_EDITOR
        public void Save(ISerializer serializer)
        {
            serializer.WriteInt32(m_EditorVersion, "Version");

            serializer.WriteString(m_Name, "Name");
            serializer.WriteBoolean(m_Show, "Show");

            serializer.WriteList(m_DisplayKeys, "Display Keys", (key, index) => {
                serializer.WriteStructure($"Display Key {index}", () => {
                    key.Save(serializer);
                });
            });
        }

        public void Load(IDeserializer deserializer)
        {
            var version = deserializer.ReadInt32("Version");

            if (version >= 2)
            {
                m_Name = deserializer.ReadString("Name");
                m_Show = deserializer.ReadBoolean("Show");
            }

            m_DisplayKeys = deserializer.ReadList("Display Keys", (index) => {
                var key = new DisplayKey();
                deserializer.ReadStructure($"Display Key {index}", () => {
                    key.Load(deserializer);
                });
                return key;
            });
        }

        public void Export(ISerializer serializer)
        {
            serializer.WriteInt32(m_RuntimeVersion, "Version");

            serializer.WriteString(m_Name, "Name");

            serializer.WriteList(m_DisplayKeys, "Display Keys", (key, index) => {
                serializer.WriteStructure($"Display Key {index}", () => {
                    key.Export(serializer, m_Manager.GetCustomDataTranslator());
                });
            });
        }
#endif

        public void Import(IDeserializer deserializer)
        {
            deserializer.ReadInt32("Version");

            m_Name = deserializer.ReadString("Name");

            m_DisplayKeys = deserializer.ReadList("Display Keys", (index) => {
                var key = new DisplayKey();
                deserializer.ReadStructure($"Display Key {index}", () => {
                    key.Import(deserializer);
                });
                return key;
            });
        }

        private List<DisplayKey> m_DisplayKeys = new();
        private string m_Name;
        private bool m_Show = true;
        private DisplayKeyManager m_Manager;
        private const int m_EditorVersion = 2;
        private const int m_RuntimeVersion = 1;
    }
}