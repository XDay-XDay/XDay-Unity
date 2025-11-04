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
using System.Collections.Generic;
using UnityEditor;

namespace XDay.UtilityAPI.Editor
{
    public partial class CurveRegionCreator
    {
        public class BlockRegion
        {
            public int TerritoryID;
            public Mesh RegionMesh;
        }

        public class BlockEdge
        {
            public int TerritoryID;
            public int NeighbourTerritoryID;
            public Mesh EdgeMesh;
        }

        public class Block
        {
            public int Index;
            public Bounds Bounds;
            public List<BlockRegion> Regions = new List<BlockRegion>();
            public List<BlockEdge> Edges = new List<BlockEdge>();
            public string PrefabPath;
        }

        //合并区域的mesh，生成lod1
        private void CombineTerritoryMesh(float totalWidth, float totalHeight, int horizontalTileCount, int verticalTileCount, bool generateAssets, string folder, int lod)
        {
            //这个texture只是用在prefab展示中，游戏运行时根据mask data来创建新的mask texture
            var maskTexture = CreateMaskTexture(folder, lod, out string maskTexturePath);
            m_LOD1MaskTextureWidth = maskTexture.width;
            m_LOD1MaskTextureHeight = maskTexture.height;
            m_LOD1MaskTextureData = maskTexture.GetPixels32();
            maskTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(maskTexturePath);

            m_Blocks = new List<Block>(horizontalTileCount * verticalTileCount);
            HashSet<int> addedToBlock = new HashSet<int>();

            AssetDatabase.StartAssetEditing();

            try
            {
                float blockWidth = totalWidth / horizontalTileCount;
                float blockHeight = totalHeight / verticalTileCount;
                int blockIndex = 0;
                for (int v = 0; v < verticalTileCount; ++v)
                {
                    for (int h = 0; h < horizontalTileCount; ++h)
                    {
                        float minX = blockWidth * h;
                        float minY = blockHeight * v;
                        float maxX = minX + blockWidth;
                        float maxY = minY + blockHeight;
                        var blockBounds = new Bounds(new Vector3((minX + maxX) * 0.5f, 0, (minY + maxY) * 0.5f), new Vector3(blockWidth, 0, blockHeight));

                        var block = new Block();
                        block.Index = blockIndex;
                        m_Blocks.Add(block);

                        for (int i = 0; i < m_Territories.Count; ++i)
                        {
                            //check region
                            var t = m_Territories[i];
                            GameObject regionObject = t.GetGameObject(Territory.ObjectType.Region);
                            Debug.Assert(regionObject != null);
                            if (!addedToBlock.Contains(regionObject.GetInstanceID()))
                            {
                                Mesh regionMesh = regionObject.GetComponent<MeshFilter>().sharedMesh;
                                Bounds meshBounds = regionMesh.bounds;
                                if (meshBounds.Intersects(blockBounds))
                                {
                                    addedToBlock.Add(regionObject.GetInstanceID());
                                    var blockRegion = new BlockRegion();
                                    blockRegion.TerritoryID = t.RegionID;
                                    blockRegion.RegionMesh = regionMesh;
                                    block.Regions.Add(blockRegion);
                                }
                            }

                            //check edge
                            var sharedEdges = t.SharedEdges;
                            if (sharedEdges == null)
                            {
                                //process combined edges
                                Mesh edgeMesh = t.CombinedEdge.Mesh;
                                Bounds edgeBounds = edgeMesh.bounds;
                                var edgeRect = Helper.ToRect(edgeBounds);
                                var blockRect = Helper.ToRect(blockBounds);
                                if (edgeRect.Overlaps(blockRect))
                                {
                                    var blockEdge = new BlockEdge();
                                    blockEdge.TerritoryID = t.RegionID;
                                    blockEdge.NeighbourTerritoryID = 0;
                                    blockEdge.EdgeMesh = edgeMesh;
                                    block.Edges.Add(blockEdge);
                                }
                            }
                            else
                            {
                                for (int e = 0; e < sharedEdges.Count; ++e)
                                {
                                    var obj = sharedEdges[e].GameObject;
                                    if (!addedToBlock.Contains(obj.GetInstanceID()))
                                    {
                                        Mesh edgeMesh = obj.GetComponent<MeshFilter>().sharedMesh;
                                        Bounds edgeBounds;
                                        if (edgeMesh == null)
                                        {
                                            var mirroredSharedEdge = GetSharedEdge(sharedEdges[e].NeighbourRegionID, sharedEdges[e].SelfRegionID);
                                            var mirrorEdgeMesh = mirroredSharedEdge.GameObject.GetComponent<MeshFilter>().sharedMesh;
                                            edgeBounds = mirrorEdgeMesh.bounds;
                                        }
                                        else
                                        {
                                            edgeBounds = edgeMesh.bounds;
                                        }
                                        var edgeRect = Helper.ToRect(edgeBounds);
                                        var blockRect = Helper.ToRect(blockBounds);
                                        if (edgeRect.Overlaps(blockRect))
                                        {
                                            addedToBlock.Add(obj.GetInstanceID());
                                            var blockEdge = new BlockEdge();
                                            blockEdge.TerritoryID = t.RegionID;
                                            blockEdge.NeighbourTerritoryID = sharedEdges[e].NeighbourRegionID;
                                            blockEdge.EdgeMesh = edgeMesh;
                                            block.Edges.Add(blockEdge);
                                        }
                                    }
                                }
                            }
                        }

                        CombineBlock(block, generateAssets, folder, lod, maskTexture);
                        ++blockIndex;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh();
        }

        private SharedEdgeWithNeighbourTerritroy GetSharedEdge(int territoryID, int neighbourTerritoryID)
        {
            foreach (var t in m_Territories)
            {
                foreach (var edge in t.SharedEdges)
                {
                    if (edge.SelfRegionID == territoryID && edge.NeighbourRegionID == neighbourTerritoryID)
                    {
                        return edge;
                    }
                }
            }
            return null;
        }

        private void CombineBlock(Block block, bool generateAssets, string folder, int lod, Texture2D maskTexture)
        {
            var combinedMesh = new Mesh();
            CalculateBlockMeshBoundsAndCenter(block, out Vector3 center, out Bounds bounds);
            block.Bounds = bounds;

            List<Vector3> positions = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> indices = new List<int>();
            List<Vector3> combinedPositions = new List<Vector3>();
            List<Vector4> combinedUVs = new List<Vector4>();
            List<int> combinedIndices = new List<int>();
            //combine regions
            foreach (var region in block.Regions)
            {
                int offset = combinedPositions.Count;
                int regionIndex = GetTerritoryIndex(region.TerritoryID);
                region.RegionMesh.GetVertices(positions);
                region.RegionMesh.GetUVs(0, uvs);
                if (uvs.Count == 0)
                {
                    for (int i = 0; i < positions.Count; ++i)
                    {
                        uvs.Add(Vector2.zero);
                    }
                }
                region.RegionMesh.GetIndices(indices, 0);
                for (int i = 0; i < positions.Count; ++i)
                {
                    combinedPositions.Add(positions[i] - center);
                }
                for (int i = 0; i < uvs.Count; ++i)
                {
                    combinedUVs.Add(new Vector4(uvs[i].x, uvs[i].y, regionIndex, 1.0f));
                }
                for (int i = 0; i < indices.Count; ++i)
                {
                    combinedIndices.Add(indices[i] + offset);
                }
            }

            //combine edges
            foreach (var edge in block.Edges)
            {
                if (edge.EdgeMesh == null)
                {
                    continue;
                }
                int edgeIndex = GetEdgeIndex(edge.TerritoryID, edge.NeighbourTerritoryID);
                int offset = combinedPositions.Count;
                edge.EdgeMesh.GetVertices(positions);
                edge.EdgeMesh.GetUVs(0, uvs);
                edge.EdgeMesh.GetIndices(indices, 0);
                for (int i = 0; i < positions.Count; ++i)
                {
                    combinedPositions.Add(positions[i] - center);
                }
                for (int i = 0; i < uvs.Count; ++i)
                {
                    combinedUVs.Add(new Vector4(uvs[i].x, uvs[i].y * 0.5f, edgeIndex + m_Territories.Count, 0));
                }
                for (int i = 0; i < indices.Count; ++i)
                {
                    combinedIndices.Add(indices[i] + offset);
                }
            }

            combinedMesh.SetVertices(combinedPositions);
            combinedMesh.SetUVs(0, combinedUVs);
            combinedMesh.SetIndices(combinedIndices, MeshTopology.Triangles, 0);
            combinedMesh.RecalculateBounds();
            combinedMesh.UploadMeshData(true);

            block.PrefabPath = $"{folder}/combined_region_prefab_{block.Index}_lod{lod}.prefab";
            if (generateAssets && !string.IsNullOrEmpty(folder) && combinedPositions.Count > 0)
            {
                string meshPath = $"{folder}/combined_region_lod{lod}_mesh_{block.Index}.asset";

                if (m_Input.Settings.GenerateUnityAssets)
                {
                    AssetDatabase.CreateAsset(combinedMesh, meshPath);

                    var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    var filter = obj.GetComponent<MeshFilter>();
                    filter.sharedMesh = combinedMesh;
                    var renderer = obj.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = m_Input.Settings.RegionMaterial;
                    if (renderer.sharedMaterial != null)
                    {
                        renderer.sharedMaterial.SetTexture("_Mask", maskTexture);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("出错了", "需要设置LOD1的Region Material参数, shader需要有_Mask", "确定");
                    }

                    PrefabUtility.SaveAsPrefabAsset(obj, block.PrefabPath);

                    Helper.DestroyUnityObject(obj);
                }
                else
                {
                    var mtl = m_Input.Settings.RegionMaterial;
                    Debug.Assert(false, "todo");
                    //AddMeshAsset(combinedMesh, meshPath);
                    //AddPrefabAsset(meshPath, AssetDatabase.GetAssetPath(mtl), block.prefabPath);
                    if (mtl == null)
                    {
                        EditorUtility.DisplayDialog("出错了", "需要设置LOD1的Region Material参数, shader需要有_Mask", "确定");
                    }
                }
            }
        }

        private Texture2D CreateMaskTexture(string folder, int lod, out string texturePath)
        {
            int totalPixelCount = m_EdgeAssetsInfo.Count + m_Territories.Count;
            Vector2Int textureSize = Helper.NextPOTSize(totalPixelCount);
            Texture2D texture = new Texture2D(textureSize.x, textureSize.y, TextureFormat.RGB24, false);
            texture.filterMode = FilterMode.Point;
            for (int i = 0; i < m_Territories.Count; ++i)
            {
                int x = i % textureSize.x;
                int y = i / textureSize.x;
                texture.SetPixel(x, y, m_Territories[i].Color);
            }

            for (int i = 0; i < m_EdgeAssetsInfo.Count; ++i)
            {
                int pixelOffset = i + m_Territories.Count;
                int x = pixelOffset % textureSize.x;
                int y = pixelOffset / textureSize.x;
                texture.SetPixel(x, y, new Color(0, 0, 0, 0));
            }

            texture.Apply();

            texturePath = $"{folder}/combined_region_lod{lod}_mask.tga";
            byte[] bytes = texture.EncodeToTGA();
            System.IO.File.WriteAllBytes(texturePath, bytes);

            AssetDatabase.ImportAsset(texturePath);
            //AssetDatabase.Refresh();

            return texture;
        }

        private void CalculateBlockMeshBoundsAndCenter(Block block, out Vector3 center, out Bounds bounds)
        {
            List<Vector3> positions = new List<Vector3>();
            float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
            //combine regions
            foreach (var region in block.Regions)
            {
                var mesh = region.RegionMesh;
                region.RegionMesh.GetVertices(positions);
                for (int i = 0; i < positions.Count; ++i)
                {
                    var pos = positions[i];
                    if (pos.x < minX)
                    {
                        minX = pos.x;
                    }
                    if (pos.y < minY)
                    {
                        minY = pos.y;
                    }
                    if (pos.z < minZ)
                    {
                        minZ = pos.z;
                    }
                    if (pos.x > maxX)
                    {
                        maxX = pos.x;
                    }
                    if (pos.y > maxY)
                    {
                        maxY = pos.y;
                    }
                    if (pos.z > maxZ)
                    {
                        maxZ = pos.z;
                    }
                }
            }

            //combine edges
            foreach (var edge in block.Edges)
            {
                if (edge.EdgeMesh != null)
                {
                    edge.EdgeMesh.GetVertices(positions);
                    for (int i = 0; i < positions.Count; ++i)
                    {
                        var pos = positions[i];
                        if (pos.x < minX)
                        {
                            minX = pos.x;
                        }
                        if (pos.y < minY)
                        {
                            minY = pos.y;
                        }
                        if (pos.z < minZ)
                        {
                            minZ = pos.z;
                        }
                        if (pos.x > maxX)
                        {
                            maxX = pos.x;
                        }
                        if (pos.y > maxY)
                        {
                            maxY = pos.y;
                        }
                        if (pos.z > maxZ)
                        {
                            maxZ = pos.z;
                        }
                    }
                }
            }

            center = new Vector3((minX + maxX) * 0.5f, 0, (minZ + maxZ) * 0.5f);
            bounds = new Bounds(center, new Vector3(maxX - minX, 0, maxZ - minZ));
        }

        private int GetTerritoryIndex(int territoryID)
        {
            for (int i = 0; i < m_Territories.Count; ++i)
            {
                if (m_Territories[i].RegionID == territoryID)
                {
                    return i;
                }
            }
            Debug.Assert(false, "Can't be here!");
            return -1;
        }

        private int GetEdgeIndex(int territoryID, int neighbourTerritoryID)
        {
            for (int i = 0; i < m_EdgeAssetsInfo.Count; ++i)
            {
                if (m_EdgeAssetsInfo[i].TerritoryID == territoryID &&
                    m_EdgeAssetsInfo[i].NeighbourTerritoyID == neighbourTerritoryID)
                {
                    return i;
                }
            }
            Debug.Assert(false, "Can't be here!");
            return -1;
        }
    }
}

