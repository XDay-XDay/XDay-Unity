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
using XDay.UtilityAPI;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using XDay.AssetAPI;

namespace XDay.WorldAPI
{
    [XDaySerializableClass("World")]
    public abstract partial class World : IWorld, ISerializable
    {
        public float Width => m_Width;
        public float Height => m_Height;
        public bool Inited => m_Inited;
        public int PluginCount => m_Plugins.Count;
        public int LODCount => m_LODSystem.LODCount;
        public int ID => m_Setup.ID;
        public string Name => m_Setup.Name;
        public WorldSetup Setup => m_Setup;
        public ICameraVisibleAreaCalculator CameraVisibleAreaCalculator => m_CameraVisibleAreaCalculator;
        public GameObject Root => m_WorldRenderer.Root;
        public IGameObjectPool GameObjectPool => m_WorldRenderer.Cache;
        public IWorldLODSystem WorldLODSystem => m_LODSystem;
        public ISerializableFactory SerializableFactory => m_SerializableFactory;
        public ICameraManipulator CameraManipulator
        {
            set
            {
                if (m_Manipulator != value)
                {
                    m_Manipulator?.SetActive(false);
                    m_Manipulator = value;
                    m_Manipulator?.SetActive(true);
                }
            }
            get => m_Manipulator;
        }
        public Bounds Bounds => new(new Vector3(m_Width * 0.5f, 0, m_Height * 0.5f), new Vector3(m_Width, 0, m_Height));
        public string GameFolder => Setup.GameFolder;
        public string EditorFolder => Setup.EditorFolder;
        public IAssetLoader AssetLoader => m_AssetLoader;
        public abstract string TypeName { get; }

        public World()
        {
        }

        public World(
            WorldSetup setup, 
            ICameraVisibleAreaCalculator calculator, 
            IAssetLoader assetLoader, 
            ICameraManipulator manipulator, 
            ISerializableFactory serialzableFactory, 
            float width, 
            float height)
        {
            m_Setup = setup;
            m_Plugins = new();
            m_Objects = new();
            m_AssetLoader = assetLoader;
            m_CameraVisibleAreaCalculator = calculator;
            m_Manipulator = manipulator;
            m_SerializableFactory = serialzableFactory;
            m_Width = width;
            m_Height = height;

            m_LODSystem = new WorldLODSystem();
        }

        public int AllocateObjectID()
        {
            return --m_NextObjectID;
        }

        public T QueryObject<T>(int id) where T : class, IWorldObject
        {
            if (id == 0)
            {
                return null;
            }

            m_Objects.TryGetValue(id, out var obj);
            return obj as T;
        }

        public async UniTask InitAsync(CancellationToken token)
        {
            PreInit();

            foreach (var plugin in m_Plugins)
            {
                await plugin.InitAsync(this, token);
                token.ThrowIfCancellationRequested();
            }

            PostInit();
        }

        public virtual void Init()
        {
            PreInit();

            foreach (var plugin in m_Plugins)
            {
                plugin.Init(this);
            }

            PostInit();
        }

        public int QueryPluginIndex(IWorldPlugin plugin)
        {
            for (var i = 0; i < m_Plugins.Count; ++i)
            {
                if (m_Plugins[i] == plugin)
                {
                    return i;
                }
            }
            return -1;
        }

        public T QueryPlugin<T>() where T : class, IWorldPlugin
        {
            foreach (var plugin in m_Plugins)
            {
                if (plugin is T p)
                {
                    return p;
                }
            }
            return null;
        }

        public List<T> QueryPlugins<T>() where T : class, IWorldPlugin
        {
            var plugins = new List<T>();
            foreach (var plugin in m_Plugins)
            {
                if (plugin is T p)
                {
                    plugins.Add(p);
                }
            }
            return plugins;
        }

        public T QueryPlugin<T>(string name) where T : class, IWorldPlugin
        {
            foreach (var plugin in m_Plugins)
            {
                if (plugin.Name == name &&
                    plugin is T p)
                {
                    return p;
                }
            }
            return null;
        }

