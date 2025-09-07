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

using XDay.UtilityAPI.Editor;
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Editor
{
    internal class PluginLODSystemEditor
    {
        public void InspectorGUI(IPluginLODSystem pluginLODSystem, Action<int, int> onLODCountChanged, string title, Func<int, bool> lodCountFilter)
        {
            m_PluginLODSystem = pluginLODSystem;
            m_ShowLOD = EditorGUILayout.Foldout(m_ShowLOD, new GUIContent(title, "层LOD"));
            if (m_ShowLOD)
            {
                EditorGUILayout.BeginVertical("GroupBox");

                DrawLODCount(onLODCountChanged, lodCountFilter);

                RefreshLODNames();

                DrawLODList();

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawLODList()
        {
            EditorGUILayout.BeginVertical();

            for (var i = 0; i < m_PluginLODSystem.LODCount; i++)
            {
                var lod = m_PluginLODSystem.GetLOD(i);
                m_ShowEachLODs[i] = EditorGUILayout.Foldout(m_ShowEachLODs[i], m_LODNames[i]);
                if (m_ShowEachLODs[i] && i > 0)
                {
                    EditorHelper.IndentLayout(() =>
                    {
                        EditorGUILayout.BeginHorizontal();

                        GUI.enabled = false;
                        EditorGUILayout.TextField("名称", lod.Name);
                        GUI.enabled = true;

                        if (GUILayout.Button("修改", GUILayout.MaxWidth(60)))
                        {
                            var window = EditorWindow.GetWindow<PluginLODSystemEditorWindow>("设置LOD");
                            EditorHelper.MakeWindowCenterAndSetSize(window);
                            window.Open(lod.Name, m_PluginLODSystem, m_PluginLODSystem.WorldLODSystem, (oldName, newName) => {
                                var lod = m_PluginLODSystem.QueryLOD(oldName);
                                lod.Name = newName;
                            });
                        }

                        EditorGUILayout.EndHorizontal();

                        lod.Tolerance = EditorGUILayout.FloatField(new GUIContent("缓冲高度(米)", ""), lod.Tolerance);
                        lod.RenderLOD = EditorGUILayout.IntField(new GUIContent("模型LOD", "使用哪个LOD的模型"), lod.RenderLOD);
                    });   
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawLODCount(Action<int, int> onLODCountChanged, Func<int, bool> lodCountFilter)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("LOD数量", m_PluginLODSystem.LODCount.ToString());
            if (GUILayout.Button("修改"))
            {
                var parameters = new List<ParameterWindow.Parameter>
                {
                    new ParameterWindow.IntParameter("数量", "", m_PluginLODSystem.LODCount),
                };

                ParameterWindow.Open("修改LOD名称", parameters, (p) => {
                    ParameterWindow.GetInt(p[0], out var newLODCount);
                    if (newLODCount <= 0)
                    {
                        return false;
                    }

                    var oldCount = m_PluginLODSystem.LODCount;

                    if (lodCountFilter == null || lodCountFilter(newLODCount))
                    {
                        if (newLODCount > m_PluginLODSystem.WorldLODSystem.LODCount)
                        {
                            EditorUtility.DisplayDialog("出错了", "层的LOD个数应该不超过地图LOD个数!", "确定");
                        }
                        else
                        {
                            m_PluginLODSystem.LODCount = newLODCount;
                            if (oldCount != newLODCount)
                            {
                                onLODCountChanged?.Invoke(oldCount, newLODCount);
                            }
                        }
                    }

                    return true;
                });
            }

            EditorGUILayout.EndHorizontal();
        }

        private void RefreshLODNames()
        {
            if (m_LODNames == null || m_LODNames.Length != m_PluginLODSystem.LODCount)
            {
                var n = m_PluginLODSystem.LODCount;
                m_ShowEachLODs = new bool[n];
                for (var i = 0; i < n; i++)
                {
                    m_ShowEachLODs[i] = true;
                }

                m_LODNames = new string[n];
                for (var i = 0; i < n; i++)
                {
                    m_LODNames[i] = $"LOD {i}";
                }
            }
        }

        private string[] m_LODNames;
        private bool[] m_ShowEachLODs;
        private bool m_ShowLOD = true;
        private IPluginLODSystem m_PluginLODSystem;
    }
}

//XDay