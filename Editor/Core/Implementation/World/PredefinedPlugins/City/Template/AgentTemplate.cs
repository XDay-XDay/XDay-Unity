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
using UnityEditor;

namespace XDay.WorldAPI.City.Editor
{
    class AgentTemplate
    {
        public AgentTemplate()
        {
        }

        public AgentTemplate(int id)
        {
            m_ID = id;
            m_Size = Vector2Int.one;
        }

        public void Save(ISerializer writer, IObjectIDConverter converter)
        {
            writer.WriteInt32(m_Version, "AgentTemplate.Version");

            writer.WriteObjectID(m_ID, "ID", converter);
            writer.WriteInt32(m_Type, "Type");
            writer.WriteString(m_Name, "Name");
            writer.WriteVector2Int(m_Size, "Size");
            writer.WriteBoolean(m_ShowInInspector, "Show In Inspector");
            writer.WriteString(m_PrefabGUID, "Prefab GUID");
        }

        public void Load(IDeserializer reader)
        {
            reader.ReadInt32("AgentTemplate.Version");

            m_ID = reader.ReadInt32("ID");
            m_Type = reader.ReadInt32("Type");
            m_Name = reader.ReadString("Name");
            m_Size = reader.ReadVector2Int("Size");
            m_ShowInInspector = reader.ReadBoolean("Show In Inspector");
            m_PrefabGUID = reader.ReadString("Prefab GUID");
        }

        public int ID { get => m_ID; set => m_ID = value; }
        public string Name { get => m_Name; set => m_Name = value; }
        public int Type { get => m_Type; set => m_Type = value; }
        public bool ShowInInspector { get => m_ShowInInspector; set => m_ShowInInspector = value; }
        public Vector2Int Size { get => m_Size; set => m_Size = value; }
        public GameObject Prefab
        {
            get
            {
                if (m_Prefab == null)
                {
                    var path = AssetDatabase.GUIDToAssetPath(m_PrefabGUID);
                    m_Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }

                return m_Prefab;
            }
            set
            {
                m_Prefab = value;
                m_PrefabGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_Prefab));
            }
        }

        [SerializeField]
        int m_ID;

        [SerializeField]
        int m_Type;

        [SerializeField]
        string m_Name;

        [SerializeField]
        Vector2Int m_Size;

        [SerializeField]
        bool m_ShowInInspector = true;

        GameObject m_Prefab;

        [SerializeField]
        string m_PrefabGUID;

        const int m_Version = 1;
    }
}