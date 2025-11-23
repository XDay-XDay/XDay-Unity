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

namespace XDay.WorldAPI.FOW.Editor
{
    public partial class FogSystem
    {
        public enum LayerType
        {
            //用户定义，对编辑器无特殊意义
            UserDefined,
        }

        public abstract class LayerBase : WorldObject
        {
            public string Name { get => m_Name; set => m_Name = value; }
            public int HorizontalGridCount => m_HorizontalGridCount;
            public int VerticalGridCount => m_VerticalGridCount;
            public float GridWidth => m_GridWidth;
            public float GridHeight => m_GridHeight;
            public float Width => m_Width;
            public float Height => m_Height;
            public Vector2 Origin => m_Origin;
            public int HorizontalBlockCount => m_HorizontalBlockCount;
            public int VerticalBlockCount => m_VerticalBlockCount;
            public Color Color { get => m_Color; set => m_Color = value; }
            public LayerType Type { get => m_Type; set => m_Type = value; }
            public bool GridVisible { get => m_GridVisible; set => m_GridVisible = value; }
            protected override bool EnabledInternal { get => m_Visible; set => m_Visible = value; }
            protected override WorldObjectVisibility VisibilityInternal
            {
                set { }
                get => WorldObjectVisibility.Visible;
            }
            public FogSystem System => World.QueryObject<FogSystem>(m_FogSystemID);

            public LayerBase()
            {
            }

            public LayerBase(int id, int objectIndex, int fogSystemID,
                string name, 
                int horizontalGridCount, int verticalGridCount, float gridWidth, float gridHeight, Vector2 origin, 
                int horizontalBlockCount, int verticalBlockCount, LayerType type, Color color)
                : base(id, objectIndex)
            {
                m_Name = name;
                m_HorizontalGridCount = horizontalGridCount;
                m_VerticalGridCount = verticalGridCount;
                m_GridWidth = gridWidth;
                m_GridHeight = gridHeight;
                m_Origin = origin;
                m_HorizontalBlockCount = horizontalBlockCount;
                m_VerticalBlockCount = verticalBlockCount;
                m_Type = type;
                m_Color = color;
                m_FogSystemID = fogSystemID;
            }

            protected override void OnInit()
            {
                m_Width = m_GridWidth * m_HorizontalGridCount;
                m_Height = m_GridHeight * m_VerticalGridCount;
            }

            protected override void OnUninit()
            {
            }

            internal Vector2Int PositionToCoordinate(float x, float z)
            {
                var coordX = Mathf.FloorToInt((x - m_Origin.x) / m_GridWidth);
                var coordY = Mathf.FloorToInt((z - m_Origin.y) / m_GridHeight);
                return new Vector2Int(coordX, coordY);
            }

            internal Vector3 CoordinateToPosition(int x, int y)
            {
                return new Vector3(
                    x * m_GridWidth + m_Origin.x,
                    0,
                    y * m_GridHeight + m_Origin.y);
            }

            internal Vector3 CoordinateToCenterPosition(int x, int y)
            {
                return new Vector3(
                    (x + 0.5f) * m_GridWidth + m_Origin.x,
                    0,
                    (y + 0.5f) * m_GridHeight + m_Origin.y);
            }

            internal abstract Color32 GetColor(int x, int y);

            internal bool SizeEqual(LayerBase otherLayer)
            {
                return 
                    Mathf.Approximately(m_GridWidth, otherLayer.GridWidth) &&
                    Mathf.Approximately(m_GridHeight, otherLayer.GridHeight) &&
                    Mathf.Approximately(m_HorizontalGridCount, otherLayer.HorizontalGridCount) &&
                    Mathf.Approximately(m_VerticalGridCount, otherLayer.VerticalGridCount);
            }

