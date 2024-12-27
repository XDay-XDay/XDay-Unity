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
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace XDay.WorldAPI.Editor
{
    internal partial class ResourceGroupSystem
    {
        public void InspectorGUI(ResourceDisplayFlags flags)
        {
            m_ShowGroup = EditorGUILayout.Foldout(m_ShowGroup, "Groups");
            if (m_ShowGroup)
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        DrawGroupSelection();
                        DrawAddButton();
                        DrawRenameButton();
                        DrawRemoveButton();
                    }
                    EditorGUILayout.EndHorizontal();

                    if (m_SelectedGroupIndex >= 0)
                    {
                        m_Groups[m_SelectedGroupIndex].InspectorGUI(flags);
                    }
                }
                EditorGUILayout.EndVertical();
                --EditorGUI.indentLevel;
            }
        }

        private void DrawRenameButton()
        {
            if (WorldHelper.ImageButton("edit.png", "Rename Group"))
            {
                ChangeGroupName(m_SelectedGroupIndex);   
            }
        }
        
        private void DrawRemoveButton()
        {
            if (WorldHelper.ImageButton("delete.png", "Delete Group"))
            {
                if (EditorUtility.DisplayDialog("Delete Group", "Continue?", "Yes", "No"))
                {
                    RemoveGroup(m_SelectedGroupIndex);
                }
            }
        }

        private void DrawAddButton()
        {
            if (WorldHelper.ImageButton("add.png", "Add Group"))
            {
                var parameters = new List<ParameterWindow.Parameter>()
                {
                    new ParameterWindow.StringParameter("Group Name", "", "New Group"),
                };
                ParameterWindow.Open("Group Setting", parameters, (p) =>
                {
                    var ok = ParameterWindow.GetString(p[0], out var name);
                    if (ok)
                    {
                        foreach (var groupName in m_GroupNames)
                        {
                            if (name == groupName)
                            {
                                return false;
                            }
                        }

                        AddGroup(name);
                        return true;
                    }
                    return false;
                });
            }
        }

        private void DrawGroupSelection()
        {
            if (m_GroupNames.Length != m_Groups.Count)
            {
                m_GroupNames = new string[m_Groups.Count];
            }
            for (var i = 0; i < m_Groups.Count; i++)
            {
                m_GroupNames[i] = m_Groups[i].Name;
            }

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Resource Group", GUILayout.MaxWidth(110));

                var newIndex = EditorGUILayout.Popup(m_SelectedGroupIndex, m_GroupNames);
                if (newIndex != m_SelectedGroupIndex)
                {
                    SetSelectedGroup(newIndex);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}

//XDay