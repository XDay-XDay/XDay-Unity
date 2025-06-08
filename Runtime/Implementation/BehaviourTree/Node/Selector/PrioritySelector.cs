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



namespace XDay.BehaviourTreeAPI
{
    [System.Serializable]
    public class PrioritySelectorState : CompoundNodeState
    {
        public PrioritySelectorState(Node node) : base(node)
        {
        }

        protected override Node DoCreateNode(BehaviourTree tree)
        {
            return new PrioritySelector(ID, Name, tree);
        }
    }

    /// <summary>
    /// 按优先级选择节点，当子节点sortOrder改变后节点会从第一个节点开始执行
    /// </summary>
    [BehaviourGroup("Selector")]
    public class PrioritySelector : CompoundNode
    {
        public PrioritySelector(int id, string name, BehaviourTree tree) : base(id, name, tree)
        {
        }

        protected override NodeStatus OnTick()
        {
            if (m_Children.Count == 0)
            {
                return NodeStatus.Success;
            }

            Sort();

            var status = m_Children[m_CurrentChild].Tick();
            if (status == NodeStatus.Running)
            {
                return NodeStatus.Running;
            }

            if (status == NodeStatus.Success)
            {
                return Complete(true);
            }

            ++m_CurrentChild;
            if (m_CurrentChild >= m_Children.Count)
            {
                return Complete(false);
            }

            return NodeStatus.Running;
        }

        protected override void OnChildSortOrderChanged(Node child)
        {
            Reset();
        }

        protected override void OnReset()
        {
            base.OnReset();
            m_NeedSort = true;
        }

        private void Sort()
        {
            if (m_NeedSort)
            {
                m_Children.Sort((a, b) => { return a.SortOrder.CompareTo(b.SortOrder); });
                m_NeedSort = false;
            }
        }

        internal override NodeState CreateState()
        {
            return new PrioritySelectorState(this);
        }

        private bool m_NeedSort = true;
    }
}
