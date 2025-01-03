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

using UnityEngine;
using UnityEditor;
using XDay.WorldAPI;

namespace XDay.UtilityAPI.Editor
{
    internal partial class BrushStyleManager
    {
        public void InspectorGUI()
        {
            EditorGUILayout.BeginVertical();

            DrawBlurSetting();

            DrawStyles();

            EditorGUILayout.EndVertical();
        }

        private void DrawBlurSetting()
        {
            m_ShowBlur = EditorGUILayout.Foldout(m_ShowBlur, "Blur setting");
            if (m_ShowBlur)
            {
                EditorHelper.IndentLayout(() =>
                {
                    EditorGUILayout.BeginHorizontal();

                    m_BlurPassCount = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Blur Pass", ""), m_BlurPassCount), 1, m_BlurPassCountMax);

                    if (WorldHelper.ImageButton("blur", "blur brush"))
                    {
                        (SelectedStyle as BrushStyle)?.Blur(m_BlurPassCount);
                    }

                    if (WorldHelper.ImageButton("noblur", "remove brush blur"))
                    {
                        (SelectedStyle as BrushStyle)?.RemoveBlur();
                    }
                    EditorGUILayout.EndHorizontal();
                });
            }
        }

        private void DrawStyles()
        {
            EditorGUILayout.BeginHorizontal();
            {
                DrawFold();
                DrawRefreshButton();
                DrawOpenButton();
            }
            EditorGUILayout.EndHorizontal();
            if (m_ShowStyles)
            {
                EditorHelper.IndentLayout(() =>
                {
                    var begin = false;
                    var countPerRow = Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 120) / 60);
                    for (var idx = 0; idx < m_Styles.Count; ++idx)
                    {
                        if (idx % countPerRow == 0)
                        {
                            if (begin)
                            {
                                EditorGUILayout.EndHorizontal();
                            }
                            begin = true;
                            EditorGUILayout.BeginHorizontal();
                        }

                        DrawStyle(idx, GetStyle(idx).GetTexture(false), idx == m_SelectedIndex);

                        if (idx == m_Styles.Count - 1)
                        {
                            GUILayout.FlexibleSpace();
                        }
                    }

                    if (begin)
                    {
                        EditorGUILayout.EndHorizontal();
                    }
                });
            }
        }

        private void DrawStyle(int index, Texture2D texture, bool selected)
        {
            if (EditorHelper.DrawImageWithSelection(texture, selected, 60) != selected)
            {
                if (index >= 0 && index < m_Styles.Count)
                {
                    m_SelectedIndex = index;
                }
            }
        }

        private void DrawOpenButton()
        {
            if (WorldHelper.ImageButton("file-open", "Open style folder"))
            {
                EditorHelper.ShowInExplorer(m_StyleTextureFolder);
            }
        }

        private void DrawRefreshButton()
        {
            if (WorldHelper.ImageButton("refresh", "Refresh styles"))
            {
                Refresh(true);
            }
        }

        private void DrawFold()
        {
            m_ShowStyles = EditorGUILayout.Foldout(m_ShowStyles, "Brush");
        }
    }
}

//XDay