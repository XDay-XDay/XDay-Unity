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

namespace XDay.WorldAPI.Region.Editor
{
    internal partial class RegionSystemLayer
    {
        public List<Vector3> GetOutlinePolygon(int id, List<Vector2Int> coords)
        {
            m_EdgeStartPos.Clear();
            //忽略hole
            for (int i = 0; i < coords.Count; ++i)
            {
                int x = coords[i].x;
                int y = coords[i].y;
                int left = GetRegionID(x - 1, y);
                if (left != id)
                {
                    AddBorderEdge(x, y, x, y + 1);
                }
                int top = GetRegionID(x, y + 1);
                if (top != id)
                {
                    AddBorderEdge(x, y + 1, x + 1, y + 1);
                }
                int right = GetRegionID(x + 1, y);
                if (right != id)
                {
                    AddBorderEdge(x + 1, y + 1, x + 1, y);
                }
                int bottom = GetRegionID(x, y - 1);
                if (bottom != id)
                {
                    AddBorderEdge(x + 1, y, x, y);
                }
            }

            return ConnectEdges();
        }

        private void AddBorderEdge(int startX, int startY, int endX, int endY)
        {
            var startPos = CoordinateToPosition(startX, startY);
            if (!m_EdgeStartPos.ContainsKey(startPos))
            {
                var endPos = CoordinateToPosition(endX, endY);
                var borderEdge = new BorderEdge { Start = startPos, End = endPos };
                m_EdgeStartPos.Add(startPos, borderEdge);
            }
        }

        private List<Vector3> ConnectEdges()
        {
            List<Vector3> outline = new();
            int nEdges = m_EdgeStartPos.Count;
            Debug.Assert(nEdges > 0);
            BorderEdge firstEdge = null;
            foreach (var p in m_EdgeStartPos)
            {
                firstEdge = p.Value;
                break;
            }

            outline.Add(firstEdge.Start);

            for (int i = 1; i < nEdges; ++i)
            {
                m_EdgeStartPos.TryGetValue(firstEdge.End, out var nextEdge);
                Debug.Assert(nextEdge != null);

                if (!SameDirection(firstEdge, nextEdge))
                {
                    outline.Add(nextEdge.Start);
                }

                firstEdge = nextEdge;
            }

            return outline;
        }

        private bool SameDirection(BorderEdge a, BorderEdge b)
        {
            var dirA = a.End - a.Start;
            var dirB = b.End - b.Start;
            dirA.Normalize();
            dirB.Normalize();
            float dot = Vector3.Dot(dirA, dirB);
            if (Mathf.Approximately(dot, 1) || Mathf.Approximately(dot, -1))
            {
                return true;
            }
            return false;
        }

        private class BorderEdge
        {
            public Vector3 Start;
            public Vector3 End;
        }

        private Dictionary<Vector3, BorderEdge> m_EdgeStartPos = new();

        private const int m_ExportVersion = 1;
    }
}
