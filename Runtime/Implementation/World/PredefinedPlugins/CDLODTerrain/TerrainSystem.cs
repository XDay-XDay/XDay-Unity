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

namespace XDay.WorldAPI.CDLODTerrain
{
    /// <summary>
    /// quadtree node的深度和lod是相反的,node深度为0代表最高级的lod,即顶点最少的一级
    /// 假设height map精度和地形大小是1:1的，即一个像素代表1米
    /// </summary>
    [Preserve]
    public partial class TerrainSystem : WorldPlugin
    {
        public override List<string> GameFileNames => new() { "terrain_lod_tile" };
        public int HeightMapWidth => m_HeightMapWidth;
        public int HeightMapHeight => m_HeightMapHeight;
        public float[] HeightMapData => m_HeightMapData;
        public float[] MorphStartDistanceAtNodeDepth => m_MorphStartDistanceAtNodeDepth;
        public float[] MorphEndDistanceAtNodeDepth => m_MorphEndDistanceAtNodeDepth;
        public float[] DistanceAtNodeDepth => m_LODDistanceAtNodeDepth;
        public int LeafNodeSize => m_LeafNodeSize;
        internal TerrainSystemRenderer Renderer => m_Renderer;
        public override IPluginLODSystem LODSystem => m_LODSystem;
        public int XTileCount => m_XTileCount;
        public int YTileCount => m_YTileCount;
        public Vector3 Center => m_Center;
        public float TileWidth => m_TileWidth;
        public float TileHeight => m_TileHeight;
        public int LODCount => m_LODSystem.LODCount;
        public override Quaternion Rotation { get => m_Rotation; set => throw new System.NotImplementedException(); }
        public override string Name { get => m_Name; set => throw new System.NotImplementedException(); }
        public override Bounds Bounds => new(Center, new Vector3(m_Size.x, 0, m_Size.y));
        public override string TypeName => "CDLODTerrainSystem";

        public TerrainSystem()
        {
        }

        protected override void InitInternal()
        {
            m_Size.x = m_XTileCount * m_TileWidth;
            m_Size.y = m_YTileCount * m_TileHeight;
            m_Center = new(m_Origin.x + m_Size.x * 0.5f, 0, m_Origin.y + m_Size.y * 0.5f);
            m_CameraVisibleAreaUpdater = ICameraVisibleAreaUpdater.Create(World);

            Debug.Assert(Mathf.IsPowerOfTwo((int)m_TileWidth));

            m_LeafNodeSize = Mathf.CeilToInt(m_TileWidth / Mathf.Pow(2, LODCount - 1));
            m_MinLODViewDistance = m_LeafNodeSize;

            CalculateLODDistance();

            m_ResourceDescriptorSystem.Init(World);

            foreach (var tile in m_Tiles)
            {
                tile?.Init(this);
            }

            InitRendererInternal();
        }

        protected override void UninitInternal()
        {
            m_ResourceDescriptorSystem.Uninit();
            m_Renderer?.Uninitialize();
            m_Renderer = null;
        }

        protected override void InitRendererInternal()
        {
            m_LastUpdateCameraPosition = Vector3.zero;
            m_Renderer = new TerrainSystemRenderer(this);
        }

        protected override void UninitRendererInternal()
        {
            m_Renderer?.Uninitialize();
            m_Renderer = null;
        }

        protected override void UpdateInternal(float dt)
        {
            CullTiles();

            UpdateLOD(World.CameraManipulator.RenderPosition);
        }

        internal float GetHeight(int x, int y, out bool valid)
        {
            if (x >= 0 && x < m_HeightMapWidth &&
                y >= 0 && y < m_HeightMapHeight)
            {
                valid = true;
                return m_HeightMapData[y * m_HeightMapWidth + x];
            }
            valid = false;
            return 0;
        }

