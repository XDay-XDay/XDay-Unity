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
using UnityEngine.Pool;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.CDLODTerrain
{
    internal class TileRenderer
    {
        public bool IsVisible => m_Tile.IsVisible;

        public TileRenderer(Tile tile, Texture2D heightMapTexture, IWorldAssetLoader loader)
        {
            m_Tile = tile;

            m_MaterialPool = new ObjectPool<Material>(
                createFunc: () => { return Object.Instantiate(m_Material); }, 
                actionOnDestroy: (mtl) => { Helper.DestroyUnityObject(mtl); }, defaultCapacity: 4);

            var ground = tile.Terrain;
            m_Material = loader.Load<Material>(tile.MaterialPath);
            m_Material.SetVector("_MapSize", new Vector4(ground.Bounds.size.x, ground.Bounds.size.z, 0, 0));
            m_Material.SetTexture("_HeightMap", heightMapTexture);
            // x = meshGridResolution (32), y = meshGridResolution / 2, z = 2/meshGridResolution
            var leafNodeSize = ground.LeafNodeSize;
            m_Material.SetVector("_MeshParameters", new Vector4(leafNodeSize, leafNodeSize / 2, 2.0f / leafNodeSize, 0));
            var tilePosition = ground.CoordinateToWorldPosition(tile.X, tile.Y);
            m_Material.SetVector("_TileBounds", new Vector4(ground.TileWidth, ground.TileHeight, tilePosition.x, tilePosition.z));
        }

        public void OnDestroy()
        {
            m_Material = null;
            m_MaterialPool.Clear();
            m_MaterialPool = null;
        }

        public void UpdateLOD(Vector3 cameraPosition, Rect viewport)
        {
            m_CurrentViewport = viewport;
            UpdateLOD(m_Tile.QuadTree.RootNode, cameraPosition, m_Tile.Terrain.DistanceAtNodeDepth);
        }

        /// <summary>
        /// 如果需要绘制parent
        /// </summary>
        /// <param name="node"></param>
        /// <param name="cameraPosition"></param>
        /// <param name="depthDistance"></param>
        /// <returns></returns>
        private bool UpdateLOD(QuadTreeNode node, Vector3 cameraPosition, float[] depthDistance)
        {
            var ground = m_Tile.Terrain;
            var quadTree = m_Tile.QuadTree;

            ground.GetNodeBounds(node, quadTree.StartX, quadTree.StartY, out var nodeBoundsMin, out var nodeBoundsMax);

            if (!Helper.RectOverlap(nodeBoundsMin.x, nodeBoundsMin.z, nodeBoundsMax.x, nodeBoundsMax.z, m_CurrentViewport.x, m_CurrentViewport.y, m_CurrentViewport.xMax, m_CurrentViewport.yMax))
            {
                return false;
            }

            var distance = depthDistance[node.Depth];
            if (node.Depth > 0)
            {
                //检测视野范围是否和node的bounding box相交
                var overlapBounds = Helper.SphereAABBIntersectSq(cameraPosition, distance * distance, nodeBoundsMin, nodeBoundsMax);
                if (!overlapBounds)
                {
                    return false;
                }
            }

            if (node.Depth == ground.LODCount - 1)
            {
                //is leaf
                AddRenderInfo(node, true, true, true, true);
                return true;
            }

            //检测子节点是否需要使用新的lod
            var bottomLeftSelected = UpdateLOD(node.BottomLeftChild, cameraPosition, depthDistance);
            var bottomRightSelected = UpdateLOD(node.BottomRightChild, cameraPosition, depthDistance);
            var topLeftSelected = UpdateLOD(node.TopLeftChild, cameraPosition, depthDistance);
            var topRightSelected = UpdateLOD(node.TopRightChild, cameraPosition, depthDistance);
            //如果所有子节点都被选中�?就不绘制父节点了,由子节点自己绘制
            var allSelected = bottomLeftSelected && bottomRightSelected && topLeftSelected && topRightSelected;
            if (!allSelected)
            {
                //selected的节点负责绘制自�?
                AddRenderInfo(node, !bottomLeftSelected, !bottomRightSelected, !topLeftSelected, !topRightSelected);
            }

            return true;
        }

        private void AddRenderInfo(QuadTreeNode node, bool drawBottomLeft, bool drawBottomRight, bool drawTopLeft, bool drawTopRight)
        {
            var ground = m_Tile.Terrain;
            var quadTree = m_Tile.QuadTree;
            var renderer = ground.Renderer;

            ground.GetNodeBounds(node, quadTree.StartX, quadTree.StartY, out var boundsMin, out var boundsMax);

            var scale = boundsMax - boundsMin;
            var drawFullQuad = drawBottomLeft && drawBottomRight && drawTopLeft && drawTopRight;

            if (drawFullQuad)
            {
                var name = $"Renderable_{$"[{node.DebugID}]Full"}_{node.Depth}";
                if (Helper.RectOverlap(boundsMin.x, boundsMin.z, boundsMax.x, boundsMax.z, m_CurrentViewport.x, m_CurrentViewport.y, m_CurrentViewport.xMax, m_CurrentViewport.yMax))
                {
                    var id = GetNodeUniqueID(node.Depth, node.Code, m_Full);
                    if (!renderer.Mark(id))
                    {
                        renderer.AddRenderable(id, boundsMin, scale, node.Depth, renderer.FullMesh, name, m_MaterialPool);
                    }
                }
            }
            else
            {
                var halfWidth = scale.x * 0.5f;
                var halfHeight = scale.z * 0.5f;

                if (drawBottomLeft)
                {
                    var name = $"Renderable_{$"[{node.DebugID}]BottomLeft"}_{node.Depth}";
                    if (Helper.RectOverlap(boundsMin.x, boundsMin.z, boundsMax.x - halfWidth, boundsMax.z - halfHeight, m_CurrentViewport.x, m_CurrentViewport.y, m_CurrentViewport.xMax, m_CurrentViewport.yMax))
                    {
                        var id = GetNodeUniqueID(node.Depth, node.Code, m_BottomLeft);
                        if (!renderer.Mark(id))
                        {
                            renderer.AddRenderable(id, boundsMin, scale, node.Depth, renderer.BottomLeftMesh, name, m_MaterialPool);
                        }
                    }
                }
                if (drawBottomRight)
                {
                    var name = $"Renderable_{$"[{node.DebugID}]BottomRight"}_{node.Depth}";
                    if (Helper.RectOverlap(boundsMin.x + halfWidth, boundsMin.z, boundsMax.x, boundsMax.z - halfHeight, m_CurrentViewport.x, m_CurrentViewport.y, m_CurrentViewport.xMax, m_CurrentViewport.yMax))
                    {
                        var id = GetNodeUniqueID(node.Depth, node.Code, m_BottomRight);
                        if (!renderer.Mark(id))
                        {
                            renderer.AddRenderable(id, boundsMin, scale, node.Depth, renderer.BottomRightMesh, name, m_MaterialPool);
                        }
                    }
                }
                if (drawTopLeft)
                {
                    var name = $"Renderable_{$"[{node.DebugID}]TopLeft"}_{node.Depth}";
                    if (Helper.RectOverlap(boundsMin.x, boundsMin.z + halfHeight, boundsMax.x - halfWidth, boundsMax.z, m_CurrentViewport.x, m_CurrentViewport.y, m_CurrentViewport.xMax, m_CurrentViewport.yMax))
                    {
                        var id = GetNodeUniqueID(node.Depth, node.Code, m_TopLeft);
                        if (!renderer.Mark(id))
                        {
                            renderer.AddRenderable(id, boundsMin, scale, node.Depth, renderer.TopLeftMesh, name, m_MaterialPool);
                        }
                    }
                }
                if (drawTopRight)
                {
                    var name = $"Renderable_{$"[{node.DebugID}]TopRight"}_{node.Depth}";
                    if (Helper.RectOverlap(boundsMin.x + halfWidth, boundsMin.z + halfHeight, boundsMax.x, boundsMax.z, m_CurrentViewport.x, m_CurrentViewport.y, m_CurrentViewport.xMax, m_CurrentViewport.yMax))
                    {
                        var id = GetNodeUniqueID(node.Depth, node.Code, m_TopRight);
                        if (!renderer.Mark(id))
                        {
                            renderer.AddRenderable(id, boundsMin, scale, node.Depth, renderer.TopRightMesh, name, m_MaterialPool);
                        }
                    }
                }
            }
        }

        private long GetNodeUniqueID(long depth, long code, long corner)
        {
            return (code << 32) | (depth << 16) | (m_Tile.Index * 10 + corner);
        }

        private Rect m_CurrentViewport;
        private Material m_Material;
        private ObjectPool<Material> m_MaterialPool;
        private readonly Tile m_Tile;
        private const int m_Full = 1;
        private const int m_BottomLeft = 2;
        private const int m_BottomRight = 3;
        private const int m_TopLeft = 4;
        private const int m_TopRight = 5;
    }
}
