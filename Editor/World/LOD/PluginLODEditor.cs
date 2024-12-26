/*
 * Copyright (c) 2024 XDay
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
    internal class PluginLODEditor
    {
        public void InspectorGUI(IPluginLODSystem pluginLODSystem, Action<int, int> onLODCountChanged, string title, Func<int, bool> lodCountFilter)
        {
            m_PluginLODSystem = pluginLODSystem;
            m_ShowLOD = EditorGUILayout.Foldout(m_ShowLOD, new GUIContent(title, "Plugin LODs"));
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
                        EditorGUILayout.TextField("Name", lod.Name);
                        GUI.enabled = true;

                        if (GUILayout.Button("Change", GUILayout.MaxWidth(60)))
                        {
                            var window = EditorWindow.GetWindow<PluginLODEditorWindow>("Set LOD");
                            EditorHelper.MakeWindowCenterAndSetSize(window);
                            window.Open(lod.Name, m_PluginLODSystem, m_PluginLODSystem.WorldLODSystem, (oldName, newName) => {
                                var lod = m_PluginLODSystem.QueryLOD(oldName);
                                lod.Name = newName;
                            });
                        }

                        EditorGUILayout.EndHorizontal();

                        lod.Tolerance = EditorGUILayout.FloatField(new GUIContent("Tolerance", ""), lod.Tolerance);
                    });   
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawLODCount(Action<int, int> onLODCountChanged, Func<int, bool> lodCountFilter)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("LOD Count", m_PluginLODSystem.LODCount.ToString());
            if (GUILayout.Button("Change"))
            {
                var parameters = new List<ParameterWindow.Parameter>
                {
                    new ParameterWindow.IntParameter("Count", "", m_PluginLODSystem.LODCount),
                };

                ParameterWindow.Open("Change LOD Name", parameters, (p) => {
                    ParameterWindow.GetInt(p[0], out var newLODCount);
                    if (newLODCount <= 0)
                    {
                        return false;
                    }

                    var oldCount = m_PluginLODSystem.LODCount;

                    if (lodCountFilter == null || lodCountFilter(newLODCount))
                    {
                        m_PluginLODSystem.LODCount = newLODCount;
                        if (oldCount != newLODCount)
                        {
                            onLODCountChanged?.Invoke(oldCount, newLODCount);
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Error", "LOD count should be <= world lod count!", "OK");
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