        void CalculateLODDistance()
        {
            var lodCount = m_LODSystem.LODCount;
            m_LODDistanceAtNodeDepth = new float[lodCount];
            m_MorphStartDistanceAtNodeDepth = new float[lodCount];
            m_MorphEndDistanceAtNodeDepth = new float[lodCount];

            float start = 0;
            for (var i = 0; i < lodCount; ++i)
            {
                m_LODDistanceAtNodeDepth[lodCount - 1 - i] = start + Mathf.Pow(2, i) * m_MinLODViewDistance;
                start = m_LODDistanceAtNodeDepth[lodCount - 1 - i];
            }

            float prevPos = 0;
            for (var i = 0; i < lodCount; i++)
            {
                var index = lodCount - i - 1;
                m_MorphEndDistanceAtNodeDepth[index] = m_LODDistanceAtNodeDepth[index];
                m_MorphStartDistanceAtNodeDepth[index] = prevPos + (m_MorphEndDistanceAtNodeDepth[index] - prevPos) * m_MorphRatio;

                prevPos = m_MorphStartDistanceAtNodeDepth[index];
            }
        }

        void UpdateLOD(Vector3 cameraPosition)
        {
            var lodSelectPosition = World.CameraManipulator.FocusPoint;
            lodSelectPosition.y = cameraPosition.y;

            if (cameraPosition != m_LastUpdateCameraPosition)
            {
                m_LastUpdateCameraPosition = cameraPosition;

                m_Renderer?.UpdateLOD(lodSelectPosition, m_CameraVisibleAreaUpdater.CurrentArea);
            }
        }

        //TODO: optimize this
        void GetXYWidthHeight(int rootWidth, int rootHeight, int code, int depth, out int x, out int y, out int width, out int height)
        {
            x = 0;
            y = 0;
            width = rootWidth / (int)Mathf.Pow(2, depth);
            height = rootHeight / (int)Mathf.Pow(2, depth);
            var k = (int)Mathf.Pow(10, depth);
            depth += 1;
            for (var i = 0; i < depth; ++i)
            {
                var bit = code / k;
                if (bit == 1)
                {
                    y += rootHeight;
                }
                else if (bit == 2)
                {
                    x += rootWidth;
                    y += rootHeight;
                }
                else if (bit == 3)
                {
                    x += rootWidth;
                }
                else if (bit != 0)
                {
                    Debug.Assert(false);
                }
                code %= k;
                k /= 10;
                rootWidth /= 2;
                rootHeight /= 2;
            }
        }

        internal void GetNodeBounds(QuadTreeNode node, int offsetX, int offsetY, out Vector3 nodeBoundsMin, out Vector3 nodeBoundsMax)
        {
            GetXYWidthHeight((int)TileWidth, (int)TileHeight, node.Code, node.Depth, out var nodeX, out var nodeY, out var nodeWidth, out var nodeHeight);

            nodeX += offsetX;
            nodeY += offsetY;

            if (!(nodeX == node.X &&
                nodeY == node.Y &&
                nodeWidth == node.Width &&
                nodeHeight == node.Height))
            {
                Debug.Assert(false);
            }

            var x = m_Origin.x + nodeX * (m_Size.x / m_HeightMapWidth);
            var z = m_Origin.y + nodeY * (m_Size.y / m_HeightMapHeight);
            var width = nodeWidth * (m_Size.x / m_HeightMapWidth);
            var height = nodeHeight * (m_Size.y / m_HeightMapHeight);
            nodeBoundsMin = new Vector3(x, node.MinHeight, z);
            nodeBoundsMax = new Vector3(x + width, node.MaxHeight, z + height);
        }

