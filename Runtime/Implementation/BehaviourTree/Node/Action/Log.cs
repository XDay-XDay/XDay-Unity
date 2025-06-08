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
    public class LogState : NodeState
    {
        public string Text;
        public int TargetVarID;

        public LogState(Node node) : base(node)
        {
        }

        protected override Node DoCreateNode(BehaviourTree tree)
        {
            return new Log(ID, Name, tree)
            {
                Text = Text,
            };
        }
    }

    [BehaviourGroup("Action")]
    [Description("Êä³öÈÕÖ¾")]
    public sealed class Log : Action
    {
        public string Text { get => m_Text; set => m_Text = value; }
        [VariableScope(VariableScope.Global)]
        public Variable<int> Target;
        public override float Progress { get => 1; }

        public Log(int id, string name, BehaviourTree tree) : base(id, name, tree)
        {
        }

        protected override NodeStatus OnTick()
        {
            Debug.Log(m_Text);
            return Complete(true);
        }

        internal override NodeState CreateState()
        {
            return new LogState(this);
        }

        internal override void SetState(NodeState state)
        {
            base.SetState(state);

            var s = state as LogState;
            s.Text = m_Text;
            s.TargetVarID = Target != null ? Target.ID : 0;
        }

        protected override Node DoClone(bool cloneOne)
        {
            var log = m_Tree.CreateNode(GetType(), Name, this) as Log;
            log.Text = m_Text;
            return log;
        }

        [SerializeField]
        private string m_Text = "";
    }
}
