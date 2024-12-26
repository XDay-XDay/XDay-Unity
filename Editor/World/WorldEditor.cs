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
using XDay.CameraAPI.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace XDay.WorldAPI.Editor
{
    internal static partial class WorldEditor
    {
        public static event System.Action EventRepaint;
        public static Camera Camera => SceneView.GetAllSceneCameras()[0];

        public static void Init()
        {
            Uninit();

            m_Root = new GameObject(WorldDefine.WORLD_EDITOR_NAME);

            CreateWorldSystem();

            RegisterPlugins();

            CreateSceneObjects();
        }

        public static void Uninit()
        {
            m_PluginsInfo.Clear();

            m_Cancel?.Cancel();
            m_Cancel = null;

            m_WorldSystem?.UnloadWorlds();

            SetActiveWorld(null);

            Helper.DestroyUnityObject(m_Root);
            m_Root = null;
        }

        public static void Update()
        {
            CheckSelection();

            if (IsInEditorScene())
            {
                m_WorldSystem?.Update();
            }
        }

        private static void SetActiveWorld(EditorWorld world)
        {
            if (m_ActiveWorld != world)
            {
                SelectedPluginIndex = -1;
                m_ActiveWorld = world;
            }
        }

        private static void CreateWorldSystem()
        {
            var createInfo = new TaskSystemCreateInfo();
            createInfo.LayerInfo.Add(new TaskLayerInfo(1, 1));

            m_WorldSystem = new EditorWorldManager();
            m_WorldSystem.Init(
                EditorHelper.QueryAssetFilePath<WorldSetupManager>(), 
                new EditorWorldAssetLoader(), 
                ITaskSystem.Create(createInfo), null);
        }

        private static void RegisterPlugins()
        {
            foreach (var type in Helper.QueryTypes<EditorWorldPlugin>(false))
            {
                foreach (var attribute in type.GetCustomAttributes(false))
                {
                    if (attribute.GetType() == typeof(WorldPluginMetadataAttribute))
                    {
                        var metadata = attribute as WorldPluginMetadataAttribute;
                        m_WorldSystem.RegisterPluginLoadInfo(type.Name, metadata.EditorFileName);
                        m_PluginsInfo.Add(new WorldPluginInfo(type, metadata.EditorFileName, metadata.DisplayName, metadata.IsSingleton, metadata.PluginCreateWindowType));
                    }
                }   
            }
        }

        private static void CreateWorld()
        {
            if (m_WorldSystem?.FirstWorld != null)
            {
                Debug.LogError("world is running!");
                return;
            }

            var setupManager = EditorHelper.QueryAsset<WorldSetupManager>();
            if (setupManager == null)
            {
                Debug.LogError("setup manager not found, please create one!");
                return;
            }

            ShowCreateWindow(setupManager);
        }

        private static void GameSerialize()
        {
            m_ActiveWorld?.GameSerialize();

            EditorSerialize();
        }

        private static void EditorSerialize()
        {
            m_ActiveWorld?.EditorSerialize();
        }

        private static void CreateSceneObjects()
        {
            var light = new GameObject("Light");
            var lightcomp = light.AddComponent<Light>();
            lightcomp.type = LightType.Directional;
            lightcomp.intensity = 2.0f;
            light.transform.forward = new Vector3(0, -1, 1).normalized;
        }

        private static void ResetScene()
        {
            m_Cancel?.Cancel();

            Uninit();

            CreateScene();

            GUIUtility.ExitGUI();
        }

        private static bool IsInEditorScene()
        {
            return !Application.isPlaying && 
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == WorldDefine.WORLD_EDITOR_NAME;
        }

        private static string[] CreatePluginNames()
        {
            UpdatePluginNames();

            return m_PluginNames;
        }

        private static void CheckSelection()
        {
            if (m_ActiveWorld != null)
            {
                var selection = Selection.activeGameObject;
                for (var i = 0; i < m_ActiveWorld.PluginCount; ++i)
                {
                    var plugin = m_ActiveWorld.GetPlugin(i) as EditorWorldPlugin;
                    if (plugin.Root == selection)
                    {
                        SelectedPluginIndex = i;
                        return;
                    }
                }

                if (SelectedPluginIndex == -1 && m_ActiveWorld.PluginCount > 0)
                {
                    SelectedPluginIndex = 0;
                }
            }
        }

        private static void ShowCreateWindow(WorldSetupManager setupManager)
        {
            var parameters = new List<ParameterWindow.Parameter>
            {
                new ParameterWindow.IntParameter("World ID", "", setupManager.GetValidID()),
                new ParameterWindow.FloatParameter("World Width", "", 200),
                new ParameterWindow.FloatParameter("World Height", "", 200),
                new ParameterWindow.StringParameter("World Name", "", $"{setupManager.GetValidName()}")
            };
            ParameterWindow.Open("Create World", parameters, (p) => {
                var ok = ParameterWindow.GetInt(p[0], out var worldID);
                ok &= ParameterWindow.GetFloat(p[1], out var worldWidth);
                ok &= ParameterWindow.GetFloat(p[2], out var worldHeight);
                ok &= ParameterWindow.GetString(p[3], out var worldName);
                if (ok && 
                worldWidth > 0 &&
                worldHeight > 0)
                {
                    Init();

                    var config = setupManager.AddSetup(worldID, worldName, $"{worldName.Replace(' ', '_')}CameraSetup");
                    if (config != null)
                    {
                        Helper.CreateDirectory(config.GameFolder);
                        Helper.CreateDirectory(config.EditorFolder);

                        CameraSetupCreator.Create(config.CameraSetupFilePath);

                        setupManager.Save();

                        AssetDatabase.Refresh();

                        SetActiveWorld(m_WorldSystem.CreateEditorWorld(config, worldWidth, worldHeight) as EditorWorld);

                        EditorSerialize();

                        return true;
                    }
                }
                return false;
            });
        }

        private static EditorWorldPlugin SelectedPlugin => m_ActiveWorld?.GetPlugin(SelectedPluginIndex) as EditorWorldPlugin;

        private static int SelectedPluginIndex
        {
            get
            {
                if (m_ActiveWorld == null ||
                    m_ActiveWorld.PluginCount == 0)
                {
                    return -1;
                }

                return m_ActiveWorld.SelectedPluginIndex;
            }

            set
            {
                if (m_ActiveWorld != null &&
                    m_ActiveWorld.SelectedPluginIndex != value)
                {
                    if (m_ActiveWorld.SelectedPluginIndex >= 0)
                    {
                        var oldPlugin = m_ActiveWorld.GetPlugin(m_ActiveWorld.SelectedPluginIndex) as EditorWorldPlugin;
                        oldPlugin?.SetSelected(false);
                    }

                    m_ActiveWorld.SelectedPluginIndex = value;

                    if (value >= 0)
                    {
                        var newPlugin = m_ActiveWorld.GetPlugin(m_ActiveWorld.SelectedPluginIndex) as EditorWorldPlugin;
                        newPlugin?.SetSelected(true);

                        Selection.activeGameObject = newPlugin.Root;

                        EventRepaint?.Invoke();
                    }   
                }
            }
        }

        private static GameObject m_Root;
        private static EditorWorldManager m_WorldSystem;
        private static string[] m_PluginNames;
        private static EditorWorld m_ActiveWorld;
    }
}

//XDay