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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Tile
{
    [Preserve]
    internal partial class TileSystem : WorldPlugin, ITileSystem
    {
        public override List<string> GameFileNames => new() { "tile" };
        public override string TypeName => "TileSystem";
        public override IPluginLODSystem LODSystem => m_LODSystem;
        public override Quaternion Rotation { get => m_Rotation; set => throw new System.NotImplementedException(); }
        public override string Name { get => m_Name; set => throw new System.NotImplementedException(); }
        public IResourceDescriptorSystem ResourceDescriptorSystem => m_DescriptorSystem;
        public ICameraVisibleAreaUpdater CameraVisibleAreaUpdater => m_CameraVisibleAreaUpdater;
        public GameObject Root => m_Root;
        public MaskTextureManager MaskTextureManager => m_MaskTextureManager;

        public TileSystem()
        {
        }

        protected override void InitInternal()
        {
            m_Root = new GameObject(m_Name);
            m_Root.transform.SetParent(World.Root.transform, true);

            m_CameraVisibleAreaUpdater = ICameraVisibleAreaUpdater.Create(World.CameraVisibleAreaCalculator);

            m_DescriptorSystem.Init(World);

            foreach (var layer in m_Layers)
            {
                layer.Init(this);
            }

            m_CameraVisibleAreaUpdater.Reset();

            m_CurrentLayer = m_Layers[0];
        }

        protected override void UninitInternal()
        {
            m_DescriptorSystem.Uninit();
            foreach (var layer in m_Layers)
            {
                layer.Uninit();
            }

            Helper.DestroyUnityObject(m_Root);
            m_Root = null;

            m_MaskTextureManager?.OnDestroy();
        }

        protected override void InitRendererInternal()
        {
            throw new System.NotImplementedException();
        }

        protected override void UninitRendererInternal()
        {
            throw new System.NotImplementedException();
        }

        protected override void UpdateInternal(float dt)
        {
            var cameraPos = World.CameraManipulator.RenderPosition;

            var viewportChanged = m_CameraVisibleAreaUpdater.BeginUpdate();
            var lodChanged = LODSystem.Update(cameraPos.y);
            if (lodChanged)
            {
                UpdateLOD();
            }
            else if (viewportChanged)
            {
                m_CurrentLayer.Update();
            }

            m_CameraVisibleAreaUpdater.EndUpdate();

            m_MaskTextureManager?.Update();
        }

        protected override void LoadGameDataInternal(string pluginName, IWorld world)
        {
            var reader = world.QueryGameDataDeserializer(world.ID, $"tile@{pluginName}");

            var version = reader.ReadInt32("FlatTile.Version");

            m_DescriptorSystem = reader.ReadSerializable<IResourceDescriptorSystem>("Resource Descriptor System", true);
            m_LODSystem = reader.ReadSerializable<IPluginLODSystem>("Plugin LOD System", true);
            m_Rotation = reader.ReadQuaternion("Rotation");

            LoadNormalLayer(reader);
            LoadBakedLayers(world);

            reader.Uninit();
        }

        private void LoadBakedLayers(IWorld world)
        {
            var reader = world.QueryGameDataDeserializer(world.ID, WorldDefine.BAKED_TILES_FILE_NAME);
            if (reader == null)
            {
                return;
            }

            reader.ReadInt32("Version");

            List<string> usedPrefabs = reader.ReadStringList("Use Prefabs");
            var bounds = reader.ReadRect("Bounds");
            var lodCount = reader.ReadInt32("Baked Tiles Count");
            for (int i = 0; i < lodCount; ++i)
            {
                int yTileCount = reader.ReadInt32("Rows");
                int xTileCount = reader.ReadInt32("Cols");
                float lodHeight = reader.ReadSingle("LOD Height");
                BakedTileData[] tiles = new BakedTileData[xTileCount * yTileCount];
                var idx = 0;
                for (int r = 0; r < yTileCount; ++r)
                {
                    for (int c = 0; c < xTileCount; ++c)
                    {
                        var prefabIndex = reader.ReadInt32("Prefab Index");
                        if (prefabIndex < 0)
                        {
                            tiles[idx] = null;
                        }
                        else
                        {
                            tiles[idx] = new BakedTileData(usedPrefabs[prefabIndex]);
                        }
                        ++idx;
                    }
                }

                var layer = new BakedTileLayer($"Baked Layer {i}", xTileCount, yTileCount, bounds.width / xTileCount, bounds.height / yTileCount, bounds.min, tiles);
                m_Layers.Add(layer);

                m_LODSystem.AddLOD($"Baked LOD {i}", lodHeight, 0);
            }
        }

        private void LoadNormalLayer(IDeserializer reader)
        {
            var xTileCount = reader.ReadInt32("X Tile Count");
            var yTileCount = reader.ReadInt32("Y Tile Count");
            var origin = reader.ReadVector2("Origin");
            var tileWidth = reader.ReadSingle("Tile Width");
            var tileHeight = reader.ReadSingle("Tile Height");
            m_Name = reader.ReadString("Name");

            var tiles = new NormalTileData[yTileCount * xTileCount];
            for (var y = 0; y < yTileCount; ++y)
            {
                for (var x = 0; x < xTileCount; ++x)
                {
                    var path = reader.ReadString("");
                    //var maskTexturePath = reader.ReadString("Mask Texture Path");
                    var maskTexturePath = "";
                    bool hasHeight = reader.ReadBoolean("Has Height");
                    bool exportHeight = reader.ReadBoolean("Export Height");
                    float[] vertexHeights = null;
                    if (exportHeight)
                    {
                        vertexHeights = reader.ReadSingleArray("Vertex Heights");
                    }
                    if (!string.IsNullOrEmpty(path))
                    {
                        var index = y * xTileCount + x;
                        tiles[index] = new NormalTileData(path, vertexHeights, hasHeight, maskTexturePath);
                    }
                }
            }

            var usedTilePrefabPaths = reader.ReadStringArray("Used Tile Prefab Paths");
            var layer = new NormalTileLayer("Base Layer", xTileCount, yTileCount, tileWidth, tileHeight, origin, tiles, usedTilePrefabPaths);
            m_Layers.Add(layer);
        }

        private void UpdateLOD()
        {
            var prevLOD = m_LODSystem.PreviousLOD;
            var curLOD = m_LODSystem.CurrentLOD;
            var prevLayer = GetLayer(prevLOD);
            m_CurrentLayer = GetLayer(curLOD);
            if (prevLayer != m_CurrentLayer)
            {
                prevLayer.Hide();
                m_CurrentLayer.Show();
            }
            else
            {
                m_CurrentLayer.Update();
            }
        }

        private TileLayerBase GetLayer(int lod)
        {
            return m_Layers[lod];
        }

        public void SetTileMaterialUpdater(ITileMaterialUpdater updater)
        {
            foreach (var layer in m_Layers)
            {
                layer.SetTileMaterialUpdater(updater);
            }
        }

        public void UpdateMaterialInRange(float minX, float minZ, float maxX, float maxZ, TileMaterialUpdaterTiming timing)
        {
            foreach (var layer in m_Layers)
            {
                layer.UpdateMaterialInRange(minX, minZ, maxX, maxZ, timing);
            }
        }

        public void EnableDynamicMaskLoading(ITextureLoader loader)
        {
            m_MaskTextureManager?.OnDestroy();

            m_MaskTextureManager = new(loader);
        }

        private IResourceDescriptorSystem m_DescriptorSystem;
        private IPluginLODSystem m_LODSystem;
        private Quaternion m_Rotation;
        private string m_Name;
        private ICameraVisibleAreaUpdater m_CameraVisibleAreaUpdater;
        private readonly List<TileLayerBase> m_Layers = new();
        private TileLayerBase m_CurrentLayer;
        private GameObject m_Root;
        private MaskTextureManager m_MaskTextureManager;
    }
}
