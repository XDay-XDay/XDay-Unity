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

using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using XDay.CameraAPI;
using XDay.UtilityAPI;

namespace XDay.WorldAPI
{
    public partial class WorldManager
    {
        public async UniTask<IWorld> LoadWorldAsync(string name, Camera camera)
        {
            name = ConvertName(name);
            var config = m_SetupManager.QuerySetup(name);
            if (config != null)
            {
                return await LoadWorldAsyncInternal(config, camera);
            }
            Debug.LogError($"load world failed: {name}");
            return null;
        }

        public IWorld LoadWorld(string name, Camera camera)
        {
            name = ConvertName(name);

            var config = m_SetupManager.QuerySetup(name);
            if (config == null)
            {
                Debug.LogError($"load world {name} failed");
                return null;
            }

            return LoadWorldInternal(config, camera);
        }

        private async UniTask<IWorld> LoadWorldAsyncInternal(WorldSetup config, Camera camera)
        {
            var source = new UniTaskCompletionSource<IWorld>();

            LoadWorldAsyncInternal(config, (world) => {
                OnLoadFinished(world, camera);

                source.TrySetResult(world);
            });

            return await source.Task;
        }

        private void LoadWorldAsyncInternal(WorldSetup config, Action<IWorld> onLoadingFinished)
        {
            var world = new GameWorld(config, m_AssetLoader, null, m_SerializableFactory, m_PluginLoader);
            m_Worlds.Add(world);

            m_PluginLoader.Load(world.ID, config.GameFolder);

            var job = m_TaskSystem.GetTask<GameWorldLoadTask>();
            job.Init(world, true, onLoadingFinished);
            m_TaskSystem.ScheduleTask(job);
        }

        private IWorld LoadWorldInternal(WorldSetup config, Camera camera)
        {
            var world = QueryWorld(config.ID);
            if (world != null)
            {
                Debug.LogError($"world {config.Name} already loaded");
                return world;
            }

            var gameWorld = new GameWorld(config, m_AssetLoader, null, m_SerializableFactory, m_PluginLoader);
            m_Worlds.Add(gameWorld);

            m_PluginLoader.Load(gameWorld.ID, config.GameFolder);
            gameWorld.LoadGame();

            OnLoadFinished(world, camera);

            return gameWorld;
        }

        private void OnLoadFinished(IWorld world, Camera camera)
        {
            var w = world as World;
            var cameraSetupPath = w.Setup.CameraSetupFilePath;
            var text = m_AssetLoader.LoadText(cameraSetupPath);
            var setup = new CameraSetup(Helper.GetPathName(cameraSetupPath, false));
            setup.Load(text);

            var manipulator = ICameraManipulator.Create(camera, setup, m_DeviceInput);
            manipulator.SetActive(true);

            world.CameraManipulator = manipulator;
            manipulator.SetFocusPointBounds(world.Bounds.min, world.Bounds.max);
            manipulator.EnableFocusPointClampXZ = true;
        }

        private string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = m_SetupManager.PreviewWorldName;
            }
            if (string.IsNullOrEmpty(name))
            {
                var setups = m_SetupManager.Setups;
                if (setups.Count > 0)
                {
                    name = setups[0].Name;
                }
                Debug.LogError($"ActorWorldName is null, will use first world setup name {name}");
            }
            Debug.Assert(!string.IsNullOrEmpty(name), "Invalid world name!");
            return name;
        }

        public class GameWorldLoadTask : TaskBase
        {
            public override int Layer => 1;

            public void Init(World world, bool initWorld, Action<World> onLoadingFinished)
            {
                m_World = world;
                m_InitWorld = initWorld;
                m_OnLoadFinished = onLoadingFinished;
            }

            public override ITaskOutput Run()
            {
                m_LoadSuccess = (m_World as GameWorld).LoadData();
                return null;
            }

            public override void OnTaskCompleted(ITaskOutput result)
            {
                try
                {
                    if (!m_LoadSuccess)
                    {
                        Debug.LogError($"Load world {m_World.Name} failed!");
                    }
                    else
                    {
                        if (m_InitWorld)
                        {
                            m_World.Init();
                        }
                        m_OnLoadFinished?.Invoke(m_World);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            }

            private Action<World> m_OnLoadFinished;
            private bool m_InitWorld;
            protected bool m_LoadSuccess;
            protected World m_World;
        }
    }
}

//XDay