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

using System;
using System.Collections.Generic;
using UnityEngine;

namespace XDay.UtilityAPI.Shape
{
    [Serializable]
    public class Shape2DColliderData
    {
        public Rect Bounds => m_Polygon.Bounds;
        public Rect WorldBounds => m_Polygon.Bounds.ToBounds().Transform(m_Transform.localToWorldMatrix).ToRect();
        public int VertexCount => m_Polygon.VertexCount;
        public float VertexDisplaySize => m_VertexDisplaySize;
        public bool ShowVertexIndex => m_ShowVertexIndex;
        public List<Vector3> VerticesCopy => m_Polygon.VerticesCopy;
        public bool IsValid => m_Transform != null;

        public void Initialize(Transform transform)
        {
            Debug.Assert(transform != null, "Transform is null");
            m_Transform = transform;

            if (m_Polygon == null || m_Polygon.Vertices == null)
            {
                Create(new List<GameObject>() { transform.gameObject });
            }
        }

        public void Create(List<GameObject> gameObjects)
        {
            var polygon = new List<Vector3>();

            var boundingHull = ConvexHull2DBuilder.BuildConvexHull(inWorldSpace: false, gameObjects);
            if (boundingHull.Count == 0)
            {
                var bounds = gameObjects[0].QueryBounds();
                if (bounds.size == Vector3.zero)
                {
                    bounds = new Bounds(Vector3.zero, Vector3.one * 2.0f);
                }
                var min = bounds.min;
                var max = bounds.max;
                polygon = new List<Vector3> {
                    new(min.x, 0, min.z),
                    new(max.x, 0, min.z),
                    new(max.x, 0, max.z),
                    new(min.x, 0, max.z),
                    };
            }
            else
            {
                for (var k = 0; k < boundingHull.Count; ++k)
                {
                    polygon.Add(new Vector3(boundingHull[k].x, 0, boundingHull[k].z));
                }
            }

            var simplifiedPolygon = PolygonSimplification.Process(polygon);
            m_Polygon = new Polygon(simplifiedPolygon);

            Reverse();

            m_VertexDisplaySize = Mathf.Max(m_Polygon.Bounds.width, m_Polygon.Bounds.height) * 0.05f;
        }

        public void Reverse()
        {
            if (m_Polygon != null && m_Polygon.IsClockwiseWinding)
            {
                m_Polygon.Reverse();
            }
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

        public List<Vector3> GetPolyonInLocalSpace()
        {
            return m_Polygon.Vertices;
        }

        private Transform m_Transform;

        [SerializeField]
        [HideInInspector]
        private Polygon m_Polygon;

        [SerializeField]
        [HideInInspector]
        private bool m_ShowVertexIndex = false;

        [SerializeField]
        [HideInInspector]
        private float m_VertexDisplaySize = 1.0f;
    }
}
