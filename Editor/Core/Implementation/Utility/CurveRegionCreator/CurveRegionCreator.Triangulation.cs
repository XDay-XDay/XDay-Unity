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
using System.Threading.Tasks;

namespace XDay.UtilityAPI.Editor
{
    public partial class CurveRegionCreator
    {
        public void TriangulateTerritories(string folder, int lod, bool displayProgressBar, bool generateAssets, bool useMultithreading)
        {
            if (useMultithreading)
            {
                if (lod == 1)
                {
                    //不生成lod1的资源
                    generateAssets = false;
                }
                Task[] tasks = new Task[m_Territories.Count];
                for (int i = 0; i < m_Territories.Count; ++i)
                {
                    int idx = i;
                    var task = Task.Run(() =>
                    {
                        TriangulateTerritoryMultithreadingStep1(m_Territories[idx], folder, lod, generateAssets);
                    });

                    tasks[i] = task;
                }

                Task.WaitAll(tasks);

                for (int i = 0; i < m_Territories.Count; ++i)
                {
                    TriangulateTerritoryMultithreadingStep3(m_Territories[i], folder, lod, generateAssets);
                    TriangulateTerritoryMultithreadingStep4(m_Territories[i], folder, lod, generateAssets);
                }
            }
            else
            {
                for (int i = 0; i < m_Territories.Count; ++i)
                {
                    TriangulateTerritory(m_Territories[i], folder, lod, generateAssets);
                    if (displayProgressBar)
                    {
                        bool cancel = EditorUtility.DisplayCancelableProgressBar("Generating Region Data", $"Triangulating Territory {i + 1}/{m_Territories.Count}", 0.5f + (float)(i + 1) / m_Territories.Count);
                        if (cancel)
                        {
                            break;
                        }
                    }
                }
            }
        }

        private void CalculateTerritoryAllSharedEdgeSplineVertices(Territory t)
        {
            for (int i = 0; i < t.SharedEdges.Count; ++i)
            {
                CalculateSharedEdgeSplineVertices(t.SharedEdges[i], t.RegionID);
            }
        }

        private void CalculateSharedEdgeSplineVertices(SharedEdgeWithNeighbourTerritroy sharedEdge, int regionID)
        {
            List<Vector3> vertices = new List<Vector3>();
            for (int i = 0; i < sharedEdge.ControlPoints.Count - 1; ++i)
            {
                var s = sharedEdge.ControlPoints[i].Position;
                var e = sharedEdge.ControlPoints[i + 1].Position;
                var evaluatedPoints = FindPoints(s, e);
                foreach (var p in evaluatedPoints)
                {
                    if (!vertices.Contains(p.Pos))
                    {
                        vertices.Add(p.Pos);
                    }
                }
            }
            sharedEdge.EvaluatedVertices = vertices;

            //EditorUtils.CreateDrawLineStrip($"shared edge {regionID}", vertices, mInput.settings.vertexDisplayRadius);
        }

