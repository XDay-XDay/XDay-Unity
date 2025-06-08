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
using Priority_Queue;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.NavigationAPI
{
    public partial class MyNavigationSystem
    {
        //寻路图节点
        internal class Node : FastPriorityQueueNode
        {
            public int Key { get { return EdgeIndex; } }
            public float Cost { get { return PathCost + HCost; } }

            //navigation edge index
            public int EdgeIndex;
            public float PathCost;
            public float HCost;
            public Node From;
        }

        //用户管理线程时使用
        public class ThreadContextData
        {
            internal FastPriorityQueue<Node> PriorityQueue { get { return m_PriorityQueue; } }
            internal bool[] VisitedEdges { get { return m_VisitedEdges; } }

            public ThreadContextData(int navigationEdgeCount)
            {
                m_PriorityQueue = new(maxNodes: 10000);
                m_VisitedEdges = new bool[navigationEdgeCount];
            }

            public void Reset()
            {
                System.Array.Clear(m_VisitedEdges, 0, m_VisitedEdges.Length);
                m_PriorityQueue.Clear();
                m_OpenList.Clear();
            }

            internal Node GetOpenListNode(int key)
            {
                m_OpenList.TryGetValue(key, out var node);
                return node;
            }

            internal void AddToOpenList(Node node)
            {
                m_OpenList.Add(node.Key, node);
            }

            internal void RemoveFromOpenList(Node node)
            {
                m_OpenList.Remove(node.Key);
            }

            private readonly FastPriorityQueue<Node> m_PriorityQueue;
            //如果edge超多,可以优化为bit field
            private readonly bool[] m_VisitedEdges;      
            private readonly Dictionary<int, Node> m_OpenList = new();
        }

        //context data:有用户提供的线程上下文,如果为null,则使用框架内部的context data,多个线程必须创建多个context data
        public bool FindPath(Vector3 start, Vector3 end, List<Vector3> path, ThreadContextData context, PathFindingOption options = PathFindingOption.SimplifyPath)
        {
            context ??= mDefaultContext;
            path.Clear();
            context.Reset();

            Triangle startTriangle;
            Triangle endTriangle;
            if (options.HasFlag(PathFindingOption.XYZAxis))
            {
                startTriangle = FindTriangleContains3D(start, out var realStart);
                start = realStart;
                endTriangle = FindTriangleContains3D(end, out var realEnd);
                end = realEnd;
            }
            else
            {
                startTriangle = FindTriangleContains2D(start);
                endTriangle = FindTriangleContains2D(end);
            }

            if (startTriangle == null || endTriangle == null)
            {
                return false;
            }

            if (startTriangle == endTriangle)
            {
                path.Add(start);
                path.Add(end);
                return true;
            }

            var priorityQueue = context.PriorityQueue;
            var visitedEdges = context.VisitedEdges;

            int navigationEdgeCount = startTriangle.GetNavigationEdgeCount();
            for (int i = 0; i < navigationEdgeCount; ++i)
            {
                var edgeIndex = startTriangle.GetNavigationEdgeIndex(i);
                var edgeMiddlePos = GetEdgeMiddlePoint(edgeIndex);
                Node node = new()
                {
                    PathCost = Vector3.Distance(edgeMiddlePos, start),
                    HCost = Vector3.Distance(edgeMiddlePos, end),
                    EdgeIndex = edgeIndex
                };
                context.AddToOpenList(node);
                priorityQueue.Enqueue(node, node.Cost);
            }

            bool foundPath = false;
            while (priorityQueue.Count > 0)
            {
                var node = priorityQueue.Dequeue();
                context.RemoveFromOpenList(node);
                var edgeMiddlePos = GetEdgeMiddlePoint(node.EdgeIndex);

                visitedEdges[node.EdgeIndex] = true;

                if (node.EdgeIndex == endTriangle.EdgeIndex0 ||
                    node.EdgeIndex == endTriangle.EdgeIndex1 ||
                    node.EdgeIndex == endTriangle.EdgeIndex2)
                {
                    GetPath(node, path, start, end, options);
                    foundPath = true;
                    break;
                }

                var neighbours = m_ConnectedNavigationEdges[node.EdgeIndex];
                foreach (var neighbourEdgeIndex in neighbours)
                {
                    if (visitedEdges[neighbourEdgeIndex] == false)
                    {
                        var neighbourEdgeMiddlePos = GetEdgeMiddlePoint(neighbourEdgeIndex);

                        float pathCost = node.PathCost + Vector3.Distance(neighbourEdgeMiddlePos, edgeMiddlePos);
                        float hCost = Vector3.Distance(neighbourEdgeMiddlePos, end);
                        Node neighbourNode = context.GetOpenListNode(neighbourEdgeIndex);
                        if (neighbourNode == null)
                        {
                            neighbourNode = new Node
                            {
                                EdgeIndex = neighbourEdgeIndex,
                                PathCost = pathCost,
                                HCost = hCost,
                                From = node
                            };
                            priorityQueue.Enqueue(neighbourNode, neighbourNode.Cost);
                            context.AddToOpenList(neighbourNode);
                        }
                        else if (neighbourNode.PathCost > pathCost)
                        {
                            neighbourNode.From = node;
                            neighbourNode.HCost = hCost;
                            neighbourNode.PathCost = pathCost;
                            priorityQueue.UpdatePriority(neighbourNode, neighbourNode.Cost);
                        }
                    }
                }
            }

            return foundPath;
        }

        private void GetPath(Node node, List<Vector3> points, Vector3 start, Vector3 end, PathFindingOption options)
        {
            if (options.HasFlag(PathFindingOption.SimplifyPath))
            {
                List<int> edges = new List<int>();
                while (node != null)
                {
                    edges.Add(node.EdgeIndex);
                    node = node.From;
                }
                Helper.ReverseList(edges);
                OptimizePath(start, end, edges);
                points.AddRange(mFunnelPath);
            }
            else
            {
                points.Add(end);
                while (node != null)
                {
                    var pos = GetEdgeMiddlePoint(node.EdgeIndex);
                    points.Add(pos);
                    node = node.From;
                }
                points.Add(start);
                Helper.ReverseList(points);
            }
        }
    }
}
