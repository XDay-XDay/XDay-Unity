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



using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace ShaderTool
{
    public class ShaderSearchWindow : EditorWindow
    {
        [MenuItem("XDay/Rendering/URP Assistant")]
        private static void Open()
        {
            GetWindow<ShaderSearchWindow>();
        }

        public void OnEnable()
        {
            m_Search = new ShaderSearch();

            m_SearchDirectories = new List<string>()
            {
                "Library/PackageCache/com.unity.render-pipelines.core@405083404c88",
                "Library/PackageCache/com.unity.render-pipelines.universal@82fb1398b3b8",
            };
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("New"))
                {
                    Create();
                }

                if (GUILayout.Button("Save"))
                {
                    Save();
                }

                if (GUILayout.Button("Load"))
                {
                    Load();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (m_SearchData != null)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUIUtility.labelWidth = 100;
                    m_SearchText = EditorGUILayout.TextField("Search Text", m_SearchText, GUILayout.MaxWidth(700));
                    EditorGUIUtility.labelWidth = 0;

                    m_SearchData.ShowDetail = EditorGUILayout.ToggleLeft("Show Detail", m_SearchData.ShowDetail, GUILayout.MaxWidth(100));
                    m_SearchData.MatchWord = EditorGUILayout.ToggleLeft("Match Word", m_SearchData.MatchWord, GUILayout.MaxWidth(100));

                    if (GUILayout.Button("Search", GUILayout.MaxWidth(60)))
                    {
                        var result = m_Search.Search(m_SearchDirectories, m_SearchText, m_SearchData.MatchWord);
                        if (result.Entries.Count > 0)
                        {
                            m_SearchData.AddResult(result);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUIUtility.labelWidth = 100;
                    m_OpenFilePath = EditorGUILayout.TextField("Open File", m_OpenFilePath, GUILayout.MaxWidth(700));
                    EditorGUIUtility.labelWidth = 0;

                    if (GUILayout.Button("Open File", GUILayout.MaxWidth(100)))
                    {
                        m_Search.OpenFile(m_OpenFilePath);
                    }
                }
                EditorGUILayout.EndHorizontal();

                m_ScrollPos = GUILayout.BeginScrollView(m_ScrollPos);

                foreach (var result in m_SearchData.Results)
                {
                    bool removed = false;
                    EditorGUILayout.BeginHorizontal();
                    {
                        result.Display = EditorGUILayout.Foldout(result.Display, $"{result.Text}-{result.Entries.Count}");
                        if (GUILayout.Button("X", GUILayout.MaxWidth(30)))
                        {
                            removed = true;
                            m_SearchData.RemoveResult(result.Text);
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    if (removed)
                    {
                        break;
                    }

                    if (result.Display)
                    {
                        for (var i = 0; i < result.Entries.Count; i++)
                        {
                            var entry = result.Entries[i];
                            EditorGUILayout.BeginHorizontal();
                            EditorGUIUtility.labelWidth = 20;
                            EditorGUILayout.PrefixLabel($"{i}");
                            EditorGUIUtility.labelWidth = 0;
                            if (GUILayout.Button("Open", GUILayout.MaxWidth(50)))
                            {
                                EditorHelper.OpenCSFile(entry.File, entry.LineNumber);
                            }
                            EditorGUILayout.TextField(entry.Text);
                            if (GUILayout.Button("Detail", GUILayout.MaxWidth(50)))
                            {
                                entry.ShowDetail = !entry.ShowDetail;
                            }
                            EditorGUILayout.EndHorizontal();
                            if (entry.ShowDetail || m_SearchData.ShowDetail)
                            {
                                EditorGUILayout.TextArea(entry.Detail);
                            }
                        }
                    }
                }
                GUILayout.EndScrollView();
            }
        }

        private void Create()
        {
            m_SearchData = CreateInstance<ShaderSearchData>();
        }

        private void Save()
        {
            if (File.Exists(m_Path))
            {
                EditorUtility.SetDirty(m_SearchData);
                AssetDatabase.SaveAssets();
            }
            else
            {
                AssetDatabase.CreateAsset(m_SearchData, m_Path);
                AssetDatabase.Refresh();
            }
        }

        private void Load()
        {
            m_SearchData = AssetDatabase.LoadAssetAtPath<ShaderSearchData>(m_Path);
        }

        private string m_SearchText;
        private string m_OpenFilePath;
        private ShaderSearch m_Search;
        private List<string> m_SearchDirectories;
        private ShaderSearchData m_SearchData;
        private Vector2 m_ScrollPos;
        private const string m_Path = "Assets/SearchData.asset";
    }
}
