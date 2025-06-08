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
using UnityEngine;
using XDay.AssetAPI;

namespace XDay.WorldAPI
{
    public delegate void CameraAltitudeChangeCallback(float oldAltitude, float newAltitude);

    internal partial class GameWorld : World
    {
        public event LODChangeCallback EventLODChanged;
        public event CameraAltitudeChangeCallback EventCameraAltitudeChanged;
        public override string TypeName => "GameWorld";
        public override int CurrentLOD => m_PluginLODSystem.CurrentLOD;

        public GameWorld()
        {
            m_PluginLODSystem = new();
        }

        public GameWorld(IWorldManager worldManager, WorldSetup setup, IAssetLoader assetLoader, ICameraManipulator manipulator, ISerializableFactory serializableFactory, WorldPluginLoader pluginLoader, float width = 0, float height = 0)
            : base(worldManager, setup, new SLGCameraVisibleAreaCalculator(), assetLoader, manipulator, serializableFactory, width, height)
        {
            m_PluginLoader = pluginLoader;
        }

        public override void Init()
        {
            base.Init();

            if (m_PluginLODSystem == null)
            {
                m_PluginLODSystem = new(WorldLODSystem.LODCount);
            }
            m_PluginLODSystem.Init(WorldLODSystem);

            InitRendererInternal();
        }

        protected override void InitRendererInternal()
        {
            var debugger = m_WorldRenderer.Root.AddComponent<WorldDebugger>();
            debugger.Init(this);
        }

        public override IDeserializer QueryGameDataDeserializer(int worldID, string dataFileName)
        {
            return m_PluginLoader.GetPluginDeserializer(worldID, dataFileName);
        }

        public void GameDeserialize(IDeserializer deserializer, string label)
        {
            deserializer.ReadInt32("World.Version");

            m_LODSystem = deserializer.ReadSerializable<WorldLODSystem>("LOD System", true);
            m_Width = deserializer.ReadSingle("Width");
            m_Height = deserializer.ReadSingle("Height");
            m_Plugins = m_PluginLoader.LoadPlugins(this);
        }

        public override void Update(float dt)
        {
            if (Inited)
            {
                if (m_Manipulator != null)
                {
                    CameraVisibleAreaCalculator.Update(m_Manipulator.Camera);
                }

                foreach (var plugin in m_Plugins)
                {
                    plugin.Update(dt);
                }

                if (m_Manipulator != null)
                {
                    var cameraPos = m_Manipulator.RenderPosition;

                    if (m_PluginLODSystem.Update(cameraPos.y))
                    {
                        EventLODChanged?.Invoke(m_PluginLODSystem.PreviousLOD, m_PluginLODSystem.CurrentLOD);
                    }

                    if (!Mathf.Approximately(m_LastCheckCameraAltitude, cameraPos.y))
                    {
                        if (m_LastCheckCameraAltitude != 0)
                        {
                            EventCameraAltitudeChanged?.Invoke(m_LastCheckCameraAltitude, cameraPos.y);
                        }

                        m_LastCheckCameraAltitude = cameraPos.y;
                    }
                }
            }
        }

        public void LoadGame()
        {
            OnDestroy();

            LoadData();

            Init();
        }

        public bool LoadData()
        {
            var deserializer = m_PluginLoader.GetMainDeserializer(ID);
            if (deserializer != null)
            {
                GameDeserialize(deserializer, "");

                deserializer.Uninit();
                return true;
            }

            Debug.Assert(false, "load world data failed!");
            return false;
        }

        public override void RegisterLODChangeEvent(LODChangeCallback callback)
        {
            m_PluginLODSystem.EventLODChanged += callback;
        }

        public override void UnregisterLODChangeEvent(LODChangeCallback callback)
        {
            m_PluginLODSystem.EventLODChanged -= callback;
        }

        private PluginLODSystem m_PluginLODSystem;
        private WorldPluginLoader m_PluginLoader;
        private float m_LastCheckCameraAltitude;
    }
}


//XDay