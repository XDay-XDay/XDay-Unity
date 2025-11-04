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

namespace XDay.UtilityAPI.Editor
{
    public partial class CurveRegionCreator
    {
        private void TriangulateTerritoryMultithreadingStep1(Territory territory, string folder, int lod, bool generateAssets)
        {
            SimpleStopwatch w = new();
            w.Begin();
            //计算一个territory所有shared edge的顶点
            CalculateTerritoryAllSharedEdgeSplineVertices(territory);
            w.Stop();
            Debug.Log($"CalculateTerritoryAllSharedEdgeSplineVertices elapsed time: {w.ElapsedSeconds} seconds");

            if (!string.IsNullOrEmpty(folder))
            {
                PolygonTriangulator.Triangulate(territory.Outline, out territory.RegionMeshVertices, out territory.RegionMeshIndices);
            }
            else
            {
                territory.RegionMeshVertices = null;
                territory.RegionMeshIndices = null;
            }
        }

        //must be called in main thread
        private void TriangulateTerritoryMultithreadingStep3(Territory territory, string folder, int lod, bool generateAssets)
        {
            var meshVertices = territory.RegionMeshVertices;
            var meshIndices = territory.RegionMeshIndices;
            if (meshVertices != null)
            {
                float yOffset = 1.0f;
                var mesh = new Mesh();
                mesh.SetVertices(territory.RegionMeshVertices);
                if (m_Input.Settings.UseVertexColorForRegionMesh)
                {
                    mesh.SetColors(CreateVertexColor(territory.Color, meshVertices.Length));
                }
                mesh.SetIndices(meshIndices, MeshTopology.Triangles, 0);
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
                    var localMesh = CreateLocalMesh(mesh, territory.RegionID);
                    string name = $"region_{territory.RegionID}_lod{lod}";
                    territory.PrefabPath = $"{folder}/{name}.prefab";
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
        }

        private void TriangulateTerritoryMultithreadingStep4(Territory territory, string folder, int lod, bool generateAssets)
        {
            //生成inner outline和edge mesh
            territory.InnerOutline = CreateInnerOutlineMesh(territory, territory.Outline, folder, lod, generateAssets);
        }
    }
}

