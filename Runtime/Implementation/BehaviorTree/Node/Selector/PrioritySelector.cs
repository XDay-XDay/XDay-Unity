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
    /// <summary>
    /// 按优先级选择节点，当子节点sortOrder改变后节点会从第一个节点开始执行
    /// </summary>
    public class PrioritySelector : CompoundNode
    {
        public override NodeStatus Tick()
        {
            Sort();

            var status = m_Children[m_CurrentChild].Tick();
            if (status == NodeStatus.Running)
            {
                return NodeStatus.Running;
            }

            if (status == NodeStatus.Success)
            {
                Reset();
                return NodeStatus.Success;
            }

            ++m_CurrentChild;
            if (m_CurrentChild >= m_Children.Count)
            {
                Reset();
                return NodeStatus.Fail;
            }

            return NodeStatus.Running;
        }

        protected override void OnChildSortOrderChanged(Node child)
        {
            Reset();
        }

        protected override void OnReset()
        {
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

        private bool m_NeedSort = true;
    }
}
