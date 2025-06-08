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

#if UNITY_EDITOR

using System;
using System.Collections.Generic;

namespace XDay.BehaviourTreeAPI
{
    public partial class VariableManager
    {
        public int GroupCount => m_Groups.Count;

        public Group GetGroupInfo(int index)
        {
            if (index >= 0 && index < m_Groups.Count)
            {
                return m_Groups[index];
            }
            return null;
        }

        public Group GetGroupInfo(Type valueType)
        {
            foreach (var group in m_Groups)
            {
                if (group.Type == valueType)
                {
                    return group;
                }
            }
            return null;
        }

        public void OnAddVariable(IVariable v)
        {
            UpdateGroup(v);
        }

        public void OnRemoveVariable(IVariable v)
        {
            UpdateGroup(v);
        }

        public void UpdateGroupNames()
        {
            foreach (var group in m_Groups)
            {
                for (var i = 0; i < group.VariableNames.Length; i++)
                {
                    var v = GetVariableByID(group.VariableIDs[i]);
                    group.VariableNames[i] = v.Name;
                }
            }
        }
        
        private void UpdateGroup(IVariable v)
        {
            Type valueType = v.GetType().GetGenericArguments()[0];
            var oldGroup = GetGroup(valueType);
            if (oldGroup != null)
            {
                m_Groups.Remove(oldGroup);
            }
            var group = CreateGroup(valueType);
            m_Groups.Add(group);
        }

        private Group CreateGroup(Type type)
        {
            List<string> names = new();
            List<int> ids = new();
            Group group = new();
            foreach (var variable in m_Variables)
            {
                Type valueType = variable.GetType().GetGenericArguments()[0];
                if (valueType == type)
                {
                    names.Add(variable.Name);
                    ids.Add(variable.ID);
                }
            }
            group.VariableNames = names.ToArray();
            group.VariableIDs = ids.ToArray();
            group.Type = type;
            return group;
        }

        private Group GetGroup(Type type)
        {
            foreach (var group in m_Groups)
            {
                if (group.Type == type)
                {
                    return group;
                }
            }
            return null;
        }

        private readonly List<Group> m_Groups = new();

        public class Group
        {
            public bool Draw;
            public Type Type;
            public string[] VariableNames;
            public int[] VariableIDs;

            public int GetVariableIndex(int id)
            {
                for (var i = 0; i < VariableIDs.Length; ++i)
                {
                    if (id == VariableIDs[i])
                    {
                        return i;
                    }
                }
                return -1;
            }
        }
    }
}

#endif