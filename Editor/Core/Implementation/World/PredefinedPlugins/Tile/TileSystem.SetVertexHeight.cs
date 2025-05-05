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
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Tile.Editor
{
    internal partial class TileSystem
    {
        public float ClipMinHeight { get => m_ClipMinHeight; set => m_ClipMinHeight = value; }
        public bool GetGroundHeightInGame { set { m_GetGroundHeightInGame = value; } get { return m_GetGroundHeightInGame; } }
        public bool GenerateMeshCollider { set { m_GenerateMeshCollider = value; } get { return m_GenerateMeshCollider; } }
        public bool OptimizeMesh { set { m_OptimizeMesh = value; } get { return m_OptimizeMesh; } }
        public bool CreateHeightMeshAsPrefab { get { return m_CreateHeightMeshAsPrefab; } set { m_CreateHeightMeshAsPrefab = value; } }
        public bool GenerateObjMesh { get { return m_GenerateOBJMeshFile; } set { m_GenerateOBJMeshFile = value; } }
        public bool CreateMaterialForGroundPrefab { get { return m_CreateMaterialForGroundPrefab; } set { m_CreateMaterialForGroundPrefab = value; } }
        public Vector2Int[] NeighbourOffset => m_NeighbourOffset;

        public void FixNormal()
        {
            int horizontalTileCount = m_XTileCount;
            int verticalTileCount = m_YTileCount;

            for (int i = 0; i < verticalTileCount; ++i)
            {
                for (int j = 0; j < horizontalTileCount; ++j)
                {
                    FixTile(j, i);
                }
            }
        }

        private void FixTile(int x, int y)
        {
            var tile = GetTile(x, y);
            if (tile != null)
            {
                var tileHeights = tile.VertexHeights;
                if (tileHeights == null)
                {
                    return;
                }

                var curTileMesh = m_Renderer.GetTerrainHeightMesh(x, y);
                if (curTileMesh != null)
                {
                    var curTileNormals = curTileMesh.normals;
                    int r1 = tile.MeshResolution + 1;
                    for (int i = 0; i < m_NeighbourOffset.Length; ++i)
                    {
                        int nx = m_NeighbourOffset[i].x + x;
                        int ny = m_NeighbourOffset[i].y + y;
                        var neighbourTile = GetTile(nx, ny);
                        if (neighbourTile != null && neighbourTile.VertexHeights != null && neighbourTile.MeshResolution == tile.MeshResolution)
                        {
                            var neighbourTileMesh = m_Renderer.GetTerrainHeightMesh(nx, ny);
                            var neighbourTileNormal = neighbourTileMesh.normals;

                            if (i == 0)
                            {
                                //fix left edge normals
                                for (int v = 0; v < r1; ++v)
                                {
                                    int neighbourVertexIdx = v * r1 + r1 - 1;
                                    int curTileVertexIdx = v * r1;
                                    Vector3 averageNormal = curTileNormals[curTileVertexIdx] + neighbourTileNormal[neighbourVertexIdx];
                                    averageNormal.Normalize();
                                    curTileNormals[curTileVertexIdx] = averageNormal;
                                    neighbourTileNormal[neighbourVertexIdx] = averageNormal;
                                }
                            }
                            else if (i == 1)
                            {
                                //fix top edge normals
                                for (int h = 0; h < r1; ++h)
                                {
                                    int neighbourVertexIdx = h;
                                    int curTileVertexIdx = (r1 - 1) * r1 + h;
                                    Vector3 averageNormal = curTileNormals[curTileVertexIdx] + neighbourTileNormal[neighbourVertexIdx];
                                    averageNormal.Normalize();
                                    curTileNormals[curTileVertexIdx] = averageNormal;
                                    neighbourTileNormal[neighbourVertexIdx] = averageNormal;
                                }
                            }
                            else if (i == 2)
                            {
                                //fix right edge normals
                                for (int v = 0; v < r1; ++v)
                                {
                                    int neighbourVertexIdx = v * r1;
                                    int curTileVertexIdx = v * r1 + r1 - 1;
                                    Vector3 averageNormal = curTileNormals[curTileVertexIdx] + neighbourTileNormal[neighbourVertexIdx];
                                    averageNormal.Normalize();
                                    curTileNormals[curTileVertexIdx] = averageNormal;
                                    neighbourTileNormal[neighbourVertexIdx] = averageNormal;
                                }
                            }
                            else if (i == 3)
                            {
                                //fix bottom edge normals
                                for (int h = 0; h < r1; ++h)
                                {
                                    int neighbourVertexIdx = (r1 - 1) * r1 + h;
                                    int curTileVertexIdx = h;

                                    Vector3 averageNormal = curTileNormals[curTileVertexIdx] + neighbourTileNormal[neighbourVertexIdx];
                                    averageNormal.Normalize();
                                    curTileNormals[curTileVertexIdx] = averageNormal;
                                    neighbourTileNormal[neighbourVertexIdx] = averageNormal;
                                }
                            }
                            else
                            {
                                Debug.Assert(false, "todo");
                            }

                            neighbourTileMesh.normals = neighbourTileNormal;
                        }
                    }
                    curTileMesh.normals = curTileNormals;
                    curTileMesh.UploadMeshData(false);
                }
            }
        }

        public void SetTileResolution(int tileX, int tileY, int resolution, bool updateMesh)
        {
            var tile = GetTile(tileX, tileY);
            if (tile != null)
            {
                bool resolutionChange = tile.SetResolution(resolution);
                if (resolutionChange)
                {
#if ENABLE_CLIP_MASK
                    tile.InitClipMask($"Tile {tileX}_{tileY}", mTileWidth, mTileHeight, mColorArrayPool, null);
#endif
                }
            }
            if (updateMesh)
            {
                m_Renderer.UpdateMesh(tileX, tileY, true);
            }
        }

        public void SetHeights(int tileX, int tileY, int minX, int minY, int maxX, int maxY, int resolution,
            List<float> heights, bool updateMesh, bool dontChangeEdgeVertexHeight)
        {
            SetHeight(tileX, tileY, minX, minY, maxX, maxY, resolution, heights, dontChangeEdgeVertexHeight);
            if (updateMesh)
            {
                m_Renderer.UpdateMesh(tileX, tileY, true);
            }
        }

        public void SetHeight(int tileX, int tileY, int minX, int minY, int maxX, int maxY, int resolution,
            float height, bool updateMesh, bool dontChangeEdgeVertexHeight)
        {
            SetHeight(tileX, tileY, minX, minY, maxX, maxY, resolution, height, dontChangeEdgeVertexHeight);
            if (updateMesh)
            {
                m_Renderer.UpdateMesh(tileX, tileY, true);
            }
        }

        public void UpdateMesh(int tileX, int tileY, bool alwaysCreateMesh)
        {
            m_Renderer.UpdateMesh(tileX, tileY, alwaysCreateMesh);
        }

        public float GetHeightAtPos(float x, float z)
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

        //计算这个tile的uv
        void CalculateTileUV(TileObject tile)
        {
            if (tile == null)
            {
                return;
            }

            var prefabPath = tile.AssetPath;
            Vector2[] tileUVs;
            m_CachedTileUVs.TryGetValue(prefabPath, out tileUVs);
            if (tileUVs == null)
            {
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (obj != null)
                {
                    var meshFilter = obj.GetComponentInChildren<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        int vertexCount = meshFilter.sharedMesh.vertexCount;
                        if (vertexCount != 4)
                        {
                            return;
                        }

                        var uv0 = meshFilter.sharedMesh.uv;
                        var positions = meshFilter.sharedMesh.vertices;
                        Debug.Assert(uv0.Length == 4);
                        float minX = Mathf.Min(positions[0].x, positions[1].x, positions[2].x, positions[3].x);
                        float minZ = Mathf.Min(positions[0].z, positions[1].z, positions[2].z, positions[3].z);
                        float maxX = Mathf.Max(positions[0].x, positions[1].x, positions[2].x, positions[3].x);
                        float maxZ = Mathf.Max(positions[0].z, positions[1].z, positions[2].z, positions[3].z);

                        tileUVs = new Vector2[4];

                        for (int i = 0; i < 4; ++i)
                        {
                            if (Helper.Approximately(positions[i].x, minX, 0.001f) &&
                                Helper.Approximately(positions[i].z, minZ, 0.001f))
                            {
                                tileUVs[0] = uv0[i];
                            }

                            if (Helper.Approximately(positions[i].x, maxX, 0.001f) &&
                                Helper.Approximately(positions[i].z, minZ, 0.001f))
                            {
                                tileUVs[1] = uv0[i];
                            }

                            if (Helper.Approximately(positions[i].x, minX, 0.001f) &&
                                Helper.Approximately(positions[i].z, maxZ, 0.001f))
                            {
                                tileUVs[2] = uv0[i];
                            }

                            if (Helper.Approximately(positions[i].x, maxX, 0.001f) &&
                                Helper.Approximately(positions[i].z, maxZ, 0.001f))
                            {
                                tileUVs[3] = uv0[i];
                            }
                        }

                        m_CachedTileUVs[prefabPath] = tileUVs;
                    }
                }
            }

            tile.UVs = tileUVs;
        }

        public void SetLODMesh(int lod)
        {
            for (int i = 0; i < YTileCount; ++i)
            {
                for (int j = 0; j < XTileCount; ++j)
                {
                    var tile = GetTile(j, i);
                    if (tile != null)
                    {
                        m_Renderer.SetLODMesh(j, i, lod);
                    }
                }
            }
        }

        public void RetoreToEditorMesh()
        {
            for (int i = 0; i < YTileCount; ++i)
            {
                for (int j = 0; j < XTileCount; ++j)
                {
                    var tile = GetTile(j, i);
                    if (tile != null)
                    {
                        m_Renderer.RestoreToEditorMesh(j, i);
                    }
                }
            }
        }

        public Mesh GetHeightMesh(int x, int y, int lod)
        {
            return m_Renderer.GetTerrainRuntimeHeightMesh(x, y, lod);
        }

#if ENABLE_CLIP_MASK
        public void ClearClipMask()
        {
            for (int i = 0; i < YTileCount; ++i)
            {
                for (int j = 0; j < XTileCount; ++j)
                {
                    var tile = GetTile(i, j);
                    if (tile != null)
                    {
                        tile.ClearClipMask();
                    }
                }
            }
        }

        public void ShowClipMask(bool visible)
        {
            for (int i = 0; i < YTileCount; ++i)
            {
                for (int j = 0; j < XTileCount; ++j)
                {
                    var tile = GetTile(i, j);
                    if (tile != null)
                    {
                        tile.ShowClipMask(visible);
                    }
                }
            }
        }
#endif

        public string CheckIfGetReadyToPaintHeight()
        {
            int h = XTileCount;
            int v = YTileCount;
            for (int i = 0; i < v; ++i)
            {
                for (int j = 0; j < h; ++j)
                {
                    m_Renderer.GetTerrainTileMeshAndGameObject(j, i, out var mesh, out var _);
                    if (mesh == null)
                    {
                        return $"tile x:{j}, y:{i} 没有Mesh";
                    }
                    if (mesh.vertexCount > 4 && !m_Renderer.IsRuntimeMesh(mesh))
                    {
                        return $"tile x:{j}, y:{i} 顶点数大于4,绘制后会替换成编辑器生成的Mesh";
                    }
                }
            }

            return "";
        }

        private void SetHeight(int tileX, int tileY, int minX, int minY, int maxX, int maxY, int resolution, List<float> heights, bool dontChangeEdgeVertexHeight)
        {
            var tile = GetTile(tileX, tileY);
            if (tile != null)
            {
                tile.SetHeight(minX, minY, maxX, maxY, resolution, heights, 0, dontChangeEdgeVertexHeight);
            }
        }

        private void SetHeight(int tileX, int tileY, int minX, int minY, int maxX, int maxY, int resolution, float height, bool dontChangeEdgeVertexHeight)
        {
            var tile = GetTile(tileX, tileY);
            if (tile != null)
            {
                tile.SetHeight(minX, minY, maxX, maxY, resolution, null, height, dontChangeEdgeVertexHeight);
            }
        }

        private Dictionary<string, Vector2[]> m_CachedTileUVs = new();
        private Vector2Int[] m_NeighbourOffset = new Vector2Int[4] 
        {
            new(-1, 0),
            new(0, 1),
            new(1, 0),
            new(0, -1),
        };
        //高度相关数据
        //游戏运行时是否需要取高度,如果要,则需要导出tile的高度数据(float[]),只要一个地表带高度,则运行时terrain mesh是一定会生成的
        private bool m_GetGroundHeightInGame = false;
        //是否给带高度的地表生成mesh collider
        private bool m_GenerateMeshCollider = false;
        private bool m_OptimizeMesh = true;
        //将地表mesh的带高度部分单独生成prefab作为装饰物
        private bool m_CreateHeightMeshAsPrefab = false;
        private bool m_GenerateOBJMeshFile = false;
        //三角形低于该高度就被裁剪
        private float m_ClipMinHeight;
        //是否为每个地形prefab生成单独材质
        private bool m_CreateMaterialForGroundPrefab;
        private TileLODMeshCreator m_TileMeshCreator;
    }
}

