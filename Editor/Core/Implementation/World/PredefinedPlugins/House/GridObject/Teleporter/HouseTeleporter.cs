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
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.House.Editor
{
    internal class HouseTeleporter
    {
        public House House => m_House;
        public int ID => m_ID;
        public Vector3 WorldPosition => m_Root.transform.position;
        public Vector3 LocalPosition
        {
            get => m_LocalPosition;
            set
            {
                m_LocalPosition = value;
                if (m_Root != null)
                {
                    m_Root.transform.position = m_House.Root.transform.TransformPoint(m_LocalPosition);
                }
            }
        }
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

        public HouseTeleporter()
        {
        }

        public HouseTeleporter(int id)
        {
            m_ID = id;
        }

        public virtual void Initialize(House house)
        {
            m_House = house;
            m_Root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_Root.name = "Sphere";
            m_Root.transform.localScale = Vector3.one * house.GridSize;
            m_Root.transform.position = m_House.Root.transform.TransformPoint(m_LocalPosition);
            m_Root.transform.SetParent(house.TeleporterRoot.transform);
            m_Root.AddComponent<NoKeyDeletion>();
            m_Material = new Material(Shader.Find("XDay/Transparent"));
            m_Root.GetComponent<MeshRenderer>().sharedMaterial = m_Material;
            AddBehaviour();
        }

        public void OnDestroy()
        {
            Object.DestroyImmediate(m_Material);
            Object.DestroyImmediate(m_Root);
        }

        public void OnPositionChanged()
        {
            m_LocalPosition = m_House.Root.transform.InverseTransformPoint(m_Root.transform.position);
        }

        public virtual void Save(ISerializer writer, IObjectIDConverter converter)
        {
            writer.WriteInt32(m_Version, "HouseTeleporter.Version");
            
            writer.WriteObjectID(m_ID, "ID", converter);
            writer.WriteString(m_Name, "Name");
            writer.WriteBoolean(m_ShowInInspector, "Show In Inspector");
            OnPositionChanged();
            writer.WriteVector3(m_LocalPosition, "Local Position");
        }

        public virtual void Load(IDeserializer reader)
        {
            reader.ReadInt32("HouseTeleporter.Version");

            m_ID = reader.ReadInt32("ID");
            m_Name = reader.ReadString("Name");
            m_ShowInInspector = reader.ReadBoolean("Show In Inspector");
            m_LocalPosition = reader.ReadVector3("Local Position");
        }

        protected virtual void AddBehaviour()
        {
            var behaviour = m_Root.AddComponent<HouseTeleporterBehaviour>();
            behaviour.Initialize(m_House.ID, this, (e) => { WorldEditor.EventSystem.Broadcast(e); });
        }

        public void RotateAround(Vector3 worldPos)
        {
            var local = WorldPosition - worldPos;
            local = Quaternion.Euler(0, 180, 0) * local;
            m_Root.transform.position = worldPos + local;

            OnPositionChanged();
        }

        [SerializeField]
        private int m_ID;
        [SerializeField]
        private string m_Name;
        [SerializeField]
        private bool m_ShowInInspector = true;
        [SerializeField]
        private Vector3 m_LocalPosition;
        protected GameObject m_Root;
        protected Material m_Material;
        private House m_House;
        private const int m_Version = 1;
    }
}
