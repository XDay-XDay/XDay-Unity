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
    public class SelectorState : DynamicNodeState
    {
        public SelectorState(Node node, bool dynamic) : base(node, dynamic)
        {
        }

        protected override Node DoCreateNode(BehaviourTree tree)
        {
            return new Selector(ID, Name, tree, Dynamic);
        }
    }

    public class Selector : DynamicNode
    {
        public Selector(int id, string name, BehaviourTree tree)
            : this(id, name, tree, false)
        {
        }

        public Selector(int id, string name, BehaviourTree tree, bool dynamic) 
            : base(id, name, tree, dynamic)
        {
        }

        protected override NodeStatus OnTick()
        {
            if (m_Children.Count == 0)
            {
                return NodeStatus.Success;
            }

            EvaluateDynamicNodeConditionsChange();

            while (true)
            {
                var runningChild = RunningChild;
                if (runningChild == null)
                {
                    break;
                }

                var status = runningChild.Tick();
                if (status == NodeStatus.Running)
                {
                    return status;
                }
                if (status == NodeStatus.Success)
                {
                    return Complete(true);
                }

                //current child failed, select next task
                ++m_CurrentChild;
                if (m_CurrentChild >= m_Children.Count)
                {
                    return Complete(false);
                }

                //如果runningChild是条件类节点,直接在同一帧内开始下一个节点的执行,避免延迟
                if (runningChild.CanExecute())
                {
                    break;
                }
            }

            return NodeStatus.Running;
        }

        internal override NodeState CreateState()
        {
            return new SelectorState(this, Dynamic);
        }
    }
}
