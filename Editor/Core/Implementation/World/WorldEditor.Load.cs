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

using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEditor;
using System.IO;
using XDay.UtilityAPI;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace XDay.WorldAPI.Editor
{
    internal static partial class WorldEditor
    {
        public static async UniTask LoadWorldAsyncUniTask()
        {
            var setup = QuerySetup();
            if (setup != null)
            {
                Init();

                var world = await m_WorldSystem.LoadEditorWorldAsync(setup) as EditorWorld;
                if (world != null)
                {
                    SetActiveWorld(world);
                }

                try
                {
                    m_Cancel = new CancellationTokenSource();
                    await world.InitAsync(m_Cancel.Token);
                }
                catch (OperationCanceledException)
                {
                    Debug.Log("Async world loading cancelled!");
                }
            }
        }

        public static void LoadWorldAsync()
        {
            var setup = QuerySetup();
            if (setup != null)
            {
                Uninit();

                CreateScene();

                Init();

                m_WorldSystem.LoadEditorWorldAsync(setup, (world) => { SetActiveWorld(world as EditorWorld); });
            }
        }

        public static void LoadWorld()
        {
            var setup = QuerySetup();
            if (setup != null)
            {
                Uninit();

                CreateScene();

                Init();

                SetActiveWorld(m_WorldSystem.LoadEditorWorld(setup));
            }
        }

        private static WorldSetup QuerySetup()
        {
            var setupManager = EditorHelper.QueryAsset<WorldSetupManager>();
            if (setupManager == null)
            {
                EditorUtility.DisplayDialog("Error", "WorldSetupManager not found!", "OK");
                return null;
            }

            var path = EditorPrefs.GetString(WorldDefine.LAST_OPEN_FILE_PATH);

            var selectedFolder = EditorUtility.OpenFolderPanel(
                "Select World",
                string.IsNullOrEmpty(path) ? "EditorWorld" : path, 
                "");
            if (!string.IsNullOrEmpty(selectedFolder))
            {
                var name = Path.GetFileNameWithoutExtension(selectedFolder);
                var setup = setupManager.QuerySetup(name);

                EditorPrefs.SetString(WorldDefine.LAST_OPEN_FILE_PATH, selectedFolder); 

                return setup;
            }

            return null;
        }

        public static void CreateScene()
        {
            EditorSceneManager.newSceneCreated -= OnSceneCreated;
            EditorSceneManager.newSceneCreated += OnSceneCreated;
            EditorSceneManager.sceneClosed -= OnSceneClosed;
            EditorSceneManager.sceneClosed += OnSceneClosed;

            m_IsCreatingEditorScene = true;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = WorldDefine.WORLD_EDITOR_NAME;
        }

        private static void OnSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            if (m_IsCreatingEditorScene)
            {
                m_IsCreatingEditorScene = false;

                UndoSystem.AddUndoRedoCallback(OnRedoUndoApplied);
            }
        }

        private static void OnSceneClosed(Scene scene)
        {
            if (scene.name == WorldDefine.WORLD_EDITOR_NAME)
            {
                UndoSystem.RemoveUndoRedoCallback(OnRedoUndoApplied);

                Uninit();
            }
        }

        private static void OnRedoUndoApplied(UndoAction action, bool undo)
        {
            var window = EditorHelper.GetEditorWindow<WorldEditorEntrance>();
            if (window != null)
            {
                window.Repaint();
            }
        }

        private static bool m_IsCreatingEditorScene = false;
        private static CancellationTokenSource m_Cancel;
    }
}

//XDay