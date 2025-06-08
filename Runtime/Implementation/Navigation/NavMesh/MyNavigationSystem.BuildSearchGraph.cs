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

namespace XDay.NavigationAPI
{
    public partial class MyNavigationSystem
    {
        void BuildEdgeSearchGraph()
        {
            m_ConnectedNavigationEdges = new List<int>[m_NavigationEdges.Count];
            //2d导航网格的一条navigation edge最多相连4个navigation edge, 3d导航网格一条edge可以连接多个navigation edge
            for (int i = 0; i < m_NavigationEdges.Count; ++i)
            {
                m_ConnectedNavigationEdges[i] = new List<int>(4);
            }

            foreach (var triangle in m_Triangles)
            {
                int edgeCount = triangle.GetNavigationEdgeCount();
                for (int loop = 0; loop < edgeCount; ++loop)
                {
                    int edgeIndex = triangle.GetNavigationEdgeIndex(loop);
                    for (int i = 1; i < edgeCount; ++i)
                    {
                        int otherEdgeIndex = triangle.GetNavigationEdgeIndex((loop + i) % edgeCount);
                        m_ConnectedNavigationEdges[edgeIndex].Add(otherEdgeIndex);
                        Debug.Assert(m_ConnectedNavigationEdges[edgeIndex].Count <= 4);
                    }
                }
            }
        }
    }
}
