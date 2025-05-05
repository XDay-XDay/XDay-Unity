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
using XDay.UtilityAPI.Editor;

namespace XDay.WorldAPI.Tile.Editor
{
    internal partial class TileSystemRenderer
    {
        public void PreprocessHeightMeshies(string[] usedPrefabPaths)
        {
            if (usedPrefabPaths != null)
            {
                for (int i = 0; i < usedPrefabPaths.Length; ++i)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(usedPrefabPaths[i]);
                    var filter = prefab.GetComponentInChildren<MeshFilter>();
                    if (filter != null)
                    {
                        var mesh = filter.sharedMesh;
                        if (mesh != null)
                        {
                            m_RuntimeHeightMeshManager.SetOriginalMesh(usedPrefabPaths[i], mesh);
                        }
                    }
                }
            }
        }

        public void GetTerrainTileMeshAndGameObject(int x, int y, out Mesh mesh, out GameObject gameObject)
        {
            mesh = null;
            gameObject = null;
            var tileData = m_TileSystem.GetTile(x, y);
            if (tileData != null)
            {
                gameObject = m_TileGameObjects[y * m_TileSystem.YTileCount + x];
                if (gameObject != null)
                {
                    var filters = gameObject.GetComponentsInChildren<MeshFilter>();
                    if (filters != null)
                    {
                        for (int i = 0; i < filters.Length; ++i)
                        {
                            //if (filters[i].tag != MapCoreDef.MAP_IGNORE_MESH_TAG)
                            {
                                mesh = filters[i].sharedMesh;
                            }
                        }
                    }
                }
            }
        }

        public Mesh GetTerrainHeightMesh(int x, int y)
        {
            TerrainHeightData data;
            m_HeightMeshes.TryGetValue(new Vector2Int(x, y), out data);
            if (data != null)
            {
                return data.Mesh;
            }
            return null;
        }

        public Mesh GetTerrainTileMesh(int tileX, int tileY)
        {
            var obj = m_TileGameObjects[tileY * m_TileSystem.XTileCount + tileX];
            var filter = obj.GetComponentInChildren<MeshFilter>();
            if (filter != null)
            {
                return filter.sharedMesh;
            }
            return null;
        }

        public bool IsRuntimeMesh(Mesh mesh)
        {
            foreach (var kv in m_HeightMeshes)
            {
                if (kv.Value.Mesh == mesh)
                {
                    return true;
                }
            }
            return false;
        }

        public void UpdateMesh(int tileX, int tileY, bool alwaysCreateMesh)
        {
            var tile = m_TileSystem.GetTile(tileX, tileY);
            if (tile == null || !tile.IsMeshDirty)
            {
                return;
            }

            tile.IsMeshDirty = false;
            var obj = m_TileGameObjects[tileY * m_TileSystem.YTileCount + tileX];
            if (obj != null)
            {
                var filter = obj.GetComponentInChildren<MeshFilter>();
                if (filter != null)
                {
                    var key = new Vector2Int(tileX, tileY);
                    TerrainHeightData oldData;
                    bool found = m_HeightMeshes.TryGetValue(key, out oldData);
                    TerrainHeightData newData = CreateTerrainMesh(tileX, tileY, oldData, alwaysCreateMesh);
                    if (newData != null)
                    {
                        if (newData != oldData)
                        {
                            oldData?.OnDestroy();
                        }
                        m_HeightMeshes[key] = newData;
                        filter.sharedMesh = newData.Mesh;
                    }
                }
            }
        }

        public void SetLODMesh(int x, int y, int lod)
        {
            var lodMesh = m_RuntimeHeightMeshManager.GetHeightMesh(x, y, lod);
            if (lodMesh != null)
            {
                var obj = m_TileGameObjects[y * m_TileSystem.YTileCount + x];
                if (obj != null)
                {
                    var filter = obj.GetComponentInChildren<MeshFilter>();
                    filter.sharedMesh = lodMesh;
                }
            }
        }

        public void RestoreToEditorMesh(int x, int y)
        {
            m_HeightMeshes.TryGetValue(new Vector2Int(x, y), out var heightData);
            Mesh editorMesh = null;
            if (heightData != null)
            {
                editorMesh = heightData.Mesh;
            }
            else
            {
                var tile = m_TileSystem.GetTile(x, y);
                var lod0PrefabPath = tile.AssetPath;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(lod0PrefabPath);
                if (prefab != null)
                {
                    var filter = prefab.GetComponentInChildren<MeshFilter>();
                    if (filter != null)
                    {
                        editorMesh = filter.sharedMesh;
                    }
                }
            }

            if (editorMesh != null)
            {
                var obj = m_TileGameObjects[y * m_TileSystem.YTileCount + x];
                if (obj != null)
                {
                    var filter = obj.GetComponentInChildren<MeshFilter>();
                    filter.sharedMesh = editorMesh;
                }
            }
            else
            {
                Debug.LogError($"Restore To Editor Mesh failed in tile: ({x}, {y})");
            }
        }

