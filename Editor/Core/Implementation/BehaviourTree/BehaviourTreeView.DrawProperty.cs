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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.BehaviourTreeAPI.Editor
{
    internal partial class BehaviourTreeView
    {
        private void DrawProperties(Node node)
        {
            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
            DrawClassFields(node, false);
            EditorGUILayout.EndScrollView();
        }

        private void DrawClassFields(object target, bool showType, string parentLabel = "")
        {
            if (target == null)
            {
                return;
            }

            Type type = target.GetType();
            FieldInfo[] fields = type.GetFields(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly
            );

            EditorGUIUtility.labelWidth = 100;
            EditorGUILayout.BeginVertical();
            foreach (FieldInfo field in fields)
            {
                if ((field.IsDefined(typeof(SerializeField), false) || field.IsPublic)
                    && !field.IsDefined(typeof(HideInInspector), false))
                {
                    if (IsPrimitiveType(field.FieldType))
                    {
                        DrawPrimitiveField(field, target, showType);
                    }
                    else if (field.FieldType == typeof(string))
                    {
                        DrawStringField(field, target, showType);
                    }
                    else if (IsGenericList(field.FieldType))
                    {
                        DrawListField(field, target, parentLabel, showType);
                    }
                    else if (IsArray(field.FieldType))
                    {
                        DrawArrayField(field, target, parentLabel, showType);
                    }
                    else if (IsVariable(field.FieldType))
                    {
                        DrawVariableField(field, target);
                    }
                    else if (field.FieldType.IsClass)
                    {
                        DrawClassField(field, target, showType);
                    }
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUIUtility.labelWidth = 0;
        }

        private void DrawPrimitiveField(FieldInfo field, object target, bool showType)
        {
            object value = field.GetValue(target);
            string label = ObjectNames.NicifyVariableName(field.Name);
            string name = showType ? $"{label}({field.FieldType.Name})" : label;
            // 根据类型调用不同绘制方法
            if (field.FieldType == typeof(int))
            {
                int newValue = EditorGUILayout.IntField(name, (int)value);
                field.SetValue(target, newValue);
            }
            else if (field.FieldType == typeof(float))
            {
                float newValue = EditorGUILayout.FloatField(name, (float)value);
                field.SetValue(target, newValue);
            }
            else if (field.FieldType == typeof(Color))
            {
                var newValue = EditorGUILayout.ColorField(name, (Color)value);
                field.SetValue(target, newValue);
            }
            else if (field.FieldType == typeof(Vector2))
            {
                var newValue = EditorGUILayout.Vector2Field(name, (Vector2)value);
                field.SetValue(target, newValue);
            }
            else if (field.FieldType == typeof(Vector3))
            {
                var newValue = EditorGUILayout.Vector3Field(name, (Vector3)value);
                field.SetValue(target, newValue);
            }
            else if (field.FieldType == typeof(Vector4))
            {
                var newValue = EditorGUILayout.Vector4Field(name, (Vector4)value);
                field.SetValue(target, newValue);
            }
            else if (field.FieldType == typeof(Vector2Int))
            {
                var newValue = EditorGUILayout.Vector2IntField(name, (Vector2Int)value);
                field.SetValue(target, newValue);
            }
            else if (field.FieldType == typeof(Vector3Int))
            {
                var newValue = EditorGUILayout.Vector3IntField(name, (Vector3Int)value);
                field.SetValue(target, newValue);
            } 
            else if (field.FieldType == typeof(Rect))
            {
                var newValue = EditorGUILayout.RectField(name, (Rect)value);
                field.SetValue(target, newValue);
            }
            else if (field.FieldType == typeof(RectInt))
            {
                var newValue = EditorGUILayout.RectIntField(name, (RectInt)value);
                field.SetValue(target, newValue);
            }
            else if (field.FieldType == typeof(bool))
            {
                var newValue = EditorGUILayout.Toggle(name, (bool)value);
                field.SetValue(target, newValue);
            }
            else
            {
                Debug.Assert(false);
            }
        }

        private object DrawPrimitiveField(object value, string parentLabel)
        {
            string label = $"{parentLabel}";

            var type = value.GetType();
            // 根据类型调用不同绘制方法
            if (type == typeof(int))
            {
                int newValue = EditorGUILayout.IntField(label, (int)value);
                return newValue;
            }
            else if (type == typeof(float))
            {
                float newValue = EditorGUILayout.FloatField(label, (float)value);
                return newValue;
            }
            else if (type == typeof(Color))
            {
                var newValue = EditorGUILayout.ColorField(label, (Color)value);
                return newValue;
            }
            else if (type == typeof(Vector2))
            {
                var newValue = EditorGUILayout.Vector2Field(label, (Vector2)value);
                return newValue;
            }
            else if (type == typeof(Vector3))
            {
                var newValue = EditorGUILayout.Vector3Field(label, (Vector3)value);
                return newValue;
            }
            else if (type == typeof(Vector4))
            {
                var newValue = EditorGUILayout.Vector4Field(label, (Vector4)value);
                return newValue;
            }
            else if (type == typeof(Vector2Int))
            {
                var newValue = EditorGUILayout.Vector2IntField(label, (Vector2Int)value);
                return newValue;
            }
            else if (type == typeof(Vector3Int))
            {
                var newValue = EditorGUILayout.Vector3IntField(label, (Vector3Int)value);
                return newValue;
            }
            else if (type == typeof(Rect))
            {
                var newValue = EditorGUILayout.RectField(label, (Rect)value);
                return newValue;
            }
            else if (type == typeof(RectInt))
            {
                var newValue = EditorGUILayout.RectIntField(label, (RectInt)value);
                return newValue;
            }
            else if (type == typeof(bool))
            {
                var newValue = EditorGUILayout.Toggle(label, (bool)value);
                return newValue;
            }
            else
            {
                Debug.Assert(false);
            }
            return null;
        }

        private void DrawStringField(FieldInfo field, object target, bool showType )
        {
            string currentValue = (string)field.GetValue(target);
            string label = ObjectNames.NicifyVariableName(field.Name);
            label = showType ? $"{label}({field.FieldType.Name})" : label;
            string newValue = EditorGUILayout.TextField(label, currentValue);
            if (newValue != currentValue)
            {
                field.SetValue(target, newValue);
            }
        }

        private object DrawStringField(object value, string label)
        {
            var oldValue = value as string;
            return EditorGUILayout.TextField(label, oldValue);
        }

        private void DrawClassField(FieldInfo field, object target, bool showType)
        {
            string label = ObjectNames.NicifyVariableName(field.Name);
            label = showType ? $"{label}({field.FieldType.Name})" : label;

            if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
            {
                //unity engine object
                var obj = field.GetValue(target) as UnityEngine.Object;
                var newObj = EditorGUILayout.ObjectField(label, obj, field.FieldType, false);
                if (newObj != obj)
                {
                    field.SetValue(target, newObj);
                }
            }
            else
            {
                EditorGUILayout.BeginVertical();
                object classInstance = field.GetValue(target);

                // 处理 null 实例
                if (classInstance == null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                    if (GUILayout.Button("Create Instance"))
                    {
                        classInstance = Activator.CreateInstance(field.FieldType);
                        field.SetValue(target, classInstance);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    // 折叠控制
                    bool isExpanded = GetExpansionState(field);
                    isExpanded = EditorGUILayout.Foldout(isExpanded, label, true, EditorStyles.foldoutHeader);

                    if (isExpanded)
                    {
                        EditorGUI.indentLevel++;
                        DrawClassFields(classInstance, showType, $"{field.Name}");
                        EditorGUI.indentLevel--;
                    }

                    SetExpansionState(field, isExpanded);
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawListField(FieldInfo field, object target, string parentLabel, bool showType)
        {
            IList list = (IList)field.GetValue(target);
            Type elementType = field.FieldType.GetGenericArguments()[0];

            string label = showType ? $"{field.Name}({field.FieldType.Name})" : field.Name;
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            EditorHelper.IndentLayout(() =>
            {
                // 列表大小控制
                int newSize = EditorGUILayout.DelayedIntField("Size", list.Count);
                if (newSize < 0)
                {
                    newSize = 0;
                }
                ResizeList(list, newSize, elementType);

                // 绘制每个元素
                for (int i = 0; i < list.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    bool removed = false;
                    // 元素操作按钮
                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        list.RemoveAt(i);
                        removed = true;
                    }

                    if (IsPrimitiveType(elementType))
                    {
                        list[i] = DrawPrimitiveField(list[i], $"{parentLabel}Element {i}: ");
                    }
                    else
                    {
                        // 递归绘制元素内容
                        DrawClassFields(list[i], showType, $"{parentLabel}Element {i}: ");
                    }

                    EditorGUILayout.EndHorizontal();

                    if (removed)
                    {
                        break;
                    }
                }

                // 添加按钮
                if (GUILayout.Button("+ Add Element"))
                {
                    list.Add(Activator.CreateInstance(elementType));
                }
            });
        }

        private void DrawArrayField(FieldInfo field, object target, string parentLabel, bool showType)
        {
            Array array = (Array)field.GetValue(target);
            Type elementType = field.FieldType.GetElementType();
            string label = showType ? $"{field.Name}({field.FieldType.Name})" : field.Name;

            EditorGUILayout.LabelField($"{parentLabel}{label}", EditorStyles.boldLabel);

            EditorHelper.IndentLayout(() =>
            {
                int newSize = EditorGUILayout.DelayedIntField("Size", array.Length);
                if (newSize < 0)
                {
                    newSize = 0;
                }
                if (newSize != array.Length)
                {
                    ResizeArray(ref array, newSize, elementType);
                    field.SetValue(target, array);
                }

                // 绘制每个元素
                for (int i = 0; i < array.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    if (IsPrimitiveType(elementType))
                    {
                        array.SetValue(DrawPrimitiveField(array.GetValue(i), $"{parentLabel}Element {i}: "), i);
                    }
                    else if (elementType == typeof(string))
                    {
                        array.SetValue(DrawStringField(array.GetValue(i), $"{parentLabel}Element {i}: "), i);
                    }
                    else
                    {
                        // 递归绘制元素内容
                        DrawClassFields(array.GetValue(i), showType, $"{parentLabel}Element {i}: ");
                    }

                    EditorGUILayout.EndHorizontal();
                }
            });
        }

        private void DrawVariableField(FieldInfo field, object target)
        {
            IVariable variable = (IVariable)field.GetValue(target);
            var scopeAttribute = field.GetCustomAttribute<VariableScopeAttribute>();
            VariableScope scope = VariableScope.Local;
            if (scopeAttribute != null)
            {
                scope = scopeAttribute.Scope;
            }
            VariableManager variableManager = m_Tree.VariableManager;
            if (scope == VariableScope.Global)
            {
                variableManager = Global.GlobalVariableManager;
            }
            Type valueType = field.FieldType.GetGenericArguments()[0];

            var group = variableManager.GetGroupInfo(valueType);
            var names = group == null ? m_EmptyNames : group.VariableNames;
            var ids = group == null ? m_EmptyIDs : group.VariableIDs;
            int oldIndex = -1;
            if (variable != null)
            {
                oldIndex = group.GetVariableIndex(variable.ID);
            }

            EditorGUILayout.BeginHorizontal();
            var newIndex = EditorGUILayout.Popup($"{field.Name}({valueType.Name})", oldIndex, names);
            if (newIndex != oldIndex)
            {
                var newVariable = variableManager.GetVariableByID(ids[newIndex]);
                field.SetValue(target, newVariable);
            }
            if (scope == VariableScope.Global)
            {
                GUILayout.Button(new GUIContent("G", "G是Global变量, L是Local变量"), GUILayout.MaxWidth(20));
            }
            else
            {
                GUILayout.Button(new GUIContent("L", "G是Global变量, L是Local变量"), GUILayout.MaxWidth(20));
            }
            if (GUILayout.Button(new GUIContent("X", "清理变量数据"), GUILayout.MaxWidth(20)))
            {
                field.SetValue(target, null);
            }
            if (GUILayout.Button(new GUIContent("=>", "跳转到变量定义"), GUILayout.MaxWidth(25)))
            {
                if (variable != null)
                {
                    m_SelectedPage = PageType.Variable;
                    m_SearchText = variable.Name;
                    ExpandVariableList(null, true, variableManager);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        // 判断是否为基本类型
        private bool IsPrimitiveType(Type type)
        {
            return type.IsPrimitive ||
                   type == typeof(Vector2) ||
                   type == typeof(Vector3) ||
                   type == typeof(Vector4) ||
                   type == typeof(Vector2Int) ||
                   type == typeof(Vector3Int) ||
                   type == typeof(Rect) ||
                   type == typeof(RectInt) ||
                   type == typeof(Color);
        }

        // 判断是否为泛型列表
        private bool IsGenericList(Type type)
        {
            return type.IsGenericType &&
                   type.GetGenericTypeDefinition() == typeof(List<>);
        }

        private bool IsArray(Type type)
        {
            return type.IsArray;
        }

        private bool IsVariable(Type type)
        {
            return typeof(IVariable).IsAssignableFrom(type);
        }

        private bool GetExpansionState(FieldInfo field)
        {
            string key = $"{field.DeclaringType.Name}.{field.Name}";
            return m_ExpansionStates.TryGetValue(key, out bool state) ? state : false;
        }

        private void SetExpansionState(FieldInfo field, bool state)
        {
            string key = $"{field.DeclaringType.Name}.{field.Name}";
            m_ExpansionStates[key] = state;
        }

        private void ResizeArray(ref Array array, int newSize, Type elementType)
        {
            if (newSize < 0)
            {
                return;
            }

            Array array2 = array;
            if (array2 == null)
            {
                array = Array.CreateInstance(elementType, newSize);
            }
            else if (array2.Length != newSize)
            {
                var array3 = Array.CreateInstance(elementType, newSize);
                Array.Copy(array2, 0, array3, 0, (array2.Length > newSize) ? newSize : array2.Length);
                array = array3;
                for (var i = 0; i < array.Length; ++i)
                {
                    if (array.GetValue(i) == null)
                    {
                        var val = GetDefaultValue(elementType);
                        array.SetValue(val, i);
                    }
                }
            }
        }

        private void ResizeList(IList list, int newSize, Type elementType)
        {
            while (list.Count > newSize)
            {
                list.RemoveAt(list.Count - 1);
            }

            while (list.Count < newSize)
            {
                list.Add(CreateInstance(elementType));
            }
        }

        private object CreateInstance(Type type)
        {
            try
            {
                return Activator.CreateInstance(type);
            }
            catch
            {
                Debug.LogError($"Failed to create instance of {type.Name}");
                return null;
            }
        }

        private static object GetDefaultValue(Type type)
        {
            // 如果是 string，返回空字符串
            if (type == typeof(string))
            {
                return string.Empty;
            }

            // 如果是值类型（如 int），返回 default(T)
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            // 如果是类且有无参构造函数，返回实例
            if (type.IsClass && type.GetConstructor(Type.EmptyTypes) != null)
            {
                return Activator.CreateInstance(type);
            }

            return null;
        }

        private Vector2 m_ScrollPos;
        // 折叠状态存储字典
        private readonly Dictionary<string, bool> m_ExpansionStates = new();
        private string[] m_EmptyNames = new string[0];
        private int[] m_EmptyIDs = new int[0];
    }
}
