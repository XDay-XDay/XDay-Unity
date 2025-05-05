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
using UnityEditor;
using UnityEngine;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Tile.Editor
{
    internal partial class TileLODMeshCreator
    {
        private class HeightMeshClipState
        {
            public HeightMeshClipState(int tileX, int tileY)
            {
                TileX = tileX;
                TileY = tileY;
            }

            public List<bool[,]> IsMeshQuadRemovedInEachLOD = new List<bool[,]>();
            public List<Mesh> LODMeshes = new List<Mesh>();
            public List<int> LODResolution = new List<int>();
            public int TileX { get; }
            public int TileY { get; }
        }

        private List<HeightMeshClipState> CalculateHeightMeshClipStates()
        {
            SimpleStopwatch w = new SimpleStopwatch();
            w.Begin();
            EditorUtility.DisplayProgressBar("Calculate Clip State", "Calculating Clip State", 0);

            List<HeightMeshClipState> states = new();

            int h = m_TileSystem.XTileCount;
            int v = m_TileSystem.YTileCount;
            //create lod mesh
            for (int i = 0; i < v; ++i)
            {
                for (int j = 0; j < h; ++j)
                {
                    var tile = m_TileSystem.GetTile(j, i);
                    if (tile != null && 
                        tile.VertexHeights != null)
                    {
                        var mesh = m_TileSystem.Renderer.GetTerrainTileMesh(j, i);
                        HeightMeshClipState state = new(j, i);
                        states.Add(state);
                        int step = 1;
                        int lod0Resolution = tile.MeshResolution;
                        for (int lod = 0; lod < m_TileSystem.LODCount; ++lod)
                        {
                            Mesh localMesh;
                            if (lod == 0)
                            {
                                localMesh = Object.Instantiate(mesh);
                            }
                            else
                            {
                                int newStep = step * 2;
                                //保证vertex数量大于1
                                if (lod0Resolution / newStep > 0)
                                {
                                    step = newStep;
                                }
                                localMesh = CreateLODMesh(mesh, lod0Resolution, step);
                            }

                            state.LODMeshes.Add(localMesh);
                            state.LODResolution.Add(lod0Resolution / step);
                        }
                    }
                }
            }

#if false
            var allObstacles = GetAllObstaclces(m_TileSystem.World);
            //clip in multithreading
            for (int i = 0; i < states.Count; ++i)
            {
                var state = states[i];
                for (int lod = 0; lod < m_TileSystem.LODCount; ++lod)
                {
                    var tilePosition = m_TileSystem.CoordinateToUnrotatedPosition(state.TileX, state.TileY);
                    var tile = m_TileSystem.GetTile(state.TileX, state.TileY);
                    bool[,] isMeshQuadRemoved = ClipHole(state.LODResolution[lod], tilePosition, false, lod, allObstacles, m_TileSystem.ClipMinHeight, tile.ClipMask);
                    state.IsMeshQuadRemovedInEachLOD.Add(isMeshQuadRemoved);
                }
            }
#endif

            w.Stop();
            Debug.Log($"Calculate Clip State cost: {w.ElapsedSeconds} seconds");

            return states;
        }

        private List<IObstacle> GetAllObstaclces(IWorld world)
        {
            List<IObstacle> allObstacles = new();
            for (var i = 0; i < world.PluginCount; ++i)
            {
                var plugin = world.GetPlugin(i);
                if (plugin is IObstacleSource obstacleSource)
                {
                    var obstacles = obstacleSource.GetObstacles();
                    if (obstacles != null)
                    {
                        allObstacles.AddRange(obstacles);
                    }
                }
            }
            return allObstacles;
        }

#if false
        public bool[,] ClipHole(int resolution, Vector3 tilePosition, bool onlyClipInLOD0, int lod, List<MapCollisionData> clipperCollisions, float clipMinHeight, ClipMask clipMask)
        {
            bool[,] removed = new bool[resolution, resolution];
            if (!onlyClipInLOD0 || lod == 0)
            {
                float tileSize = layerData.tileWidth;
                float gridSize = tileSize / resolution;

                //check clip mask
                if (clipMask != null)
                {
                    int blockSize = Mathf.RoundToInt(Mathf.Pow(2, lod));
                    for (int i = 0; i < resolution; ++i)
                    {
                        for (int j = 0; j < resolution; ++j)
                        {
                            removed[i, j] = IsBlockClipped(j, i, blockSize, clipMask, mPaintHeightParameters.parameter.clipLODType);
                        }
                    }
                }

                //check clip height
                if (clipMinHeight != 0)
                {
                    for (int i = 0; i < resolution; ++i)
                    {
                        for (int j = 0; j < resolution; ++j)
                        {
                            float minX = j * gridSize;
                            float maxX = minX + gridSize;
                            float minZ = i * gridSize;
                            float maxZ = minZ + gridSize;

                            var v0 = new Vector3(minX, 0, minZ) + tilePosition;
                            var v1 = new Vector3(minX, 0, maxZ) + tilePosition;
                            var v2 = new Vector3(maxX, 0, maxZ) + tilePosition;
                            var v3 = new Vector3(maxX, 0, minZ) + tilePosition;

                            float h0 = m_TileSystem.GetHeightAtPos(v0.x, v0.z);
                            float h1 = m_TileSystem.GetHeightAtPos(v1.x, v1.z);
                            float h2 = m_TileSystem.GetHeightAtPos(v2.x, v2.z);
                            float h3 = m_TileSystem.GetHeightAtPos(v3.x, v3.z);
                            if (h0 < clipMinHeight &&
                                h1 < clipMinHeight &&
                                h2 < clipMinHeight &&
                                h3 < clipMinHeight)
                            {
                                removed[i, j] = true;
                            }
                        }
                    }
                }

                //check clipper polygons
                List<Vector3> gridPolygon = new List<Vector3>();
                var copy = new List<Vector3>();
                foreach (var collision in clipperCollisions)
                {
                    copy.Clear();

                    var outline = collision.GetWorldSpaceOutlineVerticesList(PrefabOutlineType.NavMeshObstacle);
                    //check if tile intersect with polygon
                    gridPolygon.Clear();
                    gridPolygon.Add(new Vector3(tilePosition.x, 0, tilePosition.z));
                    gridPolygon.Add(new Vector3(tilePosition.x, 0, tilePosition.z + tileSize));
                    gridPolygon.Add(new Vector3(tilePosition.x + tileSize, 0, tilePosition.z + tileSize));
                    gridPolygon.Add(new Vector3(tilePosition.x + tileSize, 0, tilePosition.z));
                    if (!GeometricAlgorithm.IsPolygonIntersected(gridPolygon, outline))
                    {
                        continue;
                    }

                    copy.AddRange(outline);
                    for (int i = 0; i < copy.Count; ++i)
                    {
                        copy[i] -= tilePosition;
                    }

                    for (int i = 0; i < resolution; ++i)
                    {
                        for (int j = 0; j < resolution; ++j)
                        {
                            gridPolygon.Clear();
                            float minX = j * gridSize;
                            float maxX = minX + gridSize;
                            float minZ = i * gridSize;
                            float maxZ = minZ + gridSize;

                            var v0 = new Vector3(minX, 0, minZ);
                            var v1 = new Vector3(minX, 0, maxZ);
                            var v2 = new Vector3(maxX, 0, maxZ);
                            var v3 = new Vector3(maxX, 0, minZ);

                            gridPolygon.Add(v0);
                            gridPolygon.Add(v1);
                            gridPolygon.Add(v2);
                            gridPolygon.Add(v3);

                            if (GeometricAlgorithm.IsPolygonFullInsideOfPolygon(gridPolygon, copy))
                            {
                                removed[i, j] = true;
                            }
                        }
                    }
                }
            }

            return removed;
        }
#endif

        private bool IsBlockClipped(int blockX, int blockY, int blockSize, ClipMask clipMask, ClipLODType clipLODType)
        {
            int clipCount = 0;
            for (int i = 0; i < blockSize; ++i)
            {
                for (int j = 0; j < blockSize; ++j)
                {
                    int x = blockX * blockSize + j;
                    int y = blockY * blockSize + i;
                    if (clipMask.IsClipped(x, y))
                    {
                        ++clipCount;
                    }
                }
            }

            if (clipLODType == ClipLODType.ClipAll)
            {
                return clipCount == blockSize * blockSize;
            }
            if (clipLODType == ClipLODType.ClipAny)
            {
                return clipCount > 0;
            }
            Debug.Assert(false);
            return false;
        }
    }
}


