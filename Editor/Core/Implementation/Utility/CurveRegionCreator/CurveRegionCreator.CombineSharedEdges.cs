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
using UnityEditor;
using System.Collections.Generic;

namespace XDay.UtilityAPI.Editor
{
    public partial class CurveRegionCreator
    {
        private class SharedEdgeInfo
        {
            public int SelfTerritoryID;
            public int NeighbourTerritoryID;
            public Mesh Mesh;
            public string OriginalPrefabPath;
            public string OriginalMeshPath;
        }

        private class ModifiedEndPointVertex
        {
            public ModifiedEndPointVertex(Vector3 oldPos, Vector3 newPos)
            {
                OldPosition = oldPos;
                NewPosition = newPos;
            }

            public Vector3 OldPosition;
            public Vector3 NewPosition;
        }

        private class SharedEdgePair
        {
            public List<SharedEdgeInfo> EdgePair = new List<SharedEdgeInfo>();
        }

        private void CombineTerritorySharedEdges(bool generateAssets, int lod, bool useMultithreading)
        {
            if (useMultithreading)
            {
                Debug.Assert(false, "todo");
                //CombineTerritorySharedEdgesMultiThreading(generateAssets, lod);
            }
            else
            {
                CombineTerritorySharedEdgesSingleThread(generateAssets, lod);
            }
        }

