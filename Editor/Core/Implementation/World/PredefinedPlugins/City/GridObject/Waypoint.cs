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
using XDay.UtilityAPI;

namespace XDay.WorldAPI.City.Editor
{
    internal class Waypoint
    {
        public int ID { get => m_ID; set => m_ID = value; }
        public int EventID { get => m_EventID; set => m_EventID = value; }
        public int ConnectedID { get => m_ConnectedID; set => m_ConnectedID = value; }
        public bool Enabled { get => m_Enabled; set => m_Enabled = value; }
        public string Name
        {
            get => m_Name;
            set
            {
                m_Name = value;
                if (m_Root != null)
                {
                    m_Root.name = value;
                }
            }
        }
        public bool ShowInInspector { get => m_ShowInInspector; set => m_ShowInInspector = value; }
        public bool Visible { get => m_Root.activeSelf; set => m_Root.SetActive(value); }
        public GameObject GameObject => m_Root;

        public void Initialize(Grid grid)
        {
            m_Grid = grid;

            m_Root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_Root.name = "Sphere";
            m_Root.transform.localScale = Vector3.one * grid.GridSize;
            m_Root.transform.position = m_Position;
            m_Root.transform.SetParent(grid.WaypointRoot.transform);
            m_Root.AddComponent<NoKeyDeletion>();
        }

        public void OnDestroy()
        {
            Object.DestroyImmediate(m_Root);
        }

        public void Save(ISerializer writer)
        {
            writer.WriteInt32(m_Version, "Waypoint.Version");
            writer.WriteInt32(m_ID, "ID");
            writer.WriteInt32(m_ConnectedID, "Connected ID");
            writer.WriteInt32(m_EventID, "Event ID");
            writer.WriteString(m_Name, "Name");
            writer.WriteBoolean(m_ShowInInspector, "Show In Inspector");
            writer.WriteVector3(m_Root.transform.position, "Position");
            writer.WriteBoolean(m_Enabled, "Enabled");
        }

        public void Load(IDeserializer reader)
        {
            reader.ReadInt32("Waypoint.Version");
            m_ID = reader.ReadInt32("ID");
            m_ConnectedID = reader.ReadInt32("Connected ID");
            m_EventID = reader.ReadInt32("Event ID");
            m_Name = reader.ReadString("Name");
            m_ShowInInspector = reader.ReadBoolean("Show In Inspector");
            m_Position = reader.ReadVector3("Position");
            m_Enabled = reader.ReadBoolean("Enabled");
        }

        [SerializeField]
        private int m_ID;
        [SerializeField]
        private int m_ConnectedID;
        [SerializeField]
        private int m_EventID;
        [SerializeField]
        private string m_Name;
        [SerializeField]
        private bool m_ShowInInspector = true;
        [SerializeField]
        private Vector3 m_Position;
        [SerializeField]
        private bool m_Enabled;
        private GameObject m_Root;
        private Grid m_Grid;
        private const int m_Version = 1;
    }
}
