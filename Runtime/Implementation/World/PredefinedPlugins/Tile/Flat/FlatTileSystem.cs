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
using UnityEngine.Scripting;

namespace XDay.WorldAPI.Tile
{
    [Preserve]
    internal partial class FlatTileSystem: TileSystemBase
    {
        public override List<string> GameFileNames => new() { "flat_tile" };
        public override string TypeName => "FlatTileSystem";

        public FlatTileSystem()
        {
        }

        protected override void InitInternal()
        {
            base.InitInternal();

            m_DescriptorSystem.Init(World);

            foreach (var tile in m_Tiles)
            {
                tile?.Init(m_DescriptorSystem);
            }

            m_Size.x = m_XTileCount * m_TileWidth;
            m_Size.y = m_YTileCount * m_TileHeight;
            InitRendererInternal();
        }

        protected override void UninitInternal()
        {
            m_DescriptorSystem.Uninit();
            m_Renderer?.OnDestroy();
        }

        protected override void InitRendererInternal()
        {
            m_CameraVisibleAreaUpdater.Reset();
            m_Renderer = new FlatTileSystemRenderer(this);
        }

        protected override void UninitRendererInternal()
        {
            m_Renderer?.OnDestroy();
            m_Renderer = null;
        }

        protected override void UpdateInternal()
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
                UpdateVisibleArea();
            }

            m_CameraVisibleAreaUpdater.EndUpdate();
        }

        private FlatTileData GetTile(int x, int y)
        {
            if (x >= 0 && x < XTileCount &&
                y >= 0 && y < YTileCount)
            {
                return m_Tiles[y * XTileCount + x];
            }
            return null;
        }

        private void ShowTile(bool visible, int x, int y, FlatTileData tile, int lod)
        {
            tile.Visible = visible;
            m_Renderer?.ToggleActivateState(x, y, tile, lod);
        }

        protected override void LoadGameDataInternal(string pluginName, IWorld world)
        {
            var reader = world.QueryGameDataDeserializer(world.ID, $"flat_tile@{pluginName}");

            reader.ReadInt32("FlatTile.Version");

            m_DescriptorSystem = reader.ReadSerializable<IResourceDescriptorSystem>("Resource Descriptor System", true);
            m_LODSystem = reader.ReadSerializable<IPluginLODSystem>("Plugin LOD System", true);
            m_Rotation = reader.ReadQuaternion("Rotation");
            m_XTileCount = reader.ReadInt32("X Tile Count");
            m_YTileCount = reader.ReadInt32("Y Tile Count");
            m_Origin = reader.ReadVector2("Origin");
            m_TileWidth = reader.ReadSingle("Tile Width");
            m_TileHeight = reader.ReadSingle("Tile Height");
            m_Name = reader.ReadString("Name");

            m_Tiles = new FlatTileData[m_YTileCount * m_XTileCount];
            for (var y = 0; y < m_YTileCount; ++y)
            {
                for (var x = 0; x < m_XTileCount; ++x)
                {
                    var path = reader.ReadString("");
                    if (!string.IsNullOrEmpty(path))
                    {
                        var index = y * m_XTileCount + x;
                        m_Tiles[index] = new FlatTileData(path);
                    }
                }
            }

            reader.Uninit();
        }

        private void UpdateLOD()
        {
            var oldRect = CalculateCoordinateBounds(m_CameraVisibleAreaUpdater.PreviousArea);
            var newRect = CalculateCoordinateBounds(m_CameraVisibleAreaUpdater.CurrentArea);
            var newMin = newRect.min;
            var newMax = newRect.max;
            var oldMin = oldRect.min;
            var oldMax = oldRect.max;

            for (var y = oldMin.y; y <= oldMax.y; ++y)
            {
                for (var x = oldMin.x; x <= oldMax.x; ++x)
                {
                    var tile = GetTile(x, y);
                    if (tile != null)
                    {
                        ShowTile(false, x, y, tile, LODSystem.PreviousLOD);
                    }
                }
            }

            for (var y = newMin.y; y <= newMax.y; ++y)
            {
                for (var x = newMin.x; x <= newMax.x; ++x)
                {
                    var tile = GetTile(x, y);
                    if (tile != null)
                    {
                        ShowTile(true, x, y, tile, LODSystem.CurrentLOD);
                    }
                }
            }
        }

        private void UpdateVisibleArea()
        {
            var oldBounds = CalculateCoordinateBounds(m_CameraVisibleAreaUpdater.PreviousArea);
            var newBounds = CalculateCoordinateBounds(m_CameraVisibleAreaUpdater.CurrentArea);

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
                                ShowTile(false, x, y, tile, LODSystem.CurrentLOD);
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
                                ShowTile(true, x, y, tile, LODSystem.CurrentLOD);
                            }
                        }
                    }
                }
            }
        }

        private FlatTileData[] m_Tiles;
        private FlatTileSystemRenderer m_Renderer;
        private IResourceDescriptorSystem m_DescriptorSystem;
    }
}

//XDay