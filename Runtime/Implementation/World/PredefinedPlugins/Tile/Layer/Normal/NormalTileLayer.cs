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
    internal class NormalTileLayer : TileLayerBase
    {
        public NormalTileLayer(string name, 
            int xTileCount, 
            int yTileCount, 
            float tileWidth, 
            float tileHeight, 
            Vector2 origin, 
            NormalTileData[] tiles, 
            string[] usedTilePrefabPaths)
            : base(name, xTileCount, yTileCount, tileWidth, tileHeight, origin)
        {
            m_Tiles = tiles;
            m_UsedTilePrefabPaths = usedTilePrefabPaths;
        }

        public override void Init(TileSystem tileSystem)
        {
            base.Init(tileSystem);

            foreach (var tile in m_Tiles)
            {
                tile?.Init(tileSystem.ResourceDescriptorSystem);
            }

            m_Renderer = new NormalTileLayerRenderer(this, m_UsedTilePrefabPaths);
            m_UsedTilePrefabPaths = null;
        }

        public override void Uninit()
        {
            m_Renderer?.OnDestroy();
            m_Renderer = null;
        }

        private NormalTileData GetTile(int x, int y)
        {
            if (x >= 0 && x < XTileCount &&
                y >= 0 && y < YTileCount)
            {
                return m_Tiles[y * XTileCount + x];
            }
            return null;
        }

        private void ShowTile(bool visible, int x, int y, NormalTileData tile, int lod)
        {
            tile.Visible = visible;
            m_Renderer?.ToggleActivateState(x, y, tile, lod);
        }

        public override void Hide()
        {
            var areaUpdater = m_TileSystem.CameraVisibleAreaUpdater;
            var oldRect = CalculateCoordinateBounds(areaUpdater.PreviousArea);
            var oldMin = oldRect.min;
            var oldMax = oldRect.max;
            var lodSystem = m_TileSystem.LODSystem;
            for (var y = oldMin.y; y <= oldMax.y; ++y)
            {
                for (var x = oldMin.x; x <= oldMax.x; ++x)
                {
                    var tile = GetTile(x, y);
                    if (tile != null)
                    {
                        ShowTile(false, x, y, tile, lodSystem.PreviousLOD);
                    }
                }
            }
        }

        public override void Show()
        {
            var areaUpdater = m_TileSystem.CameraVisibleAreaUpdater;
            var newRect = CalculateCoordinateBounds(areaUpdater.CurrentArea);
            var newMin = newRect.min;
            var newMax = newRect.max;
            var lodSystem = m_TileSystem.LODSystem;
            for (var y = newMin.y; y <= newMax.y; ++y)
            {
                for (var x = newMin.x; x <= newMax.x; ++x)
                {
                    var tile = GetTile(x, y);
                    if (tile != null)
                    {
                        ShowTile(true, x, y, tile, lodSystem.CurrentLOD);
                    }
                }
            }
        }

        public override void Update()
        {
            var areaUpdater = m_TileSystem.CameraVisibleAreaUpdater;
            var oldBounds = CalculateCoordinateBounds(areaUpdater.PreviousArea);
            var newBounds = CalculateCoordinateBounds(areaUpdater.CurrentArea);

            var lodSystem = m_TileSystem.LODSystem;
            if (!Helper.CompareEqual(oldBounds, newBounds))
            {
                var oldMin = oldBounds.min;
                var oldMax = oldBounds.max;
                var newMin = newBounds.min;
                var newMax = newBounds.max;

                for (var y = oldMin.y; y <= oldMax.y; ++y)
                {
                    for (var x = oldMin.x; x <= oldMax.x; ++x)
                    {
                        if (y < newMin.y || y > newMax.y ||
                            x < newMin.x || x > newMax.x)
                        {
                            var tile = GetTile(x, y);
                            if (tile != null)
                            {
                                ShowTile(false, x, y, tile, lodSystem.CurrentLOD);
                            }
                        }
                    }
                }

                for (var y = newMin.y; y <= newMax.y; ++y)
                {
                    for (var x = newMin.x; x <= newMax.x; ++x)
                    {
                        if (y < oldMin.y || y > oldMax.y ||
                            x < oldMin.x || x > oldMax.x)
                        {
                            var tile = GetTile(x, y);
                            if (tile != null)
                            {
                                ShowTile(true, x, y, tile, lodSystem.CurrentLOD);
                            }
                        }
                    }
                }
            }
        }

        public override float GetHeightAtPos(float x, float z)
        {
            var coord = RotatedPositionToCoordinate(x, z);
            var tile = GetTile(coord.x, coord.y);
            if (tile != null)
            {
                if (tile.VertexHeights == null)
                {
                    return 0;
                }

                float localX = coord.x * m_TileWidth + m_Origin.x;
                float localZ = coord.y * m_TileHeight + m_Origin.y;
                float gridSize = m_TileWidth / tile.MeshResolution;
                return tile.GetHeightAtPos(x - localX, z - localZ, gridSize);
            }

            return 0;
        }

        internal override void UpdateMaterialInRange(float minX, float minZ, float maxX, float maxZ, TileMaterialUpdaterTiming timing)
        {
            var minCoord = WorldPositionToCoordinate(minX, minZ);
            var maxCoord = WorldPositionToCoordinate(maxX, maxZ);

            m_Renderer.UpdateMaterialInRange(minCoord.x - 1, minCoord.y - 1, maxCoord.x + 1, maxCoord.y + 1, timing);
        }

        protected override void OnSetTileMaterialUpdater()
        {
            m_Renderer.OnSetTileMaterialUpdater();
        }

        private NormalTileData[] m_Tiles;
        private NormalTileLayerRenderer m_Renderer;
        private string[] m_UsedTilePrefabPaths;
    }
}

//XDay