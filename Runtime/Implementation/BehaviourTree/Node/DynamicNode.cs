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
    public class DynamicNodeState : CompoundNodeState
    {
        public bool Dynamic;

        public DynamicNodeState(Node node, bool dynamic) : base(node)
        {
            Dynamic = dynamic;
        }

        protected override Node DoCreateNode(BehaviourTree tree)
        {
            return new Selector(ID, Name, tree, Dynamic);
        }
    }

    public abstract class DynamicNode : CompoundNode
    {
        public bool Dynamic { get => m_Dynamic; set => m_Dynamic = value; }

        public DynamicNode(int id, string name, BehaviourTree tree, bool dynamic)
            : base(id, name, tree)
        {
            m_Dynamic = dynamic;
        }

        protected void EvaluateDynamicNodeConditionsChange()
        {
            //动态节点会重新评估高优先级节点的执行
            if (Dynamic)
            {
                bool statusChanged = EvaluateHigherPriorityConditions(this);
                if (statusChanged)
                {
                    Reset();
                }
            }
        }

        private bool EvaluateHigherPriorityConditions(CompoundNode node)
        {
            foreach (var child in node.Children)
            {
                if (child is Condition condition)
                {
                    var newStatus = condition.TestSuccess();
                    if (condition.ExecuteCount > 0 &&
                        newStatus != condition.Status)
                    {
                        return true;
                    }
                }
                else if (child is DynamicNode dynamicNode && dynamicNode.Dynamic)
                {
                    //递归判断子节点
                    var statusChanged = EvaluateHigherPriorityConditions(child as CompoundNode);
                    if (statusChanged)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool m_Dynamic = false;
    }
}
