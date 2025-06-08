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

using UnityEditor;
using UnityEngine;

namespace XDay.WorldAPI.City.Editor
{
    internal abstract class RoomPrefabBase
    {
        public int ID { get => m_ID; set => m_ID = value; }  
        public bool ShowInInspector { get => m_ShowInInspector; set => m_ShowInInspector = value; }
        public string Name 
        {
            get => m_Name; 
            set
            {
                if (m_Name != value)
                {
                    m_Name = value;
                    OnSetName();
                }
            }
        }
        public GameObject Prefab 
        { 
            get => m_Prefab;
            set
            {
                if (m_Prefab != value)
                {
                    m_Prefab = value;

                    OnPrefabChange();
                }
            }
        }
        public Vector2Int Size { get => m_Size; set => m_Size = value; }

        public RoomPrefabBase()
        {
        }

        public RoomPrefabBase(int id)
        {
            m_ID = id;
        }

        public virtual void Save(ISerializer writer, IObjectIDConverter translater)
        {
            writer.WriteInt32(m_Version, "RoomPrefabBase.Version");
            writer.WriteObjectID(m_ID, "ID", translater);

            writer.WriteVector2Int(m_Size, "Size");
            writer.WriteBoolean(m_ShowInInspector, "Show In Inspector");
            writer.WriteString(m_Name, "Name");
            var prefabGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_Prefab));
            writer.WriteString(prefabGUID, "GUID");
        }

        public virtual void Load(IDeserializer reader)
        {
            reader.ReadInt32("RoomPrefabBase.Version");
            m_ID = reader.ReadInt32("ID");
            m_Size = reader.ReadVector2Int("Size");
            m_ShowInInspector = reader.ReadBoolean("Show In Inspector");
            m_Name = reader.ReadString("Name");
            m_PrefabGUID = reader.ReadString("GUID");
        }

        protected abstract void OnSetName();
        protected abstract void OnPrefabChange();

        [SerializeField]
        private int m_ID;
        [SerializeField]
        protected string m_Name;
        [SerializeField]
        protected Vector2Int m_Size = Vector2Int.one;
        [SerializeField]
        protected bool m_ShowInInspector = true;
        [SerializeField]
        protected GameObject m_Prefab;
        protected string m_PrefabGUID;
        protected Grid m_Grid;
        private const int m_Version = 1;
    }
}
