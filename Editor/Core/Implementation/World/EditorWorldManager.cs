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
using XDay.CameraAPI;
using XDay.InputAPI;
using XDay.UtilityAPI;
using System;
using UnityEditor;
using UnityEngine;
using XDay.AssetAPI;

namespace XDay.WorldAPI.Editor
{
    internal class EditorWorldManager : WorldManager
    {
        public override void Init(string setupFilePath, IAssetLoader assetLoader, ITaskSystem taskSystem, IDeviceInput deviceInput)
        {
            base.Init(setupFilePath, assetLoader, taskSystem, deviceInput);

            m_PluginLoader = new EditorWorldPluginLoader(m_SerializableFactory);

            UndoSystem.Init(new UndoSerializer(m_SerializableFactory, this), QueryObjectUndo, QueryObjectUndoRelay);
        }

        public void RegisterPluginLoadInfo(string typeName, string fileName)
        {
            m_PluginLoader.RegisterPluginLoadFile(typeName, fileName);
        }

        public void LoadEditorWorldAsync(WorldSetup setup, Action<World> onLoadFinished)
        {
            var manipulator = ICameraManipulator.Create(SceneView.GetAllSceneCameras()[0], new CameraSetup("noop"), IDeviceInput.Create());
            var world = new EditorWorld(setup, m_AssetLoader, manipulator, m_SerializableFactory, m_PluginLoader);
            m_Worlds.Add(world);

            var job = m_TaskSystem.GetTask<EditorWorldLoadTask>();
            job.Init(GetEditorFilePath(setup), world, true, onLoadFinished);
            m_TaskSystem.ScheduleTask(job);
        }

        public async UniTask<IWorld> LoadEditorWorldAsync(WorldSetup setup)
        {
            var manipulator = ICameraManipulator.Create(SceneView.GetAllSceneCameras()[0], new CameraSetup("noop"), IDeviceInput.Create());
            var world = new EditorWorld(setup, m_AssetLoader, manipulator, m_SerializableFactory, m_PluginLoader);
            m_Worlds.Add(world);

            var source = new UniTaskCompletionSource<World>();

            var job = m_TaskSystem.GetTask<EditorWorldLoadTask>();
            job.Init(GetEditorFilePath(setup), world, false, (world) => { source.TrySetResult(world); });
            m_TaskSystem.ScheduleTask(job);

            await source.Task;

            return world;
        }

        public IWorld LoadEditorWorld(string name)
        {
            var config = m_SetupManager.QuerySetup(name);
            if (config != null)
            {
                return LoadEditorWorld(config);
            }
            
            Debug.LogError($"Load world {name} failed!");
            return null;
        }

        public EditorWorld LoadEditorWorld(WorldSetup setup)
        {
            if (QueryWorld(setup.ID) != null)
            {
                Debug.LogError($"world {setup.Name} already loaded!");
                return null;
            }

            var manipulator = ICameraManipulator.Create(SceneView.GetAllSceneCameras()[0], new CameraSetup("dummy"), IDeviceInput.Create());
            var world = new EditorWorld(setup, m_AssetLoader, manipulator, m_SerializableFactory, m_PluginLoader);
            m_Worlds.Add(world);

            world.Load(GetEditorFilePath(setup));
            return world;
        }

        public IWorld CreateEditorWorld(WorldSetup config, float width, float height)
        {
            if (config == null)
            {
                Debug.LogError("config is null!");
                return null;
            }

            if (QueryWorld(config.ID) != null)
            {
                Debug.LogError($"ID {config.ID} already exists!");
                return null;
            }

            var manipulator = ICameraManipulator.Create(SceneView.GetAllSceneCameras()[0], new CameraSetup("dummy"), IDeviceInput.Create());
            var world = new EditorWorld(config, m_AssetLoader, manipulator, m_SerializableFactory, m_PluginLoader, width, height);
            world.Init();
            m_Worlds.Add(world);

            return world;
        }

        private IWorldObjectContainer QueryObjectUndoRelay(int worldID, int containerID)
        {
            var world = QueryWorld(worldID) as EditorWorld;
            var container = world.QueryObjectUndo(containerID);
            if (container == null)
            {
                return world;
            }
            return container as IWorldObjectContainer;
        }

        private IWorldObject QueryObjectUndo(int worldID, int objectID)
        {
            var world = QueryWorld(worldID) as EditorWorld;
            return world.QueryObjectUndo(objectID);
        }

        protected override void UnloadWorldsInternal()
        {
            UndoSystem.Clear();
        }

        protected override void UnloadWorldInternal(int id)
        {
            UndoSystem.Clear();
        }

        internal class EditorWorldLoadTask : GameWorldLoadTask
        {
            public void Init(string path, World world, bool initWorld, Action<World> onLoadingFinished)
            {
                m_Path = path;

                Init(world, initWorld, onLoadingFinished);
            }

            public override ITaskOutput Run()
            {
                m_LoadSuccess = (m_World as EditorWorld).LoadData(m_Path);
                return null;
            }

            private string m_Path;
        }

        private string GetEditorFilePath(WorldSetup setup) => $"{setup.EditorFolder}/{WorldDefine.EDITOR_FILE_NAME}.bytes";
        private EditorWorldPluginLoader m_PluginLoader;
    }
}

//XDay