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
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.CodeStripping
{
    internal class CodeStripperEditor : EditorWindow
    {
        [MenuItem("XDay/代码剔除")]
        private static void Open()
        {
            GetWindow<CodeStripperEditor>("代码剔除");
        }

        private void OnDisable()
        {
            m_Setting = null;
        }

        private void OnGUI()
        {
            Load();

            m_Stripper ??= new CodeStripper();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("生成link.xml"))
            {
                Save();

                m_Setting = EditorHelper.QueryAsset<CodeStripperSetting>();
                m_Stripper = new();
                m_Stripper.Generate(m_Setting, new List<string>());

                if (m_Setting == null)
                {
                    m_Setting = ScriptableObject.CreateInstance<CodeStripperSetting>();
                    AssetDatabase.CreateAsset(m_Setting, "Assets/CodeStripperSetting.asset");
                    AssetDatabase.Refresh();
                }

                Save();
            }
            EditorGUILayout.EndHorizontal();

            m_ShowEditor = EditorGUILayout.ToggleLeft("显示Editor程序集", m_ShowEditor);
            m_SearchText = EditorGUILayout.TextField("搜索", m_SearchText);

            foreach (var group in m_Stripper.Groups.Values)
            {
                group.CalculateRuntimeAssemblyCount();
            }

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
            DrawList();
            EditorGUILayout.EndScrollView();
        }

        private void DrawList()
        {
            var groups = m_Stripper.Groups.Values;
            foreach (var group in groups)
            {
                DrawGroup(group);
            }
        }

        private void DrawGroup(AssemblyGroup group)
        {
            var count = m_ShowEditor ? group.Assemblies.Count : group.RuntimeAssemblyCount;
            group.Show = EditorGUILayout.Foldout(group.Show, $"{group.Name} 数量:{count}个");
            if (group.Show || !string.IsNullOrEmpty(m_SearchText))
            {
                EditorGUI.indentLevel++;
                foreach (var assembly in group.Assemblies)
                {
                    DrawItem(group, assembly);
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawItem(AssemblyGroup group, AssemblyInfo assembly)
        {
            if (!m_ShowEditor && assembly.EditorOnly)
            {
                return;
            }

            if (!string.IsNullOrEmpty(m_SearchText))
            {
                if (assembly.Name.IndexOf(m_SearchText, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return;
                }

                group.Show = true;
            }

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !assembly.EditorOnly;
            assembly.PreserveOption = (PreserveOption)EditorGUILayout.EnumPopup(GUIContent.none, assembly.PreserveOption, GUILayout.MaxWidth(150));
            GUI.enabled = true;
            EditorGUIUtility.fieldWidth = 70;
            EditorGUIUtility.labelWidth = 55;

            if (m_ShowEditor)
            {
                assembly.EditorOnly = EditorGUILayout.Toggle("Editor", assembly.EditorOnly);
            }

            EditorGUIUtility.labelWidth = 0;
            EditorGUIUtility.fieldWidth = 0;
            if (assembly.EditorOnly)
            {
                EditorGUILayout.TextField(GUIContent.none, assembly.Assembly.GetName().Name, m_EditorTextStyle, GUILayout.MinWidth(400));
            }
            else
            {
                EditorGUILayout.TextField(GUIContent.none, assembly.Assembly.GetName().Name, GUILayout.MinWidth(400));
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void Load()
        {
            if (m_EditorTextStyle == null)
            {
                m_EditorTextStyle = new GUIStyle(GUI.skin.label);
                m_EditorTextStyle.normal.textColor = Color.red;
            }
        }

        private void Save()
        {
            if (m_Setting != null)
            {
                m_Setting.Sync(m_Stripper);
                EditorUtility.SetDirty(m_Setting);
                AssetDatabase.SaveAssets();
            }
        }

        private CodeStripper m_Stripper;
        private CodeStripperSetting m_Setting;
        private Vector2 m_ScrollPos;
        private bool m_ShowEditor = false;
        private GUIStyle m_EditorTextStyle;
        private string m_SearchText;
    }
}