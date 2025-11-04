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
using System.IO;
using UnityEditor;
using UnityEngine;

namespace XDay.WorldAPI.Editor
{
    internal partial class ResourceGroup
    {
        public void InspectorGUI(ResourceDisplayFlags flags)
        {
            EditorGUILayout.BeginVertical();
            {
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        DrawPrefabSelection();
                        DrawAddPrefabButton();
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        DrawDirectorySelection();
                        DrawAddDirectoryButton(flags);
                    }
                    EditorGUILayout.EndHorizontal();
                }

                DrawResources(flags.HasFlag(ResourceDisplayFlags.CanRemove));
            }
            EditorGUILayout.EndVertical();
        }

        public void AddInDirectory(string directory, bool addLOD0Only)
        {
            if (directory != null)
            {
                var prefabPaths = new List<string>();
                foreach (var filePath in Directory.EnumerateFiles(directory, "*.prefab", SearchOption.AllDirectories))
                {
                    var lod = WorldHelper.GetLODIndex(filePath);
                    if (lod <= 0 || !addLOD0Only)
                    {
                        prefabPaths.Add(filePath.Replace('\\', '/'));
                    }
                }

                prefabPaths.Sort((x, y) => { return x.CompareTo(y); });
                for (var i = 0; i < prefabPaths.Count; ++i)
                {
                    Add(prefabPaths[i], m_GroupSystem.CheckTransform, false);
                }
            }
        }

        private void DrawResources(bool canRemove)
        {
            if (m_LabelStyle == null)
            {
                m_LabelStyle = new GUIStyle(GUI.skin.label);
                m_LabelStyle.normal.textColor = Color.red;
            }

            m_DrawResource = true;

            m_RemoveQueue.Clear();

            for (var i = 0; i < m_Resources.Count; i++)
            {
                if (!m_DrawResource)
                {
                    break;
                }

                if (m_Resources[i] == null ||
                    m_Resources[i].Prefab == null)
                {
                    DrawMissingResource(i);
                }
                else
                {
                    DrawResource(i, canRemove);
                }
            }

            ClearRemoveQueue();
        }

        private void DrawMissingResource(int index)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.TextField("无效模型", m_LabelStyle);
                if (GUILayout.Button(new GUIContent("Delete", "删除模型")))
                {
                    m_RemoveQueue.Add(index);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawResource(int index, bool removable)
        {
            var oldPadding = GUI.skin.button.padding;
            GUI.skin.button.padding = new RectOffset(2, 2, 2, 2);

            EditorGUILayout.BeginHorizontal();
            {
                DrawSelection(index);

                GUILayout.Space(20);

                GUILayout.Label(m_Resources[index].Name, GUILayout.MaxWidth(300));

                DrawIcon(index);

                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.Space();

                        DrawSelectButton(index);
                        DrawIndexButton(index);
                        DrawDeleteButton(index, removable);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            GUI.skin.button.padding = oldPadding;
        }

        private void DrawIcon(int index)
        {
            GUILayout.Label(m_Resources[index].Icon, GUILayout.MaxHeight(80));
        }

        private void DrawIndexButton(int index)
        {
            if (WorldHelper.ImageButton("number.png", "修改模型序号"))
            {
                var parameters = new List<ParameterWindow.Parameter>()
                {
                    new ParameterWindow.IntParameter("新序号", "", index),
                };
                var idx = index;
                ParameterWindow.Open("修改模型序号", parameters, (p) =>
                {
                    var ok = ParameterWindow.GetInt(p[0], out var newIndex);
                    if (ok && idx != newIndex && newIndex >= 0)
                    {
                        ChangeIndex(idx, newIndex);
                        return true;
                    }
                    return false;
                });
            }
        }

        private void DrawSelectButton(int index)
        {
            if (WorldHelper.ImageButton("select.png", "选中Prefab文件"))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(m_Resources[index].Path);
                EditorGUIUtility.PingObject(Selection.activeObject);
            }
        }

        private void DrawDeleteButton(int index, bool removable)
        {
            if (removable)
            {
                if (WorldHelper.ImageButton("delete.png", "从模型组中删除模型"))
                {
                    if (EditorUtility.DisplayDialog("删除模型", "继续?", "确定", "取消"))
                    {
                        m_RemoveQueue.Add(index);
                    }
                }
            }
        }

        private void DrawSelection(int index)
        {
            var selected = m_SelectedIndex == index;
            EditorGUIUtility.labelWidth = 40;
            var newSelected = EditorGUILayout.ToggleLeft(index.ToString(), selected, GUILayout.MaxWidth(60));
            EditorGUIUtility.labelWidth = 0;
            if (newSelected != selected && newSelected)
            {
                SetSelection(index);
            }
        }

        private void DrawPrefabSelection()
        {
            EditorGUIUtility.labelWidth = 100;
            var prefab = EditorGUILayout.ObjectField("模型", m_SelectedPrefab, typeof(GameObject), false, null) as GameObject;
            EditorGUIUtility.labelWidth = 0;

            if (EditorHelper.IsPrefab(prefab))
            {
                m_SelectedPrefab = prefab;
            }
        }

        private void DrawAddPrefabButton()
        {
            if (WorldHelper.ImageButton("add.png", "添加所选Prefab到模型组"))
            {
                if (m_SelectedPrefab != null)
                {
                    var lod = WorldHelper.GetLODIndex(AssetDatabase.GetAssetPath(m_SelectedPrefab));
                    if (lod > 0)
                    {
                        EditorUtility.DisplayDialog("出错了", "只能添加LOD0的模型", "确定");
                    }
                    else
                    {
                        Add(AssetDatabase.GetAssetPath(m_SelectedPrefab), m_GroupSystem.CheckTransform, true);
                    }
                }
            }
        }

        private void DrawDirectorySelection()
        {
            EditorGUIUtility.labelWidth = 100;
            m_SelectedDirectory = EditorGUILayout.ObjectField("目录", m_SelectedDirectory, typeof(DefaultAsset), false, null) as DefaultAsset;
            EditorGUIUtility.labelWidth = 0;
        }

        private void DrawAddDirectoryButton(ResourceDisplayFlags flags)
        {
            if (WorldHelper.ImageButton("add.png", "添加所选目录的所有Prefab到模型组里"))
            {
                var folderPath = m_SelectedDirectory == null ? null : AssetDatabase.GetAssetPath(m_SelectedDirectory);
                AddInDirectory(folderPath, flags.HasFlag(ResourceDisplayFlags.AddLOD0Only));
            }
        }

        private GUIStyle m_LabelStyle;
        private DefaultAsset m_SelectedDirectory;
        private bool m_DrawResource = true;
    }
}

//XDay