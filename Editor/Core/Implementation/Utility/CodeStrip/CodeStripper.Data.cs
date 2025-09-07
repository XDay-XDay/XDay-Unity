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
using System.Reflection;

namespace XDay.CodeStripping
{
    public enum PreserveOption
    {
        //保留Assembly里指定类型
        PreserveType,
        //保留Assembly所有数据
        PreserveAssembly,
        //不保留
        DontPreserve,
    }

    public class AssemblyInfo
    {
        public Assembly Assembly { get; set; }
        public string Name => Assembly.GetName().Name;
        //保留的类型
        public List<Type> PreservedTypes = new();
        public PreserveOption PreserveOption { get; set; } = PreserveOption.DontPreserve;
        public bool EditorOnly { get; set; } = false;
    }

    public class AssemblyGroup
    {
        public string Name { get; set; }
        public List<AssemblyInfo> Assemblies { get; set; } = new();
        public int RuntimeAssemblyCount => m_RuntimeAssemblyCount;
        public bool Show { get; set; }

        public void Sort()
        {
            Assemblies.Sort((a, b) =>
            {
                return a.Assembly.GetName().Name.CompareTo(b.Assembly.GetName().Name); 
            });
        }

        public void CalculateRuntimeAssemblyCount()
        {
            m_RuntimeAssemblyCount = 0;
            foreach (var assembly in Assemblies)
            {
                if (!assembly.EditorOnly)
                {
                    ++m_RuntimeAssemblyCount;
                }
            }
        }

        private int m_RuntimeAssemblyCount;
    }
}
