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
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XDay.BehaviourTreeAPI
{
    public enum NodeStatus
    {
        Running,
        Success,
        Fail,
    }

    public enum NodeEvent
    {
        Awake,
        Start,
        Stop,
        Reset,
    }

    public abstract class Node
    {
        public int ID => m_ID;
        public CompoundNode Parent => m_Parent;
        public int ExecuteCount => m_ExecuteCount;
        public bool Completed => m_Status != NodeStatus.Running;
        public bool Started => m_Started;
        public bool HasBreakpoint { get => m_HasBreakpoint; set => m_HasBreakpoint = value; }
        public string Name { get => m_Name; set => m_Name = value; }
        public string Comment { get => m_Comment; set => m_Comment = value; }
        public NodeStatus Status => m_Status;
        public event Action<Node, NodeEvent> Event;
        public abstract float Progress { get; }
        public int SortOrder
        {
            get => m_SortOrder;
            set
            {
                if (m_SortOrder != value)
                {
                    m_SortOrder = value;
                    m_Parent?.OnChildSortOrderChanged(this);
                }
            }
        }

        public Node(int id, string name, BehaviourTree tree)
        {
            m_ID = id;
            m_Name = name;
            m_Tree = tree;
        }

        public Node Clone(bool cloneOne)
        {
            Node node = DoClone(cloneOne);
            node.Comment = m_Comment;
            node.SortOrder = m_SortOrder;
            node.HasBreakpoint = m_HasBreakpoint;
            return node;
        }

        public void SetParent(CompoundNode parent)
        {
            if (m_Parent != parent)
            {
                bool ok = CheckNoLoop(parent);
                if (ok)
                {
                    if (parent != null &&
                        parent.Children.Count == parent.MaxChildrenCount())
                    {
                        parent.Children[^1].SetParent(null);
                    }

                    m_Parent?.RemoveChild(this);
                    m_Parent = parent;
                    m_Parent?.AddChild(this);
                }
                else
                {
                    Debug.LogError($"{Name} can't set parent {parent.Name}, loop detected!");
                }
            }
        }

        public void Awake()
        {
            OnAwake();
            OnPostAwake();
        }

        public void Stop()
        {
            OnStop();
            OnPostStop();
        }

        public NodeStatus Tick()
        {
            if (!m_Started)
            {
                m_Started = true;
                OnStart();
            }

            m_Status = OnTick();
            return m_Status;
        }

        //能否执行
        public virtual bool CanExecute() { return true; }

        protected NodeStatus Complete(bool success)
        {
            Reset();
            return success ? NodeStatus.Success : NodeStatus.Fail;
        }

        internal virtual void Reset()
        {
            m_Started = false;
            OnReset();
            ++m_ExecuteCount;
        }

        protected virtual void OnChildSortOrderChanged(Node child) { }
        protected virtual NodeStatus OnTick()
        {
            return NodeStatus.Success;
        }

        /// <summary>
        /// 行为树开始一次执行时调用
        /// </summary>
        protected virtual void OnAwake()
        {
            m_Status = NodeStatus.Running;
            Event?.Invoke(this, NodeEvent.Awake);
        }
        private protected virtual void OnPostAwake() { }
        protected virtual void OnStop()
        {
            Event?.Invoke(this, NodeEvent.Stop);
            Reset();
        }
        private protected virtual void OnPostStop() { }
        protected virtual void OnReset()
        {
            Event?.Invoke(this, NodeEvent.Reset);
        }
        /// <summary>
        /// 在每个node第一次tick时执行
        /// </summary>
        protected virtual void OnStart()
        {
            if (m_HasBreakpoint)
            {
#if UNITY_EDITOR
                EditorApplication.isPaused = true;
#endif
            }

            Event?.Invoke(this, NodeEvent.Start);
        }

        protected abstract Node DoClone(bool cloneOne);

        internal abstract NodeState CreateState();
        internal virtual void SetState(NodeState state)
        {
            state.ID = m_ID;
            state.Name = m_Name;
            state.Comment = m_Comment;
            state.SortOrder = m_SortOrder;
            state.ParentID = m_Parent == null ? 0 : m_Parent.ID;
            state.HasBreakpoint = m_HasBreakpoint;
        }

        //是否是ancestor的后代,如果ancestor是this,也返回true
        public bool IsDescendantOf(Node ancestor)
        {
            if (ancestor is not CompoundNode compound)
            {
                return false;
            }

            return compound.ContainsChild(this);
        }

        public int GetSiblingIndex()
        {
            if (m_Parent == null)
            {
                return -1;
            }

            return m_Parent.Children.IndexOf(this);
        }

        private bool CheckNoLoop(Node newParent)
        {
            if (newParent == null)
            {
                return true;
            }

            if (this is CompoundNode self)
            {
                return !self.ContainsChild(newParent);
            }
            return true;
        }

        [SerializeField]
        private readonly int m_ID;
        [SerializeField]
        private string m_Name;
        [SerializeField]
        private string m_Comment;
        //从小到大排序
        [SerializeField]
        private int m_SortOrder = 0;
        [SerializeField]
        private bool m_HasBreakpoint = false;
        private bool m_Started = false;
        protected NodeStatus m_Status = NodeStatus.Running;
        [SerializeField]
        private CompoundNode m_Parent;
        private int m_ExecuteCount = 0;
        protected readonly BehaviourTree m_Tree;
    }
}