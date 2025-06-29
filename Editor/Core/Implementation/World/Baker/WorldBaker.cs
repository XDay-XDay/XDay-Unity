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
using System.IO;
using UnityEngine;
using UnityEditor;
using XDay.UtilityAPI;
using XDay.UtilityAPI.Editor;

namespace XDay.WorldAPI.Editor
{
    //每一级烘培lod的设置
    internal class WorldBakerLODSetting
    {
        public WorldBakerLODSetting(float bakeHeight, float lodHeight, int resolution, Material material)
        {
            BakingHeight = bakeHeight;
            LODHeight = lodHeight;
            Resolution = resolution;
            Material = material;
        }

        //相机烘焙高度
        public float BakingHeight { get; set; }
        //LOD切换高度
        public float LODHeight { get; set; }
        //相对于上一级的块大小
        public int Resolution { get; set; }
        //使用哪个材质
        public Material Material { get; set; }
    }

    internal class WorldBakerSetting
    {
        public Camera Camera;
        public Rect BakingBounds;
        public WorldBakerLODSetting[] LODs;
        public string OutputFolder;
        public int BakingTextureSize = 1024;
        public Shader DefaultBakingShader;
        public string TexturePropertyName = "_MainTex";
        public bool EnableDepthTexture = false;
    }

    //将地图烘培成多个分块
    internal class WorldBaker
    {
        public void Bake(List<IWorldBakeable> bakeables, WorldBakerSetting setting)
        {
            m_BakerSetting = setting;
            Debug.Assert(m_BakedTilesInEachLOD == null);
            m_BakedTilesInEachLOD = new List<BakedTileData[,]>();

            FileUtil.DeleteFileOrDirectory(setting.OutputFolder);

            CreateOutputFolder(setting.OutputFolder);

            BakeLODs(bakeables);

            SaveData(setting.OutputFolder);
        }

        private void BakeLODs(List<IWorldBakeable> bakeables)
        {
            for (int i = 0; i < m_BakerSetting.LODs.Length; ++i)
            {
                //生成额外的lod数据
                var lod = m_BakerSetting.LODs[i];
                BakeLOD(bakeables, i, lod);
            }
        }

        private void CreateOutputFolder(string outputFolder)
        {
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }
        }

        /*生成lod数据
         * lod: lod设置的索引
         */
        private void BakeLOD(List<IWorldBakeable> bakeables, int lod, WorldBakerLODSetting lodSetting)
        {
            float maxRange = Mathf.Max(m_BakerSetting.BakingBounds.width, m_BakerSetting.BakingBounds.height);
            float tileSize = maxRange / lodSetting.Resolution;

            int horizontalTileCountInThisLOD = lodSetting.Resolution;
            int verticalTileCountInThisLOD = lodSetting.Resolution;

            var lodTiles = new BakedTileData[horizontalTileCountInThisLOD, verticalTileCountInThisLOD];
            m_BakedTilesInEachLOD.Add(lodTiles);

            Vector3 blockPos;
            int idx = 0;
            for (int i = 0; i < verticalTileCountInThisLOD; ++i)
            {
                for (int j = 0; j < horizontalTileCountInThisLOD; ++j)
                {
                    EditorUtility.DisplayProgressBar($"Baking LOD {lod}...", $"Processing tile {j}_{i}", 1f * (idx + 1) / (horizontalTileCountInThisLOD * verticalTileCountInThisLOD));

                    var prefabIndex = CreatePrefab(bakeables, lod - 1, j, i, tileSize, lodSetting, out blockPos);
                    if (prefabIndex >= 0)
                    {
                        BakedTileData tileData;
                        if (lod == 0)
                        {
                            var pos = CalculateLODTilePosition(Helper.ToVector3XZ(m_BakerSetting.BakingBounds.min), j, i, tileSize);
                            tileData = new BakedTileData(pos, prefabIndex);
                        }
                        else
                        {
                            tileData = new BakedTileData(blockPos, prefabIndex);
                        }
                        lodTiles[i, j] = tileData;
                    }

                    ++idx;
                }
            }

            EditorUtility.ClearProgressBar();
        }

