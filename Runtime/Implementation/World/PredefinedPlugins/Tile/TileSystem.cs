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

namespace XDay.WorldAPI.Tile
{
    [Preserve]
    internal partial class TileSystem : WorldPlugin, ITileSystem
    {
        public override List<string> GameFileNames => new() { "tile" };
        public override string TypeName => "TileSystem";
        public override IPluginLODSystem LODSystem => m_LODSystem;
        public int XTileCount => m_XTileCount;
        public int YTileCount => m_YTileCount;
        public Vector3 Center => m_Center;
        public float TileWidth => m_TileWidth;
        public float TileHeight => m_TileHeight;
        public override Quaternion Rotation { get => m_Rotation; set => throw new System.NotImplementedException(); }
        public override string Name { get => m_Name; set => throw new System.NotImplementedException(); }

        public TileSystem()
        {
        }

        protected override void InitInternal()
        {
            m_Size.x = m_XTileCount * m_TileWidth;
            m_Size.y = m_YTileCount * m_TileHeight;
            m_Center = new(m_Origin.x + m_Size.x * 0.5f, 0, m_Origin.y + m_Size.y * 0.5f);
            m_CameraVisibleAreaUpdater = ICameraVisibleAreaUpdater.Create(World.CameraVisibleAreaCalculator);

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

            m_Renderer = new TileSystemRenderer(this, m_UsedTilePrefabPaths);
            m_UsedTilePrefabPaths = null;
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

        public Vector3 CoordinateToLocalPosition(int x, int y)
        {
            return new Vector3(
                x * m_TileWidth - m_Size.x * 0.5f,
                0,
                y * m_TileHeight - m_Size.y * 0.5f);
        }

        public Vector2Int WorldPositionToCoordinate(float x, float z)
        {
            var pos = WorldPositionToLocal(x, z);
            return new Vector2Int(
                Mathf.FloorToInt((pos.x - m_Origin.x) / m_TileWidth),
                Mathf.FloorToInt((pos.z - m_Origin.y) / m_TileHeight));
        }

        protected RectInt CalculateCoordinateBounds(Rect rect)
        {
            var min = WorldPositionToCoordinate(rect.xMin, rect.yMin);
            var max = WorldPositionToCoordinate(rect.xMax, rect.yMax);
            return new RectInt(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        private Vector3 WorldPositionToLocal(float x, float z)
        {
            var center = Center;
            return Quaternion.Inverse(m_Rotation) * new Vector3(x - center.x, 0, z - center.z) + new Vector3(center.x, 0, center.z);
        }

        private TileData GetTile(int x, int y)
        {
            if (x >= 0 && x < XTileCount &&
                y >= 0 && y < YTileCount)
            {
                return m_Tiles[y * XTileCount + x];
            }
            return null;
        }

        private void ShowTile(bool visible, int x, int y, TileData tile, int lod)
        {
            tile.Visible = visible;
            m_Renderer?.ToggleActivateState(x, y, tile, lod);
        }

        protected override void LoadGameDataInternal(string pluginName, IWorld world)
        {
            var reader = world.QueryGameDataDeserializer(world.ID, $"tile@{pluginName}");

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

            m_Tiles = new TileData[m_YTileCount * m_XTileCount];
            for (var y = 0; y < m_YTileCount; ++y)
            {
                for (var x = 0; x < m_XTileCount; ++x)
                {
                    var path = reader.ReadString("");
                    bool hasHeight = reader.ReadBoolean("Has Height");
                    bool exportHeight = reader.ReadBoolean("Export Height");
                    float[] vertexHeights = null;
                    if (exportHeight)
                    {
                        vertexHeights = reader.ReadSingleArray("Vertex Heights");
                    }
                    if (!string.IsNullOrEmpty(path))
                    {
                        var index = y * m_XTileCount + x;
                        m_Tiles[index] = new TileData(path, vertexHeights, hasHeight);
                    }
                }
            }

            m_UsedTilePrefabPaths = reader.ReadStringArray("Used Tile Prefab Paths");

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

        public Vector2Int UnrotatedPositionToCoordinate(float x, float z)
        {
            return new Vector2Int(Mathf.FloorToInt(x / m_TileWidth), Mathf.FloorToInt(z / m_TileHeight));
        }

        public Vector3 CoordinateToUnrotatedPosition(int x, int y)
        {
            return new Vector3(x * m_TileWidth, 0, y * m_TileHeight);
        }

        public Vector2Int RotatedPositionToCoordinate(float x, float z)
        {
            var center = Center;
            var unrotatedPos = Quaternion.Inverse(m_Rotation) * new Vector3(x - center.x, 0, z - center.z) + new Vector3(center.x, 0, center.z);
            return new Vector2Int(Mathf.FloorToInt((unrotatedPos.x - m_Origin.x) / m_TileWidth), Mathf.FloorToInt((unrotatedPos.z - m_Origin.y) / m_TileHeight));
        }

        private TileData[] m_Tiles;
        private TileSystemRenderer m_Renderer;
        private IResourceDescriptorSystem m_DescriptorSystem;
        private IPluginLODSystem m_LODSystem;
        private Vector2 m_Size;
        private int m_XTileCount;
        private int m_YTileCount;
        private Vector3 m_Center;
        private float m_TileWidth;
        private float m_TileHeight;
        private Quaternion m_Rotation;
        private string m_Name;
        private ICameraVisibleAreaUpdater m_CameraVisibleAreaUpdater;
        private Vector2 m_Origin;
        private string[] m_UsedTilePrefabPaths;
    }
}

//XDay