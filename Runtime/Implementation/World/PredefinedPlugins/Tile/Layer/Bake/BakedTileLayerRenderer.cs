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
    internal class BakedTileLayerRenderer
    {
        public GameObject Root => m_Root;

        public BakedTileLayerRenderer(BakedTileLayer layer)
        {
            var tileSystem = layer.TileSystem;
            m_GameObjects = new GameObject[layer.YTileCount * layer.XTileCount];
            m_Root = new GameObject(layer.Name);
            m_Root.transform.SetParent(tileSystem.Root.transform, false);
            m_Root.transform.SetPositionAndRotation(layer.Center, tileSystem.Rotation);
            m_Layer = layer;
        }

        public void OnDestroy()
        {
            foreach (var obj in m_GameObjects)
            {
                Helper.DestroyUnityObject(obj);
            }
            Helper.DestroyUnityObject(m_Root);
        }

        public void ToggleActivateState(int x, int y, BakedTileData tile)
        {
            if (tile.Visible)
            {
                ShowTile(x, y, tile);
            }
            else
            {
                HideTile(x, y, tile);
            }
        }

        private void HideTile(int x, int y, BakedTileData tile)
        {
            var idx = y * m_Layer.XTileCount + x;
            var gameObject = m_GameObjects[idx];
            if (gameObject == null)
            {
                return;
            }

            m_Layer.TileSystem.World.GameObjectPool.Release(tile.Path, gameObject);
            m_GameObjects[idx] = null;
        }

        private void ShowTile(int x, int y, BakedTileData tile)
        {
            var idx = y * m_Layer.XTileCount + x;
            if (m_GameObjects[idx] != null)
            {
                return;
            }

            var gameObject = m_Layer.TileSystem.World.GameObjectPool.Get(tile.Path);
            gameObject.transform.position = m_Layer.CoordinateToLocalPosition(x, y);
            gameObject.transform.SetParent(m_Root.transform, false);
            m_GameObjects[idx] = gameObject;
        }

        private readonly GameObject m_Root;
        private readonly GameObject[] m_GameObjects;
        private readonly BakedTileLayer m_Layer;
    };
}

//XDay