        private void CombineTerritorySharedEdgesSingleThread(bool generateAssets, int lod)
        {
            if (lod == 1 || m_Input.Settings.CombineEdgesOfOneRegion)
            {
                generateAssets = false;
            }

            m_ModifiedEndPoints.Clear();
            m_FixedEdgeMeshies.Clear();

            List<SharedEdgePair> allSharedEdgePairs = new List<SharedEdgePair>();
            for (int i = 0; i < m_Territories.Count; ++i)
            {
                var sharedEdges = m_Territories[i].SharedEdges;
                foreach (var edge in sharedEdges)
                {
                    if (edge.NeighbourRegionID != 0)
                    {
                        SharedEdgePair pair = FindEdgePair(allSharedEdgePairs, edge);
                        if (pair == null)
                        {
                            pair = new SharedEdgePair();
                            allSharedEdgePairs.Add(pair);
                        }
                        var sharedEdgeInfo = new SharedEdgeInfo();
                        sharedEdgeInfo.SelfTerritoryID = edge.SelfRegionID;
                        sharedEdgeInfo.NeighbourTerritoryID = edge.NeighbourRegionID;
                        sharedEdgeInfo.Mesh = edge.GameObject.GetComponent<MeshFilter>().sharedMesh;
                        sharedEdgeInfo.OriginalPrefabPath = edge.PrefabPath;
                        sharedEdgeInfo.OriginalMeshPath = edge.MeshPath;
                        pair.EdgePair.Add(sharedEdgeInfo);
                    }
                }
            }

            //modify mesh
            foreach (var pair in allSharedEdgePairs)
            {
                ModifyEdgePairMesh(pair, generateAssets, lod);
            }

            //修改neighbour territory id 为0的edge的头尾顶点坐标,保证能衔接上合并成一条边的shared edge
            foreach (var p in m_ModifiedEndPoints)
            {
                foreach (var t in m_Territories)
                {
                    foreach (var edge in t.SharedEdges)
                    {
                        if (edge.NeighbourRegionID == 0)
                        {
                            FixEdgeEndPoint(edge, p, generateAssets);
                        }
                    }
                }
            }

            foreach (var edge in m_FixedEdgeMeshies)
            {
                if (generateAssets)
                {
                    if (m_Input.Settings.GenerateUnityAssets)
                    {
                        //Debug.LogError($"create asset in FixEdgeEndPoint {edge.meshPath}");
                        //create edge mesh
                        var mesh = edge.GameObject.GetComponent<MeshFilter>().sharedMesh;
                        var localMesh = CreateLocalMesh(mesh, edge.SelfRegionID);
                        AssetDatabase.CreateAsset(localMesh, edge.MeshPath);

                        string name = Helper.GetPathName(edge.PrefabPath, false);
                        var edgeObj = new GameObject(name);
                        edgeObj.transform.position = new Vector3(0, 0, 0);
                        var renderer = edgeObj.AddComponent<MeshRenderer>();
                        var filter = edgeObj.AddComponent<MeshFilter>();
                        filter.sharedMesh = localMesh;
                        renderer.sharedMaterial = m_Input.Settings.EdgeMaterial;
                        PrefabUtility.SaveAsPrefabAsset(edgeObj, edge.PrefabPath);
                        Helper.DestroyUnityObject(edgeObj);
                    }
                    else
                    {
                        //生成自定义格式
                        var mesh = edge.GameObject.GetComponent<MeshFilter>().sharedMesh;
                        var localMesh = CreateLocalMesh(mesh, edge.SelfRegionID);

                        Debug.Assert(false, "todo");
                        //AddMeshAsset(localMesh, edge.meshPath);
                        //AddPrefabAsset(edge.meshPath, AssetDatabase.GetAssetPath(m_Input.settings.edgeMaterial), edge.prefabPath);
                    }
                }
            }
        }

#if false
        void CombineTerritorySharedEdgesMultiThreading(bool generateAssets, int lod)
        {
            mModifiedEndPoints.Clear();
            mFixedEdgeMeshies.Clear();

            List<SharedEdgePair> allSharedEdgePairs = new List<SharedEdgePair>();
            for (int i = 0; i < m_Regions.Count; ++i)
            {
                var sharedEdges = m_Regions[i].sharedEdges;
                foreach (var edge in sharedEdges)
                {
                    if (edge.neighbourRegionID != 0)
                    {
                        SharedEdgePair pair = FindEdgePair(allSharedEdgePairs, edge);
                        if (pair == null)
                        {
                            pair = new SharedEdgePair();
                            allSharedEdgePairs.Add(pair);
                        }
                        var sharedEdgeInfo = new SharedEdgeInfo();
                        sharedEdgeInfo.selfTerritoryID = edge.selfRegionID;
                        sharedEdgeInfo.neighbourTerritoryID = edge.neighbourRegionID;
                        sharedEdgeInfo.mesh = edge.gameObject.GetComponent<MeshFilter>().sharedMesh;
                        sharedEdgeInfo.originalPrefabPath = edge.prefabPath;
                        sharedEdgeInfo.originalMeshPath = edge.meshPath;
                        pair.edgePair.Add(sharedEdgeInfo);
                    }
                }
            }

            //modify mesh
            foreach (var pair in allSharedEdgePairs)
            {
                ModifyEdgePairMesh(pair, generateAssets, lod);
            }

            //修改neighbour territory id 为0的edge的头尾顶点坐标,保证能衔接上合并成一条边的shared edge
            foreach (var p in mModifiedEndPoints)
            {
                foreach (var t in m_Regions)
                {
                    foreach (var edge in t.sharedEdges)
                    {
                        if (edge.neighbourRegionID == 0)
                        {
                            FixEdgeEndPoint(edge, p, generateAssets);
                        }
                    }
                }
            }

            foreach (var edge in mFixedEdgeMeshies)
            {
                if (generateAssets)
                {
                    //Debug.LogError($"create asset in FixEdgeEndPoint {edge.meshPath}");
                    //create edge mesh
                    var mesh = edge.gameObject.GetComponent<MeshFilter>().sharedMesh;
                    var localMesh = CreateLocalMesh(mesh, edge.selfRegionID);
                    AssetDatabase.CreateAsset(localMesh, edge.meshPath);

                    string name = Utils.GetPathName(edge.prefabPath, false);
                    var edgeObj = new GameObject(name);
                    edgeObj.transform.position = new Vector3(0, 0, 0);
                    var renderer = edgeObj.AddComponent<MeshRenderer>();
                    var filter = edgeObj.AddComponent<MeshFilter>();
                    filter.sharedMesh = localMesh;
                    renderer.sharedMaterial = mInput.settings.edgeMaterial;
                    PrefabUtility.SaveAsPrefabAsset(edgeObj, edge.prefabPath);
                    Helper.DestroyUnityObject(edgeObj);
                }
            }
        }
#endif

