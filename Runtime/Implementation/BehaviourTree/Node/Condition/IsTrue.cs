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
    public class IsTrueState : NodeState
    {
        public bool Toggle;

        public IsTrueState(Node node) : base(node)
        {
        }

        protected override Node DoCreateNode(BehaviourTree tree)
        {
            return new IsTrue(ID, Name, tree)
            {
                Toggle = Toggle,
            };
        }
    }

    [BehaviourGroup("Condition")]
    [Description("")]
    public sealed class IsTrue : Condition
    {
        public bool Toggle 
        {
            get => m_Toggle;
            set => m_Toggle = value; 
        }
        public override float Progress
        {
            get
            {
                return 1;
            }
        }

        public IsTrue(int id, string name, BehaviourTree tree)
            : base(id, name, tree)
        {
        }

        internal override NodeState CreateState()
        {
            return new IsTrueState(this);
        }

        internal override void SetState(NodeState state)
        {
            base.SetState(state);

            var s = state as IsTrueState;
            s.Toggle = Toggle;
        }

        protected override Node DoClone(bool cloneOne)
        {
            var isTrue = m_Tree.CreateNode<IsTrue>(Name, this);
            isTrue.Toggle = Toggle;
            return isTrue;
        }

        public override NodeStatus TestSuccess()
        {
            return m_Toggle ? NodeStatus.Success : NodeStatus.Fail;
        }

        [SerializeField]
        private bool m_Toggle = false;
    }
}
