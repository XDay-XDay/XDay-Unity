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
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.SystemAPI
{
    public class SystemInitInfo
    {
        /// <summary>
        /// unload一个system时是否清理其他system对该system的field值
        /// </summary>
        public bool ClearReferencedFieldsWhenUnloadSystem = true;

        /// <summary>
        /// 传入System.OnCreate的参数
        /// </summary>
        public Func<object> GetSystemDataFunc;
    }

    /// <summary>
    /// 管理System, System使用接口访问,不允许继承
    /// </summary>
    public static class SystemManager
    {
        public static bool Init(SystemInitInfo initInfo = null)
        {
            initInfo ??= new();
            m_InitInfo = initInfo;
            return LoadGlobalSystems();
        }

        public static void Uninit()
        {
            UnloadSystems();
        }

        public static void Update(float dt)
        {
            foreach (var kv in m_UpdatableSystems)
            {
                kv.Value.Update(dt);
            }
        }

        public static void LateUpdate(float dt)
        {
            foreach (var kv in m_LateUpdatableSystems)
            {
                kv.Value.LateUpdate(dt);
            }
        }

        public static void FixedUpdate()
        {
            foreach (var kv in m_FixedUpdatableSystems)
            {
                kv.Value.FixedUpdate();
            }
        }

        public static void LoadSystemGroup(Type groupType)
        {
#if UNITY_EDITOR
            Debug.Assert(typeof(ISystemGroup).IsAssignableFrom(groupType));
#endif
            foreach (var type in m_SortedInterfaceTypes)
            {
                var metadata = GetSystemMetadata(type);
                if (metadata != null && metadata.Group == groupType && metadata.CreateTiming == SystemCreateTiming.CreateOnStartup)
                {
                    CreateSystem(type, metadata);
                }
            }
        }

        public static void UnloadSystemGroup(Type groupType)
        {
            if (m_GroupedSystems.TryGetValue(groupType, out var systemList))
            {
                //先卸载无依赖的System
                systemList.Sort((a, b) =>
                {
                    var orderA = GetSystemUpdateOrder(m_ClassTypeToInterfaceTypes[a.GetType()]);
                    var orderB = GetSystemUpdateOrder(m_ClassTypeToInterfaceTypes[b.GetType()]);
                    return orderB.CompareTo(orderA);
                });
                foreach (var system in systemList)
                {
                    UnloadSystem(system);
                }
                m_GroupedSystems.Remove(groupType);
            }
        }

        public static T GetSystem<T>() where T : class, ISystem
        {
            return DoGetSystem(typeof(T)) as T;
        }

        public static T TryGetSystem<T>() where T : class, ISystem
        {
            m_TypeToSystems.TryGetValue(typeof(T), out var system);
            return system as T;
        }

        public static void GetSavableSystems(List<ISaveable> sableSystems)
        {
            sableSystems.Clear();
            sableSystems.AddRange(m_SaveableSystems.Values);
        }

        /// <summary>
        /// 只加载GlobalGroup的System
        /// </summary>
        /// <returns></returns>
        private static bool LoadGlobalSystems()
        {
            var parser = new SystemParser();
            bool ok = parser.Parse(out m_SortedInterfaceTypes, out m_InterfaceTypeToClassTypes, m_SystemDependencies, (a, b) =>
            {
                Debug.LogError($"{a}=>{b}产生循环依赖");
                return true;
            });
            if (!ok)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.ExitPlaymode();
#endif
                return false;
            }
            foreach (var kv in m_InterfaceTypeToClassTypes)
            {
                m_ClassTypeToInterfaceTypes.Add(kv.Value, kv.Key);
            }

            int updateOrder = 0;
            foreach (var type in m_SortedInterfaceTypes)
            {
                m_SystemUpdateOrders.Add(type, ++updateOrder);
            }

            foreach (var type in m_SortedInterfaceTypes)
            {
                var metadata = GetSystemMetadata(type);
                if (metadata == null ||
                    (metadata.Group == typeof(IGlobalSystemGroup) && metadata.CreateTiming == SystemCreateTiming.CreateOnStartup))
                {
                    CreateSystem(type, metadata);
                }
            }

            return true;
        }

        private static void UnloadSystems()
        {
            List<ISystem> allSystems = new();
            foreach (var kv in m_TypeToSystems)
            {
                allSystems.Add(kv.Value);
            }
            allSystems.Sort((a, b) =>
            {
                var orderA = GetSystemUpdateOrder(m_ClassTypeToInterfaceTypes[a.GetType()]);
                var orderB = GetSystemUpdateOrder(m_ClassTypeToInterfaceTypes[b.GetType()]);
                return orderB.CompareTo(orderA);
            });
            foreach (var system in allSystems)
            {
                UnloadSystem(system);
            }

            Debug.Assert(m_UpdatableSystems.Count == 0);
            Debug.Assert(m_LateUpdatableSystems.Count == 0);
            Debug.Assert(m_FixedUpdatableSystems.Count == 0);
            Debug.Assert(m_SaveableSystems.Count == 0);
            Debug.Assert(m_TypeToSystems.Count == 0);
            m_UpdatableSystems.Clear();
            m_LateUpdatableSystems.Clear();
            m_FixedUpdatableSystems.Clear();
            m_SaveableSystems.Clear();
            m_TypeToSystems.Clear();
            m_GroupedSystems.Clear();
            m_SystemUpdateOrders.Clear();
            m_SystemDependencies.Clear();
            m_InterfaceTypeToClassTypes.Clear();
            m_ClassTypeToInterfaceTypes.Clear();
            m_InitInfo = null;
        }

        private static void UnloadSystem(ISystem system)
        {
            //清除其他system对该system的field数据
            ClearReferencedFields(system);

            system.OnDestroy();

            //注意这里没有清理m_GroupedSystem保存的数据,会在外部统一清理
            var interfaceType = m_ClassTypeToInterfaceTypes[system.GetType()];
            var order = GetSystemUpdateOrder(interfaceType);
            m_UpdatableSystems.Remove(order);
            m_LateUpdatableSystems.Remove(order);
            m_FixedUpdatableSystems.Remove(order);
            m_SaveableSystems.Remove(order);
            bool ok = m_TypeToSystems.Remove(interfaceType);
            Debug.Assert(ok);
        }

        private static SystemMetadataAttribute GetSystemMetadata(Type type)
        {
            return Helper.GetClassAttribute<SystemMetadataAttribute>(type, true);
        }

        private static ISystem CreateSystem(Type interfaceType, SystemMetadataAttribute metadata)
        {
            var classType = m_InterfaceTypeToClassTypes[interfaceType];

            //先创建依赖的system类对象
            if (m_SystemDependencies.TryGetValue(interfaceType, out var dependencies))
            {
                foreach (var dependency in dependencies)
                {
                    DoGetSystem(dependency);
                }
            }

            m_TypeToSystems.TryGetValue(interfaceType, out var system);
            if (system != null)
            {
                SetSystemFields(system);
                return system;
            }

            system = System.Activator.CreateInstance(classType) as ISystem;
            SetSystemFields(system);

            system.OnCreate(m_InitInfo.GetSystemDataFunc == null ? null : m_InitInfo.GetSystemDataFunc?.Invoke());

            Debug.Log($"System {system} created!");

            var updateOrder = GetSystemUpdateOrder(interfaceType);
            if (system is IUpdatable updatable)
            {
                m_UpdatableSystems.Add(updateOrder, updatable);
            }

            if (system is ILateUpdatable lateUpdatable)
            {
                m_LateUpdatableSystems.Add(updateOrder, lateUpdatable);
            }

            if (system is IFixedUpdatable fixedUpdatable)
            {
                m_FixedUpdatableSystems.Add(updateOrder, fixedUpdatable);
            }

            if (system is ISaveable saveable)
            {
                m_SaveableSystems.Add(updateOrder, saveable);
            }

            m_TypeToSystems[interfaceType] = system;

            Type groupType = typeof(IGlobalSystemGroup);
            if (metadata != null)
            {
                groupType = metadata.Group;
            }
            if (!m_GroupedSystems.TryGetValue(groupType, out var list))
            {
                list = new();
                m_GroupedSystems[groupType] = list;
            }
            list.Add(system);

            return system;
        }

        private static void SetSystemFields(ISystem system)
        {
            var fields = GetFieldsOfSystemType(system.GetType());
            foreach (var field in fields)
            {
                var fieldSystem = DoGetSystem(field.FieldType);
                Debug.Assert(fieldSystem != null, $"System is null, set field: {field.Name} of type {field.FieldType} failed!");
                field.SetValue(system, fieldSystem);
            }
        }

        private static void ClearReferencedFields(ISystem system)
        {
            if (!m_InitInfo.ClearReferencedFieldsWhenUnloadSystem)
            {
                return;
            }

            foreach (var kv in m_TypeToSystems)
            {
                var otherSystem = kv.Value;
                if (otherSystem == system)
                {
                    continue;
                }
                var fields = GetFieldsOfSystemType(otherSystem.GetType());
                var interfaceType = m_ClassTypeToInterfaceTypes[system.GetType()];
                foreach (var field in fields)
                {
                    if (field.FieldType == interfaceType)
                    {
                        field.SetValue(otherSystem, null);
                    }
                }
            }
        }

        private static int GetSystemUpdateOrder(Type interfaceType)
        {
            return m_SystemUpdateOrders[interfaceType];
        }

        private static ISystem DoGetSystem(Type systemInterfaceType)
        {
            m_TypeToSystems.TryGetValue(systemInterfaceType, out var system);
            if (system == null)
            {
                var metadata = GetSystemMetadata(systemInterfaceType);
                system = CreateSystem(systemInterfaceType, metadata);
            }
            return system;
        }

        private static List<FieldInfo> GetFieldsOfSystemType(Type type)
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

        //按照依赖关系从最少依赖到最多依赖更新
        private static readonly SortedDictionary<int, IUpdatable> m_UpdatableSystems = new();
        private static readonly SortedDictionary<int, ILateUpdatable> m_LateUpdatableSystems = new();
        private static readonly SortedDictionary<int, IFixedUpdatable> m_FixedUpdatableSystems = new();
        private static readonly SortedDictionary<int, ISaveable> m_SaveableSystems = new();
        private static readonly Dictionary<Type, ISystem> m_TypeToSystems = new();
        //key是system kv interfaceType
        private static readonly Dictionary<Type, List<ISystem>> m_GroupedSystems = new();
        private static readonly Dictionary<Type, int> m_SystemUpdateOrders = new();
        //key是interface
        private static readonly Dictionary<Type, List<Type>> m_SystemDependencies = new();
        private static Dictionary<Type, Type> m_InterfaceTypeToClassTypes;
        private static readonly Dictionary<Type, Type> m_ClassTypeToInterfaceTypes = new();
        private static SystemInitInfo m_InitInfo;
        private static List<Type> m_SortedInterfaceTypes = new();
    }
}
