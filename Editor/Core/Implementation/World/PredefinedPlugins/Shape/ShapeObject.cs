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

using System.Collections.Generic;
using UnityEngine;
using XDay.UtilityAPI.Shape;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Shape.Editor
{
    public class ShapeObject : WorldObject, IObstacle
    {
        internal ShapeSystemLayer Layer => m_ShapeSystemLayer;
        public bool Walkable => m_Attribute.HasFlag(ObstacleAttribute.Walkable);
        public ObstacleAttribute Attribute { get => m_Attribute; set => m_Attribute = value; }
        public int AreaID { get => m_AreaID; set => m_AreaID = value; }
        public int CustomID { get => m_CustomID; set => m_CustomID = value; }
        public float Height { get => m_Height; set => m_Height = value; }
        public override Vector3 Scale { get => m_Scale; set => m_Scale = value; }
        public override Vector3 Position { get => m_Position; set => m_Position = value; }
        public override Quaternion Rotation { get => m_Rotation; set => m_Rotation = value; }
        public string Name { get => m_Name; set => m_Name = value; }
        public Color RenderColor => m_UseOverriddenColor ? m_OverriddenColor : m_Color;
        public Color Color => m_Color;
        public int VertexCount => m_Polygon.VertexCount;
        public IAspectContainer AspectContainer => m_AspectContainer;
        public List<Vector3> VerticesCopy => m_Polygon.VerticesCopy;
        public List<Vector3> LocalPolygon => m_Polygon.Vertices;
        public List<Vector3> WorldPolygon
        {
            get
            {
                var vertices = VerticesCopy;
                for (var i = 0; i < vertices.Count; ++i)
                {
                    vertices[i] = TransformToWorldPosition(vertices[i]);
                }
                return vertices;
            }
        }
        public Rect WorldBounds
        {
            get
            {
                return m_Polygon.Bounds.ToBounds().Transform(GetLocalToWorldMatrix()).ToRect();
            }
        }
        public override string TypeName => "EditorShapeObject";
        protected override WorldObjectVisibility VisibilityInternal
        {
            set { }
            get => WorldObjectVisibility.Visible;
        }
        protected override bool EnabledInternal
        {
            set => m_Enabled = value;
            get => m_Enabled;
        }

        public ShapeObject()
        {
        }

        public ShapeObject(int id, int index, int shapeLayerID, List<Vector3> localVertices, Vector3 position) 
            : base(id, index)
        {
            m_ShapeLayerID = shapeLayerID;
            m_Polygon = new Polygon(localVertices);
            Position = position;
        }

        protected override void OnInit()
        {
            m_ShapeSystemLayer = World.QueryObject<ShapeSystemLayer>(m_ShapeLayerID);
        }

        protected override void OnUninit()
        {
        }

        public bool Hit(Vector3 worldPos, float radius, out int index)
        {
            index = -1;
            var localPos = TransformToLocalPosition(worldPos);
            for (var i = 0; i < m_Polygon.Vertices.Count; ++i)
            {
                if ((m_Polygon.Vertices[i] - localPos).sqrMagnitude <= radius * radius)
                {
                    index = i;
                    return true;
                }
            }
            return false;
        }

        public Vector3 TransformToLocalPosition(Vector3 worldPosition)
        {
            var transformMatrix = Matrix4x4.TRS(Position, Rotation, Scale);
            var worldToLocal = transformMatrix.inverse;
            return worldToLocal.MultiplyPoint(worldPosition);
        }

        public Vector3 TransformToWorldPosition(Vector3 localPosition)
        {
            var transformMatrix = Matrix4x4.TRS(Position, Rotation, Scale);
            return transformMatrix.MultiplyPoint(localPosition);
        }

        public void MoveShape(Vector3 offset)
        {
            m_Polygon.Move(offset);
        }

        public bool InsertVertex(int index, Vector3 localPosition)
        {
            return m_Polygon.InsertVertex(index, localPosition);
        }

        public bool DeleteVertex(int index)
        {
            return m_Polygon.DeleteVertex(index);
        }

        public void MoveVertex(int index, Vector3 offset)
        {
            m_Polygon.MoveVertex(index, offset);
        }

        public Vector3 GetVertexPosition(int index)
        {
            return m_Polygon.GetVertexPosition(index);
        }

        public Vector3 GetVertexWorldPosition(int index)
        {
            return TransformToWorldPosition(m_Polygon.GetVertexPosition(index));
        }

        public List<Vector3> GetPolyonInLocalSpace()
        {
            return m_Polygon.Vertices;
        }

        public void Reverse()
        {
            if (m_Polygon != null && m_Polygon.IsClockwiseWinding)
            {
                m_Polygon.Reverse();
            }
        }

        private Matrix4x4 GetLocalToWorldMatrix()
        {
            return Matrix4x4.TRS(Position, Rotation, Scale);
        }

        public void SetOverriddenColor(Color color)
        {
            m_OverriddenColor = color;
        }

        public void UseOverriddenColor(bool use)
        {
            m_UseOverriddenColor = use;
        }

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            base.GameSerialize(serializer, label, converter);
            serializer.WriteInt32(m_Version, "ShapeObject.Version");
            serializer.WriteObjectID(m_ShapeLayerID, "Shape System Layer ID", converter);
            serializer.WriteBoolean(m_Enabled, "Is Enabled");
            serializer.WriteVector3(m_Position, "Position");
            serializer.WriteQuaternion(m_Rotation, "Rotation");
            serializer.WriteVector3(m_Scale, "Scale");
            serializer.WriteString(m_Name, "Name");
            serializer.WriteColor(m_Color, "Color");
            serializer.WriteVector3List(m_Polygon.Vertices, "Vertices");
            serializer.WriteStructure("Aspect Container", () =>
            {
                m_AspectContainer.Serialize(serializer);
            });
            serializer.WriteSingle(m_Height, "Height");
            serializer.WriteInt32(m_AreaID, "Area ID");
            serializer.WriteInt32(m_CustomID, "Custom ID");
            serializer.WriteInt32((int)m_Attribute, "Attribute");
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            base.EditorDeserialize(deserializer, label);
            var version = deserializer.ReadInt32("ShapeObject.Version");
            m_ShapeLayerID = deserializer.ReadInt32("Shape System Layer ID");
            m_Enabled = deserializer.ReadBoolean("Is Enabled");
            m_Position = deserializer.ReadVector3("Position");
            m_Rotation = deserializer.ReadQuaternion("Rotation");
            m_Scale = deserializer.ReadVector3("Scale");
            m_Name = deserializer.ReadString("Name");
            m_Color = deserializer.ReadColor("Color");
            var vertices = deserializer.ReadVector3List("Vertices");
            m_Polygon = new Polygon(vertices);
            deserializer.ReadStructure("Aspect Container", () =>
            {
                m_AspectContainer = IAspectContainer.Create();
                m_AspectContainer.Deserialize(deserializer);
            });
            m_Height = deserializer.ReadSingle("Height");
            m_AreaID = deserializer.ReadInt32("Area ID");
            m_CustomID = deserializer.ReadInt32("Custom ID");
            m_Attribute = (ObstacleAttribute)deserializer.ReadInt32("Attribute");
        }

        public override bool SetAspect(int objectID, string name, IAspect aspect)
        {
            if (base.SetAspect(objectID, name, aspect))
            {
                return true;
            }

            if (name == ShapeDefine.ENABLE_SHAPE_NAME)
            {
                SetEnabled(aspect.GetBoolean());
                return true;
            }

            if (name == ShapeDefine.SHAPE_NAME)
            {
                m_Name = aspect.GetString();
                return true;
            }

            if (name == ShapeDefine.ROTATION_NAME)
            {
                m_Rotation = aspect.GetQuaternion();
                return true;
            }

            if (name == ShapeDefine.SCALE_NAME)
            {
                m_Scale = aspect.GetVector3();
                return true;
            }

            if (name == ShapeDefine.POSITION_NAME)
            {
                m_Position = aspect.GetVector3();
                return true;
            }

            if (name == ShapeDefine.COLOR_NAME)
            {
                m_Color = aspect.GetColor();
                return true;
            }

            if (name.StartsWith(ShapeDefine.VERTEX_POSITION_NAME))
            {
                var tokens = name.Split("-");
                var index = int.Parse(tokens[1]);
                m_Polygon.SetVertexPosition(index, aspect.GetVector3());
                return true;
            }

            return false;
        }

        public override IAspect GetAspect(int objectID, string name)
        {
            var aspect = base.GetAspect(objectID, name);
            if (aspect != null)
            {
                return aspect;
            }

            if (name == ShapeDefine.ENABLE_SHAPE_NAME)
            {
                return IAspect.FromBoolean(m_Enabled);
            }

            if (name == ShapeDefine.SHAPE_NAME)
            {
                return IAspect.FromString(m_Name);
            }

            if (name == ShapeDefine.ROTATION_NAME)
            {
                return IAspect.FromQuaternion(Rotation);
            }

            if (name == ShapeDefine.SCALE_NAME)
            {
                return IAspect.FromVector3(Scale);
            }

            if (name == ShapeDefine.POSITION_NAME)
            {
                return IAspect.FromVector3(Position);
            }

            if (name == ShapeDefine.COLOR_NAME)
            {
                return IAspect.FromColor(Color);
            }

            if (name.StartsWith(ShapeDefine.VERTEX_POSITION_NAME))
            {
                var tokens = name.Split("-");
                var index = int.Parse(tokens[1]);
                return IAspect.FromVector3(m_Polygon.GetVertexPosition(index));
            }

            Debug.Assert(false, $"Unknown aspect {name}");
            return null;
        }

        [SerializeField]
        private int m_LayerID;
        [SerializeField]
        private Polygon m_Polygon;
        [SerializeField]
        private Vector3 m_Position;
        [SerializeField]
        private Vector3 m_Scale = Vector3.one;
        [SerializeField]
        private Quaternion m_Rotation = Quaternion.identity;
        [SerializeField]
        private bool m_Enabled = true;
        [SerializeField]
        private string m_Name = "Shape";
        [SerializeField]
        private IAspectContainer m_AspectContainer = IAspectContainer.Create();
        [SerializeField]
        private Color m_Color = Color.white;
        [SerializeField]
        private int m_AreaID = 0;
        [SerializeField]
        private int m_CustomID = 0;
        [SerializeField]
        private float m_Height = 0;
        [SerializeField]
        private ObstacleAttribute m_Attribute;
        [SerializeField]
        private int m_ShapeLayerID;
        private Color m_OverriddenColor = Color.white;
        private bool m_UseOverriddenColor = false;
        private ShapeSystemLayer m_ShapeSystemLayer;
        private const int m_Version = 1;
    }
}