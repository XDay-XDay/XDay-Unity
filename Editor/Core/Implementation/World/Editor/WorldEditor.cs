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
using XDay.CameraAPI.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using System;

namespace XDay.WorldAPI.Editor
{
    public static partial class WorldEditor
    {
        public static event System.Action EventRepaint;
        public static Camera Camera => SceneView.GetAllSceneCameras()[0];
        public static WorldManager WorldManager => m_WorldManager;
        public static IEventSystem EventSystem => m_EventSystem;
        public static EditorWorld ActiveWorld => m_ActiveWorld;

        public static void Init()
        {
            Uninit();

            m_EventSystem = IEventSystem.Create();

            m_Root = new GameObject(WorldDefine.WORLD_EDITOR_NAME);
            m_Root.tag = "EditorOnly";
            m_Root.AddComponent<PhysxSetup>();
            m_Root.AddComponent<XDayWorldEditor>();

            CreateWorldSystem();

            RegisterPlugins();

            SearchHooks();
        }

        public static void Uninit()
        {
            if (m_Hooks != null)
            {
                foreach (var hook in m_Hooks)
                {
                    hook.OnUninitEditor();
                }
                m_Hooks = null;
            }

            m_PluginsInfo.Clear();

            m_Cancel?.Cancel();
            m_Cancel = null;

            m_WorldManager?.UnloadWorlds();

            SetActiveWorld(null);

            Helper.DestroyUnityObject(m_Root);
            m_Root = null;

            m_EventSystem = null;

            m_CameraEditor = null;

            EnableRenderFeature(false);
        }

        public static void Update(float dt)
        {
            CheckSelection();

            if (IsInEditorScene())
            {
                m_WorldManager?.Update(dt);
            }
        }

        private static void SetActiveWorld(EditorWorld world)
        {
            if (m_ActiveWorld != world)
            {
                SelectedPluginIndex = -1;
                m_ActiveWorld = world;

                if (world != null)
                {
                    m_CameraEditor = new();
                    m_CameraEditor.Load(world.Setup.CameraSetupFilePath);

                    //打开LOD RenderFeature
                    EnableRenderFeature(true);
                }
            }
        }

        private static void EnableRenderFeature(bool enable)
        {
            var renderDatas = EditorHelper.QueryAssets<UniversalRendererData>();
            foreach (var renderData in renderDatas)
            {
                foreach (var feature in renderData.rendererFeatures)
                {
                    if (feature.name == "DrawObjectLOD")
                    {
                        feature.SetActive(enable);
                        break;
                    }
                }
            }
        }

        private static void CreateWorldSystem()
        {
            var createInfo = new TaskSystemCreateInfo();
            createInfo.LayerInfo.Add(new TaskLayerInfo(1, 1));

            m_WorldManager = new EditorWorldManager();
            m_WorldManager.Init(
                EditorHelper.QueryAssetFilePath<WorldSetupManager>(), 
                new EditorWorldAssetLoader(), 
                ITaskSystem.Create(createInfo), null);
        }

        private static void RegisterPlugins()
        {
            foreach (var type in Common.QueryTypes<EditorWorldPlugin>(false))
            {
                foreach (var attribute in type.GetCustomAttributes(false))
                {
                    if (attribute.GetType() == typeof(WorldPluginMetadataAttribute))
                    {
                        var metadata = attribute as WorldPluginMetadataAttribute;
                        m_WorldManager.RegisterPluginLoadInfo(type.Name, metadata.EditorFileName);
                        m_PluginsInfo.Add(new WorldPluginInfo(type, metadata.EditorFileName, metadata.DisplayName, metadata.IsSingleton, metadata.PluginCreateWindowType));
                    }
                }   
            }
        }

        private static void CreateWorld()
        {
            if (m_WorldManager?.FirstWorld != null)
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

        public static void GameSerialize()
        {
            m_ActiveWorld?.GameSerialize();

            if (m_ActiveWorld != null)
            {
                foreach (var hook in m_Hooks)
                {
                    hook.OnExportGameData();
                }
            }

            EditorSerialize();
        }

        private static void EditorSerialize()
        {
            SaveSceneObjects();

            m_CameraEditor?.Save();

            m_ActiveWorld?.EditorSerialize();

            if (m_ActiveWorld != null)
            {
                foreach (var hook in m_Hooks)
                {
                    hook.OnSaveEditorData();
                }
            }
        }

        private static void SaveSceneObjects()
        {
            if (m_ActiveWorld != null)
            {
                var scene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.SaveScene(scene, m_ActiveWorld.Setup.SceneFilePath, true);
            }
        }

        private static void ResetScene(bool exitGUI = true)
        {
            m_Cancel?.Cancel();

            Uninit();

            CreateScene("");

            if (exitGUI)
            {
                GUIUtility.ExitGUI();
            }
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
                new ParameterWindow.IntParameter("地图ID", "", setupManager.GetValidID()),
                new ParameterWindow.FloatParameter("地图宽(米)", "", 200),
                new ParameterWindow.FloatParameter("地图高(米)", "", 200),
                new ParameterWindow.StringParameter("地图名称", "", $"{setupManager.GetValidName()}")
            };
            ParameterWindow.Open("新建地图", parameters, (p) => {
                var ok = ParameterWindow.GetInt(p[0], out var worldID);
                ok &= ParameterWindow.GetFloat(p[1], out var worldWidth);
                ok &= ParameterWindow.GetFloat(p[2], out var worldHeight);
                ok &= ParameterWindow.GetString(p[3], out var worldName);
                if (ok && 
                worldWidth > 0 &&
                worldHeight > 0)
                {
                    ResetScene(false);

                    Init();

                    var config = setupManager.AddSetup(worldID, worldName, $"{worldName.Replace(' ', '_')}CameraSetup");
                    if (config != null)
                    {
                        Helper.CreateDirectory(config.GameFolder);
                        Helper.CreateDirectory(config.EditorFolder);

                        CameraSetupCreator.Create(config.CameraSetupFilePath);

                        setupManager.Save();

                        AssetDatabase.Refresh();

                        SetActiveWorld(m_WorldManager.CreateEditorWorld(config, worldWidth, worldHeight) as EditorWorld);

                        EditorSerialize();

                        EditorPrefs.SetString(WorldDefine.LAST_OPEN_FILE_PATH, config.EditorFolder);

                        return true;
                    }
                }
                return false;
            });
        }

        private static EditorWorldPlugin SelectedPlugin => m_ActiveWorld?.GetPlugin(SelectedPluginIndex) as EditorWorldPlugin;

        public static int SelectedPluginIndex
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
                if (value == -1 &&
                    m_ActiveWorld != null &&
                    m_ActiveWorld.PluginCount > 0)
                {
                    value = 0;
                }

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

        private static void SearchHooks()
        {
            m_Hooks = EditorHelper.QueryAssets<WorldEditorHook>();
            foreach (var hook in m_Hooks)
            {
                hook.OnInitEditor();
            }
        }

        private static GameObject m_Root;
        private static EditorWorldManager m_WorldManager;
        private static string[] m_PluginNames;
        private static EditorWorld m_ActiveWorld;
        private static List<WorldEditorHook> m_Hooks;
        private static IEventSystem m_EventSystem;
    }
}

//XDay
