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


using UnityEngine;

namespace XDay.BehaviourTreeAPI
{
    [System.Serializable]
    public class RepeatState : CompoundNodeState
    {
        public int RepeatCount;

        public RepeatState(Node node) : base(node)
        {
        }

        protected override Node DoCreateNode(BehaviourTree tree)
        {
            return new Repeat(ID, Name, tree)
            {
                RepeatCount = RepeatCount,
            };
        }
    }

    /// <summary>
    /// 循环
    /// </summary>
    [BehaviourGroup("Decorator")]
    public class Repeat : Decorator
    {
        [SerializeField]
        public int RepeatCount = 1;

        public override float Progress
        {
            get
            {
                if (RepeatCount == 0)
                {
                    return 1;
                }
                return Mathf.Clamp01((float)m_LoopCount / RepeatCount);
            }
        }

        public Repeat(int id, string name, BehaviourTree tree)
            : base(id, name, tree)
        {
        }

        protected override NodeStatus OnTick()
        {
            if (m_Children.Count == 0)
            {
                return Complete(true);
            }

            var status = RunningChild.Tick();
            if (status != NodeStatus.Running)
            {
                ++m_LoopCount;
                if (m_LoopCount == RepeatCount)
                {
                    return Complete(true);
                }
            }
            return NodeStatus.Running;
        }

        protected override void OnReset()
        {
            base.OnReset();

            m_LoopCount = 0;
        }

        internal override NodeState CreateState()
        {
            return new RepeatState(this);
        }

        internal override void SetState(NodeState state)
        {
            base.SetState(state);

            var s = state as RepeatState;
            s.RepeatCount = RepeatCount;
        }

        private int m_LoopCount = 0;
    }
}
