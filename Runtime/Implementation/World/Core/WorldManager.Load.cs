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
            var setup = m_SetupManager.QuerySetup(name);
            if (setup != null)
            {
                return await LoadWorldAsyncInternal(setup, camera);
            }
            Debug.LogError($"load world failed: {name}");
            return null;
        }

        public async UniTask<IWorld> LoadWorldAsync(int worldID, Camera camera)
        {
            var setup = m_SetupManager.QuerySetup(worldID);
            if (setup != null)
            {
                return await LoadWorldAsyncInternal(setup, camera);
            }
            Debug.LogError($"load world failed: {worldID}");
            return null;
        }

        public IWorld LoadWorld(string name, Camera camera)
        {
            name = ConvertName(name);

            var setup = m_SetupManager.QuerySetup(name);
            if (setup == null)
            {
                Debug.LogError($"load world {name} failed");
                return null;
            }

            return LoadWorldInternal(setup, camera);
        }

        private async UniTask<IWorld> LoadWorldAsyncInternal(WorldSetup setup, Camera camera)
        {
            var source = new UniTaskCompletionSource<IWorld>();

            LoadWorldAsyncInternal(setup, (world) => {
                OnLoadFinished(world, camera);

                source.TrySetResult(world);
            });

            return await source.Task;
        }

        private void LoadWorldAsyncInternal(WorldSetup setup, Action<IWorld> onLoadingFinished)
        {
            var world = new GameWorld(setup, m_AssetLoader, null, m_SerializableFactory, m_PluginLoader);
            m_Worlds.Add(world);

            m_PluginLoader.Load(world.ID, setup.GameFolder);

            var job = m_TaskSystem.GetTask<GameWorldLoadTask>();
            job.Init(world, true, onLoadingFinished);
            m_TaskSystem.ScheduleTask(job);
        }

        private IWorld LoadWorldInternal(WorldSetup setup, Camera camera)
        {
            var world = QueryWorld(setup.ID);
            if (world != null)
            {
                Debug.LogError($"world {setup.Name} already loaded");
                return world;
            }

            var gameWorld = new GameWorld(setup, m_AssetLoader, null, m_SerializableFactory, m_PluginLoader);
            m_Worlds.Add(gameWorld);

            m_PluginLoader.Load(gameWorld.ID, setup.GameFolder);
            gameWorld.LoadGame();

            OnLoadFinished(gameWorld, camera);

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
            manipulator.SetFocusPointBounds(world.Bounds.min.ToVector2(), world.Bounds.max.ToVector2());
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