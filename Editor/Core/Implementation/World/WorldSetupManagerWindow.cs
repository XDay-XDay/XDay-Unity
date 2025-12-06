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
    internal class WorldSetupManagerWindow : EditorWindow
    {
        [MenuItem("XDay/地图/创建配置文件", false, 30)]
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

            EditorUtility.DisplayDialog("出错了", $"位置文件已经存在,在路径{AssetDatabase.GetAssetPath(setupManager)}", "确定");
        }

        [MenuItem("XDay/地图/地图管理", false, 31)]
        private static void OpenSetupWindow()
        {
            GetWindow<WorldSetupManagerWindow>("地图配置").Show();
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
            m_DrawWorlds = EditorGUILayout.Foldout(m_DrawWorlds, "所有地图");
            if (m_DrawWorlds)
            {
                EditorHelper.IndentLayout(() =>
                {
                    EditorGUILayout.IntField("地图数", m_SetupManager.Setups.Count);
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
                setup.Name = EditorGUILayout.TextField("地图名称", setup.Name);
                setup.ID = EditorGUILayout.IntField("地图ID", setup.ID);
                GUI.enabled = false;
                setup.CameraSetupFileName = EditorGUILayout.TextField("相机设置", setup.CameraSetupFileName);
                setup.GameFolder = EditorHelper.ObjectField<DefaultAsset>("游戏目录", setup.GameFolder);
                setup.EditorFolder = Helper.ToRelativePath(EditorHelper.DirectoryField("地编目录", setup.EditorFolder), Directory.GetCurrentDirectory());
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

            var newIndex = EditorGUILayout.Popup("预览地图", previewWorldIndex, m_WorldNames);
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
                EditorUtility.DisplayDialog("出错了", "地图数据目录为空,无法复制地图", "确定");
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

        private void RenameWorld(WorldSetup setup, string newWorldName)
        {
            var dir = Helper.GetFolderPath(setup.EditorFolder);
            var oldGameFolder = setup.GameFolder;
            var oldEditorFolder = setup.EditorFolder;
            var newEditorFolder = $"{dir}/{newWorldName}";
            dir = Helper.GetFolderPath(setup.GameFolder);
            var newGameFolder = $"{dir}/{newWorldName}";

            if (Directory.Exists(newEditorFolder) || Directory.Exists(newGameFolder))
            {
                Debug.LogError("目标目录已经存在,无法重命名!");
                return;
            }

            if (m_SetupManager.PreviewWorldName == setup.Name) 
            {
                m_SetupManager.PreviewWorldName = newWorldName;
            }
            setup.Name = newWorldName;
            setup.EditorFolder = newEditorFolder;
            setup.GameFolder = newGameFolder;

            Helper.RenameFolder(oldGameFolder, setup.GameFolder);
            Helper.RenameFolder(oldEditorFolder, setup.EditorFolder);

            AssetDatabase.Refresh();

            var oldSetupFilePath = setup.CameraSetupFilePath;

            setup.CameraSetupFileName = $"{newWorldName}CameraSetup";
            FileUtil.MoveFileOrDirectory(oldSetupFilePath, setup.CameraSetupFilePath);

            m_SetupManager.Save();

            AssetDatabase.Refresh();
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
            GUI.enabled = string.IsNullOrEmpty(m_SetupManager.EditorFolder);
            var directory = EditorHelper.DirectoryField("地编目录", m_SetupManager.EditorFolder, "EditorWorld", () => { Save(); });
            m_SetupManager.EditorFolder = Helper.ToRelativePath(directory, Directory.GetCurrentDirectory());
            GUI.enabled = true;
        }

        private void DrawGameFolder()
        {
            GUI.enabled = string.IsNullOrEmpty(m_SetupManager.GameFolder);
            m_SetupManager.GameFolder = EditorHelper.ObjectField<DefaultAsset>("游戏目录", m_SetupManager.GameFolder, 0, () => { EditorUtility.SetDirty(m_SetupManager); });
            GUI.enabled = true;
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();

            if (GUILayout.Button("保存", GUILayout.MaxWidth(40)))
            {
                Save();
            }

            if (GUILayout.Button("选中配置文件", GUILayout.MaxWidth(80)))
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

                    DrawRenameButton(setup);

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
            if (GUILayout.Button("删除", GUILayout.MaxWidth(40)))
            {
                var ok = EditorUtility.DisplayDialog("注意", "确定删除?", "确定", "取消");
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

        private void DrawRenameButton(WorldSetup setup)
        {
            if (GUILayout.Button("重命名", GUILayout.MaxWidth(50)))
            {
                var parameters = new List<ParameterWindow.Parameter>()
                {
                    new ParameterWindow.StringParameter("新名称", "", setup.Name + "_New"),
                };
                ParameterWindow.Open("重命名地图", parameters, (p) => {
                    var ok = ParameterWindow.GetString(p[0], out var newName);
                    if (ok && m_SetupManager.QuerySetup(newName) == null)
                    {
                        RenameWorld(setup, newName);
                        return true;
                    }
                    return false;
                });

                GUIUtility.ExitGUI();

                Save();
            }
        }

        private bool DrawCloneButton(WorldSetup setup)
        {
            if (GUILayout.Button("复制", GUILayout.MaxWidth(40)))
            {
                var parameters = new List<ParameterWindow.Parameter>()
                {
                    new ParameterWindow.StringParameter("New World", "", setup.Name + "_New"),
                };
                ParameterWindow.Open("复制地图", parameters, (p) => {
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
            if (GUILayout.Button(new GUIContent("地编目录", "打开地编数据目录"), GUILayout.MaxWidth(100)))
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