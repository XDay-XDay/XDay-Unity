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
    class GridLabelTemplate
    {
        public GridLabelTemplate()
        {
        }

        public GridLabelTemplate(int objectID, string name, Color color, byte value, float cost, bool walkable)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));

            m_ObjectID = objectID;
            m_Name = name;
            m_Color = color;
            m_Value = value;
            m_Cost = cost;
            m_Walkable = walkable;
        }

        public void Save(ISerializer writer, IObjectIDConverter converter)
        {
            writer.WriteInt32(m_Version, "GridLabelTemplate.Version");

            writer.WriteObjectID(m_ObjectID, "ID", converter);
            writer.WriteString(m_Name, "Name");
            writer.WriteColor(m_Color, "Color");
            writer.WriteBoolean(m_ShowInInspector, "Show In Inspector");
            writer.WriteByte(m_Value, "Value");
            writer.WriteSingle(m_Cost, "Cost");
            writer.WriteBoolean(m_Walkable, "Walkable");
        }

        public void Load(IDeserializer reader)
        {
            reader.ReadInt32("GridLabelTemplate.Version");

            m_ObjectID = reader.ReadInt32("ID");
            m_Name = reader.ReadString("Name");
            m_Color = reader.ReadColor("Color");
            m_ShowInInspector = reader.ReadBoolean("Show In Inspector");
            m_Value = reader.ReadByte("Value");
            m_Cost = reader.ReadSingle("Cost");
            m_Walkable = reader.ReadBoolean("Walkable");
        }

        public int ObjectID => m_ObjectID;
        public string Name { get => m_Name; set => m_Name = value; }
        public Color Color { get => m_Color; set => m_Color = value; }
        public bool ShowInInspector { get => m_ShowInInspector; set => m_ShowInInspector = value; }
        public byte Value { get => m_Value; set => m_Value = value; }
        public float Cost { get => m_Cost;  set => m_Cost = value; }
        public bool Walkable { get => m_Walkable;  set => m_Walkable = value; }

        [SerializeField]
        int m_ObjectID;

        [SerializeField]
        byte m_Value;

        [SerializeField]
        string m_Name;

        [SerializeField]
        Color m_Color = Color.white;

        [SerializeField]
        bool m_ShowInInspector = true;

        [SerializeField]
        float m_Cost = 0;

        [SerializeField]
        bool m_Walkable;

        const int m_Version = 1;
    }
}

