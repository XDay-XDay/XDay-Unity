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

using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using XDay.UtilityAPI;
using XDay.UtilityAPI.Editor;

namespace XDay.WorldAPI.Tile.Editor
{
    internal partial class TileLODMeshCreator
    {
        public TileLODMeshCreator(TileSystem tileSystem)
        {
            m_TileSystem = tileSystem;
        }

        //生成游戏运行时使用的各个lod的mesh
        public void GenerateHeightMeshes()
        {
            //将tile的mesh还原成格子mesh
            m_TileSystem.RetoreToEditorMesh();

            string folder = TileSystemDefine.GetFullTerrainMeshFolderPath(m_TileSystem.World.GameFolder);
            FileUtil.DeleteFileOrDirectory(folder);
            Directory.CreateDirectory(folder);

            m_TileSystem.FixNormal();
            //calculate clip state
            List<HeightMeshClipState> clipStates = CalculateHeightMeshClipStates();

            AssetDatabase.StartAssetEditing();
            try
            {
                if (m_TileSystem.CreateHeightMeshAsPrefab)
                {
                    //create terrain prefab folder
                    var terrainPrefabFolder = TileSystemDefine.GetFullTerrainPrefabFolderPath(m_TileSystem.World.GameFolder);

                    if (!Directory.Exists(terrainPrefabFolder))
                    {
                        Directory.CreateDirectory(terrainPrefabFolder);
                        AssetDatabase.ImportAsset(terrainPrefabFolder);
                    }
                    EditorHelper.DeleteFolderContent(terrainPrefabFolder);
                }

                for (int i = 0; i < clipStates.Count; ++i)
                {
                    EditorUtility.DisplayProgressBar("Create Tile LOD Mesh", $"Creating {i + 1}/{clipStates.Count}", (i + 1) / (float)clipStates.Count);
                    CreateTileHeightMesh(clipStates[i], 
                        !m_TileSystem.GetGroundHeightInGame && m_TileSystem.OptimizeMesh, 
                        m_TileSystem.GenerateObjMesh, 
                        m_TileSystem.CreateHeightMeshAsPrefab);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.ToString());
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            EditorUtility.ClearProgressBar();

            AssetDatabase.Refresh();
        }

        private void CreateTileHeightMesh(HeightMeshClipState state, bool optimizeMesh, bool generateObjMesh, bool createHeightMeshAsPrefab)
        {
            for (int lod = 0; lod < m_TileSystem.LODCount; ++lod)
            {
                var localMesh = state.LODMeshes[lod];
                if (optimizeMesh)
                {
                    OptimizeMesh(state, state.TileX, state.TileY, state.LODResolution[lod], lod, false, createHeightMeshAsPrefab);
                }

                var path = TileSystemDefine.GetTerrainMeshPath(m_TileSystem.World.GameFolder, state.TileX, state.TileY, lod);

                if (generateObjMesh)
                {
                    OBJModelExporter.Create(Helper.RemoveExtension(path) + ".obj", localMesh.vertices, localMesh.uv, localMesh.colors, localMesh.triangles);
                }

                localMesh.UploadMeshData(true);

                if (!createHeightMeshAsPrefab)
                {
                    AssetDatabase.CreateAsset(localMesh, path);
                }
            }
        }

        private Mesh CreateLODMesh(Mesh lod0Mesh, int originalResolution, int step)
        {
            Vector3[] lod0Vertices = lod0Mesh.vertices;
            Vector2[] lod0UVs = lod0Mesh.uv;
            int curLODResolution = originalResolution / step;
            int curLODVertexCount = curLODResolution + 1;
            Vector3[] curLODVertices = new Vector3[curLODVertexCount * curLODVertexCount];
            Vector2[] curLODUVs = new Vector2[curLODVertexCount * curLODVertexCount];
            List<int> indices = new List<int>();

            for (int i = 0; i <= curLODResolution; ++i)
            {
                for (int j = 0; j <= curLODResolution; ++j)
                {
                    int oi = i * step;
                    int oj = j * step;
                    int dstIdx = i * (curLODResolution + 1) + j;
                    int srcIdx = oi * (originalResolution + 1) + oj;
                    curLODVertices[dstIdx] = lod0Vertices[srcIdx];
                    curLODUVs[dstIdx] = lod0UVs[srcIdx];

                    if (i < curLODResolution && j < curLODResolution)
                    {
                        int v0 = dstIdx;
                        int v1 = v0 + 1;
                        int v2 = v0 + curLODResolution + 1;
                        int v3 = v2 + 1;
                        indices.Add(v0);
                        indices.Add(v2);
                        indices.Add(v3);
                        indices.Add(v0);
                        indices.Add(v3);
                        indices.Add(v1);
                    }
                }
            }
            Debug.Assert(indices.Count == curLODResolution * curLODResolution * 6);

            var curLODMesh = new Mesh();
            curLODMesh.vertices = curLODVertices;
            curLODMesh.uv = curLODUVs;
            curLODMesh.triangles = indices.ToArray();
            curLODMesh.RecalculateBounds();
            curLODMesh.RecalculateNormals();

            return curLODMesh;
        }

        private void OptimizeMesh(HeightMeshClipState state, int tileX, int tileY, int resolution, int lod, bool onlyClipInLOD0, bool createHeightMeshAsPrefab)
        {
            Debug.Assert(false, "todo");
#if false
            var mesh = state.lodMeshes[lod];
            var meshQuadRemoved = state.isMeshQuadRemovedInEachLOD[lod];

            var tile = mLayerData.GetTile(tileX, tileY);
            var vertices = mesh.vertices;

            var normals = mesh.normals;
            float blockSize = mLayerData.tileWidth / resolution;
            List<MergeRects.ConnectedTiles> connectedTiles = MergeRects.ProcessTiles(resolution, resolution, blockSize,
                                (int bx, int by) =>
                                {
                                    int v0 = by * (resolution + 1) + bx;
                                    int v1 = v0 + 1;
                                    int v2 = v0 + resolution + 1;
                                    int v3 = v2 + 1;
                                    bool valid =
                                    vertices[v0].y == 0 &&
                                    vertices[v1].y == 0 &&
                                    vertices[v2].y == 0 &&
                                    vertices[v3].y == 0 &&
                                    !meshQuadRemoved[by, bx];

                                    return valid;
                                },
                                (int bx, int by) => { return new Vector3(bx * blockSize, 0, by * blockSize); },
                                (int bx, int by) => { return new Vector3((bx + 0.5f) * blockSize, 0, (by + 0.5f) * blockSize); }
                                );

            if (connectedTiles.Count > 0)
            {
                List<Vector3> optimizedVertices = new List<Vector3>();
                List<Vector3> optimizedNormals = new List<Vector3>();
                List<int> optimizedIndices = new List<int>();

                //计算哪些tile是有高度的
                bool[,] removedTiles = new bool[resolution, resolution];
                for (int k = 0; k < connectedTiles.Count; ++k)
                {
                    var tiles = connectedTiles[k].tiles;
                    int rows = tiles.GetLength(0);
                    int cols = tiles.GetLength(1);
                    var rects = connectedTiles[k].rectangles;
                    var bounds = connectedTiles[k].bounds;
                    for (int i = 0; i < rows; ++i)
                    {
                        for (int j = 0; j < cols; ++j)
                        {
                            if (tiles[i, j] == 1)
                            {
                                //tile是平地,后续会合并,所以删除
                                int gx = bounds.minX + j;
                                int gy = bounds.minY + i;
                                removedTiles[gy, gx] = true;
                            }
                        }
                    }

                }

                //add not optimized vertices
                for (int i = 0; i < resolution; ++i)
                {
                    for (int j = 0; j < resolution; ++j)
                    {
                        if (!removedTiles[i, j] ||
                            //即使tile是平地,如果要创建prefab,需要多出一部分平地和平面的地表衔接
                            (createHeightMeshAsPrefab && IsEdgeTile(j, i, vertices, resolution)))
                        {
                            //添加有高度的tile
                            bool isRemoved = meshQuadRemoved[i, j];
                            AddQuad(j, i, blockSize, resolution, vertices, normals, optimizedVertices, optimizedNormals, optimizedIndices, isRemoved);
                        }
                    }
                }

                if (createHeightMeshAsPrefab)
                {
                    CreateHeightMeshPrefab(optimizedVertices, optimizedNormals, optimizedIndices, tile, tileX, tileY, lod);
                }

                for (int k = 0; k < connectedTiles.Count; ++k)
                {
                    var rects = connectedTiles[k].rectangles;
                    var bounds = connectedTiles[k].bounds;
                    //add optimized vertices
                    foreach (var r in rects)
                    {
                        //合并平地tile
                        int gMinX = r.minX + bounds.minX;
                        int gMinY = r.minY + bounds.minY;
                        int gMaxX = r.maxX + bounds.minX;
                        int gMaxY = r.maxY + bounds.minY;
                        AddOptimizedQuad(gMinX, gMinY, gMaxX, gMaxY, blockSize, optimizedVertices, optimizedNormals, optimizedIndices);
                    }
                }

                mesh.Clear();
                mesh.SetVertices(optimizedVertices);
                mesh.uv = CalculateOptimizedUVs(optimizedVertices, mLayerData.tileWidth, mLayerData.tileHeight, tile.uvs);
                mesh.SetIndices(optimizedIndices, MeshTopology.Triangles, 0);
                mesh.RecalculateBounds();

                //fix edge vertex normals
#if true
                Vector2Int[] neighbourOffset = new Vector2Int[4]
                {
                    new Vector2Int(-1, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 0),
                    new Vector2Int(0, -1),
                };
                Vector2Int[] vertexOffset = new Vector2Int[4]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1),
                    new Vector2Int(1, 0),
                };
                for (int i = 0; i < resolution; ++i)
                {
                    for (int j = 0; j < resolution; ++j)
                    {
                        if (!removedTiles[i, j])
                        {
                            //check if tile is edge tile
                            bool isEdgeTile = false;
                            for (int p = 0; p < 4; ++p)
                            {
                                int nx = j + neighbourOffset[p].x;
                                int ny = i + neighbourOffset[p].y;
                                if (nx >= 0 && nx < resolution && ny >= 0 && ny < resolution)
                                {
                                    if (removedTiles[ny, nx])
                                    {
                                        isEdgeTile = true;
                                        break;
                                    }
                                }
                            }

                            if (isEdgeTile)
                            {
                                for (int p = 0; p < 4; ++p)
                                {
                                    Vector3 v = new Vector3((j + vertexOffset[p].x) * blockSize, 0, (i + vertexOffset[p].y) * blockSize);
                                    int idx = Utils.FindVertexXZ(optimizedVertices, v, 0.001f);
                                    if (idx >= 0 && optimizedVertices[idx].y == 0 && !Utils.Approximately(optimizedNormals[idx], Vector3.up, 0.001f))
                                    {
                                        //只有edge tile并且顶点高度为0的顶点normal才设置为0
                                        optimizedNormals[idx] = Vector3.up;
                                    }
                                }
                            }
                        }
                    }
                }
#endif
                mesh.SetNormals(optimizedNormals);
            }
#if false
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            for (int i = 0; i < connectedTiles.Count; ++i)
            {
                foreach (var r in connectedTiles[i].rectangles) {
                    int gMinX = r.minX + connectedTiles[i].bounds.minX;
                    int gMinY = r.minY + connectedTiles[i].bounds.mGetinY;
                    int gMaxX = r.maxX + connectedTiles[i].bounds.minX;
                    int gMaxY = r.maxY + connectedTiles[i].bounds.minY;
                    Vector3 minPos = new Vector3(gMinX * blockSize, 0, gMinY * blockSize);
                    Vector3 maxPos = new Vector3((gMaxX + 1) * blockSize, 0, (gMaxY + 1) * blockSize);
                    var dpObj = new GameObject(i.ToString());
                    var dp = dpObj.AddComponent<DrawBounds>();
                    dp.bounds.SetMinMax(minPos, maxPos);
                }
            }
#endif

