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
    public class FailState : CompoundNodeState
    {
        public FailState(Node node) : base(node)
        {
        }

        protected override Node DoCreateNode(BehaviourTree tree)
        {
            return new Fail(ID, Name, tree);
        }
    }

    /// <summary>
    /// 返回失败
    /// </summary>
    [BehaviourGroup("Decorator")]
    public class Fail : Decorator
    {
        public override float Progress
        {
            get
            {
                if (m_Children.Count == 0)
                {
                    return 1;
                }

                return m_Children[0].Progress;
            }
        }

        public Fail(int id, string name, BehaviourTree tree) : base(id, name, tree)
        {
        }

        protected override NodeStatus OnTick()
        {
            if (m_Children.Count == 0)
            {
                return NodeStatus.Success;
            }

            var status = m_Children[0].Tick();
            if (status != NodeStatus.Running)
            {
                return Complete(false);
            }
            return NodeStatus.Running;
        }

        internal override NodeState CreateState()
        {
            return new FailState(this);
        }
    }
}
