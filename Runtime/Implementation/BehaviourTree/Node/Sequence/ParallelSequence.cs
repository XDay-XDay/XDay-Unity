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
    public class ParallelSequenceState : CompoundNodeState
    {
        public ParallelSequenceState(Node node) : base(node)
        {
        }

        protected override Node DoCreateNode(BehaviourTree tree)
        {
            return new ParallelSequence(ID, Name, tree);
        }
    }

    /// <summary>
    /// 同时执行,保证所有节点都被执行完毕,完毕后全部child返回success才返回success,只要有一个节点返回fail,则返回fail
    /// </summary>
    [BehaviourGroup("Parallel")]
    public class ParallelSequence : CompoundNode
    {
        public ParallelSequence(int id, string name, BehaviourTree tree) : base(id, name, tree)
        {
        }

        protected override NodeStatus OnTick()
        {
            if (m_Children.Count == 0)
            {
                return NodeStatus.Success;
            }

            bool hasRunningChild = false;
            int successCount = 0;
            foreach (var child in m_Children)
            {
                if (child.Status == NodeStatus.Running)
                {
                    hasRunningChild = true;
                    child.Tick();
                }
                else if (child.Status == NodeStatus.Success)
                {
                    ++successCount;
                }
            }

            if (successCount == m_Children.Count)
            {
                return Complete(true);
            }

            if (hasRunningChild)
            {
                return NodeStatus.Running;
            }

            return Complete(false);
        }

        internal override NodeState CreateState()
        {
            return new ParallelSequenceState(this);
        }
    }
}