        void CullTiles()
        {
            var cameraPos = World.CameraManipulator.RenderPosition;

            var viewportChanged = m_CameraVisibleAreaUpdater.BeginUpdate();
            /*var lodChanged = */m_LODSystem.Update(cameraPos.y);
            if (viewportChanged)
            {
                var oldBounds = CalculateCoordinateBounds(m_CameraVisibleAreaUpdater.PreviousArea);
                var newBounds = CalculateCoordinateBounds(m_CameraVisibleAreaUpdater.CurrentArea);

                if (!oldBounds.Equals(newBounds))
                {
                    var oldMinX = oldBounds.xMin;
                    var oldMinY = oldBounds.yMin;
                    var oldMaxX = oldBounds.xMax;
                    var oldMaxY = oldBounds.yMax;
                    var newMinX = newBounds.xMin;
                    var newMinY = newBounds.yMin;
                    var newMaxX = newBounds.xMax;
                    var newMaxY = newBounds.yMax;

                    for (var i = oldMinY; i <= oldMaxY; ++i)
                    {
                        for (var j = oldMinX; j <= oldMaxX; ++j)
                        {
                            if (!(i >= newMinY && i <= newMaxY &&
                                j >= newMinX && j <= newMaxX))
                            {
                                //隐藏出视野的tile
                                var tileData = GetTile(j, i);
                                if (tileData != null)
                                {
                                    tileData.IsVisible = false;
                                }
                            }
                        }
                    }

                    for (var i = newMinY; i <= newMaxY; ++i)
                    {
                        for (var j = newMinX; j <= newMaxX; ++j)
                        {
                            if (!(i >= oldMinY && i <= oldMaxY &&
                                j >= oldMinX && j <= oldMaxX))
                            {
                                //显示新加入视野的tile
                                var tile = GetTile(j, i);
                                if (tile != null)
                                {
                                    tile.IsVisible = true;
                                }
                            }
                        }
                    }
                }
            }

            m_CameraVisibleAreaUpdater.EndUpdate();
        }

        internal Tile GetTile(int x, int y)
        {
            if (x >= 0 && x < m_XTileCount &&
                y >= 0 && y < m_YTileCount)
            {
                return m_Tiles[y * m_XTileCount + x];
            }
            return null;
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

        public Vector3 CoordinateToWorldPosition(int x, int y)
        {
            return LocalToWorld(x * m_TileWidth, y * m_TileHeight);
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

        public Vector3 LocalToWorld(float x, float z)
        {
            var worldPos = m_Rotation * (new Vector3(x, 0, z) - Center) + Center;
            return worldPos;
        }

        protected override void LoadGameDataInternal(string pluginName, IWorld world)
        {
            var reader = world.QueryGameDataDeserializer(world.ID, $"terrain_lod_tile@{pluginName}");

            reader.ReadInt32("TerrainLODTile.Version");

            m_HeightMapWidth = reader.ReadInt32("Height Map Width");
            m_HeightMapHeight = reader.ReadInt32("Height Map Height");
            m_HeightMapData = reader.ReadSingleArray("Heights");
            m_ResourceDescriptorSystem = reader.ReadSerializable<ResourceDescriptorSystem>("Resource Descriptor System", true);
            m_LODSystem = reader.ReadSerializable<IPluginLODSystem>("Plugin LOD System", true);
            m_MorphRatio = reader.ReadSingle("LOD Morph Ratio");
            m_Rotation = reader.ReadQuaternion("Rotation");
            m_XTileCount = reader.ReadInt32("X Tile Count");
            m_YTileCount = reader.ReadInt32("Y Tile Count");
            m_Origin = reader.ReadVector2("Origin");
            m_TileWidth = reader.ReadSingle("Tile Width");
            m_TileHeight = reader.ReadSingle("Tile Height");
            m_Name = reader.ReadString("Name");

            m_Tiles = new Tile[m_YTileCount * m_XTileCount];
            for (var y = 0; y < m_YTileCount; ++y)
            {
                for (var x = 0; x < m_XTileCount; ++x)
                {
                    var materialPath = reader.ReadString("");
                    if (!string.IsNullOrEmpty(materialPath))
                    {
                        var index = y * m_XTileCount + x;
                        m_Tiles[index] = new Tile(materialPath, x, y, index);
                    }
                }
            }

            reader.Uninit();
        }

        private int m_HeightMapWidth;
        private int m_HeightMapHeight;
        private int m_LeafNodeSize;
        private float m_MinLODViewDistance;
        private float[] m_HeightMapData;
        private float[] m_LODDistanceAtNodeDepth;
        //每个Node深度下morph开始和结束的范围
        private float[] m_MorphStartDistanceAtNodeDepth;
        private float[] m_MorphEndDistanceAtNodeDepth;
        private float m_MorphRatio;
        private Tile[] m_Tiles;
        private Vector3 m_LastUpdateCameraPosition;
        private ResourceDescriptorSystem m_ResourceDescriptorSystem;
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
        private TerrainSystemRenderer m_Renderer;
    }
}

//Done
