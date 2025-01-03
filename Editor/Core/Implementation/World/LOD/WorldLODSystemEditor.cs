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

using XDay.UtilityAPI;
using XDay.UtilityAPI.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace XDay.WorldAPI.Editor
{
    internal class WorldLODSystemEditor
    {
        internal delegate void DelegateOnLODNameChanged(string oldName, string newName);

        public void InspectorGUI(IWorldLODSystem lodSystem, DelegateOnLODNameChanged onLODNameChanged)
        {
            m_LODSystem = lodSystem;

            m_ShowLOD = EditorGUILayout.Foldout(m_ShowLOD, new GUIContent("LOD Setting", ""));
            if (m_ShowLOD)
            {
                UpdateNames(lodSystem.LODCount);

                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.Space();

                        DrawCheckHeightButton();

                        DrawSortButton();
                    }
                    EditorGUILayout.EndHorizontal();

                    DrawLODCount();

                    DrawLODList(onLODNameChanged);
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawLOD(int index, IWorldLODSetup lod, DelegateOnLODNameChanged onLODNameChanged)
        {
            if (index > 0)
            {
                EditorGUILayout.BeginHorizontal();

                GUI.enabled = false;
                EditorGUILayout.TextField(new GUIContent("Name", "LOD Name"), lod.Name);
                GUI.enabled = true;

                if (GUILayout.Button("Change", GUILayout.MaxWidth(60)))
                {
                    ChangeLOD(lod, onLODNameChanged);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawLODList(DelegateOnLODNameChanged onLODNameChanged)
        {
            for (var i = 0; i < m_LODSystem.LODCount; i++)
            {
                var lod = m_LODSystem.GetLOD(i);

                m_DrawLODs[i] = EditorGUILayout.Foldout(m_DrawLODs[i], m_LODNames[i]);
                if (m_DrawLODs[i])
                {
                    EditorHelper.IndentLayout(() =>
                    {
                        DrawLOD(i, lod, onLODNameChanged);
                    });
                }
            }
        }

        private void DrawLODCount()
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUI.enabled = false;
                EditorGUILayout.IntField(new GUIContent("LOD Count", ""), m_LODSystem.LODCount);
                GUI.enabled = true;

                if (GUILayout.Button(new GUIContent("Change", "Change LOD Count"), GUILayout.MaxWidth(80)))
                {
                    var parameters = new List<ParameterWindow.Parameter>
                    {
                        new ParameterWindow.IntParameter("New LOD Count", "", m_LODSystem.LODCount),
                    };

                    ParameterWindow.Open("Change LOD Count", parameters, (p) => {
                        ParameterWindow.GetInt(p[0], out var newLODCount);
                        if (
                            m_LODSystem.LODCount == newLODCount ||
                            newLODCount <= 0)
                        {
                            return false;
                        }

                        m_LODSystem.LODCount = newLODCount;
                        UpdateNames(newLODCount);
                        return true;
                    });
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSortButton()
        {
            if (GUILayout.Button(new GUIContent("Sort", "Sort from low to high"), GUILayout.MaxWidth(100)))
            {
                m_LODSystem.Sort();
            }
        }

        private void DrawCheckHeightButton()
        {
            if (GUILayout.Button(new GUIContent("Check Height", "Check if height is sorted!"), GUILayout.MaxWidth(100)))
            {
                m_LODSystem.CheckIfLODIsSorted();
            }
        }

        private void UpdateNames(int n)
        {
            if (m_LODNames == null || m_LODNames.Length != n)
            {
                m_LODNames = new string[n];
                for (var i = 0; i < n; i++)
                {
                    m_LODNames[i] = $"LOD {i}";
                }
                m_DrawLODs = new bool[n];
                for (var i = 0; i < n; i++)
                {
                    m_DrawLODs[i] = true;
                }
            }
        }

        private void ChangeLOD(IWorldLODSetup lod, DelegateOnLODNameChanged onLODNameChanged)
        {
            var parameters = new List<ParameterWindow.Parameter>
            {
                new ParameterWindow.StringParameter("New Name", "", lod.Name),
            };

            ParameterWindow.Open("Change LOD Name", parameters, (p) => {
                ParameterWindow.GetString(p[0], out var newName);

                if (string.IsNullOrEmpty(newName) ||
                    m_LODSystem.QueryLOD(newName) != null ||
                    newName == lod.Name)
                {
                    return false;
                }

                var oldName = lod.Name;
                lod.Name = newName;

                onLODNameChanged?.Invoke(oldName, newName);

                return true;
            });
        }

        private IWorldLODSystem m_LODSystem;
        private string[] m_LODNames;
        private bool[] m_DrawLODs;
        private bool m_ShowLOD = true;
    }
}


//XDay