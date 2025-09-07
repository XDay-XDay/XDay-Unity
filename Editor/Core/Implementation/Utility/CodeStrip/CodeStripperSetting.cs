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

namespace XDay.CodeStripping
{
    [System.Serializable]
    public class GroupState
    {
        public string GroupName;
        public bool Show;
    }

    public class CodeStripperSetting : ScriptableObject
    {
        /// <summary>
        /// 手动指定保留的assembly
        /// </summary>
        public List<string> TypePreservedAssemblyNames = new();
        public List<string> AssemblyPreservedAssemblyNames = new();
        public List<string> DontPreservedAssemblyNames = new();
        public List<GroupState> GroupStates = new();

        internal PreserveOption GetPreserveState(string name, PreserveOption defaultOption)
        {
            if (TypePreservedAssemblyNames.Contains(name))
            {
                return PreserveOption.PreserveType;
            }

            if (AssemblyPreservedAssemblyNames.Contains(name))
            {
                return PreserveOption.PreserveAssembly;
            }

            if (DontPreservedAssemblyNames.Contains(name))
            {
                return PreserveOption.DontPreserve;
            }

            return defaultOption;
        }

        internal void Sync(CodeStripper stripper)
        {
            //group state
            foreach (var group in stripper.Groups.Values)
            {
                var groupState = GetOrAddGroupState(group.Name);
                groupState.Show = group.Show;
            }

            //assemble state
            SyncList(stripper, TypePreservedAssemblyNames, PreserveOption.PreserveType);
            SyncList(stripper, AssemblyPreservedAssemblyNames, PreserveOption.PreserveAssembly);
            SyncList(stripper, DontPreservedAssemblyNames, PreserveOption.DontPreserve);
        }

        private void SyncList(CodeStripper stripper, List<string> list, PreserveOption option)
        {
            var preservedAssemblies = stripper.GetPreservedAssemblies(option);
            for (var i = list.Count - 1; i >= 0; --i)
            {
                if (!Contains(preservedAssemblies, list[i]))
                {
                    list.RemoveAt(i);
                }
            }
            foreach (var assembly in preservedAssemblies)
            {
                if (!list.Contains(assembly.Name))
                {
                    list.Add(assembly.Name);
                }
            }
        }

        private GroupState GetOrAddGroupState(string name)
        {
            foreach (var state in GroupStates)
            {
                if (state.GroupName == name)
                {
                    return state;
                }
            }

            var newState = new GroupState() { GroupName = name };
            GroupStates.Add(newState);
            return newState;
        }

        private bool Contains(List<AssemblyInfo> preservedAssemblies, string name)
        {
            foreach (var assembly in preservedAssemblies)
            {
                if (assembly.Name == name)
                {
                    return true;
                }
            }
            return false;
        }

        internal bool GetGroupState(string name)
        {
            foreach (var group in GroupStates)
            {
                if (group.GroupName == name)
                {
                    return group.Show;
                }
            }
            return false;
        }
    }
}
