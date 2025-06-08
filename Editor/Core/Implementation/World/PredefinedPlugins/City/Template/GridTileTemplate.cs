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
    class GridTileTemplate
    {
        public GridTileTemplate()
        {
        }

        public GridTileTemplate(int id, string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));

            m_ID = id;
            m_Name = name;
        }

        public void Save(ISerializer writer, IObjectIDConverter converter)
        {
            writer.WriteInt32(m_VERSION, "GridTileTemplate.Version");

            writer.WriteObjectID(m_ID, "ID", converter);
            writer.WriteInt32(m_Type, "Type");
            writer.WriteString(m_Name, "Name");
            writer.WriteSingle(m_Cost, "Cost");
            writer.WriteColor(m_Color, "Color");
            writer.WriteBoolean(m_ShowInInspector, "Show In Inspector");
            writer.WriteBoolean(m_ReadOnly, "Read Only");
        }

        public void Load(IDeserializer reader)
        {
            reader.ReadInt32("GridTileTemplate.Version");

            m_ID = reader.ReadInt32("ID");
            m_Type = reader.ReadInt32("Type");
            m_Name = reader.ReadString("Name");
            m_Cost = reader.ReadSingle("Cost");
            m_Color = reader.ReadColor("Color");
            m_ShowInInspector = reader.ReadBoolean("Show In Inspector");
            m_ReadOnly = reader.ReadBoolean("Read Only", false);
        }

        public int ID => m_ID;
        public int Type { get => m_Type; set => m_Type = value; }
        public string Name { get => m_Name; set => m_Name = value; }
        public float Cost { set => m_Cost = value; get => m_Cost; }
        public Color Color { get => m_Color; set => m_Color = value; }
        public bool ShowInInspector { get => m_ShowInInspector; set => m_ShowInInspector = value; }
        public bool ReadOnly { get => m_ReadOnly; set => m_ReadOnly = value; }

        [SerializeField]
        int m_ID;

        [SerializeField]
        int m_Type;

        [SerializeField]
        string m_Name;

        [SerializeField]
        float m_Cost;

        [SerializeField]
        Color m_Color = Color.white;

        [SerializeField]
        bool m_ShowInInspector;

        [SerializeField]
        bool m_ReadOnly = false;

        const int m_VERSION = 1;
    }

    class GridTileInstance
    {
        public void Initialize(CityEditor cityEditor)
        {
            m_Template = cityEditor.GetGridTileTemplate(m_TemplateID);
        }

        public void Save(ISerializer writer, IObjectIDConverter converter)
        {
            writer.WriteInt32(m_Version, "GridTileInstance.Version");

            writer.WriteObjectID(m_TemplateID, "Template ID", converter);
        }

        public void Load(IDeserializer reader)
        {
            reader.ReadInt32("GridTileInstance.Version");

            m_TemplateID = reader.ReadInt32("Template ID");
        }

        public int ExistedObjectID { get; set; } = 0;
        public GridTileTemplate Template
        {
            get => m_Template;
            set
            {
                m_Template = value;
                m_TemplateID = m_Template.ID;
            }
        }
        public float Cost
        {
            get
            {
                if (ExistedObjectID != 0)
                {
                    return CityEditorDefine.BLOCK_COST;
                }

                if (m_Template == null)
                {
                    return 0;
                }
                return m_Template.Cost;
            }
        }
        public bool IsEmpty => ExistedObjectID == 0;

        [SerializeField]
        int m_TemplateID;

        GridTileTemplate m_Template;

        const int m_Version = 1;
    }
}