        private void TriangulateTerritory(Territory territory, string folder, int lod, bool generateAssets)
        {
            float yOffset = 1.0f;
            SimpleStopwatch w = new();
            w.Begin();
            //计算一个territory所有shared edge的顶点
            CalculateTerritoryAllSharedEdgeSplineVertices(territory);
            w.Stop();
            Debug.Log($"CalculateTerritoryAllSharedEdgeSplineVertices elapsed time: {w.ElapsedSeconds} seconds");

            //生成inner outline和edge mesh
            territory.InnerOutline = CreateInnerOutlineMesh(territory, territory.Outline, folder, lod, generateAssets);
#if false
            var innerOutlineObj = new GameObject($"new Inner Outline");
            var dp = innerOutlineObj.AddComponent<DrawPolygon>();
            dp.radius = mInput.settings.vertexDisplayRadius;
            dp.SetVertices(territory.innerOutline);
            innerOutlineObj.transform.SetParent(mRoot.transform, true);
            territory.SetGameObject(Territory.ObjectType.InnerOutline, innerOutlineObj);
#endif

            w.Begin();
            if (!string.IsNullOrEmpty(folder))
            {
                //内部区域mesh
                //先保证innerOutline没有self intersection
                //var validInnerOutline = RemoveSelfIntersection(territory.innerOutline);
                //var dpd = EditorUtils.CreateDrawPolygon("remove intersection", validInnerOutline, 1);

                PolygonTriangulator.Triangulate(territory.Outline, out Vector3[] meshVertices, out int[] meshIndices);
                var mesh = new Mesh();
                mesh.SetVertices(meshVertices);
                if (m_Input.Settings.UseVertexColorForRegionMesh)
                {
                    mesh.SetColors(CreateVertexColor(territory.Color, meshVertices.Length));
                }
                mesh.SetIndices(meshIndices, MeshTopology.Triangles, 0);
                mesh.UploadMeshData(true);
                var obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                obj.transform.position = new Vector3(0, yOffset, 0);
                obj.name = $"territory mesh {territory.RegionID}";
                obj.transform.SetParent(m_Root.transform, true);
                var filter = obj.GetComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                obj.transform.SetParent(m_Root.transform, true);
                var renderer = obj.GetComponent<MeshRenderer>();
                var mtl = m_Input.Settings.RegionMaterial;
                if (mtl == null)
                {
                    mtl = new Material(Shader.Find("Unlit/Color"));
                }
                else
                {
                    mtl = Object.Instantiate(mtl);
                }
                mtl.color = territory.Color;
                renderer.sharedMaterial = mtl;
                territory.SetGameObject(Territory.ObjectType.Region, obj);

                if (generateAssets && m_Input.Settings.CombineMesh == false)
                {
                    string path = $"{folder}/region_mesh_{territory.RegionID}_lod{lod}.asset";
                    string name = $"region_{territory.RegionID}_lod{lod}";
                    territory.PrefabPath = $"{folder}/{name}.prefab";
                    var localMesh = CreateLocalMesh(mesh, territory.RegionID);
                    if (m_Input.Settings.GenerateUnityAssets)
                    {
                        AssetDatabase.CreateAsset(localMesh, path);
                        var regionObj = new GameObject(name);
                        regionObj.transform.position = new Vector3(0, 0, 0);
                        var objRenderer = regionObj.AddComponent<MeshRenderer>();
                        objRenderer.sharedMaterial = m_Input.Settings.RegionMaterial;
                        var regionFilter = regionObj.AddComponent<MeshFilter>();
                        regionFilter.sharedMesh = localMesh;
                        PrefabUtility.SaveAsPrefabAsset(regionObj, territory.PrefabPath);
                        Helper.DestroyUnityObject(regionObj);
                    }
                    else
                    {
                        Debug.Assert(false, "todo");
                        //AddMeshAsset(localMesh, path);
                        //AddPrefabAsset(path, AssetDatabase.GetAssetPath(m_Input.settings.regionMaterial), territory.prefabPath);
                    }
                }
            }

            w.Stop();
            Debug.Log($"create region mesh elapsed time: {w.ElapsedSeconds} seconds");
        }

        private List<Color> CreateVertexColor(Color color, int n)
        {
            List<Color> colors = new List<Color>(n);
            for (int i = 0; i < n; ++i)
            {
                colors.Add(color);
            }
            return colors;
        }

