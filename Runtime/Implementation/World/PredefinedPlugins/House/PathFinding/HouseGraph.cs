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
using XDay.NavigationAPI;

namespace XDay.WorldAPI.House
{
    internal partial class HouseGraph
    {
        public HouseGraph(List<HouseData> houses, ITaskSystem taskSystem)
        {
            m_TaskSystem = taskSystem;

            CreateNodes(houses);

            CreateEdges(houses);
        }

        public bool FindPath(Vector3 start, Vector3 end, HouseData startHouse, HouseData targetHouse, List<List<Vector3>> pathInRooms)
        {
            if (startHouse == null || targetHouse == null)
            {
                Debug.LogError($"起点{start}或终点{end}不在房间内,无法寻路!");
                return false;
            }

            if (startHouse == targetHouse)
            {
                //同一个房间内
                var path = new List<Vector3>();
                startHouse.FindPath(start, end, path);
                pathInRooms.Add(path);
                return path.Count > 0;
            }

            var startNode = CreateVirtualNode(START_NODE_ID, "Start", start, startHouse);
            var endNode = CreateVirtualNode(END_NODE_ID, "End", end, targetHouse);

            m_GraphPathFinder = new(m_Edges, m_TaskSystem);
            List<IGraphEdge> edges = new();
            m_GraphPathFinder.CalculatePath(startNode, endNode, edges);

            for (var i = 0; i < edges.Count; ++i)
            {
                var edge = edges[i];
                var from = edge.From;
                var to = edge.To;

                if (from.GridData == to.GridData)
                {
                    var house = from.GridData as HouseData;
                    var path = new List<Vector3>();
                    house.FindPath(from.Position, to.Position, path);
                    pathInRooms.Add(path);
                }
            }

            RemoveVirtualNodeEdges(startHouse);
            RemoveVirtualNodeEdges(targetHouse);
            foreach (var edge in m_VirtualEdges)
            {
                bool ok = m_Edges.Remove(edge);
                Debug.Assert(ok);
            }
            m_VirtualEdges.Clear();

            bool removed = m_Nodes.Remove(startNode);
            Debug.Assert(removed);
            removed = m_Nodes.Remove(endNode);
            Debug.Assert(removed);

            return pathInRooms.Count > 0;
        }

        private void CreateNodes(List<HouseData> houses)
        {
            m_Nodes = new();
            foreach (var house in houses)
            {
                foreach (var teleporter in house.Teleporters)
                {
                    var node = new Node()
                    {
                        Teleporter = teleporter,
                    };
                    m_Nodes.Add(node);
                }
            }
        }

        private IGraphNode CreateVirtualNode(int id, string name, Vector3 pos, HouseData house)
        {
            var node = new VirtualNode(id, name, pos, house);
            m_Nodes.Add(node);

            foreach (var teleporter in house.Teleporters)
            {
                var teleporterNode = GetNode(teleporter);
                var toEdge = CreateEdge(node, teleporterNode, Vector3.Distance(node.Position, teleporterNode.Position), true);
                var fromEdge = CreateEdge(teleporterNode, node, Vector3.Distance(node.Position, teleporterNode.Position), true);
                m_VirtualEdges.Add(toEdge);
                m_VirtualEdges.Add(fromEdge);
                node.Edges.Add(toEdge);
                teleporterNode.Edges.Add(fromEdge);
            }

            return node;
        }

        private void RemoveVirtualNodeEdges(HouseData house)
        {
            bool removed = false;
            foreach (var teleporter in house.Teleporters)
            {
                var teleporterNode = GetNode(teleporter);
                for (var i = teleporterNode.Edges.Count - 1; i >= 0; --i)
                {
                    if (teleporterNode.Edges[i].IsVirtual)
                    {
                        removed = true;
                        teleporterNode.Edges.RemoveAt(i);
                    }
                }
            }
            Debug.Assert(removed);
        }