        public T QueryPlugin<T>(int id) where T : class, IWorldPlugin
        {
            foreach (var plugin in m_Plugins)
            {
                if (plugin.ID == id && plugin is T p)
                {
                    return p;
                }
            }
            return null;
        }

        public virtual void InitRenderer()
        {
            if (m_WorldRenderer == null)
            {
                m_WorldRenderer = new(m_Setup.Name, m_AssetLoader);
                foreach (var plugin in m_Plugins)
                {
                    plugin.InitRenderer();
                }
                InitRendererInternal();
            }
        }

        public virtual void UninitRenderer()
        {
            foreach (var plugin in m_Plugins)
            {
                plugin.UninitRenderer();
            }

            m_WorldRenderer?.OnDestroy();
            m_WorldRenderer = null;
        }

        public IWorldPlugin GetPlugin(int index)
        {
            if (index >= 0 && index < m_Plugins.Count)
            {
                return m_Plugins[index];
            }
            return null;
        }

        public bool HasPlugin(System.Type type)
        {
            foreach (var plugin in m_Plugins)
            {
                if (plugin.GetType() == type)
                {
                    return true;
                }
            }
            return false;
        }

        public virtual void OnDestroy()
        {
            m_Inited = false;

            m_Manipulator?.SetActive(false);

            foreach (var plugin in m_Plugins)
            {
                plugin.Uninit();
            }
            m_Plugins.Clear();

            m_WorldRenderer?.OnDestroy();
            m_WorldRenderer = null;

            Debug.Assert(m_Objects.Count == 0);
            m_Objects.Clear();
        }

        public bool HasPlugin(string name)
        {
            foreach (var plugin in m_Plugins)
            {
                if (plugin.Name == name)
                {
                    return true;
                }
            }
            return false;
        }

        public void RemovePlugin(int index)
        {
            if (index >= 0 && index < m_Plugins.Count)
            {
                m_Plugins[index].Uninit();
                m_Plugins.RemoveAt(index);
            }
            else
            {
                Debug.LogError($"RemovePlugin failed at index {index}");
            }
        }

        public void AddPlugin(IWorldPlugin plugin, int index)
        {
            m_Plugins.Insert(index, plugin as WorldPlugin);
        }

        public abstract void Update();

        public virtual void LateUpdate() 
        {
            m_Manipulator?.LateUpdate();
        }

        public virtual IDeserializer QueryGameDataDeserializer(int worldID, string gameDataFileName)
        {
            throw new System.NotImplementedException();
        }

        internal void RegisterObject(IWorldObject obj)
        {
            Debug.Assert(obj.ID != 0);
            m_Objects.Add(obj.ID, obj);
        }

        internal void UnregisterObject(int id)
        {
            m_Objects.Remove(id);
        }

        protected virtual void InitRendererInternal() { }

        private void PreInit()
        {
            m_WorldRenderer = new(m_Setup.Name, m_AssetLoader);
        }

        private void PostInit()
        {
            m_Inited = true;
        }

        private bool m_Inited = false;
        private int m_NextObjectID = 0;
        private WorldSetup m_Setup;
        private Dictionary<int, IWorldObject> m_Objects;
        private ICameraVisibleAreaCalculator m_CameraVisibleAreaCalculator;
        protected IAssetLoader m_AssetLoader;
        protected ICameraManipulator m_Manipulator;
        protected ISerializableFactory m_SerializableFactory;
        protected WorldRenderer m_WorldRenderer;

        [XDaySerializableField(1, "Width")]
        protected float m_Width;
        [XDaySerializableField(1, "Height")]
        protected float m_Height;
        [XDaySerializableField(1, "Plugins")]
        protected List<WorldPlugin> m_Plugins;
        [XDaySerializableField(1, "LOD System")]
        protected IWorldLODSystem m_LODSystem;
    }
}


//XDay