            public override void EditorSerialize(ISerializer writer, string mark, IObjectIDConverter converter)
            {
                writer.WriteInt32(m_EditorVersion, "LayerBase Version");

                base.EditorSerialize(writer, mark, converter);

                writer.WriteString(m_Name, "Name");
                writer.WriteInt32(m_HorizontalGridCount, "Horizontal Grid Count");
                writer.WriteInt32(m_VerticalGridCount, "Vertical Grid Count");
                writer.WriteSingle(m_GridWidth, "Grid Width");
                writer.WriteSingle(m_GridHeight, "Grid Height");
                writer.WriteInt32(m_HorizontalBlockCount, "Horizontal Block Count");
                writer.WriteInt32(m_VerticalBlockCount, "Vertical Block Count");
                writer.WriteInt32((int)m_Type, "Layer Type");
                writer.WriteColor(m_Color, "Color");
                writer.WriteBoolean(m_Visible, "Visible");
                writer.WriteBoolean(m_GridVisible, "Grid Visible");
                writer.WriteObjectID(m_FogSystemID, "Fog System ID", converter);
            }

            public override void EditorDeserialize(IDeserializer reader, string mark)
            {
                reader.ReadInt32("LayerBase Version");

                base.EditorDeserialize(reader, mark);

                m_Name = reader.ReadString("Name", "Unnamed Layer");
                m_HorizontalGridCount = reader.ReadInt32("Horizontal Grid Count");
                m_VerticalGridCount = reader.ReadInt32("Vertical Grid Count");
                m_GridWidth = reader.ReadSingle("Grid Width");
                m_GridHeight = reader.ReadSingle("Grid Height");
                m_HorizontalBlockCount = reader.ReadInt32("Horizontal Block Count");
                m_VerticalBlockCount = reader.ReadInt32("Vertical Block Count");
                m_Type = (LayerType)reader.ReadInt32("Layer Type");
                m_Color = reader.ReadColor("Color");
                m_Visible = reader.ReadBoolean("Visible");
                m_GridVisible = reader.ReadBoolean("Grid Visible");
                m_FogSystemID = reader.ReadInt32("Fog System ID");
            }

            public override void GameSerialize(ISerializer writer, string mark, IObjectIDConverter converter)
            {
                writer.WriteInt32(m_RuntimeVersion, "LayerBase Version");

                base.GameSerialize(writer, mark, converter);

                writer.WriteString(m_Name, "Name");
                writer.WriteInt32(m_HorizontalGridCount, "Horizontal Grid Count");
                writer.WriteInt32(m_VerticalGridCount, "Vertical Grid Count");
                writer.WriteSingle(m_GridWidth, "Grid Width");
                writer.WriteSingle(m_GridHeight, "Grid Height");
                writer.WriteInt32(m_HorizontalBlockCount, "Horizontal Block Count");
                writer.WriteInt32(m_VerticalBlockCount, "Vertical Block Count");
                writer.WriteColor(m_Color, "Color");
            }

            public override void GameDeserialize(IDeserializer reader, string mark)
            {
                reader.ReadInt32("LayerBase Version");

                base.GameDeserialize(reader, mark);

                m_Name = reader.ReadString("Name", "Unnamed Layer");
                m_HorizontalGridCount = reader.ReadInt32("Horizontal Grid Count");
                m_VerticalGridCount = reader.ReadInt32("Vertical Grid Count");
                m_GridWidth = reader.ReadSingle("Grid Width");
                m_GridHeight = reader.ReadSingle("Grid Height");
                m_HorizontalBlockCount = reader.ReadInt32("Horizontal Block Count");
                m_VerticalBlockCount = reader.ReadInt32("Vertical Block Count");
                m_Color = reader.ReadColor("Color");
            }

            [SerializeField]
            private string m_Name;
            [SerializeField]
            private float m_GridWidth;
            [SerializeField]
            private float m_GridHeight;
            [SerializeField]
            private int m_HorizontalGridCount;
            [SerializeField]
            private int m_VerticalGridCount;
            [SerializeField]
            private Vector2 m_Origin;
            [SerializeField]
            private Color m_Color = Color.white;
            //将地图分成多个block,每个block渲染部分grid
            [SerializeField]
            private int m_HorizontalBlockCount;
            [SerializeField]
            private int m_VerticalBlockCount;
            [SerializeField]
            private LayerType m_Type = LayerType.UserDefined;
            [SerializeField]
            private bool m_Visible = true;
            [SerializeField]
            private bool m_GridVisible = true;
            [SerializeField]
            private int m_FogSystemID;
            private float m_Width;
            private float m_Height;
            private const int m_EditorVersion = 1;
            private const int m_RuntimeVersion = 1;
        }

