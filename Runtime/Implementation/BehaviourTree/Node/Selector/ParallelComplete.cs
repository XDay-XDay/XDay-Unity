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
    public class ParallelCompleteState : CompoundNodeState
    {
        public ParallelCompleteState(Node node) : base(node)
        {
        }

        protected override Node DoCreateNode(BehaviourTree tree)
        {
            return new ParallelComplete(ID, Name, tree);
        }
    }

    /// <summary>
    /// 同时执行,只要有任意child返回success或fail,立即返回结果,不会保证所有running child都能tick一次
    /// </summary>
    [BehaviourGroup("Parallel")]
    public class ParallelComplete : CompoundNode
    {
        public ParallelComplete(int id, string name, BehaviourTree tree)
            : base(id, name, tree)
        {
        }

        protected override NodeStatus OnTick()
        {
            bool hasRunningChild = false;
            foreach (var child in m_Children)
            {
                if (!child.Completed)
                {
                    hasRunningChild = true;
                    var status = child.Tick();
                    if (status == NodeStatus.Fail)
                    {
                        return Complete(false);
                    }
                    if (status == NodeStatus.Success)
                    {
                        return Complete(true);
                    }
                }
            }

            if (hasRunningChild)
            {
                return NodeStatus.Running;
            }
            return Complete(true);
        }

        internal override NodeState CreateState()
        {
            return new ParallelCompleteState(this);
        }
    }
}
