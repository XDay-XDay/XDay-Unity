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
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Shape.Editor
{
    /// <summary>
    /// 将各个Shape相交的顶点合并成一个顶点
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
                    UndoSystem.SetAspect(vert.Shape, 
                        $"{ShapeDefine.VERTEX_POSITION_NAME}-{vert.Index}", 
                        IAspect.FromVector3(localPos), 
                        "Set Shape Vertex Position", 
                        vert.Shape.Layer.System.ID, 
                        UndoActionJoinMode.None);
                }
            }
        }

        private Grid m_Grid;
    }
}