        public class Layer : LayerBase
        {
            public override string TypeName => "EditorFogSystem.Layer";
            public uint[] Data => m_Data;
            public string FogConfigGUID { get => m_FogConfigGUID; set => m_FogConfigGUID = value; }
            public string BlurShaderGUID { get => m_BlurShaderGUID; set => m_BlurShaderGUID = value; }
            public string FogPrefabGUID { get => m_FogPrefabGUID; set => m_FogPrefabGUID = value; }

            public Layer()
            {
            }

            public Layer(int id, int objectIndex, int fogSystemID, string name, int horizontalGridCount, int verticalGridCount, 
                float gridWidth, float gridHeight, Vector2 origin, int horizontalBlockCount, int verticalBlockCount, LayerType type, Color color)
                : base(id, objectIndex, fogSystemID, name, horizontalGridCount, verticalGridCount, 
                      gridWidth, gridHeight, origin, horizontalBlockCount, verticalBlockCount, type, color)
            {
                m_Data = new uint[verticalGridCount * horizontalGridCount];
            }

            public void Set(int x, int y, uint type)
            {
                if (x >= 0 && x < HorizontalGridCount && y >= 0 && y < VerticalGridCount)
                {
                    var index = y * HorizontalGridCount + x;
                    m_Data[index] = type;
                }
            }

            public uint Get(int x, int y)
            {
                if (x >= 0 && x < HorizontalGridCount && y >= 0 && y < VerticalGridCount)
                {
                    return m_Data[y * HorizontalGridCount + x];
                }

                return default;
            }

            internal override Color32 GetColor(int x, int y)
            {
                return Get(x, y) != 0 ? Color : new Color(0, 0, 0, 0);
            }

            public override void EditorSerialize(ISerializer writer, string mark, IObjectIDConverter converter)
            {
                writer.WriteInt32(m_EditorVersion, "Layer Version");

                base.EditorSerialize(writer, mark, converter);

                writer.WriteUInt32Array(m_Data, "Data");
                writer.WriteString(m_FogConfigGUID, "Fog Config GUID");
                writer.WriteString(m_BlurShaderGUID, "Blur Shader GUID");
                writer.WriteString(m_FogPrefabGUID, "Fog Prefab GUID");
            }

            public override void EditorDeserialize(IDeserializer reader, string mark)
            {
                reader.ReadInt32("Layer Version");

                base.EditorDeserialize(reader, mark);

                m_Data = reader.ReadUInt32Array("Data");
                m_FogConfigGUID = reader.ReadString("Fog Config GUID");
                m_BlurShaderGUID = reader.ReadString("Blur Shader GUID");
                m_FogPrefabGUID = reader.ReadString("Fog Prefab GUID");
            }

            public override void GameSerialize(ISerializer writer, string mark, IObjectIDConverter converter)
            {
                writer.WriteInt32(m_RuntimeVersion, "Layer Version");

                base.GameSerialize(writer, mark, converter);

                writer.WriteUInt32Array(m_Data, "Data");
            }

            public override void GameDeserialize(IDeserializer reader, string mark)
            {
                reader.ReadInt32("Layer Version");

                base.GameDeserialize(reader, mark);

                m_Data = reader.ReadUInt32Array("Data");
            }

            [SerializeField]
            private uint[] m_Data;
            [SerializeField]
            private string m_FogPrefabGUID;
            [SerializeField]
            private string m_BlurShaderGUID;
            [SerializeField]
            private string m_FogConfigGUID;

            private const int m_EditorVersion = 1;
            private const int m_RuntimeVersion = 1;
        }
    }
}