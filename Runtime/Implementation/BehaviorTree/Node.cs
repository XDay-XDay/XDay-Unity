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
    public enum NodeStatus
    {
        Running,
        Success,
        Fail,
    }

    public class Node
    {
        public string Name { get => m_Name; set => m_Name = value; }
        public int SortOrder 
        { 
            get => m_SortOrder; 
            set
            {
                if (m_SortOrder != value)
                {
                    m_SortOrder = value;
                    m_Parent?.OnChildSortOrderChanged(this);
                }
            }
        }

        public Node()
        {
        }

        public Node(string name)
        {
            m_Name = name;
        }

        public void SetParent(CompoundNode parent)
        {
            if (m_Parent != parent)
            {
                m_Parent?.RemoveChild(this);
                m_Parent = parent;
                m_Parent?.AddChild(this);
            }
        }

        public virtual NodeStatus Tick()
        {
            return m_Status;
        }

        protected virtual void OnChildSortOrderChanged(Node child) { }

        private string m_Name;
        //从小到大排序
        private int m_SortOrder = 0;
        protected NodeStatus m_Status = NodeStatus.Success;
        private CompoundNode m_Parent;
    }
}