        //创建在lod下x,y lod tile使用的model template
        //blockPos: tile的坐标
        private int CreatePrefab(List<IWorldBakeable> bakeables, int lastLOD, int x, int y, float tileSize, WorldBakerLODSetting lodSetting, out Vector3 blockPos)
        {
            Debug.Assert(m_BakerSetting.DefaultBakingShader != null);
            blockPos = Vector3.zero;
            string tilePrefix = string.Format("Baking_Tile_{0}_{1}_LOD_{2}", x, y, lastLOD + 1);
            BakeToRenderTexture rt = new("Bake To RenderTexture", m_BakerSetting.Camera, m_BakerSetting.EnableDepthTexture);

            //create combined tiles
            var root = new GameObject(string.Format("LOD {0} Tile ({1},{2})", lastLOD + 1, x, y));

            Vector3 minPos = new(x * tileSize + m_BakerSetting.BakingBounds.x, 0, y * tileSize + m_BakerSetting.BakingBounds.y);
            Vector3 maxPos = minPos + new Vector3(tileSize, 0, tileSize);
            var bounds = new Bounds();
            bounds.SetMinMax(minPos, maxPos);

            if (lastLOD < 0)
            {
                foreach (var bakeable in bakeables)
                {
                    List<GameObject> gameObjects = bakeable.GetObjectsInRangeAtHeight(minPos, maxPos, lodSetting.BakingHeight);
                    foreach (var obj in gameObjects)
                    {
                        obj.transform.SetParent(root.transform, true);
                    }
                    m_GameObjects[bakeable] = gameObjects;
                }
            }
            else
            {
                //使用上一级的lod生成数据来生成新的lod的tile
                var tiles = m_BakedTilesInEachLOD[lastLOD];

                int rows = tiles.GetLength(0);
                int cols = tiles.GetLength(1);

                blockPos = minPos;

                float maxRange = Mathf.Max(m_BakerSetting.BakingBounds.width, m_BakerSetting.BakingBounds.height);
                float lastLODTileSize = maxRange / m_BakerSetting.LODs[lastLOD].Resolution;
                Vector3 boundsMin = Helper.ToVector3XZ(m_BakerSetting.BakingBounds.min);

                Vector2Int minCoord = FromWorldPositionToCoordinate(minPos - boundsMin, lastLODTileSize);
                Vector2Int maxCoord = FromWorldPositionToCoordinate(maxPos - boundsMin, lastLODTileSize);

                for (int i = minCoord.y; i <= maxCoord.y; ++i)
                {
                    for (int j = minCoord.x; j <= maxCoord.x; ++j)
                    {
                        if (i >= 0 && i < rows && j >= 0 && j < cols)
                        {
                            var tile = tiles[i, j];
                            if (tile != null)
                            {
                                string tilePrefabPath = m_UsedPrefabs[tile.PrefabIndex];

                                var tilePos = boundsMin + new Vector3(lastLODTileSize * j, 0, lastLODTileSize * i);
                                GameObject tileObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(tilePrefabPath));
                                tileObject.name = tilePrefabPath;
                                tileObject.transform.position = tilePos;
                                tileObject.transform.SetParent(root.transform, true);
                            }
                        }
                    }
                }
            }

            int n = root.transform.childCount;

            //render block tiles into texture
            rt.TextureSize = m_BakerSetting.BakingTextureSize;
            rt.Render(root, false, lodSetting.BakingHeight, customRenderTexture:null, false, bounds);
            string texturePath = string.Format("{0}/{1}.tga", m_BakerSetting.OutputFolder, tilePrefix);
            //save texture
            rt.SaveTexture(rt.TargetTexture, texturePath, true);
            AssetDatabase.Refresh();

            //destroy game objects
            foreach (var kv in m_GameObjects)
            {
                var bakeable = kv.Key;
                bakeable.DestroyGameObjects(kv.Value);
            }
            m_GameObjects.Clear();

            var planePrefab = GameObject.CreatePrimitive(PrimitiveType.Quad);
            //create quad mesh asset
            string meshPath = string.Format("{0}/{1}.asset", m_BakerSetting.OutputFolder, tilePrefix);
            var mesh = CreateQuadMesh(meshPath, tileSize);

            //if (generateMeshOBJ)
            //{
            //    string objPath = string.Format("{0}/{1}.obj", assetFolder, tilePrefix);
            //    //flip uv
            //    var uv = mesh.uv;
            //    for (int i = 0; i < uv.Length; ++i)
            //    {
            //        uv[i].x = 1 - uv[i].x;
            //    }
            //    OBJExporter.Export(objPath, mesh.vertices, uv, null, mesh.triangles);
            //}
            var parent = new GameObject("lod tile root");
            planePrefab.transform.parent = parent.transform;
            var collider = planePrefab.GetComponent<UnityEngine.Collider>();
            GameObject.DestroyImmediate(collider);
            var filter = planePrefab.GetComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var meshRenderer = planePrefab.GetComponent<MeshRenderer>();
            bool createMtl = false;
            var mtl = lodSetting.Material;
            if (mtl == null)
            {
                createMtl = true;
                mtl = new Material(m_BakerSetting.DefaultBakingShader);
            }
            //create Material assete
            if (createMtl)
            {
                string mtlPath = string.Format("{0}/{1}.mat", m_BakerSetting.OutputFolder, tilePrefix);
                AssetDatabase.CreateAsset(mtl, mtlPath);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            texture.wrapMode = TextureWrapMode.Clamp;
            mtl.SetTexture(m_BakerSetting.TexturePropertyName, texture);
            meshRenderer.sharedMaterial = mtl;

            var prefabPath = string.Format("{0}/{1}.prefab", m_BakerSetting.OutputFolder, tilePrefix);
            //create prefab asset
            PrefabUtility.SaveAsPrefabAsset(parent, prefabPath, out var success);
            Debug.Assert(success, "Create prefab failed!");

            Object.DestroyImmediate(parent);

            m_UsedPrefabs.Add(prefabPath);

            Debug.Log("Create Prefab " + prefabPath);

            Object.DestroyImmediate(root);
            rt.OnDestroy(true);

            AssetDatabase.Refresh();

            return n == 0 ? -1 : m_UsedPrefabs.Count - 1;
        }

