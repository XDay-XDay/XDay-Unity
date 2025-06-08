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

namespace XDay.BehaviourTreeAPI
{
    public class BehaviourTree
    {
        public int ID => m_ID;
        public string Name => m_Name;
        public Node Root => m_Root;
        public NodeStatus Status => m_Status;
        public Dictionary<int, Node> Nodes => m_AllNodes;
        public BehaviourTreeState State => m_State;
        public VariableManager VariableManager => m_VariableManager;
        public event Action<Node, Node> EventCreateNode
        {
            add
            {
                m_EventCreateNode -= value;
                m_EventCreateNode += value;
            }
            remove
            {
                m_EventCreateNode -= value;
            }
        }
        public event Action<Node> EventDestroyNode
        {
            add
            {
                m_EventDestroyNode -= value;
                m_EventDestroyNode += value;
            }
            remove
            {
                m_EventDestroyNode -= value;
            }
        }

        public BehaviourTree(int id, string name, BehaviourTreeState state, VariableManager variableManager)
        {
            m_ID = id;
            m_Name = name;
            m_State = state;
            m_VariableManager = variableManager;
        }

        /// <summary>
        /// 开始执行,会触发Awake事件
        /// </summary>
        public void Run()
        {
            if (m_Root == null)
            {
                Debug.LogError("No root");
                return;
            }

            if (m_Running)
            {
                return;
            }

            m_Running = true;

            m_Root?.Awake();
        }

        public void Tick()
        {
            if (m_Running && m_Root != null)
            {
                m_Status = m_Root.Tick();
                if (m_Status == NodeStatus.Fail ||
                    m_Status == NodeStatus.Success)
                {
                    Stop();
                }
            }
        }

        public void Stop()
        {
            if (m_Running)
            {
                m_Root?.Stop();
                m_Running = false;
            }
        }

        public void SetRoot(Node root)
        {
            if (m_Root != root)
            {
                Stop();
                m_Root = root;
            }
        }

        public T CreateNode<T>(string name, Node source) where T : Node
        {
            return CreateNode(typeof(T), name, source) as T;
        }

        public Node CreateNode(Type type, string name, Node source)
        {
            Debug.Assert(type.IsSubclassOf(typeof(Node)));

            var id = AllocateID();
            object[] args = { id, name, this };
            var node = Activator.CreateInstance(type, args) as Node;
            AddNode(node, source);
            return node;
        }

        public void AddNode(Node node, Node source = null)
        {
            if (node == null)
            {
                return;
            }

            m_AllNodes.Add(node.ID, node);
            m_EventCreateNode?.Invoke(node, source);
        }

        public void DestroyNode(Node node, bool deleteTopNodeOnly)
        {
            if (!deleteTopNodeOnly)
            {
                if (node is CompoundNode compoundNode)
                {
                    for (var i = compoundNode.Children.Count - 1; i >= 0; --i)
                    {
                        DestroyNode(compoundNode.Children[i], deleteTopNodeOnly);
                    }
                }
            }

            Debug.Log($"Destroy node: {node.Name}");

            node.SetParent(null);
            if (node is CompoundNode compound)
            {
                var children = new List<Node>(compound.Children);
                foreach (var child in children)
                {
                    child.SetParent(null);
                }
            }
            bool ok = m_AllNodes.Remove(node.ID);
            Debug.Assert(ok);
            m_EventDestroyNode?.Invoke(node);
            if (m_Root == node)
            {
                SetRoot(null);
            }
        }

        public BehaviourTreeState CreateState()
        {
            BehaviourTreeState state = ScriptableObject.CreateInstance<BehaviourTreeState>();
            state.RootID = m_Root != null ? m_Root.ID : 0;
            state.VariableDatas = m_VariableManager.CreateState().Variables;

            var sortedNodes = SortNodes();

            foreach (var node in sortedNodes)
            {
                var nodeState = node.CreateState();
                node.SetState(nodeState);
                state.NodesState.Add(nodeState);
            }

            return state;
        }

        private List<Node> SortNodes()
        {
            List<Node> nodes = new();
            nodes.AddRange(m_AllNodes.Values);
            nodes.Sort((a,b) =>
            {
                return a.GetSiblingIndex().CompareTo(b.GetSiblingIndex());
            });
            return nodes;
        }

        private int AllocateID()
        {
            var startID = 1;
            while (true)
            {
                if (!m_AllNodes.ContainsKey(startID))
                {
                    return startID;
                }
                ++startID;
            }
        }

        [SerializeField]
        private Node m_Root;
        [SerializeField]
        private readonly Dictionary<int, Node> m_AllNodes = new();
        [SerializeField]
        private readonly VariableManager m_VariableManager;
        private event Action<Node, Node> m_EventCreateNode;
        private event Action<Node> m_EventDestroyNode;
        private readonly int m_ID;
        private readonly string m_Name;
        private bool m_Running = false;
        private NodeStatus m_Status = NodeStatus.Running;
        //can't edit when running
        private BehaviourTreeState m_State;
    }
}
