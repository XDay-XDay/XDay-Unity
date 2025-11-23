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
using System.Threading.Tasks;

namespace XDay.UtilityAPI.Editor
{
    public partial class SmoothRegionCreator
    {
        public void TriangulateRegions(string folder, int lod, bool displayProgressBar, bool generateAssets)
        {
            Task[] tasks = new Task[m_Regions.Count];
            for (int i = 0; i < m_Regions.Count; ++i)
            {
                int idx = i;
                var task = Task.Run(() =>
                {
                    TriangulateStep1(m_Regions[idx], folder, lod, generateAssets);
                });

                tasks[i] = task;
            }

            Task.WaitAll(tasks);

            var meshIndexCount = 0;
            var meshVertexCount = 0;
            for (int i = 0; i < m_Regions.Count; ++i)
            {
                TriangulateStep2(m_Regions[i], folder, lod, generateAssets);

                meshIndexCount += m_Regions[i].MeshVertices.Length;
                meshVertexCount += m_Regions[i].MeshIndices.Length;
            }
            Debug.LogError($"LOD1 Total Mesh Vertex Count: {meshVertexCount}, Triangle Count: {meshIndexCount / 3}");
        }

        private void TriangulateStep1(Region region, string folder, int lod, bool generateAssets)
        {
            if (!string.IsNullOrEmpty(folder))
            {
                PolygonTriangulator.Triangulate(region.Outline, out region.MeshVertices, out region.MeshIndices);
            }
            else
            {
                region.MeshVertices = null;
                region.MeshIndices = null;
            }
        }

        private void TriangulateStep2(Region region, string folder, int lod, bool generateAssets)
        {
            var meshVertices = region.MeshVertices;
            var meshIndices = region.MeshIndices;
            if (meshVertices != null)
            {
                var mesh = new Mesh();
                mesh.SetVertices(region.MeshVertices);
                mesh.SetIndices(meshIndices, MeshTopology.Triangles, 0);

                var uv = CreateMeshUV(region.MeshVertices);
                mesh.uv = uv;

                var obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                obj.transform.position = new Vector3(0, 0.05f, 0);
                obj.name = $"region mesh {region.RegionID}";
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
                //mtl.color = region.Color;
                renderer.sharedMaterial = mtl;
                region.SetGameObject(Region.ObjectType.Region, obj);

                if (generateAssets)
                {
                    var regionConfigID = m_Input.GetRegionConfigIDFunc(region.RegionID);
                    var prefix = m_Input.GetRegionObjectNamePrefix("mesh", regionConfigID, 1);
                    var localMesh = CreateLocalMesh(mesh, region.RegionID, out var center);
                    AssetDatabase.CreateAsset(localMesh, $"{prefix}.asset");
                    var regionObj = new GameObject();
                    regionObj.transform.position = center;
                    var objRenderer = regionObj.AddComponent<MeshRenderer>();
                    objRenderer.sharedMaterial = m_Input.Settings.RegionMaterial;
                    var regionFilter = regionObj.AddComponent<MeshFilter>();
                    regionFilter.sharedMesh = localMesh;
                    PrefabUtility.SaveAsPrefabAsset(regionObj, $"{prefix}.prefab");
                    Helper.DestroyUnityObject(regionObj);
                }
            }
        }

        private Vector2[] CreateMeshUV(Vector3[] meshVertices)
        {
            Vector2[] uvs = new Vector2[meshVertices.Length];
            var bounds = Helper.CalculateBounds(meshVertices);
            var idx = 0;
            foreach (var vert in meshVertices)
            {
                uvs[idx] = new Vector2((vert.x - bounds.min.x) / bounds.size.x, (vert.z - bounds.min.z) / bounds.size.z);
                ++idx;
            }
            return uvs;
        }

        private Mesh CreateLocalMesh(Mesh mesh, int regionID, out Vector3 center)
        {
            center = m_Input.GetRegionCenterFunc(regionID);
            Mesh localMesh = Mesh.Instantiate(mesh);
            var vertices = localMesh.vertices;
            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] -= center;
            }
            localMesh.vertices = vertices;
            localMesh.RecalculateBounds();
            localMesh.UploadMeshData(true);
            return localMesh;
        }
    }
}

