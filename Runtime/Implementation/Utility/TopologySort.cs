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

namespace XDay
{
    public class TopologySort<ID> where ID : IEquatable<ID>
    {
        //检查是否有循环引用
        public bool CheckLoop(Func<ID, ID, bool> errorCallback)
        {
            bool hasLoop = false;
            for (var i = 0; i < m_Nodes.Count; ++i)
            {
                foreach (var id in m_Nodes[i].ToNodeIDs)
                {
                    if (HasCircularDependency(id, m_Nodes[i].ID))
                    {
                        if (errorCallback != null)
                        {
                            //可以通过errorCallback检测是否是真正的loop,因为某些情况下就算node有循环依赖关系,但是可以允许这种情况存在
                            hasLoop |= errorCallback(m_Nodes[i].ID, id);
                        }
                        else
                        {
                            hasLoop = true;
                        }
                    }
                }
            }
            return hasLoop;
        }

        //from is master, to is slave,拓扑排序是从根节点执行到叶子节点,所以这里from是被依赖节点,to是依赖节点
        public void AddEdge(ID fromNodeID, ID toNodeID)
        {
            var fromNode = GetNode(fromNodeID);
            if (fromNode == null)
            {
                fromNode = new Node
                {
                    ID = fromNodeID
                };
                m_Nodes.Add(fromNode);
            }

            var toNode = GetNode(toNodeID);
            if (toNode == null)
            {
                toNode = new Node
                {
                    ID = toNodeID
                };
                m_Nodes.Add(toNode);
            }

            if (!fromNode.ToNodeIDs.Contains(toNodeID))
            {
                fromNode.ToNodeIDs.Add(toNodeID);
            }
        }

        public void Sort(List<ID> sortedNodeIDs)
        {
            m_VisitedNodes.Clear();

            while (m_VisitedNodes.Count != m_Nodes.Count)
            {
                for (var i = 0; i < m_Nodes.Count; ++i)
                {
                    if (!m_VisitedNodes.Contains(m_Nodes[i].ID))
                    {
                        Traverse(m_Nodes[i], sortedNodeIDs);
                    }
                }
            }

            int n = sortedNodeIDs.Count / 2;
            for (int i = 0; i < n; ++i)
            {
                (sortedNodeIDs[i], sortedNodeIDs[sortedNodeIDs.Count - 1 - i]) = (sortedNodeIDs[sortedNodeIDs.Count - 1 - i], sortedNodeIDs[i]);
            }
        }

        private Node GetNode(ID id)
        {
            foreach (var node in m_Nodes)
            {
                if (node.ID.Equals(id))
                {
                    return node;
                }
            }
            return null;
        }

        private void Traverse(Node node, List<ID> sortedNodeIDs)
        {
            m_VisitedNodes.Add(node.ID);
            var toNodeIDs = node.ToNodeIDs;

            foreach (var neighbourName in toNodeIDs)
            {
                if (!m_VisitedNodes.Contains(neighbourName))
                {
                    var neighbourNode = GetNode(neighbourName);
                    Traverse(neighbourNode, sortedNodeIDs);
                }
            }

            sortedNodeIDs.Add(node.ID);
        }

        //检查是否有循环引用
        private bool HasCircularDependency(ID fromNodeID, ID toNodeID)
        {
            if (!fromNodeID.Equals(toNodeID))
            {
                return Search(fromNodeID, toNodeID);
            }
            return false;
        }

        private bool Search(ID fromNodeID, ID toNodeID)
        {
            List<Node> nodes = new();
            var fromNode = GetNode(fromNodeID);
            Debug.Assert(fromNode != null);
            var toNode = GetNode(toNodeID);
            Debug.Assert(toNode != null);
            nodes.Add(fromNode);
            HashSet<ID> visitedNodes = new();
            while (nodes.Count > 0)
            {
                var cur = nodes[^1];
                nodes.RemoveAt(nodes.Count - 1);
                visitedNodes.Add(cur.ID);
                if (cur == toNode)
                {
                    return true;
                }

                foreach (var neightbourNodeID in cur.ToNodeIDs)
                {
                    if (!visitedNodes.Contains(neightbourNodeID))
                    {
                        var neighbourNode = GetNode(neightbourNodeID);
                        nodes.Add(neighbourNode);
                    }
                }
            }
            return false;
        }

        private readonly List<Node> m_Nodes = new();
        private readonly HashSet<ID> m_VisitedNodes = new();

        private class Node
        {
            public ID ID;
            public List<ID> ToNodeIDs = new();
        };
    };
}