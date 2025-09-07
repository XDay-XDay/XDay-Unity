using System.Collections.Generic;
using UnityEngine;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Shape.Editor
{
    /// <summary>
    /// 将各个Shape相交的顶点算作一个
    /// </summary>
    internal partial class ShapeCombiner
    {
        public void Combine(List<ShapeObject> shapes, float vertexSize, Vector2 min, Vector2 max)
        {
            UndoSystem.NextGroupAndJoin();
            //init
            float gridSize = Mathf.Max(vertexSize * 2, 300);
            m_Grid = new Grid(min, max, gridSize);
            foreach (var shape in shapes)
            {
                var vertexCount = shape.VertexCount;
                for (var i = 0; i < vertexCount; i++)
                {
                    var worldPos = shape.GetVertexWorldPosition(i);
                    var vertex = new Vertex() { Index = i, Position = worldPos, Shape = shape };
                    m_Grid.Add(vertex);
                }
            }

            //check
            var overlaps = m_Grid.FindOverlappedVertices(vertexSize*0.5f);
            foreach (var overlap in overlaps)
            {
                Vector3 pos = Vector3.zero;
                foreach (var vert in overlap.Vertices)
                {
                    pos += vert.Position;
                }
                pos /= overlap.Vertices.Count;
                foreach (var vert in overlap.Vertices)
                {
                    var localPos = vert.Shape.TransformToLocalPosition(pos);
                    UndoSystem.SetAspect(vert.Shape, $"{ShapeDefine.VERTEX_POSITION_NAME}-{vert.Index}", IAspect.FromVector3(localPos), "Set Shape Vertex Position", vert.Shape.ShapeSystem.ID, UndoActionJoinMode.None);
                }
            }
        }

        private Grid m_Grid;
    }
}