            mesh.UploadMeshData(true);
#endif
        }

        //tile是平地,但周围的tile不是平地则是edge tile
        private bool IsEdgeTile(int x, int y, Vector3[] vertices, int resolution)
        {
            var neightbourOffset = m_TileSystem.NeighbourOffset;
            for (int i = 0; i < neightbourOffset.Length; ++i)
            {
                var nx = x + neightbourOffset[i].x;
                var ny = y + neightbourOffset[i].y;
                if (nx >= 0 && nx < resolution &&
                    ny >= 0 && ny < resolution)
                {
                    int v0 = ny * (resolution + 1) + nx;
                    int v1 = v0 + 1;
                    int v2 = v0 + resolution + 1;
                    int v3 = v2 + 1;
                    bool neighbourHasHeight =
                    vertices[v0].y != 0 ||
                    vertices[v1].y != 0 ||
                    vertices[v2].y != 0 ||
                    vertices[v3].y != 0;
                    if (neighbourHasHeight)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void AddQuad(int x, int y, float gridSize, int resolution, Vector3[] originalVertices, Vector3[] originalNormals, List<Vector3> vertices, List<Vector3> newNormals, List<int> indices, bool removed)
        {
            int oi0 = y * (resolution + 1) + x;
            int oi1 = oi0 + 1;
            int oi2 = oi0 + resolution + 1;
            int oi3 = oi2 + 1;

            Vector3 v0 = new Vector3(x * gridSize, originalVertices[oi0].y, y * gridSize);
            Vector3 v1 = new Vector3((x + 1) * gridSize, originalVertices[oi1].y, y * gridSize);
            Vector3 v2 = new Vector3(x * gridSize, originalVertices[oi2].y, (y + 1) * gridSize);
            Vector3 v3 = new Vector3((x + 1) * gridSize, originalVertices[oi3].y, (y + 1) * gridSize);

            if (removed)
            {
                v1 = v0;
                v2 = v0;
                v3 = v0;
            }

            float esp = 0.001f;
            bool added;
            var i0 = Helper.FindOrAddVertex(vertices, v0, esp, out added);
            if (added)
            {
                newNormals.Add(originalNormals[oi0]);
            }
            else
            {
                newNormals[i0] = originalNormals[oi0];
            }
            var i1 = Helper.FindOrAddVertex(vertices, v1, esp, out added);
            if (added)
            {
                newNormals.Add(originalNormals[oi1]);
            }
            else
            {
                newNormals[i1] = originalNormals[oi1];
            }
            var i2 = Helper.FindOrAddVertex(vertices, v2, esp, out added);
            if (added)
            {
                newNormals.Add(originalNormals[oi2]);
            }
            else
            {
                newNormals[i2] = originalNormals[oi2];
            }
            var i3 = Helper.FindOrAddVertex(vertices, v3, esp, out added);
            if (added)
            {
                newNormals.Add(originalNormals[oi3]);
            }
            else
            {
                newNormals[i3] = originalNormals[oi3];
            }

            indices.Add(i0);
            indices.Add(i2);
            indices.Add(i3);
            indices.Add(i0);
            indices.Add(i3);
            indices.Add(i1);
        }

        private void AddOptimizedQuad(int minX, int minY, int maxX, int maxY, float gridSize, List<Vector3> vertices, List<Vector3> normals, List<int> indices)
        {
            Vector3 v0 = new Vector3(minX * gridSize, 0, minY * gridSize);
            Vector3 v1 = new Vector3((maxX + 1) * gridSize, 0, minY * gridSize);
            Vector3 v2 = new Vector3(minX * gridSize, 0, (maxY + 1) * gridSize);
            Vector3 v3 = new Vector3((maxX + 1) * gridSize, 0, (maxY + 1) * gridSize);
            float esp = 0.001f;
            bool added;
            var i0 = Helper.FindOrAddVertex(vertices, v0, esp, out added);
            if (added)
            {
                normals.Add(Vector3.up);
            }
            else
            {
                normals[i0] = Vector3.up;
            }
            var i1 = Helper.FindOrAddVertex(vertices, v1, esp, out added);
            if (added)
            {
                normals.Add(Vector3.up);
            }
            else
            {
                normals[i1] = Vector3.up;
            }
            var i2 = Helper.FindOrAddVertex(vertices, v2, esp, out added);
            if (added)
            {
                normals.Add(Vector3.up);
            }
            else
            {
                normals[i2] = Vector3.up;
            }
            var i3 = Helper.FindOrAddVertex(vertices, v3, esp, out added);
            if (added)
            {
                normals.Add(Vector3.up);
            }
            else
            {
                normals[i3] = Vector3.up;
            }
            indices.Add(i0);
            indices.Add(i2);
            indices.Add(i3);
            indices.Add(i0);
            indices.Add(i3);
            indices.Add(i1);
        }

        private Vector2 InterpolateUV(Vector2[] cornerUVs, float rx, float ry)
        {
            Vector2 lBottom = Vector2.Lerp(cornerUVs[0], cornerUVs[1], rx);
            Vector2 lTop = Vector2.Lerp(cornerUVs[2], cornerUVs[3], rx);
            Vector2 final = Vector2.Lerp(lBottom, lTop, ry);
            return final;
        }

        private Vector2[] CalculateOptimizedUVs(List<Vector3> vertices, float tileWidth, float tileHeight, Vector2[] tileUVs)
        {
            Vector2[] uvs = new Vector2[vertices.Count];
            for (int i = 0; i < uvs.Length; ++i)
            {
                float rx = vertices[i].x / tileWidth;
                float ry = vertices[i].z / tileHeight;
                uvs[i] = InterpolateUV(tileUVs, rx, ry);
            }
            return uvs;
        }

        private void CreateHeightMeshPrefab(List<Vector3> optimizedVertices, List<Vector3> optimizedNormals, List<int> optimizedIndices, TileObject tile, int x, int y, int lod)
        {
            Debug.Assert(false, "todo");
#if false
            Mesh mesh = new Mesh();

            var center = Helper.CalculateCenter(optimizedVertices);
            List<Vector3> localVertices = new List<Vector3>();
            for (int i = 0; i < optimizedVertices.Count; ++i)
            {
                localVertices.Add(optimizedVertices[i] - center);
            }

            mesh.SetVertices(localVertices);
            mesh.SetNormals(optimizedNormals);
            mesh.uv = CalculateOptimizedUVs(optimizedVertices, m_TileSystem.TileWidth, m_TileSystem.TileHeight, tile.UVs);
            mesh.SetIndices(optimizedIndices, MeshTopology.Triangles, 0);
            mesh.RecalculateBounds();

            mesh.UploadMeshData(true);

            var gameObject = new GameObject($"Ground_{x}_{y}_lod{lod}");
            var positionSetter = gameObject.AddComponent<PositionSetter>();
            positionSetter.position = tile.Position + center;

            var filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();

            GetTerrainTileMeshAndGameObject(x, y, out _, out var tileGameObject);
            var tileMeshRenderer = tileGameObject.GetComponentInChildren<MeshRenderer>();
            renderer.sharedMaterial = tileMeshRenderer.sharedMaterial;

            //save assets
            var folder = MapCoreDef.GetFullTerrainPrefabFolderPath(SLGMakerEditor.instance.exportFolder);

            string prefix = $"{folder}/GroundTile{x}_{y}_lod{lod}";
            AssetDatabase.CreateAsset(mesh, $"{prefix}.asset");

            if (mLayerData.createMaterialForGroundPrefab)
            {
                var shader = mLayerData.terrainPrefabShader;
                if (shader == null && renderer.sharedMaterial != null)
                {
                    shader = renderer.sharedMaterial.shader;
                }
                if (shader != null)
                {
                    string mtlPath = $"{prefix}.mat";
                    Material mtl;
                    if (lod == 0)
                    {
                        if (renderer.sharedMaterial != null)
                        {
                            mtl = Object.Instantiate(renderer.sharedMaterial);
                            mtl.shader = shader;
                        }
                        else
                        {
                            mtl = new Material(shader);
                        }

                        AssetDatabase.CreateAsset(mtl, mtlPath);
                    }
                    else
                    {
                        mtl = AssetDatabase.LoadAssetAtPath<Material>($"{folder}/GroundTile{x}_{y}_lod0.mat");
                        Debug.Assert(mtl != null);
                    }
                    renderer.sharedMaterial = mtl;
                }
                else
                {
                    Debug.LogError("ground prefab shader is not set!");
                }
            }

            PrefabUtility.SaveAsPrefabAsset(gameObject, $"{prefix}.prefab");
            Helper.DestroyUnityObject(gameObject);
#endif
        }

        private readonly TileSystem m_TileSystem;
    }
}

