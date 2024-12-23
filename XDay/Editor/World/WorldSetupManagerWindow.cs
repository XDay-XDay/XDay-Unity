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

using XDay.UtilityAPI;
using XDay.UtilityAPI.Editor;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace XDay.WorldAPI.Editor
{
    internal class WorldSetupManagerWindow : EditorWindow
    {
        [MenuItem("XDay/World/Create Setup File", false, 30)]
        private static void CreateSetupFile()
        {
            var setupManager = EditorHelper.QueryAsset<WorldSetupManager>();
            if (setupManager == null)
            {
                setupManager = CreateInstance<WorldSetupManager>();
                Selection.activeObject = setupManager;
                AssetDatabase.CreateAsset(setupManager, "Assets/WorldSetupManager.asset");
                return;
            }

            EditorUtility.DisplayDialog("Error", $"Setup file already created, it's located at {AssetDatabase.GetAssetPath(setupManager)}", "OK");
        }

        [MenuItem("XDay/World/Open Setup Window", false, 31)]
        private static void OpenSetupWindow()
        {
            GetWindow<WorldSetupManagerWindow>().Show();
        }

        private void OnGUI()
        {
            var found = QuerySetupManager();
            if (!found)
            {
                return;
            }

            DrawHeader(); 

            EditorGUIUtility.labelWidth = 150;

            DrawEditorFolder();
            DrawGameFolder();
            DrawWorldList();

            EditorGUIUtility.labelWidth = 0;

            DrawSetupList();
        }

        private void DrawSetupList()
        {
            m_DrawWorlds = EditorGUILayout.Foldout(m_DrawWorlds, "Worlds");
            if (m_DrawWorlds)
            {
                EditorHelper.IndentLayout(() =>
                {
                    EditorGUILayout.IntField("World Count", m_SetupManager.Setups.Count);
                });

                m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

                foreach (var setup in m_SetupManager.Setups)
                {
                    if (DrawSetup(setup))
                    {
                        break;
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawWorldSetup(WorldSetup setup)
        {
            EditorGUIUtility.labelWidth = 200;

            EditorGUI.BeginChangeCheck();

            EditorHelper.IndentLayout(() =>
            {
                setup.Name = EditorGUILayout.TextField("World Name", setup.Name);
                setup.ID = EditorGUILayout.IntField("World ID", setup.ID);
                GUI.enabled = false;
                EditorGUILayout.TextField("Camera Setup", setup.CameraSetupFileName);
                setup.GameFolder = EditorHelper.ObjectField<DefaultAsset>("Game Folder", setup.GameFolder);
                setup.EditorFolder = Helper.ToRelativePath(EditorHelper.DirectoryField("Editor Folder", setup.EditorFolder), Directory.GetCurrentDirectory());
                GUI.enabled = true;
            });

            EditorGUIUtility.labelWidth = 0;

            if (EditorGUI.EndChangeCheck())
            {
                Save();
            }
        }

        private void DrawWorldList()
        {
            if (m_SetupManager.Setups.Count == 0)
            {
                return;
            }

            UpdateNames();

            var previewWorldIndex = -1;
            for (var i = 0; i < m_WorldNames.Length; i++)
            {
                if (m_WorldNames[i] == m_SetupManager.PreviewWorldName)
                {
                    previewWorldIndex = i;
                    break;
                }
            }

            var valueChanged = false;
            if (previewWorldIndex == -1)
            {
                previewWorldIndex = 0;
                m_SetupManager.PreviewWorldName = m_SetupManager.Setups[previewWorldIndex].Name;
                valueChanged = true;
            }

            var newIndex = EditorGUILayout.Popup("Preview World", previewWorldIndex, m_WorldNames);
            if (newIndex != previewWorldIndex)
            {
                valueChanged = true;
                m_SetupManager.PreviewWorldName = m_WorldNames[newIndex];
            }

            if (valueChanged)
            {
                Save();
            }
        }

        private bool CloneWorld(WorldSetup setup, string newWorldName)
        {
            if (string.IsNullOrEmpty(setup.GameFolder) ||
                string.IsNullOrEmpty(setup.EditorFolder))
            {
                EditorUtility.DisplayDialog("Error", "Data folder is null", "OK");
                return false;
            }

            if (string.IsNullOrEmpty(newWorldName))
            {
                return false;
            }

            FileUtil.CopyFileOrDirectory(setup.EditorFolder, $"{m_SetupManager.EditorFolder}/{newWorldName}");
            Directory.CreateDirectory($"{m_SetupManager.GameFolder}/{newWorldName}");

            m_SetupManager.AddSetup(m_SetupManager.GetValidID(), newWorldName, setup.CameraSetupFileName);
            m_SetupManager.Save();

            AssetDatabase.Refresh();

            return true;
        }

        private bool QuerySetupManager()
        {
            if (m_SetupManager == null)
            {
                m_SetupManager = EditorHelper.QueryAsset<WorldSetupManager>();
            }
            return m_SetupManager != null;
        }

        private void Save()
        {
            EditorUtility.SetDirty(m_SetupManager);
            AssetDatabase.SaveAssets();
        }

        private void DrawEditorFolder()
        {
            var directory = EditorHelper.DirectoryField("Editor Folder", m_SetupManager.EditorFolder, "EditorWorld", () => { Save(); });
            m_SetupManager.EditorFolder = Helper.ToRelativePath(directory, Directory.GetCurrentDirectory());
        }

        private void DrawGameFolder()
        {
            m_SetupManager.GameFolder = EditorHelper.ObjectField<DefaultAsset>("Game Folder", m_SetupManager.GameFolder, () => { EditorUtility.SetDirty(m_SetupManager); });
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();

            if (GUILayout.Button("Save", GUILayout.MaxWidth(60)))
            {
                Save();
            }

            if (GUILayout.Button("Select", GUILayout.MaxWidth(60)))
            {
                Selection.activeObject = m_SetupManager;
            }
            EditorGUILayout.EndHorizontal();
        }

        private bool DrawSetup(WorldSetup setup)
        {
            var quit = false;
            EditorGUILayout.BeginVertical("GroupBox");
            {
                EditorGUILayout.BeginHorizontal();
                {
                    DrawOpenEditorFolder(setup);

                    EditorGUILayout.Space();

                    quit |= DrawCloneButton(setup);

                    quit |= DrawDeleteButton(setup);
                }
                EditorGUILayout.EndHorizontal();

                DrawWorldSetup(setup);
            }
            EditorGUILayout.EndVertical();

            return quit;
        }

        private bool DrawDeleteButton(WorldSetup setup)
        {
            if (GUILayout.Button("Delete", GUILayout.MaxWidth(60)))
            {
                var ok = EditorUtility.DisplayDialog("Warning", "Continue?", "Yes", "No");
                if (ok)
                {
                    if (Directory.Exists(setup.EditorFolder))
                    {
                        FileUtil.DeleteFileOrDirectory(setup.EditorFolder);
                    }
                    if (Directory.Exists(setup.GameFolder))
                    {
                        AssetDatabase.DeleteAsset(setup.GameFolder);
                    }

                    m_SetupManager.RemoveSetup(setup);
                    m_SetupManager.Save();

                    AssetDatabase.Refresh();

                    GUIUtility.ExitGUI();
                    Save();
                }
                
                return true;
            }
            return false;
        }

        private bool DrawCloneButton(WorldSetup setup)
        {
            if (GUILayout.Button("Clone", GUILayout.MaxWidth(60)))
            {
                var parameters = new List<ParameterWindow.Parameter>()
                {
                    new ParameterWindow.StringParameter("New World", "", setup.Name + "_New"),
                };
                ParameterWindow.Open("Clone World", parameters, (p) => {
                    var ok = ParameterWindow.GetString(p[0], out var newName);
                    if (ok && m_SetupManager.QuerySetup(newName) == null)
                    {
                        CloneWorld(setup, newName);
                        return true;
                    }
                    return false;
                });

                GUIUtility.ExitGUI();

                Save();
                return true;
            }
            return false;
        }

        private void DrawOpenEditorFolder(WorldSetup setup)
        {
            if (GUILayout.Button("Editor Folder", GUILayout.MaxWidth(100)))
            {
                EditorHelper.ShowInExplorer(setup.EditorFolder);
            }
        }

        private void UpdateNames()
        {
            if (m_WorldNames == null || 
                m_WorldNames.Length != m_SetupManager.Setups.Count)
            {
                m_WorldNames = new string[m_SetupManager.Setups.Count];
            }
            for (var i = 0; i < m_WorldNames.Length; ++i)
            {
                m_WorldNames[i] = m_SetupManager.Setups[i].Name;
            }
        }

        private bool m_DrawWorlds = true;
        private Vector3 m_ScrollPos;
        private string[] m_WorldNames;
        private WorldSetupManager m_SetupManager;
    }
}

//XDay