        private List<Vector3> CreateInnerOutlineMesh(Territory territory, List<Vector3> outline, string folder, int lod, bool generateAssets)
        {
            SimpleStopwatch w = new();

            w.Begin();
            float yOffset = 1.0f;
            List<Vector3> newInnerOutline = new List<Vector3>();

            bool isLoop = true;
            List<Vector3> meshVertices = new List<Vector3>();
            List<int> meshIndices = new List<int>();

            Vector3 lastPerp = Vector3.zero;
            if (isLoop)
            {
                var dir = outline[0] - outline[outline.Count - 1];
                dir.Normalize();
                lastPerp = new Vector3(-dir.z, 0, dir.x);
            }
            int nPoints = outline.Count;
            for (int s = 0; s < nPoints; ++s)
            {
                Vector3 cur, next;
                Vector3 dir;
                if (!isLoop && s == nPoints - 1)
                {
                    lastPerp = Vector3.zero;
                    cur = outline[s];
                    next = outline[s - 1];
                    dir = cur - next;
                }
                else
                {
                    cur = outline[s];
                    next = outline[(s + 1) % nPoints];
                    dir = next - cur;
                }
                dir.Normalize();
                var curPerp = new Vector3(-dir.z, 0, dir.x);
                var perp = curPerp + lastPerp;
                perp.Normalize();
                float width = m_Input.Settings.LineWidth;
                float cosTheta = Vector3.Dot(perp, curPerp);
                float realWidth = width / cosTheta;
                lastPerp = curPerp;
                Vector3 v0 = cur;
                Vector3 v1 = cur + perp * realWidth;
                newInnerOutline.Add(v1);
                if (m_Input.Settings.MergeEdge)
                {
                    v0.y = m_Input.Settings.EdgeHeight;
                    v1.y = m_Input.Settings.EdgeHeight;
                }
                meshVertices.Add(v0);
                meshVertices.Add(v1);
            }
            
            int segmentCount = nPoints - 1;
            if (isLoop)
            {
                segmentCount += 1;
                //for uv, we have to make duplicated vertices
                meshVertices.Add(meshVertices[0]);
                meshVertices.Add(meshVertices[1]);
            }
            for (int i = 0; i < segmentCount; ++i)
            {
                int offset = i * 2;
                meshIndices.Add((0 + offset) % meshVertices.Count);
                meshIndices.Add((1 + offset) % meshVertices.Count);
                meshIndices.Add((2 + offset) % meshVertices.Count);
                meshIndices.Add((1 + offset) % meshVertices.Count);
                meshIndices.Add((3 + offset) % meshVertices.Count);
                meshIndices.Add((2 + offset) % meshVertices.Count);
            }
            w.Stop();
            //Debug.Log($"create inner outline elapsed time: {time} seconds");

            w.Begin();
            
            if (territory.SharedEdges.Count > 1)
            {
                //根据shared edge生成断开的edge mesh
                for (int i = 0; i < territory.SharedEdges.Count; ++i)
                {
                    var sharedEdge = territory.SharedEdges[i];

                    int startVertexIndex = territory.Outline.IndexOf(sharedEdge.EvaluatedVertices[0]);
                    Debug.Assert(startVertexIndex >= 0);
                    int vertexCount = sharedEdge.EvaluatedVertices.Count;
                    //calculate edge vertex
                    var edgeVertices = CalculateEdgeVertices(startVertexIndex, vertexCount, meshVertices, outline.Count);
                    //calculate edge indices
                    List<int> edgeIndices = new List<int>();
                    for (int s = 0; s < vertexCount - 1; ++s)
                    {
                        int offset = s * 2;
                        edgeIndices.Add((0 + offset) % edgeVertices.Count);
                        edgeIndices.Add((1 + offset) % edgeVertices.Count);
                        edgeIndices.Add((2 + offset) % edgeVertices.Count);
                        edgeIndices.Add((1 + offset) % edgeVertices.Count);
                        edgeIndices.Add((3 + offset) % edgeVertices.Count);
                        edgeIndices.Add((2 + offset) % edgeVertices.Count);
                    }

                    string name = $"edge_{sharedEdge.SelfRegionID}_{sharedEdge.NeighbourRegionID}_lod{lod}";

                    var edgeMesh = new Mesh();
                    edgeMesh.SetVertices(edgeVertices);
                    var uvs = CreateSplineMeshUVInRange(startVertexIndex, vertexCount, outline);
                    edgeMesh.SetUVs(0, uvs);
                    edgeMesh.SetIndices(edgeIndices, MeshTopology.Triangles, 0);
                    var edgeObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    edgeObj.transform.position = new Vector3(0, yOffset, 0);
                    edgeObj.name = name;
                    var edgeFilter = edgeObj.GetComponent<MeshFilter>();
                    edgeFilter.sharedMesh = edgeMesh;
                    var edgeRenderer = edgeObj.GetComponent<MeshRenderer>();
                    edgeRenderer.sharedMaterial = m_Input.Settings.EdgeMaterial;
                    edgeObj.transform.SetParent(m_Root.transform, true);
                    sharedEdge.GameObject = edgeObj;

                    sharedEdge.PrefabPath = $"{folder}/{name}.prefab";
                    sharedEdge.MeshPath = $"{folder}/edge_mesh_{sharedEdge.SelfRegionID}_{sharedEdge.NeighbourRegionID}_lod{lod}.asset";
                    if (!string.IsNullOrEmpty(folder) &&
                        generateAssets &&
                        m_Input.Settings.CombineMesh == false && 
                        (m_Input.Settings.MergeEdge == false || !m_Input.Settings.ShareEdge))
                    {
                        var localMesh = CreateLocalMesh(edgeMesh, territory.RegionID);
                        if (m_Input.Settings.GenerateUnityAssets)
                        {
                            //create edge mesh
                            AssetDatabase.CreateAsset(localMesh, sharedEdge.MeshPath);

                            var obj = new GameObject(name);
                            obj.transform.position = new Vector3(0, 0, 0);
                            var renderer = obj.AddComponent<MeshRenderer>();
                            var filter = obj.AddComponent<MeshFilter>();
                            filter.sharedMesh = localMesh;
                            renderer.sharedMaterial = m_Input.Settings.EdgeMaterial;
                            PrefabUtility.SaveAsPrefabAsset(obj, sharedEdge.PrefabPath);
                            Helper.DestroyUnityObject(obj);
                        }
                        else
                        {
                            Debug.Assert(false, "todo");
                            //AddMeshAsset(localMesh, sharedEdge.meshPath);
                            //AddPrefabAsset(sharedEdge.meshPath, AssetDatabase.GetAssetPath(m_Input.settings.edgeMaterial), sharedEdge.prefabPath);
                        }
                    }
                    EdgeAssetInfo edgeInfo = new EdgeAssetInfo(sharedEdge.SelfRegionID, sharedEdge.NeighbourRegionID, sharedEdge.PrefabPath, m_Input.Settings.EdgeMaterial);
                    m_EdgeAssetsInfo.Add(edgeInfo);
                }
            }
            else
            {
                string name = $"edge_{territory.RegionID}_0_lod{lod}";

                var edgeMesh = new Mesh();
                edgeMesh.SetVertices(meshVertices);
                var uvs = CreateSplineMeshUV(meshVertices, outline);
                edgeMesh.SetUVs(0, uvs);
                edgeMesh.SetIndices(meshIndices, MeshTopology.Triangles, 0);
                var edgeObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                edgeObj.transform.position = new Vector3(0, yOffset, 0);
                edgeObj.name = name;
                var edgeFilter = edgeObj.GetComponent<MeshFilter>();
                edgeFilter.sharedMesh = edgeMesh;
                var edgeRenderer = edgeObj.GetComponent<MeshRenderer>();
                edgeRenderer.sharedMaterial = m_Input.Settings.EdgeMaterial;
                edgeObj.transform.SetParent(m_Root.transform, true);
                var sharedEdge = territory.SharedEdges[0];
                sharedEdge.GameObject = edgeObj;

                sharedEdge.PrefabPath = $"{folder}/{name}.prefab";
                sharedEdge.MeshPath = $"{folder}/edge_mesh_{territory.RegionID}_0_lod{lod}.asset";
                if (!string.IsNullOrEmpty(folder) && 
                    generateAssets && 
                    m_Input.Settings.CombineMesh == false &&
                    (m_Input.Settings.MergeEdge == false || !m_Input.Settings.ShareEdge))
                {
                    var localMesh = CreateLocalMesh(edgeMesh, territory.RegionID);
                    //create mesh
                    if (m_Input.Settings.GenerateUnityAssets)
                    {
                        AssetDatabase.CreateAsset(localMesh, sharedEdge.MeshPath);

                        var obj = new GameObject(name);
                        obj.transform.position = new Vector3(0, 0, 0);
                        var renderer = obj.AddComponent<MeshRenderer>();
                        renderer.sharedMaterial = m_Input.Settings.EdgeMaterial;
                        var filter = obj.AddComponent<MeshFilter>();
                        filter.sharedMesh = localMesh;
                        PrefabUtility.SaveAsPrefabAsset(obj, sharedEdge.PrefabPath);
                        Helper.DestroyUnityObject(obj);
                    }
                    else
                    {
                        Debug.Assert(false, "todo");
                        //AddMeshAsset(localMesh, sharedEdge.meshPath);
                        //AddPrefabAsset(sharedEdge.meshPath, AssetDatabase.GetAssetPath(m_Input.settings.edgeMaterial), sharedEdge.prefabPath);
                    }
                }

                EdgeAssetInfo edgeInfo = new EdgeAssetInfo(territory.RegionID, 0, sharedEdge.PrefabPath, m_Input.Settings.EdgeMaterial);
                m_EdgeAssetsInfo.Add(edgeInfo);
            }

            w.Stop();
            //Debug.Log($"create edge mesh elapsed time: {time} seconds");

            return newInnerOutline;
        }