        public Mesh GetTerrainRuntimeHeightMesh(int x, int y, int lod)
        {
            return m_RuntimeHeightMeshManager.GetHeightMesh(x, y, lod);
        }

        //如果tile有高度,必须保证prefab没有transform旋转,只能旋转uv
        public TerrainHeightData CreateTerrainMesh(int tileX, int tileY, TerrainHeightData oldData, bool alwaysCreateMesh)
        {
            var tile = m_TileSystem.GetTile(tileX, tileY);
            if (tile == null)
            {
                return null;
            }
            if (tile.UVs == null)
            {
                Debug.LogError($"tile {tileX}-{tileY} no uv!");
                return null;
            }
            float[] heights = tile.VertexHeights;
            int resolution = tile.MeshResolution;
            if (heights == null || heights.Length == 0)
            {
                if (alwaysCreateMesh == false)
                {
                    return null;
                }

                heights = new float[4];
                resolution = 1;
            }
            int newVertexCount = (resolution + 1) * (resolution + 1);
            Mesh mesh = null;
            TerrainHeightData newData;
            if (oldData == null || newVertexCount != oldData.Mesh.vertexCount)
            {
                newData = new TerrainHeightData();
                mesh = new Mesh();
                mesh.MarkDynamic();
                newData.Mesh = mesh;
            }
            else
            {
                newData = oldData;
                mesh = oldData.Mesh;
            }
            float gridSize = m_TileSystem.TileWidth / resolution;
            var vertices = new Vector3[newVertexCount];
            var uvs = new Vector2[newVertexCount];
            var indices = new int[resolution * resolution * 6];
            int idx = 0;
            int triangleIdx = 0;
            for (int i = 0; i <= resolution; ++i)
            {
                for (int j = 0; j <= resolution; ++j)
                {
                    float x = j;
                    float z = i;
                    vertices[idx] = new Vector3(x * gridSize, heights[idx], z * gridSize);
                    float rx = (float)j / resolution;
                    float ry = (float)i / resolution;
                    uvs[idx] = InterpolateUV(tile.UVs, rx, ry);
                    ++idx;

                    if (i != resolution && j != resolution)
                    {
                        int v0 = i * (resolution + 1) + j;
                        int v1 = v0 + (resolution + 1);
                        int v2 = v1 + 1;
                        int v3 = v0 + 1;

                        indices[triangleIdx * 6] = v0;
                        indices[triangleIdx * 6 + 1] = v1;
                        indices[triangleIdx * 6 + 2] = v2;
                        indices[triangleIdx * 6 + 3] = v0;
                        indices[triangleIdx * 6 + 4] = v2;
                        indices[triangleIdx * 6 + 5] = v3;
                        ++triangleIdx;
                    }
                }
            }

            if (newVertexCount > ushort.MaxValue)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = indices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.UploadMeshData(false);

            float heightRange = mesh.bounds.size.y;
            float[] normalizedHeights = (float[])heights.Clone();
            for (int i = 0; i < normalizedHeights.Length; ++i)
            {
                if (heightRange != 0)
                {
                    normalizedHeights[i] = heights[i] / heightRange;
                }
                else
                {
                    normalizedHeights[i] = 0;
                }
            }

#if UNITY_EDITOR_WIN
            if (newData.Collider == null)
            {
                if (tile.VertexHeights != null)
                {
                    Vector3 pos = m_TileSystem.CoordinateToUnrotatedPosition(tileX, tileY);
                    newData.Collider = new PhysxTerrainCollider();
                    newData.Collider.Init(pos, resolution, m_TileSystem.TileWidth, m_TileSystem.TileHeight, normalizedHeights, heightRange);
                }
            }
            else
            {
                newData.Collider.UpdateHeights(normalizedHeights, resolution, m_TileSystem.TileWidth, m_TileSystem.TileHeight, heightRange);
            }
#endif

            return newData;
        }

        private Vector2 InterpolateUV(Vector2[] cornerUVs, float rx, float ry)
        {
            Vector2 lBottom = Vector2.Lerp(cornerUVs[0], cornerUVs[1], rx);
            Vector2 lTop = Vector2.Lerp(cornerUVs[2], cornerUVs[3], rx);
            Vector2 final = Vector2.Lerp(lBottom, lTop, ry);
            return final;
        }

        private Dictionary<Vector2Int, TerrainHeightData> m_HeightMeshes = new();
        TerrainHeightMeshManager m_RuntimeHeightMeshManager;
    }
}
