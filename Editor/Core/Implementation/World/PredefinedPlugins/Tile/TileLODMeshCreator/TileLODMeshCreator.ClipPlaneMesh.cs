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

namespace XDay.WorldAPI.Tile.Editor
{
    internal partial class TileLODMeshCreator
    {
        class PlaneMeshClipState
        {
            public PlaneMeshClipState(int tileX, int tileY)
            {
                this.tileX = tileX;
                this.tileY = tileY;
            }

            public List<Mesh> lodMeshes = new List<Mesh>();
            public int tileX { get; }
            public int tileY { get; }
        }

        public void GeneratePlaneMeshes()
        {
            //calculate clip state
            List<PlaneMeshClipState> clipStates = CalculatePlaneMeshClipStates();

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < clipStates.Count; ++i)
                {
                    EditorUtility.DisplayProgressBar("Create Tile LOD Plane Mesh", $"Creating {i + 1}/{clipStates.Count}", (i + 1) / (float)clipStates.Count);
                    CreateTilePlaneMesh(clipStates[i], m_TileSystem.GenerateObjMesh);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            EditorUtility.ClearProgressBar();

            AssetDatabase.Refresh();
        }

        void CreateTilePlaneMesh(PlaneMeshClipState state, bool generateObjMesh)
        {
            for (int lod = 0; lod < m_TileSystem.LODCount; ++lod)
            {
                var localMesh = state.lodMeshes[lod];

                var path = TileSystemDefine.GetTerrainMeshPath(m_TileSystem.World.GameFolder, state.tileX, state.tileY, lod);

                if (generateObjMesh)
                {
                    Debug.Assert(false, "todo");
                    //OBJExporter.Export(Utils.RemoveExtension(path) + ".obj", localMesh.vertices, localMesh.uv, localMesh.colors, localMesh.triangles);
                }

                localMesh.UploadMeshData(true);

                AssetDatabase.CreateAsset(localMesh, path);
            }
        }

        private List<PlaneMeshClipState> CalculatePlaneMeshClipStates()
        {
            List<PlaneMeshClipState> states = new();
            Debug.Assert(false, "todo");
#if false
            SimpleStopwatch w = new SimpleStopwatch();
            w.Begin();
            EditorUtility.DisplayProgressBar("Calculate Clip State", "Calculating Clip State", 0);

            List<List<Vector3>> clipperPolygons = new List<List<Vector3>>();
            var collisionLayer = Map.currentMap.GetMapLayer<MapCollisionLayer>();
            var clipperCollisions = new List<MapCollisionData>();
            if (collisionLayer != null)
            {
                collisionLayer.GetCollisionsOfType(clipperCollisions, CollisionAttribute.IsTerrainClipper);
                for (int i = 0; i < clipperCollisions.Count; ++i)
                {
                    clipperPolygons.Add(clipperCollisions[i].GetWorldSpaceOutlineVerticesList(PrefabOutlineType.NavMeshObstacle));
                }
            }

            int h = horizontalTileCount;
            int v = verticalTileCount;
            float tileSize = layerData.tileWidth;
            //create lod mesh
            for (int i = 0; i < v; ++i)
            {
                for (int j = 0; j < h; ++j)
                {
                    var tile = GetTile(j, i);
                    if (tile != null)
                    {
                        tile.clipped = false;
                    }

                    if (tile != null && tile.heights == null)
                    {
                        List<Vector3> meshPolygon = new List<Vector3>();
                        var tilePosition = layerData.FromCoordinateToWorldPosition(j, i);
                        meshPolygon.Add(new Vector3(tilePosition.x, 0, tilePosition.z));
                        meshPolygon.Add(new Vector3(tilePosition.x, 0, tilePosition.z + tileSize));
                        meshPolygon.Add(new Vector3(tilePosition.x + tileSize, 0, tilePosition.z + tileSize));
                        meshPolygon.Add(new Vector3(tilePosition.x + tileSize, 0, tilePosition.z));

                        var originalMesh = mLayerView.GetTerrainTileMesh(j, i);
                        Debug.Assert(originalMesh.vertexCount == 4, "如果地表无高度, 只能裁剪顶点数为4的plane mesh");

                        if (GeometricAlgorithm.IsPolygonIntersected(meshPolygon, clipperPolygons))
                        {
                            tile.clipped = true;

                            PlaneMeshClipState state = new PlaneMeshClipState(j, i);
                            states.Add(state);

                            PolygonAlgorithm.GetPolygonDifference(meshPolygon, clipperPolygons, out var noneHoles, out var holes, ClipperLib.PolyFillType.pftNonZero, ClipperLib.PolyFillType.pftNonZero);
                            Triangulator.TriangulatePolygons(noneHoles, holes, false, 0, 0, null, out var vertices, out var indices);

                            for (int lod = 0; lod < lodCount; ++lod)
                            {
                                var mesh = new Mesh();
                                Vector3[] localVertices = OffsetVertices(vertices, tilePosition);
                                mesh.vertices = localVertices;
                                mesh.triangles = indices;
                                InterpolateMeshVertices(originalMesh, mesh, vertices, tilePosition.x, tilePosition.z, tilePosition.x + tileSize,
                                    tilePosition.z + tileSize);
                                state.lodMeshes.Add(mesh);
                            }
                        }
                    }
                }
            }

            var time = w.Stop();
            Debug.Log($"Calculate Plane Mesh Clip State cost: {time} seconds");
#endif
            return states;
        }

