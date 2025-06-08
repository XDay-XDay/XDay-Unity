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
using System.Collections.Generic;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.City.Editor
{
    /// <summary>
    /// 事件
    /// </summary>
    class EventTemplate : IScenePrefabSetter
    {
        public EventTemplate()
        {
        }

        public EventTemplate(int objectID, string name, Color color, int configID)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));

            m_ObjectID = objectID;
            m_Name = name;
            m_Color = color;
            m_ConfigID = configID;
        }

        public void Initialize(Grid grid)
        {
            m_Grid = grid;

            m_Prefab.Initialize(grid.EventPrefabRoot.transform, false);
        }

        public void OnDestroy()
        {
            m_Prefab.OnDestroy();
        }

        public void SetCoordinates(int x, int y, bool set)
        {
            var coord = new Vector2Int(x, y);
            var contains = m_Coordinates.Contains(coord);
            if (set)
            {
                if (!contains)
                {
                    m_Coordinates.Add(coord);
                }
            }
            else
            {
                if (contains)
                {
                    m_Coordinates.Remove(coord);
                }
            }        
        }

        public void SyncToGridPosition()
        {
            var instance = PrefabInstance;
            if (instance != null)
            {
                var center = Center;
                instance.transform.position = m_Grid.CoordinateToGridPosition(center.x, center.y);
            }
        }

        public void Save(ISerializer writer, IObjectIDConverter converter)
        {
            writer.WriteInt32(m_Version, "EventTemplate.Version");

            writer.WriteObjectID(m_ObjectID, "ID", converter);
            writer.WriteInt32(m_ConfigID, "Config ID");
            writer.WriteString(m_Name, "Name");
            writer.WriteColor(m_Color, "Color");
            writer.WriteBoolean(m_ShowInInspector, "Show In Inspector");
            writer.WriteBoolean(m_UseGroundHeight, "Use Ground Height");

            var coords = new List<Vector2Int>(m_Coordinates);
            writer.WriteVector2IntList(coords, "Coordinate");

            writer.WriteStructure("Scene Prefab", () =>
            {
                m_Prefab.Save(writer);
            });
        }

        public void Load(IDeserializer reader)
        {
            var version = reader.ReadInt32("EventTemplate.Version");

            m_ObjectID = reader.ReadInt32("ID");
            m_ConfigID = reader.ReadInt32("Config ID");
            m_Name = reader.ReadString("Name");
            m_Color = reader.ReadColor("Color");
            m_ShowInInspector = reader.ReadBoolean("Show In Inspector");
            if (version >= 3)
            {
                m_UseGroundHeight = reader.ReadBoolean("Use Ground Height");
            }
            var coords = reader.ReadVector2IntList("Coordinate");
            m_Coordinates = new HashSet<Vector2Int>(coords);

            if (version >= 2)
            {
                reader.ReadStructure("Scene Prefab", () =>
                {
                    m_Prefab.Load(reader);
                });
            }
        }

        public int ObjectID => m_ObjectID;
        public int ConfigID { get => m_ConfigID; set => m_ConfigID = value; }
        public string Name { get => m_Name; set => m_Name = value; }
        public Color Color { get => m_Color; set => m_Color = value; }
        public bool UseGroundHeight { get => m_UseGroundHeight; set => m_UseGroundHeight = value; }
        public bool ShowInInspector { get => m_ShowInInspector; set => m_ShowInInspector = value; }
        public HashSet<Vector2Int> Coordinates => m_Coordinates;
        public Vector2Int Center => m_Coordinates.GetCenter();
        public GameObject Prefab { get => m_Prefab.Prefab;set => m_Prefab.Prefab = value; }
        public GameObject PrefabInstance => m_Prefab.Instance;
        public bool Visible { get => m_Prefab.Visible; set => m_Prefab.Visible = value; }

        public Vector3 Position
        {
            get
            {
                if (PrefabInstance != null)
                {
                    return PrefabInstance.transform.position;
                }
                return Vector3.zero;
            }
            set
            {
                if (PrefabInstance != null)
                {
                    PrefabInstance.transform.position = value;
                }
            }
        }

        public Quaternion Rotation
        {
            get
            {
                if (PrefabInstance != null)
                {
                    return PrefabInstance.transform.rotation;
                }
                return Quaternion.identity;
            }
            set
            {
                if (PrefabInstance != null)
                {
                    PrefabInstance.transform.rotation = value;
                }
            }
        }

        [SerializeField]
        int m_ObjectID;

        [SerializeField]
        int m_ConfigID;

        [SerializeField]
        string m_Name;

        [SerializeField]
        Color m_Color = Color.white;

        [SerializeField]
        bool m_UseGroundHeight = false;

        [SerializeField]
        bool m_ShowInInspector = true;

        HashSet<Vector2Int> m_Coordinates = new();

        [SerializeField]
        ScenePrefab m_Prefab = new();

        Grid m_Grid;

        const int m_Version = 3;
    }
}

