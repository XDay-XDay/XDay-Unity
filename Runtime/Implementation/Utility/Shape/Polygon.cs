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

namespace XDay.UtilityAPI.Shape
{
    [System.Serializable]
    public class Polygon
    {
        public Rect Bounds => m_Bounds;
        public List<Vector3> Vertices => m_Vertices;
        public List<Vector3> VerticesCopy => new(m_Vertices);
        public bool IsClockwiseWinding => m_Vertices.IsClockwiseWinding();
        public int VertexCount => m_Vertices.Count;

        public Polygon()
        {
        }

        public Polygon(List<Vector3> vertices)
        {
            m_Vertices = new(vertices);
            
            UpdateBounds();
        }

        public void Reset()
        {
            m_Bounds = new Rect();
            m_Vertices?.Clear();
        }

        public Polygon Instantiate(bool copyVertices)
        {
            if (copyVertices)
            {
                return new Polygon(new List<Vector3>(m_Vertices));
            }
            return new Polygon(m_Vertices);
        }

        public void RotateAround(Vector3 pivot, float angle)
        {
            var rotation = Quaternion.Euler(0, angle, 0);
            for (var i = 0; i < m_Vertices.Count; ++i)
            {
                m_Vertices[i] = rotation * (m_Vertices[i] - pivot) + pivot;
            }
        }

        public void SetVertexPosition(int index, Vector3 position)
        {
            if (index >= 0 && index < m_Vertices.Count)
            {
                m_Vertices[index] = position;
            }
        }

        public Vector3 GetVertexPosition(int index)
        {
            if (index >= 0 && index < m_Vertices.Count)
            {
                return m_Vertices[index];
            }
            Debug.Assert(false, "Invalid Index");
            return Vector3.zero;
        }

        public int FindVertexIndex(Vector3 position)
        {
            for (var i = 0; i < m_Vertices.Count; ++i)
            {
                if (position == m_Vertices[i])
                {
                    return i;
                }
            }
            return -1;
        }

        public void UpdateBounds()
        {
            m_Bounds = m_Vertices.GetBounds2D();
        }

        public void Reverse()
        {
            m_Vertices?.Reverse();
        }

        public void Move(Vector3 offset)
        {
            for (var i = 0; i < m_Vertices.Count; ++i)
            {
                m_Vertices[i] = m_Vertices[i] + offset;
            }
        }

        public bool InsertVertex(int index, Vector3 localPosition)
        {
            m_Vertices.Insert(index, localPosition);

            UpdateBounds();

            return true;
        }

        public bool DeleteVertex(int index)
        {
            if (index >= 0 && index < m_Vertices.Count)
            {
                m_Vertices.RemoveAt(index);

                UpdateBounds();

                return true;
            }

            return false;
        }

        public void MoveVertex(int index, Vector3 offset)
        {
            if (index >= 0 && index < m_Vertices.Count)
            {
                m_Vertices[index] += offset;

                UpdateBounds();
            }
        }

        [SerializeField]
        private Rect m_Bounds;
        [SerializeField]
        private List<Vector3> m_Vertices;
    }
}