        private void CreateEdges(List<HouseData> houses)
        {
            m_Edges = new();

            for (var i = 0; i < m_Nodes.Count; ++i)
            {
                for (var j = i + 1; j < m_Nodes.Count; ++j)
                {
                    CreateEdgeBetweenDifferentHouse(m_Nodes[i], m_Nodes[j]);
                }
            }

            foreach (var house in houses)
            {
                CreateEdgeInSameHouse(house);
            }
        }

        private void CreateEdgeInSameHouse(HouseData house)
        {
            var n = house.Teleporters.Count;
            if (n > 1)
            {
                for (var i = 0; i < n; i++)
                {
                    var cur = house.Teleporters[i];
                    var next = house.Teleporters[(i + 1) % n];
                    Node curNode = GetNode(cur);
                    Node nextNode = GetNode(next);

                    curNode.Edges.Add(CreateEdge(curNode, nextNode, Vector3.Distance(cur.WorldPosition, next.WorldPosition), false));
                }
            }
        }

        private void CreateEdgeBetweenDifferentHouse(IGraphNode ga, IGraphNode gb)
        {
            var a = ga as Node;
            var b = gb as Node;
            if (a.Teleporter.ConnectedID == b.Teleporter.ConfigID &&
                a.Teleporter.Enabled &&
                b.Teleporter.Enabled)
            {
                a.Edges.Add(CreateEdge(a, b, Vector3.Distance(a.Teleporter.WorldPosition, b.Teleporter.WorldPosition), false));
            }

            if (b.Teleporter.ConnectedID == a.Teleporter.ConfigID &&
                a.Teleporter.Enabled &&
                b.Teleporter.Enabled)
            {
                b.Edges.Add(CreateEdge(b, a, Vector3.Distance(a.Teleporter.WorldPosition, b.Teleporter.WorldPosition), false));
            }
        }

        private Node GetNode(Teleporter teleporter)
        {
            foreach (var node in m_Nodes)
            {
                if (node is Node n && n.Teleporter == teleporter)
                {
                    return n;
                }
            }
            return null;
        }

        private IGraphEdge CreateEdge(IGraphNode from, IGraphNode to, float cost, bool isVirtual)
        {
            var edge = new Edge(isVirtual)
            {
                From = from,
                To = to,
                Cost = cost,
            };
            m_Edges.Add(edge);
            return edge;
        }

        private class Node : IGraphNode
        {
            public int ID => Teleporter.ConfigID;
            public string Name => $"{Teleporter.House.Name}-{Teleporter.Name}";
            public Vector3 Position => Teleporter.WorldPosition;
            List<IGraphEdge> IGraphNode.Edges => Edges;
            public IGridData GridData => Teleporter.House;
            public bool IsEnabled => Teleporter.Enabled;

            public List<IGraphEdge> Edges = new();
            public Teleporter Teleporter;
        }

        private class VirtualNode : IGraphNode
        {
            public VirtualNode(int id, string name, Vector3 position, HouseData house)
            {
                ID = id;
                Name = name;
                Position = position;
                m_House = house;
            }

            int IGraphNode.ID => ID;
            string IGraphNode.Name => Name;
            List<IGraphEdge> IGraphNode.Edges => Edges;
            Vector3 IGraphNode.Position => Position;
            public IGridData GridData => m_House;
            public bool IsEnabled => true;

            public List<IGraphEdge> Edges = new();
            public Vector3 Position;
            public string Name;
            public int ID;
            private readonly HouseData m_House;
        }

        private class Edge : IGraphEdge
        {
            public IGraphNode From;
            public IGraphNode To;
            public float Cost;

            public bool Enabled => From.IsEnabled;
            IGraphNode IGraphEdge.From => From;
            IGraphNode IGraphEdge.To => To;
            public bool IsVirtual => m_Virtual;

            public Edge(bool isVirtual)
            {
                m_Virtual = isVirtual;
            }

            private readonly bool m_Virtual;
        }

        private List<IGraphNode> m_Nodes;
        private List<IGraphEdge> m_Edges;
        private List<IGraphEdge> m_VirtualEdges = new();

        private const int START_NODE_ID = 1999999;
        private const int END_NODE_ID = 2999999;
        private ITaskSystem m_TaskSystem;
        private HouseGraphAStarPathFinder m_GraphPathFinder;
    }
}