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

namespace XDay.WorldAPI.City.Editor
{
    class BuildingTemplate
    {
        public BuildingTemplate()
        {
        }

        public BuildingTemplate(int id)
        {
            m_ID = id;
        }

        public void Initialize(CityEditor city)
        {
            m_City = city;
        }

        public void Save(ISerializer writer, IObjectIDConverter translator)
        {
            writer.WriteInt32(m_Version, "BuildingTemplate.Version");

            writer.WriteObjectID(m_ID, "ID", translator);
            writer.WriteInt32(m_ConfigID, "Config ID");
            writer.WriteString(m_Name, "Name");
            writer.WriteBoolean(m_ShowInInspector, "Show In Inspector");
            writer.WriteObjectID(m_RoomID, "Room Prefab ID", translator);
        }

        public void Load(IDeserializer reader)
        {
            var version = reader.ReadInt32("BuildingTemplate.Version");

            m_ID = reader.ReadInt32("ID");
            m_ConfigID = reader.ReadInt32("Config ID");
            m_Name = reader.ReadString("Name");
            if (version == 1)
            {
                reader.ReadVector2Int("Size");
            }
            m_ShowInInspector = reader.ReadBoolean("Show In Inspector");
            if (version == 1)
            {
                reader.ReadString("Prefab GUID");
            }
            else
            {
                m_RoomID = reader.ReadInt32("Room Prefab ID");
            }
        }

        public int ID { get => m_ID; set => m_ID = value; }
        public string Name { get => m_Name; set => m_Name = value; }
        public int ConfigID { get => m_ConfigID; set => m_ConfigID = value; }
        public bool ShowInInspector { get => m_ShowInInspector; set => m_ShowInInspector = value; }
        public Vector2Int Size
        {
            get
            {
                var roomPrefab = RoomPrefab;
                if (roomPrefab != null)
                {
                    return roomPrefab.Size;
                }
                return Vector2Int.one;
            }
        }
        public int RoomID
        {
            get => m_RoomID;
            set
            {
                if (m_RoomID != value)
                {
                    m_RoomID = value;

                    m_City.OnBuildTemplatePrefabChange(this);
                }
            }
        }
        public GameObject Prefab
        {
            get
            {
                var roomPrefab = m_City.FirstGrid.RoomEditor.GetRoomPrefabByID(m_RoomID);
                if (roomPrefab != null)
                {
                    return roomPrefab.Prefab;
                }
                return null;
            }
        }

        public RoomPrefab RoomPrefab
        {
            get
            {
                var grid = m_City.FirstGrid;
                if (grid != null) 
                {
                    return grid.RoomEditor.GetRoomPrefabByID(m_RoomID);
                }
                return null;
            }
        }

        [SerializeField]
        int m_ID;

        [SerializeField]
        int m_ConfigID;

        [SerializeField]
        string m_Name;

        [SerializeField]
        bool m_ShowInInspector = true;

        [SerializeField]
        int m_RoomID;

        CityEditor m_City;

        const int m_Version = 2;
    }
}