        private void FixEdgeEndPoint(SharedEdgeWithNeighbourTerritroy edge, ModifiedEndPointVertex p, bool generateAssets)
        {
            if (edge.FixedEdgePointCount == 2)
            {
                return;
            }

            var mesh = edge.GameObject.GetComponent<MeshFilter>().sharedMesh;
            var vertices = mesh.vertices;

            bool changed = false;
            if (Helper.ToVector2(vertices[1]) == Helper.ToVector2(p.OldPosition))
            {
                vertices[1] = p.NewPosition;
                changed = true;
            }
            if (Helper.ToVector2(vertices[vertices.Length - 1]) == Helper.ToVector2(p.OldPosition))
            {
                vertices[vertices.Length - 1] = p.NewPosition;
                changed = true;
            }
            if (changed)
            {
                float edgeHeight = m_Input.Settings.EdgeHeight;
                for (int i = 0; i < vertices.Length; ++i)
                {
                    vertices[i] = new Vector3(vertices[i].x, edgeHeight, vertices[i].z);
                }
                mesh.vertices = vertices;

                ++edge.FixedEdgePointCount;
                if (m_FixedEdgeMeshies.Contains(edge) == false)
                {
                    m_FixedEdgeMeshies.Add(edge);
                }
            }
        }

        private void ModifyEdgePairMesh(SharedEdgePair pair, bool generateAssets, int lod)
        {
            var edge0 = pair.EdgePair[0];
            var edge1 = pair.EdgePair[1];
            var vertices0 = edge0.Mesh.vertices;
            var vertices1 = edge1.Mesh.vertices;
            List<Vector2> uvs0 = new List<Vector2>();
            List<Vector2> uvs1 = new List<Vector2>();
            edge0.Mesh.GetUVs(0, uvs0);
            edge1.Mesh.GetUVs(0, uvs1);
            //缩短线段宽度
            for (int i = 0; i < vertices0.Length; ++i)
            {
                if (i % 2 == 1)
                {
                    var oldPos = vertices0[i];

                    var d = vertices0[i] - vertices0[i - 1];
                    vertices0[i] = vertices0[i - 1] + d * 0.5f;

                    if (i == 1 || i == vertices0.Length - 1)
                    {
                        //把被修改过的点保存,后续用来修改相同坐标的顶点
                        m_ModifiedEndPoints.Add(new ModifiedEndPointVertex(oldPos, vertices0[i]));
                    }
                }
            }
            for (int i = 0; i < vertices1.Length; ++i)
            {
                if (i % 2 == 1)
                {
                    var oldPos = vertices1[i];

                    var d = vertices1[i] - vertices1[i - 1];
                    vertices1[i] = vertices1[i - 1] + d * 0.5f;

                    if (i == 1 || i == vertices1.Length - 1)
                    {
                        //把被修改过的点保存,后续用来修改相同坐标的顶点
                        m_ModifiedEndPoints.Add(new ModifiedEndPointVertex(oldPos, vertices1[i]));
                    }
                }
            }

            List<Vector3> allVertices = new List<Vector3>();
            List<Vector2> allUVs = new List<Vector2>();
            List<int> allIndices = new List<int>();
            allUVs.AddRange(uvs0);
            allUVs.AddRange(uvs1);
            allVertices.AddRange(vertices0);
            allVertices.AddRange(vertices1);
            allIndices.AddRange(edge0.Mesh.triangles);
            var indices1 = edge1.Mesh.triangles;
            foreach (var idx in indices1)
            {
                allIndices.Add(idx + vertices0.Length);
            }

            List<Vector3> combinedVertices = new List<Vector3>();
            List<Vector2> combinedUVs = new List<Vector2>();
            List<int> combinedIndices = new List<int>();
            Combine(allVertices, allUVs, allIndices, vertices0.Length, combinedVertices, combinedUVs, combinedIndices);

            Mesh mesh = new Mesh();

            float edgeHeight = m_Input.Settings.EdgeHeight;
            for (int i = 0; i < combinedVertices.Count; ++i)
            {
                combinedVertices[i] = new Vector3(combinedVertices[i].x, edgeHeight, combinedVertices[i].z);
            }

            mesh.SetVertices(combinedVertices);
            mesh.triangles = combinedIndices.ToArray();
            mesh.SetUVs(0, combinedUVs);

            var t0 = GetTerritory(edge0.SelfTerritoryID);
            var t1 = GetTerritory(edge1.SelfTerritoryID);
            var sharedEdge0 = t0.GetSharedEdge(edge0.SelfTerritoryID, edge0.NeighbourTerritoryID);
            var sharedEdge1 = t1.GetSharedEdge(edge1.SelfTerritoryID, edge1.NeighbourTerritoryID);

            sharedEdge0.GameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
            //将另外一条边的mesh设置为null
            //if (lod == 1)
            {
                sharedEdge1.GameObject.GetComponent<MeshFilter>().sharedMesh = null;
            }

            if (generateAssets)
            {
                if (m_Input.Settings.GenerateUnityAssets)
                {
                    //create edge mesh
                    var edgeInfo = pair.EdgePair[0];
                    var localMesh = CreateLocalMesh(mesh, edgeInfo.SelfTerritoryID);
                    AssetDatabase.CreateAsset(localMesh, edgeInfo.OriginalMeshPath);

                    //Debug.LogError($"create asset in ModifyEdgePairMesh {edgeInfo.originalMeshPath}");

                    string name = Helper.GetPathName(edgeInfo.OriginalPrefabPath, false);
                    var edgeObj = new GameObject(name);
                    edgeObj.transform.position = new Vector3(0, 0, 0);
                    var renderer = edgeObj.AddComponent<MeshRenderer>();
                    var filter = edgeObj.AddComponent<MeshFilter>();
                    filter.sharedMesh = localMesh;
                    renderer.sharedMaterial = m_Input.Settings.EdgeMaterial;
                    PrefabUtility.SaveAsPrefabAsset(edgeObj, edgeInfo.OriginalPrefabPath);
                    Helper.DestroyUnityObject(edgeObj);
                }
                else
                {
                    Debug.Assert(false, "todo");
                    var edgeInfo = pair.EdgePair[0];
                    var localMesh = CreateLocalMesh(mesh, edgeInfo.SelfTerritoryID);
                    //AddMeshAsset(localMesh, edgeInfo.originalMeshPath);
                    //AddPrefabAsset(edgeInfo.originalMeshPath, AssetDatabase.GetAssetPath(m_Input.settings.edgeMaterial), edgeInfo.originalPrefabPath);
                }
            }
        }

