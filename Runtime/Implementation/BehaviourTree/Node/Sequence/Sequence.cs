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
    public class SequenceState : DynamicNodeState
    {
		public SequenceState(Node node, bool dynamic) : base(node, dynamic)
        {
        }

        protected override Node DoCreateNode(BehaviourTree tree)
        {
            return new Sequence(ID, Name, tree, Dynamic);
        }
    }

    public class Sequence : DynamicNode
    {
        public Sequence(int id, string name, BehaviourTree tree)
            : this(id, name, tree, false)
        {
        }

        public Sequence(int id, string name, BehaviourTree tree, bool dynamic) 
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

                var status = runningChild.Tick();
                if (status == NodeStatus.Running)
                {
                    return NodeStatus.Running;
                }
                if (status == NodeStatus.Fail)
                {
                    return Complete(false);
                }

                //success
                ++m_CurrentChild;
                if (m_CurrentChild >= m_Children.Count)
                {
                    return Complete(true);
                }

                if (runningChild.CanExecute())
                {
                    break;
                }
            }

            return NodeStatus.Running;
        }

        internal override NodeState CreateState()
        {
            return new SequenceState(this, Dynamic);
        }
    }
}
