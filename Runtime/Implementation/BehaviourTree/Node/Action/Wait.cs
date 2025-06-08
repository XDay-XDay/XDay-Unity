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

namespace XDay.BehaviourTreeAPI
{
    [Serializable]
    public class WaitState : NodeState
    {
        public float WaitTime;

        public WaitState(Node node) : base(node)
        {
        }

        protected override Node DoCreateNode(BehaviourTree tree)
        {
            return new Wait(ID, Name, tree)
            {
                WaitTime = WaitTime,
            };
        }
    }

    [BehaviourGroup("Action")]
    [Description("等待N秒")]
    public sealed class Wait : Action
    {
        public float WaitTime { get => m_WaitTime; set => m_WaitTime = value; }
        public override float Progress 
        {
            get
            {
                if (WaitTime == 0)
                {
                    return 1;
                }
                return Mathf.Clamp01(m_Timer / WaitTime);
            }
        }

        public Wait(int id, string name, BehaviourTree tree)
            : base(id, name, tree)
        {
        }

        protected override void OnStart()
        {
            base.OnStart();

            m_Timer = 0;
        }

        protected override NodeStatus OnTick()
        {
            m_Timer += Time.deltaTime;
            if (m_Timer >= m_WaitTime)
            {
                return Complete(true);
            }
            return NodeStatus.Running;
        }

        internal override NodeState CreateState()
        {
            return new WaitState(this);
        }

        internal override void SetState(NodeState state)
        {
            base.SetState(state);

            var s = state as WaitState;
            s.WaitTime = WaitTime;
        }

        protected override Node DoClone(bool cloneOne)
        {
            var wait = m_Tree.CreateNode<Wait>(Name, this);
            wait.WaitTime = m_WaitTime;
            return wait;
        }

        [SerializeField]
        private float m_WaitTime = 1;
        private float m_Timer;
    }
}