        private void Combine(List<Vector3> vertices, List<Vector2> uvs, List<int> indices, int splitIndex, List<Vector3> combinedVertices, List<Vector2> combinedUVs, List<int> combinedIndices)
        {
            for (int i = 0; i < indices.Count; ++i)
            {
                var idx = indices[i];
                var pos = vertices[idx];
                int p = combinedVertices.IndexOf(pos);
                if (p == -1)
                {
                    combinedVertices.Add(pos);
                    int originalVertexIndex = vertices.IndexOf(pos);
                    if (originalVertexIndex > splitIndex)
                    {
                        originalVertexIndex -= splitIndex;
                        originalVertexIndex = vertices.Count - splitIndex - 1 - originalVertexIndex;
                    }
                    Vector2 originalUV = uvs[originalVertexIndex];
                    if (idx < splitIndex)
                    {
                        if (idx % 2 == 0)
                        {
                            combinedUVs.Add(new Vector2(originalUV.x, 0.5f));
                        }
                        else
                        {
                            combinedUVs.Add(new Vector2(originalUV.x, 0));
                        }
                    }
                    else
                    {
                        combinedUVs.Add(new Vector4(originalUV.x, 1));
                    }

                    p = combinedVertices.Count - 1;
                }
                combinedIndices.Add(p);
            }
        }

        private SharedEdgePair FindEdgePair(List<SharedEdgePair> edgePairs, SharedEdgeWithNeighbourTerritroy edge)
        {
            foreach (var pair in edgePairs)
            {
                foreach (var edgePair in pair.EdgePair)
                {
                    if ((edgePair.SelfTerritoryID == edge.SelfRegionID && edgePair.NeighbourTerritoryID == edge.NeighbourRegionID) ||
                        (edgePair.SelfTerritoryID == edge.NeighbourRegionID && edgePair.NeighbourTerritoryID == edge.SelfRegionID))
                    {
                        return pair;
                    }
                }
            }
            return null;
        }

