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

using UnityEditor.IMGUI.Controls;
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.PlayerBuildPipeline.Editor
{
    enum ColumnIndex
    {
        Name,
        Path,
        BundleName,
        RuleType,
        Description,
        AddChild,
    }

    public partial class AssetBundleRuleEditor : EditorWindow
    {
        [MenuItem("XDay/Asset/Asset Bundle Rule Editor")]
        private static void Open()
        {
            GetWindow<AssetBundleRuleEditor>("Asset Bundle Rule Editor").Show();
        }

        private void OnGUI()
        {
            GUI.enabled = m_TreeView != null;
            GUI.enabled = true;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Start"))
            {
                Start();
            }
            if (GUILayout.Button("Save"))
            {
                Save();
            }
            EditorGUILayout.EndHorizontal();

            float offset = 40;
            if (m_TreeView != null)
            {
                m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
                var rect = new Rect(0, offset, position.width, position.height - offset);
                m_TreeView.OnGUI(rect);
                EditorGUILayout.EndScrollView();
            }
        }

        private void Save()
        {
            EditorUtility.SetDirty(m_TreeView.Config);
            AssetDatabase.SaveAssets();
        }

        private void Start()
        {
            var config = EditorHelper.QueryAsset<AssetBundleRuleConfig>();
            if (config == null)
            {
                EditorUtility.DisplayDialog("出错了", "没有找到AssetBundleRuleConfig,在Assets目录下创建默认配置", "确定");
                config = ScriptableObject.CreateInstance<AssetBundleRuleConfig>();
                AssetDatabase.CreateAsset(config, "Assets/AssetBundleRuleConfig.asset");
                AssetDatabase.Refresh();
            }

            m_State = new TreeViewState();

            var headerState = CreateMultiColumnHeaderState();
            var multiColumnHeader = new MultiColumnHeader(headerState);
            multiColumnHeader.ResizeToFit();

            m_TreeView = new AssetBundleRuleTreeView(m_State, multiColumnHeader);
            m_TreeView.Build(config);
        }

        private MultiColumnHeaderState CreateMultiColumnHeaderState()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Name"),
                    contextMenuText = "Name",
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 100,
                    minWidth = 100,
                    maxWidth = 500,
                    autoResize = false,
                    allowToggleVisibility = true
                },

                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Relative Path"),
                    contextMenuText = "Relative Path",
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 200,
                    minWidth = 100,
                    maxWidth = 500,
                    autoResize = false,
                    allowToggleVisibility = true
                },

                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Bundle Name"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 100,
                    minWidth = 100,
                    maxWidth = 200,
                    autoResize = false
                },

                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Rule"),
                    contextMenuText = "Rule",
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 100,
                    minWidth = 100,
                    maxWidth = 100,
                    autoResize = false,
                    allowToggleVisibility = true
                },

                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Description"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 200,
                    maxWidth = 1000,
                    minWidth = 100,
                    autoResize = false,
                },

                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Add Child"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 80,
                    maxWidth = 1000,
                    minWidth = 80,
                    autoResize = false,
                },
            };

            var state = new MultiColumnHeaderState(columns);
            return state;
        }

        private AssetBundleRuleTreeView m_TreeView;
        private TreeViewState m_State;
        private Vector2 m_ScrollPos;
    }
}