        private List<Vector2> CreateSplineMeshUV(List<Vector3> vertices, List<Vector3> outline)
        {
            List<Vector2> uv = new List<Vector2>();
            float len = 0;
            int virtualControlPointCount = vertices.Count / 2;
            float ratio = m_Input.Settings.TextureAspectRatio;
            bool isLoop = true;
            for (int i = 0; i < virtualControlPointCount; ++i)
            {
                float r = (len / m_Input.Settings.LineWidth) / ratio;
                if (isLoop && i == virtualControlPointCount - 1)
                {
                    //将最后一段设置成整数,保证能与第一段相接,但是最后一段的贴图可能会扭曲
                    r = Mathf.CeilToInt(r);
                }

                uv.Add(new Vector2(r, 0));
                uv.Add(new Vector2(r, 1));

                if (i != virtualControlPointCount - 1)
                {
                    var d = outline[(i + 1) % outline.Count] - outline[i];
                    len += d.magnitude;
                }
            }
            return uv;
        }

        private List<Vector3> CalculateEdgeVertices(int startVertexIndex, int length, List<Vector3> meshVertices, int outlinePointCount)
        {
            List<Vector3> vertices = new List<Vector3>();
            for (int i = 0; i < length; ++i)
            {
                int pointIdx = Mod(startVertexIndex + i, outlinePointCount);
                vertices.Add(meshVertices[pointIdx * 2]);
                vertices.Add(meshVertices[pointIdx * 2 + 1]);
            }
            return vertices;
        }

        private List<Vector2> CreateSplineMeshUVInRange(int startIndex, int length, List<Vector3> outline)
        {
            List<Vector2> uv = new List<Vector2>();
            float len = 0;
            float ratio = m_Input.Settings.TextureAspectRatio;
            for (int i = 0; i < length; ++i)
            {
                float r = (len / m_Input.Settings.LineWidth) / ratio;
                if (i == length - 1)
                {
                    //截断成正数，保证贴图完整
                    r = Mathf.Ceil(r);
                }
                uv.Add(new Vector2(r, 0));
                uv.Add(new Vector2(r, 1));

                int idx = i + startIndex;
                var d = outline[(idx + 1) % outline.Count] - outline[idx % outline.Count];
                len += d.magnitude;
            }
            return uv;
        }

        private Mesh CreateLocalMesh(Mesh mesh, int territoryID)
        {
            var offset = m_Input.GetTerritoryCenterFunc(territoryID);
            Mesh localMesh = Mesh.Instantiate(mesh);
            var vertices = localMesh.vertices;
            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] -= offset;
            }
            localMesh.vertices = vertices;
            localMesh.RecalculateBounds();
            localMesh.UploadMeshData(true);
            return localMesh;
        }
    }
}