        //合并区域的分段edge成一个完整edge
        private void CombineEdgesOfTerritories()
        {
            if (m_Territories.Count > 1)
            {
                m_EdgeAssetsInfo.Clear();
                EditorUtility.DisplayProgressBar("Generating Region Data", $"Combining All Edges Of Territories", 0);
                AssetDatabase.StartAssetEditing();
                try
                {
                    for (int t = 0; t < m_Territories.Count; ++t)
                    {
                        EditorUtility.DisplayProgressBar("Generating Region Data", $"Combining All Edges Of Territories {t}", (float)t / (m_Territories.Count - 1));
                        var territory = m_Territories[t];
                        var edges = territory.SharedEdges;
                        List<CombineInstance> instances = new List<CombineInstance>();
                        var mesh = new Mesh();
                        for (int i = 0; i < edges.Count; ++i)
                        {
                            var meshFilter = edges[i].GameObject.GetComponent<MeshFilter>();
                            var inst = new CombineInstance();
                            inst.mesh = meshFilter.sharedMesh;
                            inst.transform = meshFilter.transform.localToWorldMatrix;
                            instances.Add(inst);

                            //neighbour edge
                            var neighbourTerritory = GetTerritory(edges[i].NeighbourRegionID);
                            if (neighbourTerritory != null)
                            {
                                var neighbourEdge = neighbourTerritory.GetSharedEdge(neighbourTerritory.RegionID, edges[i].SelfRegionID);

                                meshFilter = neighbourEdge.GameObject.GetComponent<MeshFilter>();
                                inst = new CombineInstance();
                                inst.mesh = meshFilter.sharedMesh;
                                inst.transform = meshFilter.transform.localToWorldMatrix;
                                instances.Add(inst);
                            }
                        }
                        mesh.CombineMeshes(instances.ToArray(), true);

                        string edge0PrefabPath = edges[0].PrefabPath;
                        string folder = Helper.GetFolderPath(edge0PrefabPath);
                        string edgePrefabPath = $"{folder}/combined_edge_{territory.RegionID}.prefab";
                        string edgeMeshPath = $"{folder}/combined_edge_{territory.RegionID}.asset";

                        if (m_Input.Settings.GenerateUnityAssets)
                        {
                            var edgeObject = new GameObject($"combined edge of territory {territory.RegionID}");
                            var filter = edgeObject.AddComponent<MeshFilter>();
                            filter.sharedMesh = mesh;
                            var renderer = edgeObject.AddComponent<MeshRenderer>();
                            renderer.sharedMaterial = edges[0].GameObject.GetComponent<MeshRenderer>().sharedMaterial;

                            var localMesh = CreateLocalMesh(mesh, territory.RegionID);
                            AssetDatabase.CreateAsset(localMesh, edgeMeshPath);

                            filter.sharedMesh = localMesh;

                            PrefabUtility.SaveAsPrefabAsset(edgeObject, edgePrefabPath);
                            Helper.DestroyUnityObject(edgeObject);
                        }
                        else
                        {
                            var localMesh = CreateLocalMesh(mesh, territory.RegionID);
                            var mtl = edges[0].GameObject.GetComponent<MeshRenderer>().sharedMaterial;
                            Debug.Assert(false, "todo");
                            //AddMeshAsset(localMesh, edgeMeshPath);
                            //AddPrefabAsset(edgeMeshPath, AssetDatabase.GetAssetPath(mtl), edgePrefabPath);
                        }
                        //reset territory edge data
                        EdgeAssetInfo edgeInfo = new EdgeAssetInfo(territory.RegionID, 0, edgePrefabPath, m_Input.Settings.EdgeMaterial);
                        m_EdgeAssetsInfo.Add(edgeInfo);

                        CombinedEdge edge = new CombinedEdge();
                        edge.Mesh = mesh;
                        edge.SelfRegionID = territory.RegionID;
                        //标记shared edge不再有效,因为被合并了
                        territory.SetCombinedEdge(edge);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }

                AssetDatabase.Refresh();
            }
        }

        private List<ModifiedEndPointVertex> m_ModifiedEndPoints = new List<ModifiedEndPointVertex>();
        private List<SharedEdgeWithNeighbourTerritroy> m_FixedEdgeMeshies = new List<SharedEdgeWithNeighbourTerritroy>();
    }
}

