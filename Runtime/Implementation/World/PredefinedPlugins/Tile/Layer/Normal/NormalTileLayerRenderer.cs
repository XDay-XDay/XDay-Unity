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

using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Tile
{
    internal class NormalTileLayerRenderer
    {
        public GameObject Root => m_Root;

        public NormalTileLayerRenderer(NormalTileLayer layer, string[] usedTilePrefabPaths)
        {
            var tileSystem = layer.TileSystem;
            m_Entries = new Entry[layer.YTileCount * layer.XTileCount];
            for (var i = 0; i < m_Entries.Length; i++)
            {
                m_Entries[i] = new Entry();
            }

            m_Root = new GameObject(layer.Name);
            m_Root.transform.SetParent(tileSystem.Root.transform, false);
            m_Root.transform.SetPositionAndRotation(layer.Center, tileSystem.Rotation);
            m_Layer = layer;

            m_MeshManager = new TerrainHeightMeshManager(tileSystem.World.GameFolder, tileSystem.World.AssetLoader);

            PreprocessHeightMeshes(usedTilePrefabPaths);
        }

        public void OnDestroy()
        {
            foreach (var entry in m_Entries)
            {
                Helper.DestroyUnityObject(entry.GameObject);
            }
            Helper.DestroyUnityObject(m_Root);
        }

        public void ToggleActivateState(int x, int y, NormalTileData tile, int lod)
        {
            if (tile.Visible)
            {
                ShowTile(x, y, tile, lod);
            }
            else
            {
                HideTile(x, y, tile, lod);
            }
        }

        private void ShowTile(int x, int y, NormalTileData tile, int lod)
        {
            var idx = y * m_Layer.XTileCount + x;

            if (m_Entries[idx].GameObject == null)
            {
                var gameObject = m_Layer.TileSystem.World.GameObjectPool.Get(tile.GetPath(lod));
                gameObject.transform.position = m_Layer.CoordinateToLocalPosition(x, y);
                gameObject.transform.SetParent(m_Root.transform, false);
                m_Entries[idx].GameObject = gameObject;
            }

            if (!tile.MaskSet)
            {
                tile.MaskSet = true;
                m_Entries[idx].Material = m_Entries[idx].GameObject.GetComponentInChildren<MeshRenderer>(true).sharedMaterial;
                m_Layer.TileMaterialUpdater?.OnNormalLayerShowTile(m_Entries[idx].Material);
            }

            m_Layer.TileSystem.MaskTextureManager?.LoadMask(m_Entries[idx].Material, m_Entries[idx].GameObject, tile.MaskTexturePath);

            if (tile.HasHeightData
                /* || tile.clipped*/)
            {
                var lodMesh = m_MeshManager.GetHeightMesh(x, y, lod);
                if (lodMesh != null)
                {
                    var filter = m_Entries[idx].GameObject.GetComponentInChildren<MeshFilter>();
                    filter.sharedMesh = lodMesh;
                }
            }

            m_Entries[idx].GameObject.SetActive(true);
        }

        private void HideTile(int x, int y, NormalTileData tile, int lod)
        {
            var idx = y * m_Layer.XTileCount + x;
            if (m_Entries[idx].GameObject == null)
            {
                return;
            }

            if (tile.HasHeightData)
            {
                var prefabPath = tile.GetPath(lod);
                var filter = m_Entries[idx].GameObject.GetComponentInChildren<MeshFilter>();
                filter.sharedMesh = m_MeshManager.GetOriginalMesh(prefabPath);
            }

            m_Layer.TileSystem.MaskTextureManager?.UnloadMask(m_Entries[idx].Material);

            m_Entries[idx].GameObject.SetActive(false);
        }

        private void PreprocessHeightMeshes(string[] usedPrefabPaths)
        {
            if (usedPrefabPaths != null)
            {
                var assetLoader = m_Layer.TileSystem.World.AssetLoader;
                for (int i = 0; i < usedPrefabPaths.Length; ++i)
                {
                    var obj = assetLoader.LoadGameObject(usedPrefabPaths[i]);
                    if (obj != null)
                    {
                        var filter = obj.GetComponentInChildren<MeshFilter>();
                        if (filter != null)
                        {
                            var mesh = filter.sharedMesh;
                            if (mesh != null)
                            {
                                m_MeshManager.SetOriginalMesh(usedPrefabPaths[i], mesh);
                            }
                        }
                        Object.Destroy(obj);
                    }
                    else
                    {
                        Debug.LogError($"PreprocessHeightMeshes: load {usedPrefabPaths[i]} failed!");
                    }
                }
            }
        }

        internal void UpdateMaterialInRange(int minX, int minY, int maxX, int maxY, TileMaterialUpdaterTiming timing)
        {
            for (var y = minY; y <= maxY; ++y)
            {
                for (var x = minX; x <= maxX; ++x)
                {
                    if (x > 0 && x < m_Layer.XTileCount &&
                        y > 0 && y < m_Layer.YTileCount)
                    {
                        var idx = y * m_Layer.XTileCount + x;
                        if (m_Entries[idx].GameObject != null)
                        {
                            m_Layer.TileMaterialUpdater.OnUpdateNormalLayerMaterial(m_Entries[idx].Material, timing);
                        }
                    }
                }
            }
        }

        internal void OnSetTileMaterialUpdater()
        {
            foreach (var entry in m_Entries)
            {
                if (entry.GameObject != null)
                {
                    entry.Material = entry.GameObject.GetComponentInChildren<MeshRenderer>(true).sharedMaterial;
                    m_Layer.TileMaterialUpdater?.OnNormalLayerShowTile(entry.Material);
                }
            }
        }

        private readonly GameObject m_Root;
        private readonly Entry[] m_Entries;
        private readonly NormalTileLayer m_Layer;
        private readonly TerrainHeightMeshManager m_MeshManager;

        private class Entry
        {
            public GameObject GameObject;
            public Material Material;
        }
    };
}

//XDay