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

namespace XDay.WorldAPI.City.Editor
{
    public enum GridType
    {
        Base = 10,
        //刚开始是荒地
        Barren = Base,
        //改造成绿地
        Grass = Base + 1,
        //绿地改造后
        Terrain = Base + 2,
    }

    /// <summary>
    /// 地块
    /// </summary>
    class AreaTemplate : ILocatorOwner
    {
        public AreaTemplate()
        {
        }

        public AreaTemplate(int objectID, string name, Color color, int configID)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));

            m_ObjectID = objectID;
            m_Name = name;
            m_Color = color;
            m_ConfigID = configID;
        }

        public void Initialize(Transform locatorParent, int gridID)
        {
            foreach (var locator in m_Locators)
            {
                locator.Initialize(locatorParent, gridID);
            }
        }

        public void SetCoordinates(int x, int y, int width, int height, bool set)
        {
            for (var i = y; i < y + height; ++i)
            {
                for (var j = x; j < x + width; ++j)
                {
                    var coord = new Vector2Int(j, i);
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
            }
        }

        public void AddLocator(Locator locator)
        {
            m_Locators.Add(locator);

            m_SelectedLocatorIndex = m_Locators.Count - 1;
        }

        public void RemoveLocator(int index)
        {
            m_Locators[index].OnDestroy();
            m_Locators.RemoveAt(index);

            --m_SelectedLocatorIndex;
            if (m_Locators.Count > 0 && m_SelectedLocatorIndex < 0)
            {
                m_SelectedLocatorIndex = 0;
            }
        }

        public void UpdateLocatorSize(float size)
        {
            foreach (var locator in m_Locators)
            {
                locator.Size = Vector3.one * size;
            }
        }

        public void Save(ISerializer writer, IObjectIDConverter converter)
        {
            writer.WriteInt32(m_Version, "SmallRegionTemplate.Version");

            writer.WriteObjectID(m_ObjectID, "ID", converter);
            writer.WriteInt32(m_ConfigID, "Config ID");
            writer.WriteString(m_Name, "Name");
            writer.WriteColor(m_Color, "Color");
            writer.WriteBoolean(m_ShowInInspector, "Show In Inspector");
            writer.WriteBoolean(m_Lock, "Lock");
            writer.WriteInt32((int)m_InitialGridType, "Transform Type");

            writer.WriteInt32(m_SelectedLocatorIndex, "Selected Locator Index");

            writer.WriteList(m_Locators, "Locators", (Locator locator, int index) =>
            {
                writer.WriteStructure($"Locator {index}", () =>
                {
                    locator.Save(writer);
                });
            });

            var coords = new List<Vector2Int>(m_Coordinates);
            writer.WriteVector2IntList(coords, "Coordinate");
        }

        public void Load(IDeserializer reader)
        {
            var version = reader.ReadInt32("SmallRegionTemplate.Version");

            m_ObjectID = reader.ReadInt32("ID");
            m_ConfigID = reader.ReadInt32("Config ID");
            m_Name = reader.ReadString("Name");
            m_Color = reader.ReadColor("Color");
            m_ShowInInspector = reader.ReadBoolean("Show In Inspector");

            if (version >= 2)
            {
                m_Lock = reader.ReadBoolean("Lock");
            }
            if (version >= 3)
            {
                var value = reader.ReadInt32("Transform Type");
                if (value < (int)GridType.Barren)
                {
                    value = (int)GridType.Barren;
                }
                m_InitialGridType = (GridType)value;

                m_SelectedLocatorIndex = reader.ReadInt32("Selected Locator Index");

                m_Locators = reader.ReadList("Locators", (int index) =>
                {
                    var locator = new Locator();
                    reader.ReadStructure($"Locator {index}", () =>
                    {
                        locator.Load(reader);
                    });
                    return locator;
                });
            }

            if (version >= 4)
            {
                var coords = reader.ReadVector2IntList("Coordinate");
                m_Coordinates = new HashSet<Vector2Int>(coords);
            }
        }

        public int ObjectID => m_ObjectID;
        public int ConfigID { get => m_ConfigID; set => m_ConfigID = value; }
        public string Name { get => m_Name; set => m_Name = value; }
        public Color Color { get => m_Color; set => m_Color = value; }
        public bool ShowInInspector { get => m_ShowInInspector; set => m_ShowInInspector = value; }
        public bool Lock { get => m_Lock; set => m_Lock = value; }
        public List<Locator> Locators => m_Locators;
        public int SelectedLocatorIndex { get => m_SelectedLocatorIndex; set => m_SelectedLocatorIndex = value; }
        public bool ShowLocatorInInspector { get => m_ShowLocatorInInspector; set => m_ShowLocatorInInspector = value; }
        public GridType InitialGridType { get => m_InitialGridType; set => m_InitialGridType = value; }
        public HashSet<Vector2Int> Coordinates => m_Coordinates;

        [SerializeField]
        int m_ObjectID;

        [SerializeField]
        int m_ConfigID;

        [SerializeField]
        string m_Name;

        [SerializeField]
        Color m_Color = Color.white;

        [SerializeField]
        bool m_ShowInInspector = true;

        //锁定后不能修改
        [SerializeField]
        bool m_Lock = false;

        [SerializeField]
        GridType m_InitialGridType = GridType.Barren;

        [SerializeField]
        bool m_ShowLocatorInInspector = true;

        [SerializeField]
        int m_SelectedLocatorIndex = -1;

        [SerializeField]
        List<Locator> m_Locators = new();

        HashSet<Vector2Int> m_Coordinates = new();

        const int m_Version = 4;
    }
}

