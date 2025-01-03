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

using XDay.CameraAPI;
using XDay.SerializationAPI;
using XDay.UtilityAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace XDay.WorldAPI.Editor
{
    internal partial class EditorWorld : World, IWorldObjectContainer
    {
        public int SelectedPluginIndex { set => m_SelectedPluginIndex = value; get => m_SelectedPluginIndex; }
        public override string TypeName => "EditorWorld";

        public EditorWorld()
        {
        }

        public EditorWorld(WorldSetup setup, 
            IWorldAssetLoader assetLoader, 
            ICameraManipulator manipulator, 
            ISerializableFactory serialzableFactory, 
            EditorWorldPluginLoader pluginLoader,
            float width = 0,
            float height = 0) 
            : base(setup, new EditorCameraVisibleAreaCalculator(), assetLoader, manipulator, serialzableFactory, width, height)
        {
            m_PluginLoader = pluginLoader; 
        }

        public override void Init()
        {
            base.Init();

            m_WorldRenderer.Root.AddComponent<NoKeyDeletion>();
        }

        public WorldPlugin QueryPlugin(GameObject gameObject)
        {
            foreach (var plugin in m_Plugins)
            {
                if ((plugin as EditorWorldPlugin).Root == gameObject)
                {
                    return plugin;
                }
            }
            return null;
        }

        public void GameSerialize()
        {
            if (!Inited)
            {
                Debug.LogError("world is initializing, can't serialize game data");
                return;
            }

            var error = ValidateBeforeSerializeGameData();
            if (!string.IsNullOrEmpty(error))
            {
                EditorUtility.DisplayDialog("Error", error, "OK");
                return;
            }

            Serialize(Setup.GameFolder, WorldDefine.GAME_FILE_NAME, false);

            CreateWorldPluginSetup();

            AssetDatabase.SaveAssets();
        }

        public void EditorSerialize()
        {
            if (!Inited)
            {
                Debug.LogError("world is initializing, can't serialize editor data");
                return;
            }

            Serialize(Setup.EditorFolder, WorldDefine.EDITOR_FILE_NAME, true);

            AssetDatabase.SaveAssets();
        }

        public override void Update()
        {
            if (Inited)
            {
                foreach (var plugin in m_Plugins)
                {
                    plugin.Update();
                }
            }
        }

        public void GameSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_RuntimeVersion, "World.Version");

            serializer.WriteSerializable(m_LODSystem, "LOD System", converter, true);
            serializer.WriteSingle(m_Width, "Width");
            serializer.WriteSingle(m_Height, "Height");

            GenerateGameData(converter);
        }

        public void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_Version, "EditorWorld.Version");

            serializer.WriteInt32(m_SelectedPluginIndex, "Selected Plugin Index");
            serializer.WriteSerializable(m_LODSystem, "LOD System", converter, false);

            serializer.WriteSingle(m_Width, "Width");
            serializer.WriteSingle(m_Height, "Height");

            foreach (var plugin in m_Plugins)
            {
                var dataSerializer = (plugin as EditorWorldPlugin).CreateEditorDataSerializer();
                if (dataSerializer != null)
                {
                    dataSerializer.WriteSerializable(plugin, plugin.TypeName, converter, false);
                    dataSerializer.Uninit();
                }
            }
        }

        public void EditorDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("EditorWorld.Version");

            m_SelectedPluginIndex = deserializer.ReadInt32("Selected Plugin Index");
            m_LODSystem = deserializer.ReadSerializable<IWorldLODSystem>("LOD System", false);

            m_Width = deserializer.ReadSingle("Width");
            m_Height = deserializer.ReadSingle("Height");

            m_Plugins = m_PluginLoader.LoadPlugins(Setup.EditorFolder);

            if (m_SelectedPluginIndex == -1 && m_Plugins.Count > 0)
            {
                m_SelectedPluginIndex = 0;
            }
        }

        public void AddObjectUndo(IWorldObject obj, int lod, int index)
        {
            AddPlugin(obj as WorldPlugin, index);
        }

        public void DestroyObjectUndo(int id)
        {
            for (var i = 0; i < m_Plugins.Count; i++)
            {
                if (m_Plugins[i].ID == id)
                {
                    RemovePlugin(i);
                    return;
                }
            }

            Debug.LogError($"remove undoable object {id} failed!");
        }

        public IWorldObject QueryObjectUndo(int id)
        {
            return QueryObject<WorldObject>(id);
        }

        private void GenerateGameData(IObjectIDConverter converter)
        {
            var gamePlugins = new List<WorldPlugin>();
            foreach (var plugin in m_Plugins)
            {
                var editorPlugin = plugin as EditorWorldPlugin;
                if (editorPlugin.Usage == WorldPluginUsage.BothInEditorAndGame ||
                    editorPlugin.Usage == WorldPluginUsage.OnlyGenerateData)
                {
                    editorPlugin.GenerateGameData(converter);
                }

                if (editorPlugin.Usage == WorldPluginUsage.BothInEditorAndGame)
                {
                    gamePlugins.Add(plugin);
                }
            }

            CreatePluginList(gamePlugins);
        }

        private void CreateWorldPluginSetup()
        {
            var setupManager = EditorHelper.QueryAsset<WorldSetupManager>();
            if (setupManager == null)
            {
                Debug.LogError("no world config manager created!");
                return;
            }

            var setup = ScriptableObject.CreateInstance<WorldPluginSetup>();
            foreach (var type in Helper.QueryTypes<WorldPlugin>(false))
            {
                var plugin = Activator.CreateInstance(type) as WorldPlugin;
                if (plugin is not EditorWorldPlugin)
                {
                    var metadata = new WorldPluginMetadata
                    {
                        DataFiles = plugin.GameFileNames,
                        TypeName = plugin.TypeName,
                    };
                    setup.Metadatas.Add(metadata);
                }
            }

            AssetDatabase.CreateAsset(setup, $"{setupManager.GameFolder}/WorldPluginSetup.asset");
            AssetDatabase.Refresh();
        }

        private void CreatePluginList(List<WorldPlugin> plugins)
        {
            var serializer = ISerializer.CreateBinary();
            serializer.WriteInt32(plugins.Count, "");
            for (var i = 0; i < plugins.Count; ++i)
            {
                serializer.WriteString(plugins[i].FileName, "");
                List<string> pluginFileNames = new();
                foreach (var name in plugins[i].GameFileNames)
                {
                    pluginFileNames.Add($"{name}@{plugins[i].FileName}");
                }
                serializer.WriteStringList(pluginFileNames, "");
            }
            serializer.Uninit();
            File.WriteAllBytes($"{GameFolder}/PluginList.bytes", serializer.Data);
        }

        private string ValidateBeforeSerializeGameData()
        {
            var error = new StringBuilder();
            foreach (var plugin in m_Plugins)
            {
                (plugin as EditorWorldPlugin).ValidateBeforeSerializeGameData(error);
            }
            return error.ToString();
        }

        internal class EditorCameraVisibleAreaCalculator : ICameraVisibleAreaCalculator
        {
            public Rect VisibleArea => new(-30000, -30000, 30000, 30000);
            public Rect ExpandedArea => VisibleArea;

            public void DebugDraw()
            {
            }

            public void Update(Camera camera)
            {
            }
        }

        private EditorWorldPluginLoader m_PluginLoader;
        [XDaySerializableField(1, "Selected Plugin")]
        private int m_SelectedPluginIndex = -1;
        private const int m_Version = 1;
        private const int m_RuntimeVersion = 1;
    }
}

//XDay