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

using XDay.SerializationAPI;
using XDay.UtilityAPI;
using System.Collections.Generic;
using UnityEngine;
using XDay.InputAPI;
using XDay.AssetAPI;

namespace XDay.WorldAPI
{
    public partial class WorldManager : IWorldManager
    {
        public IWorld FirstWorld => m_Worlds.Count > 0 ? m_Worlds[0] : null;
        public ITaskSystem TaskSystem => m_TaskSystem;
        public IAssetLoader WorldAssetLoader => m_AssetLoader;
        public ISerializableFactory SerializableFactory => m_SerializableFactory;
        internal WorldPluginLoader PluginLoaderManager => m_PluginLoader;

        public virtual void Init(string setupFilePath, IAssetLoader assetLoader, ITaskSystem taskSystem, IDeviceInput input)
        {
            m_TaskSystem = taskSystem;
            m_AssetLoader = assetLoader;
            m_DeviceInput = input;

            m_SerializableFactory = ISerializableFactory.Create();

            m_PluginLoader = new WorldPluginLoader(assetLoader, m_SerializableFactory);
            
            InitSetup(setupFilePath);
        }

        public void OnDestroy()
        {
            UnloadWorlds();

            m_AssetLoader.OnDestroy();
        }

        public IWorld QueryWorld(string name)
        {
            foreach (var world in m_Worlds)
            {
                if (world.Name == name)
                {
                    return world;
                }
            }
            return null;
        }

        public IWorld QueryWorld(int id)
        {
            foreach (var world in m_Worlds)
            {
                if (world.ID == id)
                {
                    return world;
                }
            }
            return null;
        }

        public void UnloadWorldRenderer(string name)
        {
            var world = QueryWorld(name);
            if (world != null)
            {
                UnloadWorldRender(world.ID);
            }
            else
            {
                Debug.LogError($"Unload world renderer failed: {name}");
            }
        }

        public void LoadWorldRenderer(string name)
        {
            var world = QueryWorld(name);
            if (world != null)
            {
                LoadWorldRender(world.ID);
            }
            else
            {
                Debug.LogError($"Unload world renderer failed: {name}");
            }
        }

        public void UnloadWorld(string name)
        {
            var world = QueryWorld(name);
            if (world != null)
            {
                UnloadWorld(world.ID);
            }
            else
            {
                Debug.LogError($"Unload world failed: {name}");
            }
        }

        public void UnloadWorld(int id)
        {
            var world = QueryWorld(id);
            if (world != null)
            {
                for (var i = 0; i < m_Worlds.Count; ++i)
                {
                    if (m_Worlds[i].ID == id)
                    {
                        UnloadWorldInternal(id);
                        m_Worlds[i].OnDestroy();
                        m_Worlds.RemoveAt(i);
                        break;
                    }
                }
            }
            else
            {
                Debug.LogError($"Unload world failed: {id}");
            }
        }

        public void UnloadWorlds()
        {
            foreach (var world in m_Worlds)
            {
                world.OnDestroy();
            }
            m_Worlds.Clear();

            UnloadWorldsInternal();
        }

        public void Update()
        {
            m_TaskSystem?.Update();

            foreach (var world in m_Worlds)
            {
                world.Update();
            }
        }

        public void LateUpdate()
        {
            foreach (var world in m_Worlds)
            {
                world.LateUpdate();
            }
        }

        internal T QueryWorldObject<T>(int worldID, int objectID) where T : class, IWorldObject
        {
            var world = QueryWorld(worldID);
            if (world != null)
            {
                return world.QueryObject<T>(objectID);
            }
            Debug.Assert(false, $"Query object {objectID} failed!");
            return null;
        }

        private void LoadWorldRender(int id)
        {
            var world = QueryWorld(id);
            if (world != null)
            {
                (world as World).InitRenderer();
            }
        }

        private void UnloadWorldRender(int id)
        {
            var world = QueryWorld(id);
            if (world != null)
            {
                (world as World).UninitRenderer();
            }
        }

        private void InitSetup(string setupFilePath)
        {
            m_SetupManager = m_AssetLoader.Load<WorldSetupManager>(setupFilePath);
            if (m_SetupManager == null)
            {
                Debug.LogError($"Invalid world setup file path {setupFilePath}");
            }

            var setup = m_AssetLoader.Load<WorldPluginSetup>($"{m_SetupManager.GameFolder}/WorldPluginSetup.asset");
            if (setup != null)
            {
                foreach (var metadata in setup.Metadatas)
                {
                    m_PluginLoader.RegisterPlugin(metadata.TypeName, metadata.DataFiles);
                }
            }
        }

        protected virtual void UnloadWorldInternal(int id) { }
        protected virtual void UnloadWorldsInternal() { }

        protected List<World> m_Worlds = new();
        protected WorldSetupManager m_SetupManager;
        protected IAssetLoader m_AssetLoader;
        protected ISerializableFactory m_SerializableFactory;
        protected ITaskSystem m_TaskSystem;
        private WorldPluginLoader m_PluginLoader;
        private IDeviceInput m_DeviceInput;
    }
}

//XDay