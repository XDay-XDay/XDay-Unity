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
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.House.Editor
{
    internal class HouseTeleporterInstance : HouseTeleporter
    {
        public int TeleporterID => m_TeleporterID;
        public int ConfigID { get => m_ConfigID; set => m_ConfigID = value; }
        public int ConnectedID { get => m_ConnectedID; set => m_ConnectedID = value; }
        public bool Enabled
        {
            get => m_Enabled;
            set
            {
                m_Enabled = value;
                if (m_Material != null)
                {
                    m_Material.SetColor("_Color", m_Enabled ? new Color32(0, 162, 232, 255) : Color.red);
                }
            }
        }

        public HouseTeleporterInstance()
        {
        }

        public HouseTeleporterInstance(int id, HouseTeleporter teleporter) : base(id)
        {
            m_TeleporterID = teleporter.ID;
        }

        public override void Initialize(House house)
        {
            base.Initialize(house);
            Enabled = m_Enabled;
        }

        public void CopyFrom(HouseTeleporter teleporter)
        {
            LocalPosition = teleporter.LocalPosition;
            Name = teleporter.Name;
        }

        public override void Save(ISerializer writer, IObjectIDConverter converter)
        {
            writer.WriteInt32(m_Version, "HouseTeleporterInstance.Version");

            base.Save(writer, converter);

            writer.WriteInt32(m_ConfigID, "Config ID");
            writer.WriteInt32(m_ConnectedID, "Connected ID");
            writer.WriteBoolean(m_Enabled, "Enabled");
            writer.WriteObjectID(m_TeleporterID, "Teleporter ID", converter);
        }

        public override void Load(IDeserializer reader)
        {
            reader.ReadInt32("HouseTeleporterInstance.Version");

            base.Load(reader);

            m_ConfigID = reader.ReadInt32("ID");
            m_ConnectedID = reader.ReadInt32("Connected ID");
            m_Enabled = reader.ReadBoolean("Enabled");
            m_TeleporterID = reader.ReadInt32("Teleporter ID");
        }

        protected override void AddBehaviour()
        {
            var behaviour = m_Root.AddComponent<HouseTeleporterInstanceBehaviour>();
            behaviour.Initialize(House.ID, this, (e) => { WorldEditor.EventSystem.Broadcast(e); });
        }

        [SerializeField]
        private int m_ConfigID;
        [SerializeField]
        private int m_ConnectedID;
        [SerializeField]
        private bool m_Enabled = true;
        [SerializeField]
        private int m_TeleporterID;
        private const int m_Version = 1;
    }
}