        private Vector3 CalculateLODTilePosition(Vector3 offset, int x, int y, float tileSize)
        {
            return new Vector3(x * tileSize, 0, y * tileSize) + offset;
        }

        private void SaveData(string path)
        {
            if (m_BakedTilesInEachLOD != null)
            {
                ISerializer serializer = ISerializer.CreateBinary();

                serializer.WriteInt32(m_Version, "Version");
                //save prefabs
                serializer.WriteStringList(m_UsedPrefabs, "Use Prefabs");

                //save lod tile data
                serializer.WriteRect(m_BakerSetting.BakingBounds, "Bounds");
                serializer.WriteInt32(m_BakedTilesInEachLOD.Count, "Baked Tiles Count");
                for (int i = 0; i < m_BakedTilesInEachLOD.Count; ++i)
                {
                    int rows = m_BakedTilesInEachLOD[i].GetLength(0);
                    int cols = m_BakedTilesInEachLOD[i].GetLength(1);
                    serializer.WriteInt32(rows, "Rows");
                    serializer.WriteInt32(cols, "Cols");
                    serializer.WriteSingle(m_BakerSetting.LODs[i].LODHeight, "LOD Height");
                    for (int r = 0; r < rows; ++r)
                    {
                        for (int c = 0; c < cols; ++c)
                        {
                            var tileData = m_BakedTilesInEachLOD[i][r, c];
                            if (tileData != null)
                            {
                                serializer.WriteInt32(tileData.PrefabIndex, "Prefab Index");
                            }
                            else
                            {
                                serializer.WriteInt32(-1, "Prefab Index");
                            }
                        }
                    }
                }

                serializer.Uninit();

                EditorHelper.WriteFile(serializer.Data, $"{path}/{WorldDefine.BAKED_TILES_FILE_NAME}.bytes");

                AssetDatabase.SaveAssets();

                AssetDatabase.Refresh();
            }
        }

        private Mesh CreateQuadMesh(string assetPath, float size)
        {
            Mesh mesh = new();
            float xMin = 0;
            float zMin = 0;
            float xMax = xMin + size;
            float zMax = zMin + size;
            mesh.vertices = new Vector3[4]
            {
                new(xMin, 0, zMin),
                new(xMin, 0, zMax),
                new(xMax, 0, zMax),
                new(xMax, 0, zMin),
            };

            float uMin = 0;
            float vMin = 0;
            float uMax = 1;
            float vMax = 1;
            mesh.uv = new Vector2[4]
            {
                new(uMin, vMin),
                new(uMin, vMax),
                new(uMax, vMax),
                new(uMax, vMin),
            };

            mesh.triangles = new int[6]
            {
                0,1,2,0,2,3,
            };

            AssetDatabase.CreateAsset(mesh, assetPath);
            return mesh;
        }

        private Vector2Int FromWorldPositionToCoordinate(Vector3 worldPos, float tileSize)
        {
            return new Vector2Int(Mathf.FloorToInt(worldPos.x / tileSize), Mathf.FloorToInt(worldPos.z / tileSize));
        }

        private List<string> m_UsedPrefabs = new();
        private List<BakedTileData[,]> m_BakedTilesInEachLOD;
        private WorldBakerSetting m_BakerSetting;
        private readonly Dictionary<IWorldBakeable, List<GameObject>> m_GameObjects = new();
        private const int m_Version = 1;

        //lod中每个tile的数据
        private class BakedTileData
        {
            public BakedTileData(Vector3 pos, int index)
            {
                Position = pos;
                PrefabIndex = index;
            }

            //这个tile使用的prefab的index
            public int PrefabIndex;
            //中心点坐标
            public Vector3 Position;
        }
    }
}
