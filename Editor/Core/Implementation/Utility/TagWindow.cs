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

namespace XDay.UtilityAPI.Editor
{
    internal class TagWindow : EditorWindow
    {
        public static TagWindow Open(
            string title,
            Dictionary<string, bool> tagState,
            Func<List<string>, Dictionary<string, bool>, bool> onClickOK,
            bool setSize = true,
            Action customOnGUI = null)
        {
            var window = Open(title, setSize);
            window.Show(tagState, onClickOK, customOnGUI);
            EditorApplication.delayCall += () =>
            {
                EditorGUI.FocusTextInControl("ConfirmButton");
            };

            return window;
        }

        private static TagWindow Open(string title, bool setSize = true)
        {
            var inputDialog = GetWindow<TagWindow>(title);
            if (setSize)
            {
                inputDialog.minSize = new Vector2(200, 500);
            }
            var position = inputDialog.position;
            position.center = new Rect(0f, 0f, Screen.currentResolution.width, Screen.currentResolution.height).center;
            inputDialog.position = position;

            return inputDialog;
        }

        private void Show(Dictionary<string, bool> tagState, Func<List<string>, Dictionary<string, bool>, bool> OnClickOK, Action customOnGUI, float labelWidth = 0)
        {
            m_TagState = tagState;
            m_OnClickOK = OnClickOK;
            m_LabelWidth = labelWidth;
            m_CustomOnGUI = customOnGUI;
        }

        private void OnGUI()
        {
            m_ScrollView = EditorGUILayout.BeginScrollView(m_ScrollView);
            EditorGUIUtility.labelWidth = m_LabelWidth;
            EditorGUILayout.BeginVertical();

            var tags = UnityEditorInternal.InternalEditorUtility.tags;

            EditorGUILayout.LabelField("选择你想要显示的Tag标记的物体");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全选"))
            {
                foreach (var tag in tags)
                {
                    m_TagState[tag] = true;
                }
            }

            if (GUILayout.Button("全不选"))
            {
                foreach (var tag in tags)
                {
                    m_TagState[tag] = false;
                }
            }
            EditorGUILayout.EndHorizontal();

            foreach (var tag in tags)
            {
                m_TagState[tag] = EditorGUILayout.ToggleLeft(tag, IsVisible(tag));
            }

            EditorGUILayout.EndVertical();
            EditorGUIUtility.labelWidth = 0;

            m_CustomOnGUI?.Invoke();

            EditorGUILayout.EndScrollView();

            GUI.SetNextControlName("ConfirmButton");

            if (GUILayout.Button("确定"))
            {
                if (m_OnClickOK != null)
                {
                    var visibleTags = GetVisibleTags();
                    if (m_OnClickOK(visibleTags, m_TagState))
                    {
                        Close();
                    }
                }
                else
                {
                    Close();
                }
            }
        }

        private List<string> GetVisibleTags()
        {
            List<string> tags = new();
            foreach (var kv in m_TagState)
            {
                if (kv.Value)
                {
                    tags.Add(kv.Key);
                }
            }
            return tags;
        }

        private bool IsVisible(string tag)
        {
            var found = m_TagState.TryGetValue(tag, out bool visible);
            if (found)
            {
                return visible;
            }
            return true;
        }

        private float m_LabelWidth;
        private Func<List<string>, Dictionary<string, bool>, bool> m_OnClickOK;
        private Action m_CustomOnGUI;
        private Vector2 m_ScrollView;
        private Dictionary<string, bool> m_TagState = new();
    }
}
