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
using XDay.UtilityAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace XDay.WorldAPI.Editor
{
    public partial class EditorWorld : World, IWorldObjectContainer
    {
        public int SelectedPluginIndex { set => m_SelectedPluginIndex = value; get => m_SelectedPluginIndex; }
        public override string TypeName => "EditorWorld";
        public override int CurrentLOD => throw new NotImplementedException();
        public bool AllowUndo => true;
        public Dictionary<int, IWorldObject> AllObjects => m_Objects;
        public float VisibleAreaUpdateDistance { get => m_VisibleAreaUpdateDistance; set => m_VisibleAreaUpdateDistance = value; }

        public EditorWorld()
        {
        }

        public EditorWorld(
            IWorldManager worldManager, WorldSetup setup,
            IWorldAssetLoader assetLoader, 
            ICameraManipulator manipulator, 
            ISerializableFactory serialzableFactory, 
            EditorWorldPluginLoader pluginLoader,
            float width = 0,
            float height = 0) 
            : base(worldManager, setup, new EditorCameraVisibleAreaCalculator(), assetLoader, manipulator, serialzableFactory, width, height)
        {
            m_PluginLoader = pluginLoader; 
        }

        public override void Init()
        {
            base.Init();

            SelectedPluginIndex = EditorPrefs.GetInt(WorldDefine.SELECTED_PLUGIN_INDEX, m_Plugins.Count > 0 ? 0 : -1);

            m_WorldRenderer.Root.AddComponent<NoKeyDeletion>();

            if (GetPlugin(m_SelectedPluginIndex) is EditorWorldPlugin selectedPlugin)
            {
                Selection.activeGameObject = selectedPlugin.Root;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
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

        protected override void OnUpdate(float dt)
        {
            if (Inited)
            {
                foreach (var plugin in m_Plugins)
                {
                    plugin.Update(dt);
                }
            }
        }

        public void GameSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_RuntimeVersion, "World.Version");

            serializer.WriteSerializable(m_LODSystem, "LOD System", converter, true);
            serializer.WriteSingle(m_Width, "Width");
            serializer.WriteSingle(m_Height, "Height");

            serializer.WriteBoolean(EnableGrid, "Enable Grid");
            serializer.WriteSingle(m_Grid.GridWidth, "Grid Width");
            serializer.WriteSingle(m_Grid.GridHeight, "Grid Height");
            serializer.WriteInt32(m_Grid.HorizontalGridCount, "Horizontal Grid Count");
            serializer.WriteInt32(m_Grid.VerticalGridCount, "Vertical Grid Count");
			serializer.WriteSingle(m_VisibleAreaUpdateDistance, "VisibleAreaUpdateDistance");

            GenerateGameData(converter);
        }

        public void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            serializer.WriteInt32(m_EditorVersion, "EditorWorld.Version");

            EditorPrefs.SetInt(WorldDefine.SELECTED_PLUGIN_INDEX, m_SelectedPluginIndex);
            serializer.WriteSerializable(m_LODSystem, "LOD System", converter, false);

            serializer.WriteSingle(m_Width, "Width");
            serializer.WriteSingle(m_Height, "Height");

            //save grid
            serializer.WriteBoolean(EnableGrid, "Enable Grid");
            serializer.WriteSingle(m_Grid.GridWidth, "Grid Width");
            serializer.WriteSingle(m_Grid.GridHeight, "Grid Height");
            serializer.WriteInt32(m_Grid.HorizontalGridCount, "Horizontal Grid Count");
            serializer.WriteInt32(m_Grid.VerticalGridCount, "Vertical Grid Count");
            serializer.WriteSingle(m_VisibleAreaUpdateDistance, "VisibleAreaUpdateDistance");

            foreach (var plugin in m_Plugins)
            {
                var editorPlugin = plugin as EditorWorldPlugin;
                var dataSerializer = editorPlugin.CreateEditorDataSerializer();
                if (dataSerializer != null)
                {
                    var pluginConverter = new ToPersistentID(editorPlugin.FileIDOffset);
                    dataSerializer.WriteSerializable(editorPlugin, editorPlugin.TypeName, pluginConverter, false);
                    dataSerializer.Uninit();
                }
            }
        }

        public void EditorDeserialize(IDeserializer deserializer, string label)
        {
            var version = deserializer.ReadInt32("EditorWorld.Version");

            if (version == 1)
            {
                m_SelectedPluginIndex = deserializer.ReadInt32("Selected Plugin Index");
            }
            m_LODSystem = deserializer.ReadSerializable<IWorldLODSystem>("LOD System", false);

            m_Width = deserializer.ReadSingle("Width");
            m_Height = deserializer.ReadSingle("Height");
            if (version >= 3)
            {
                m_ShowGrid = deserializer.ReadBoolean("Enable Grid");
            }
            if (version >= 4)
            {
                m_GridWidth = deserializer.ReadSingle("Grid Width", 100);
                m_GridHeight = deserializer.ReadSingle("Grid Height", 100);
                m_HorizontalGridCount = deserializer.ReadInt32("Horizontal Grid Count", 27);
                m_VerticalGridCount = deserializer.ReadInt32("Vertical Grid Count", 27);
                if (m_GridWidth == 0)
                {
                    m_GridWidth = 100;
                }
                if (m_GridHeight == 0)
                {
                    m_GridHeight = 100;
                }
                if (m_HorizontalGridCount == 0)
                {
                    m_HorizontalGridCount = Mathf.CeilToInt(m_Width / m_GridWidth);
                }
                if (m_VerticalGridCount == 0)
                {
                    m_VerticalGridCount = Mathf.CeilToInt(m_Height / m_GridHeight);
                }
            }
            if (version >= 5)
            {
                m_VisibleAreaUpdateDistance = deserializer.ReadSingle("VisibleAreaUpdateDistance");
            }

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
            foreach (var type in Common.QueryTypes<WorldPlugin>(false))
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
            public Vector2 ExpandSize { get => Vector2.zero; set { } }

            public void DebugDraw()
            {
            }

            public Vector3 GetFocusPoint(Camera camera)
            {
                return Vector3.zero;
            }

            public Rect GetNarrowVisibleAreas(Camera camera)
            {
                return new Rect();
            }

            public Rect GetVisibleAreas(Camera camera)
            {
                return new Rect();
            }

            public void Update(Camera camera)
            {
            }
        }

        private EditorWorldPluginLoader m_PluginLoader;
        [XDaySerializableField(1, "Selected Plugin")]
        private int m_SelectedPluginIndex = -1;

        private float m_VisibleAreaUpdateDistance = 5;

        private const int m_EditorVersion = 5;
        private const int m_RuntimeVersion = 2;
    }
}

//XDay