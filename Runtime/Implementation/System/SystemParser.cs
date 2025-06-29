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
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace XDay.SystemAPI
{
    internal class SystemParser
    {
        public bool Parse(out List<Type> outSortedSystemTypes,
            out Dictionary<Type, Type> interfaceToClassType,
            Dictionary<Type, List<Type>> outSystemDependencies,
            Func<string, string, bool> errorCallback)
        {
            outSystemDependencies.Clear();
            outSortedSystemTypes = new();
            Dictionary<string, Type> types = new();

            var systemInterfaceTypes = GetSystemInterfaces(out interfaceToClassType);
            foreach (var interfaceType in systemInterfaceTypes)
            {
                AddDependency(interfaceType, interfaceType);
                types.Add(interfaceType.FullName, interfaceType);
                var classType = interfaceToClassType[interfaceType];
                List<FieldInfo> fields = GetFieldsOfSystemType(classType);
                foreach (var field in fields)
                {
                    AddDependency(interfaceType, field.FieldType);
                    if (!outSystemDependencies.TryGetValue(interfaceType, out var list))
                    {
                        list = new();
                        outSystemDependencies.Add(interfaceType, list);
                    }
                    list.Add(field.FieldType);
                }
            }

            List<string> sortedTypeNames = new();
            m_Sorter.Sort(sortedTypeNames);
            foreach (var name in sortedTypeNames)
            {
                outSortedSystemTypes.Add(types[name]);
            }
            return !m_Sorter.CheckLoop(errorCallback);
        }

        private void AddDependency(Type slaveType, Type masterType)
        {
            m_Sorter.AddEdge(masterType.FullName, slaveType.FullName);
        }

        private List<FieldInfo> GetFieldsOfSystemType(Type type)
        {
            var systemType = typeof(ISystem);
            List<FieldInfo> ret = new();
            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in fields)
            {
                if (systemType.IsAssignableFrom(field.FieldType) && field.FieldType.IsInterface)
                {
                    ret.Add(field);
                }
            }
            return ret;
        }

        /// <summary>
        /// 获取实现了ISystemInterface的所有接口
        /// </summary>
        /// <returns></returns>
        private List<Type> GetSystemInterfaces(out Dictionary<Type, Type> interfaceToClassType)
        {
            var watch = new SimpleStopwatch();
            watch.Begin();
            interfaceToClassType = new();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<Type> interfaceTypes = new();

            List<Type> allSystemClassTypes = new();
            foreach (var assembly in assemblies)
            {
                if (assembly.GetCustomAttribute<SystemSearchAttribute>() == null)
                {
                    continue;
                }

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // 处理部分类型加载失败的情况
                    types = ex.Types.Where(t => t != null).ToArray();
                }
                catch
                {
                    // 忽略无法加载的程序集
                    continue;
                }

                var systemInterfaceType = typeof(ISystem);
                foreach (var type in types)
                {
                    //先找到继承自ISystem的接口类型
                    if (type.IsInterface &&
                        type != systemInterfaceType &&
                        systemInterfaceType.IsAssignableFrom(type))
                    {
                        interfaceTypes.Add(type);
                    }

                    if (type.IsClass && 
                        !type.IsAbstract && 
                        systemInterfaceType.IsAssignableFrom(type))
                    {
                        allSystemClassTypes.Add(type);
                    }
                }
            }

            //找到哪些class type实现了ISystem的接口,这里前提是System Class不能继承
            foreach (var interfaceType in interfaceTypes)
            {
                foreach (var classType in allSystemClassTypes)
                {
                    if (interfaceType.IsAssignableFrom(classType))
                    {
                        interfaceToClassType[interfaceType] = classType;
                        break;
                    }
                }
            }

            //按名字排序
            interfaceTypes.Sort((a,b)=>a.FullName.CompareTo(b.FullName));

            watch.Stop();
            Debug.Log($"初始化System时查找类型消耗: {watch.ElapsedSeconds}秒");

            return interfaceTypes;
        }

        private readonly TopologySort<string> m_Sorter = new();

    }
}
