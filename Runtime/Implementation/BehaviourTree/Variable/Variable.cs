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
    public interface IVariable
    {
        int ID { get; }
        string Name { get; set; }
        bool Draw { get; set; }

        object GetValue();
        void SetValue(object value);
    }

    public interface IVariableData
    {
    }

    [System.Serializable]
    public class Variable<T> : IVariable
    {
        public int ID => m_ID;
        public T Value;
        public string Name { get => m_Name; set=>m_Name = value; }
        public bool Draw { get; set; }

        public Variable(int id, string name)
        {
            m_ID = id;
            m_Name = name;
        }

        public object GetValue()
        {
            return Value;
        }

        public void SetValue(object value)
        {
            Value = (T)value;
        }

        [SerializeField]
        [HideInInspector]
        private int m_ID;
        [SerializeField]
        [HideInInspector]
        private string m_Name;
    }

    [System.Serializable]
    public class ExampleCustomData : IVariableData
    {
        public int IntValue;
        public string DataName;
    }
}