        Vector3[] OffsetVertices(Vector3[] globalVertices, Vector3 offset)
        {
            var localVertices = new Vector3[globalVertices.Length];
            //convert to local vertices
            for (int i = 0; i < localVertices.Length; ++i)
            {
                localVertices[i] = globalVertices[i] - offset;
            }
            return localVertices;
        }

        void GetVertexPosition(Vector3[] vertices, out int leftBottomIndex, out int leftTopIndex, out int rightTopIndex, out int rightBottomIndex)
        {
            leftBottomIndex = -1;
            leftTopIndex = -1;
            rightTopIndex = -1;
            rightBottomIndex = -1;

            Debug.Assert(vertices.Length == 4);
            float minX = Mathf.Min(vertices[0].x, vertices[1].x, vertices[2].x, vertices[3].x);
            float minZ = Mathf.Min(vertices[0].z, vertices[1].z, vertices[2].z, vertices[3].z);
            float maxX = Mathf.Max(vertices[0].x, vertices[1].x, vertices[2].x, vertices[3].x);
            float maxZ = Mathf.Max(vertices[0].z, vertices[1].z, vertices[2].z, vertices[3].z);

            for (int i = 0; i < vertices.Length; ++i)
            {
                if (Mathf.Approximately(vertices[i].x, minX) &&
                    Mathf.Approximately(vertices[i].z, minZ))
                {
                    leftBottomIndex = i;
                }
                else if (Mathf.Approximately(vertices[i].x, minX) &&
                    Mathf.Approximately(vertices[i].z, maxZ))
                {
                    leftTopIndex = i;
                }
                else if (Mathf.Approximately(vertices[i].x, maxX) &&
                    Mathf.Approximately(vertices[i].z, maxZ))
                {
                    rightTopIndex = i;
                }
                else if (Mathf.Approximately(vertices[i].x, maxX) &&
                    Mathf.Approximately(vertices[i].z, minZ))
                {
                    rightBottomIndex = i;
                }
            }
        }

        //根据地表原mesh的属性插值
        /*
         * 1 2
         * 0 3
         */
        void InterpolateMeshVertices(Mesh originalMesh, Mesh mesh, Vector3[] vertices, float minX, float minZ, float maxX, float maxZ)
        {
            int vertexCount = vertices.Length;
            bool hasUV0 = originalMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0);
            bool hasUV1 = originalMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord1);
            bool hasColor = originalMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color);
            bool hasNormal = originalMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal);
            if (hasUV0)
            {
                GetVertexPosition(originalMesh.vertices, out int leftBottomIndex, out int leftTopIndex, out int rightTopIndex, out int rightBottomIndex);

                Vector2[] uvs = new Vector2[vertexCount];
                Vector2[] originalUVs = originalMesh.uv;
                for (int i = 0; i < vertexCount; ++i)
                {
                    float x = (vertices[i].x - minX) / (maxX - minX);
                    float y = (vertices[i].z - minZ) / (maxZ - minZ);

                    float u0 = Mathf.Lerp(originalUVs[leftBottomIndex].x, originalUVs[rightBottomIndex].x, x);
                    float u1 = Mathf.Lerp(originalUVs[leftTopIndex].x, originalUVs[rightTopIndex].x, x);
                    float v0 = Mathf.Lerp(originalUVs[leftBottomIndex].y, originalUVs[rightBottomIndex].y, x);
                    float v1 = Mathf.Lerp(originalUVs[leftTopIndex].y, originalUVs[rightTopIndex].y, x);
                    float u = Mathf.Lerp(u0, u1, y);
                    float v = Mathf.Lerp(v0, v1, y);
                    uvs[i] = new Vector2(u, v);
                }
                mesh.uv = uvs;
            }
            if (hasNormal)
            {
                mesh.RecalculateNormals();
            }

            Debug.Assert(hasColor == false, "color not exported");
            Debug.Assert(hasUV1 == false, "uv1 not exported");
        }
    }
}


