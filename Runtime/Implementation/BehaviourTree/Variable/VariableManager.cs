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
using System.Collections.Generic;
using UnityEngine;

namespace XDay.BehaviourTreeAPI
{
    [Serializable]
    public partial class VariableManager
    {
        public List<IVariable> Variables => m_Variables;
        public bool Draw { get; set; } = false;
        public event Action<IVariable> EventVariableAdded
        {
            add
            {
                m_OnVariableAdded -= value;
                m_OnVariableAdded += value;
            }
            remove
            {
                m_OnVariableAdded -= value;
            }
        }
        public event Action<IVariable> EventVariableRemoved
        {
            add
            {
                m_OnVariableRemoved -= value;
                m_OnVariableRemoved += value;
            }
            remove
            {
                m_OnVariableRemoved -= value;
            }
        }

        public VariableManager()
        {
#if UNITY_EDITOR
            EventVariableAdded += OnAddVariable;
            EventVariableRemoved += OnRemoveVariable;
#endif
        }

        public Variable<T> CreateVariable<T>(string name)
        {
            var v = new Variable<T>(AllocateID(), name);
            AddVariable(v);
            return v;
        }

        public IVariable CreateVariable(Type type, string name)
        {
            var v = Activator.CreateInstance(type, AllocateID(), name) as IVariable;
            AddVariable(v);
            return v;
        }

        public void AddVariable(IVariable variable)
        {
            if (variable == null)
            {
                return;
            }
            m_Variables.Add(variable);
            m_OnVariableAdded?.Invoke(variable);
        }

        public void RemoveVariable(IVariable variable)
        {
            var index = m_Variables.IndexOf(variable);
            RemoveVariable(index);
        }

        public void RemoveVariable(int index)
        {
            if (index >= 0 && index < m_Variables.Count)
            {
                var variable = m_Variables[index];
                m_Variables.RemoveAt(index);
                m_OnVariableRemoved?.Invoke(variable);
            }
        }

        public IVariable GetVariableByIndex(int index)
        {
            if (index >= 0 && index < m_Variables.Count)
            {
                return m_Variables[index];
            }
            return null;
        }

        public IVariable GetVariableByID(int id)
        {
            foreach (var variable in m_Variables)
            {
                if (variable.ID == id)
                {
                    return variable;
                }
            }
            return null;
        }

        public VariableManagerState CreateState()
        {
            return new VariableManagerState()
            {
                Variables = new() { Variables = Variables},
            };
        }

        private int AllocateID()
        {
            var startID = 1;
            while (true)
            {
                if (GetVariableByID(startID) == null)
                {
                    return startID;
                }
                ++startID;
            }
        }

        [SerializeField]
        private readonly List<IVariable> m_Variables = new();
        private event Action<IVariable> m_OnVariableAdded;
        private event Action<IVariable> m_OnVariableRemoved;
    }
}
