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

namespace XDay.WorldAPI.Tile
{
    internal class BakedTileLayer : TileLayerBase
    {
        public BakedTileLayer(string name, int xTileCount, int yTileCount, float tileWidth, float tileHeight, Vector2 origin, BakedTileData[] tiles)
            : base(name, xTileCount, yTileCount, tileWidth, tileHeight, origin)
        {
            m_Tiles = tiles;
        }

        public override void Init(TileSystem tileSystem)
        {
            base.Init(tileSystem);

            m_Renderer = new BakedTileLayerRenderer(this);
        }

        public override void Uninit()
        {
            m_Renderer?.OnDestroy();
            m_Renderer = null;
        }

        private BakedTileData GetTile(int x, int y)
        {
            if (x >= 0 && x < XTileCount &&
                y >= 0 && y < YTileCount)
            {
                return m_Tiles[y * XTileCount + x];
            }
            return null;
        }

        private void ShowTile(bool visible, int x, int y, BakedTileData tile)
        {
            tile.Visible = visible;
            m_Renderer?.ToggleActivateState(x, y, tile);
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
                        ShowTile(false, x, y, tile);
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
            for (var y = newMin.y; y <= newMax.y; ++y)
            {
                for (var x = newMin.x; x <= newMax.x; ++x)
                {
                    var tile = GetTile(x, y);
                    if (tile != null)
                    {
                        ShowTile(true, x, y, tile);
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
            if (oldBounds != newBounds)
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
                                ShowTile(false, x, y, tile);
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
                                ShowTile(true, x, y, tile);
                            }
                        }
                    }
                }
            }
        }

        public override float GetHeightAtPos(float x, float z)
        {
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

        private readonly BakedTileData[] m_Tiles;
        private BakedTileLayerRenderer m_Renderer;
    }
}

