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

namespace XDay.BehaviourTreeAPI
{
    [System.Serializable]
    public abstract class CompoundNodeState : NodeState
    {
        public CompoundNodeState(Node node) : base(node)
        {
        }
    }

    public abstract class CompoundNode : Node
    {
        public List<Node> Children => m_Children;

        public Node RunningChild
        {
            get
            {
                if (m_CurrentChild >= 0 && m_CurrentChild < m_Children.Count)
                {
                    return m_Children[m_CurrentChild];
                }
                return null;
            }
        }

        public override float Progress
        {
            get
            {
                if (m_Children.Count == 0)
                {
                    return 1;
                }

                return m_Children[m_CurrentChild].Progress;
            }
        }

        protected CompoundNode(int id, string name, BehaviourTree tree) 
            : base(id, name, tree)
        {
        }

        internal void AddChild(Node node)
        {
            if (m_Children.Count >= MaxChildrenCount())
            {
                return;
            }

            Debug.Assert(!m_Children.Contains(node));
            m_Children.Add(node);
            OnChildAdded(node);
        }

        internal void RemoveChild(Node node)
        {
            m_Children.Remove(node);
            OnChildRemoved(node);
        }

        public bool ContainsChild(Node node)
        {
            if (this == node)
            {
                return true;
            }

            foreach (var child in Children)
            {
                if (child is CompoundNode compound)
                {
                    if (compound.ContainsChild(node))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal virtual int MaxChildrenCount() { return int.MaxValue; }
        protected virtual void OnChildRemoved(Node child) { }
        protected virtual void OnChildAdded(Node child) { }
        private protected override void OnPostAwake() 
        {
            foreach (var child in m_Children)
            {
                child.Awake();
            }
        }
        private protected override void OnPostStop()
        {
            foreach (var child in m_Children)
            {
                child.Stop();
            }
        }

        protected override Node DoClone(bool cloneOne)
        {
            var newNode = m_Tree.CreateNode(GetType(), Name, this) as CompoundNode;
            if (!cloneOne)
            {
                foreach (var child in Children)
                {
                    var newChild = child.Clone(cloneOne);
                    newChild.SetParent(newNode);
                }
            }
            return newNode;
        }

        internal override void Reset()
        {
            base.Reset();
            foreach (var child in m_Children)
            {
                child.Reset();
            }
        }

        protected override void OnReset()
        {
            base.OnReset();

            m_CurrentChild = 0;
        }

        protected int m_CurrentChild = 0;
        protected readonly List<Node> m_Children = new();
    }
}
