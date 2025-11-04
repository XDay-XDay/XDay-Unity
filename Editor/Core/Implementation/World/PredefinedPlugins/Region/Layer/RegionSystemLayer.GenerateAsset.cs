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
using XDay.UtilityAPI.Editor;
using static XDay.WorldAPI.Region.Editor.RegionSystem;

namespace XDay.WorldAPI.Region.Editor
{
    internal partial class RegionSystemLayer
    {
        private class RegionPreview
        {
            public RegionPreview(GameObject obj, Mesh mesh)
            {
                m_GameObject = obj;
                m_Mesh = mesh;
            }

            public void OnDestroy()
            {
                var mtl = m_GameObject.GetComponent<MeshRenderer>().sharedMaterial;
                Helper.DestroyUnityObject(mtl);
                Helper.DestroyUnityObject(m_GameObject);
                Helper.DestroyUnityObject(m_Mesh);
            }

            private GameObject m_GameObject;
            private Mesh m_Mesh;
        }

        public void DestroyPreviewObjects()
        {
            for (int i = 0; i < m_PreviewGameObjects.Count; ++i)
            {
                m_PreviewGameObjects[i].OnDestroy();
            }
            m_PreviewGameObjects.Clear();
        }

        public void ClearPreview()
        {
            DestroyPreviewObjects();
        }

        public void GenerateAssets(string folder)
        {
            for (int i = 0; i < m_MeshGenerationParamForLODs.Count; ++i)
            {
                GenerateLODAssets(folder, m_MeshGenerationParamForLODs[i], i);
            }
        }

        private void GenerateLODAssets(string assetFolder, EditorRegionMeshGenerationParam param, int lod)
        {
            if (param.RegionMeshMaterial == null)
            {
                Debug.LogError("Invalid Material");
                return;
            }

            for (int i = 0; i < m_Regions.Count; ++i)
            {
                var coord = GetRegionCoordinates(m_Regions[i].ID);
                if (coord.Count > 0)
                {
                    var polygon = GetOutlinePolygon(m_Regions[i].ID, coord);
                    if (param.CurveCorner)
                    {
                        polygon = ConvertToCurveOutline(polygon, param.BorderSizeRatio * m_GridWidth, param.CornerSegment);
                    }
                    var mesh = CreateTerritoryMesh(m_Regions[i].ID, polygon, m_Regions[i].Color, param.BorderSizeRatio, param.UVScale, true);

                    string prefix = $"{assetFolder}/territory_{m_Regions[i].ID}_lod{lod}.";

                    AssetDatabase.CreateAsset(mesh, prefix + "asset");

                    Material mtl = Object.Instantiate<Material>(param.RegionMeshMaterial);
                    mtl.color = m_Regions[i].Color;
                    AssetDatabase.CreateAsset(mtl, prefix + "mat");

                    GameObject obj = new(m_Regions[i].ID + " LOD1");
                    var renderer = obj.AddComponent<MeshRenderer>();
                    var filter = obj.AddComponent<MeshFilter>();
                    filter.sharedMesh = mesh;
                    renderer.sharedMaterial = mtl;

                    PrefabUtility.SaveAsPrefabAsset(obj, prefix + "prefab");
                    GameObject.DestroyImmediate(obj);
                }
            }
        }

        public void CreatePreview(int lod)
        {
            var param = m_MeshGenerationParamForLODs[lod];
            if (param.RegionMeshMaterial == null)
            {
                UnityEditor.EditorUtility.DisplayDialog("Error", "Select territory mesh material!", "OK");
                return;
            }

            DestroyPreviewObjects();

            for (int i = 0; i < m_Regions.Count; ++i)
            {
                var coord = GetRegionCoordinates(m_Regions[i].ID);
                if (coord.Count > 0)
                {
                    var polygon = GetOutlinePolygon(m_Regions[i].ID, coord);
                    if (param.CurveCorner)
                    {
                        polygon = ConvertToCurveOutline(polygon, param.BorderSizeRatio * m_GridWidth, param.CornerSegment);
                    }
                    var mesh = CreateTerritoryMesh(m_Regions[i].ID, polygon, m_Regions[i].Color, param.BorderSizeRatio, param.UVScale, false);
                    GameObject obj = new GameObject(m_Regions[i].ID + " LOD1");
                    Helper.HideGameObject(obj);
                    obj.transform.parent = m_Renderer.Root.transform;
                    var preview = new RegionPreview(obj, mesh);
                    m_PreviewGameObjects.Add(preview);
                    var renderer = obj.AddComponent<MeshRenderer>();
                    var filter = obj.AddComponent<MeshFilter>();
                    filter.sharedMesh = mesh;
                    renderer.sharedMaterial = Object.Instantiate<Material>(param.RegionMeshMaterial);
                    renderer.sharedMaterial.color = m_Regions[i].Color;
                }
            }
        }

        private Mesh CreateTerritoryMesh(int territoryID, List<Vector3> outline, Color color, float borderSizeRatio, float uvScale, bool localSpace)
        {
            Mesh mesh = new Mesh();
            List<Vector3> meshVertices = new List<Vector3>();
            List<Vector2> meshUVs = new List<Vector2>();
            List<Color> meshColors = new List<Color>();
            List<int> meshIndices = new List<int>();
            int n = outline.Count;
            float length = 0;

            Vector3 sideVertexPos0 = Vector3.zero;
            float borderSize = borderSizeRatio * m_GridWidth;

            for (int i = 0; i < n; ++i)
            {
                Vector3 cur = outline[i];
                Vector3 prev = outline[Helper.Mod(i - 1, n)];
                Vector3 next = outline[Helper.Mod(i + 1, n)];
                Vector3 curToPrev = (cur - prev).normalized;
                Vector3 nextToCur = (next - cur).normalized;
                Vector3 offsetDir = ((nextToCur + curToPrev) * 0.5f).normalized;
                Vector3 perpDir = new Vector3(offsetDir.z, 0, -offsetDir.x);
                Vector3 sideVertex = cur + perpDir * borderSize;
                meshVertices.Add(cur);
                meshVertices.Add(sideVertex);

                meshColors.Add(color);
                meshColors.Add(color);

                if (i == 0)
                {
                    sideVertexPos0 = sideVertex;
                }

                meshUVs.Add(new Vector2(0, length / uvScale));
                meshUVs.Add(new Vector2(1, length / uvScale));

                length += (next - cur).magnitude;

                if (i == n - 1)
                {
                    //由于uv连续的关系,需要在最后追加一组复制的顶点
                    meshVertices.Add(outline[0]);
                    meshVertices.Add(sideVertexPos0);
                    meshUVs.Add(new Vector2(0, length / uvScale));
                    meshUVs.Add(new Vector2(1, length / uvScale));
                    meshColors.Add(color);
                    meshColors.Add(color);
                }

                int v0 = (i * 2);
                int v1 = (i * 2 + 1);
                int v2 = (i * 2 + 2);
                int v3 = (i * 2 + 3);
                meshIndices.Add(v0);
                meshIndices.Add(v2);
                meshIndices.Add(v1);
                meshIndices.Add(v2);
                meshIndices.Add(v3);
                meshIndices.Add(v1);
            }

            if (localSpace)
            {
                ConvertToLocalSpace(territoryID, meshVertices);
            }

            mesh.SetVertices(meshVertices);
            mesh.SetUVs(0, meshUVs);
            //颜色会动态修改,所以把颜色设置在shader里
            //mesh.SetColors(meshColors);
            mesh.SetIndices(meshIndices, MeshTopology.Triangles, 0);
            mesh.UploadMeshData(true);

            return mesh;
        }

        private void ConvertToLocalSpace(int regionID, List<Vector3> vertices)
        {
            var center = GetRegionCenter(regionID);
            for (int i = 0; i < vertices.Count; ++i)
            {
                vertices[i] -= center;
            }
        }

        private class BorderEdge
        {
            public Vector3 start;
            public Vector3 end;
        }

        private List<Vector3> GetOutlinePolygon(int id, List<Vector2Int> coords)
        {
            m_StartPosToEdge.Clear();
            //忽略hole
            for (int i = 0; i < coords.Count; ++i)
            {
                int x = coords[i].x;
                int y = coords[i].y;
                int left = GetGridData(x - 1, y);
                if (left != id)
                {
                    AddBorderEdge(x, y, x, y + 1);
                }
                int top = GetGridData(x, y + 1);
                if (top != id)
                {
                    AddBorderEdge(x, y + 1, x + 1, y + 1);
                }
                int right = GetGridData(x + 1, y);
                if (right != id)
                {
                    AddBorderEdge(x + 1, y + 1, x + 1, y);
                }
                int bottom = GetGridData(x, y - 1);
                if (bottom != id)
                {
                    AddBorderEdge(x + 1, y, x, y);
                }
            }

            return ConnectEdges();
        }

        private void AddBorderEdge(int startX, int startY, int endX, int endY)
        {
            var startPos = CoordinateToPosition(startX, startY);
            if (!m_StartPosToEdge.ContainsKey(startPos))
            {
                var endPos = CoordinateToPosition(endX, endY);
                var borderEdge = new BorderEdge { start = startPos, end = endPos };
                m_StartPosToEdge.Add(startPos, borderEdge);
            }
        }

        private List<Vector3> ConnectEdges()
        {
            List<Vector3> outline = new List<Vector3>();
            int nEdges = m_StartPosToEdge.Count;
            Debug.Assert(nEdges > 0);
            //get first edge
            BorderEdge firstEdge = null;
            foreach (var p in m_StartPosToEdge)
            {
                firstEdge = p.Value;
                break;
            }

            outline.Add(firstEdge.start);

            for (int i = 1; i < nEdges; ++i)
            {
                BorderEdge nextEdge;
                m_StartPosToEdge.TryGetValue(firstEdge.end, out nextEdge);
                Debug.Assert(nextEdge != null);

                if (!SameDirection(firstEdge, nextEdge))
                {
                    outline.Add(nextEdge.start);
                }

                firstEdge = nextEdge;
            }

            return outline;
        }

        private bool SameDirection(BorderEdge a, BorderEdge b)
        {
            var dirA = a.end - a.start;
            var dirB = b.end - b.start;
            dirA.Normalize();
            dirB.Normalize();
            float dot = Vector3.Dot(dirA, dirB);
            if (Mathf.Approximately(dot, 1) || Mathf.Approximately(dot, -1))
            {
                return true;
            }
            return false;
        }

        private bool IsLeftTurn(Vector3 a, Vector3 b)
        {
            return Vector3.Cross(a, b).y < 0;
        }

        private List<Vector3> ConvertToCurveOutline(List<Vector3> outline, float borderSize, int cornerSegment)
        {
            List<Vector3> curveOutline = new List<Vector3>();
            int n = outline.Count;
            for (int i = 0; i < n; ++i)
            {
                Vector3 cur = outline[i];
                Vector3 prev = outline[Helper.Mod(i - 1, n)];
                Vector3 next = outline[Helper.Mod(i + 1, n)];
                Vector3 curToPrev = (cur - prev).normalized;
                Vector3 nextToCur = (next - cur).normalized;

                bool isLeftTurn = IsLeftTurn(curToPrev, nextToCur);

                //outer curve
                Vector3 startPosOnPrev = prev + curToPrev * borderSize;
                Vector3 endPosOnPrev = cur - curToPrev * borderSize;
                Vector3 startPosOnNext = cur + nextToCur * borderSize;
                curveOutline.Add(startPosOnPrev);
                curveOutline.Add(endPosOnPrev);
                //generate corner vertices
                Vector3 perp;
                if (isLeftTurn)
                {
                    perp = new Vector3(-curToPrev.z, 0, curToPrev.x);
                }
                else
                {
                    perp = new Vector3(curToPrev.z, 0, -curToPrev.x);
                }
                Vector3 cornerSphereCenter = endPosOnPrev + perp * borderSize;
                int quadrant = CalculateQuadrant(endPosOnPrev, startPosOnNext, cornerSphereCenter);
                List<Vector3> cornerVertices = GenerateCorner(borderSize, cornerSphereCenter, quadrant, isLeftTurn, cornerSegment);
                curveOutline.AddRange(cornerVertices);
            }

            return curveOutline;
        }

        /*
         * quadrant
         * 2 3
         * 1 0
         */
        private int CalculateQuadrant(Vector3 cornerVertexA, Vector3 cornerVertexB, Vector3 cornerSphereCenter)
        {
            if (cornerVertexA.x > cornerSphereCenter.x && cornerVertexB.z < cornerSphereCenter.z ||
                cornerVertexB.x > cornerSphereCenter.x && cornerVertexA.z < cornerSphereCenter.z)
            {
                return 0;
            }

            if (cornerVertexA.x < cornerSphereCenter.x && cornerVertexB.z < cornerSphereCenter.z ||
                cornerVertexB.x < cornerSphereCenter.x && cornerVertexA.z < cornerSphereCenter.z)
            {
                return 1;
            }

            if (cornerVertexA.x < cornerSphereCenter.x && cornerVertexB.z > cornerSphereCenter.z ||
                cornerVertexB.x < cornerSphereCenter.x && cornerVertexA.z > cornerSphereCenter.z)
            {
                return 2;
            }

            if (cornerVertexA.x > cornerSphereCenter.x && cornerVertexB.z > cornerSphereCenter.z ||
                cornerVertexB.x > cornerSphereCenter.x && cornerVertexA.z > cornerSphereCenter.z)
            {
                return 3;
            }

            Debug.Assert(false);
            return 0;
        }

        private List<Vector3> GenerateCorner(float radius, Vector3 center, int quadrant, bool isLeftTurn, int cornerSegment)
        {
            List<Vector3> cornerVertices = new List<Vector3>();
            float deltaAngle = 90.0f / (cornerSegment - 1);
            float startAngle = quadrant * 90.0f + 90;
            int start = 1;
            int end = cornerSegment - 1;
            int delta = 1;
            if (isLeftTurn)
            {
                start = cornerSegment - 1;
                end = 1;
                delta = -1;
            }
            for (int i = start; i != end; i += delta)
            {
                float angle = startAngle + i * deltaAngle;
                float x = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
                float z = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
                x += center.x;
                z += center.z;
                cornerVertices.Add(new Vector3(x, 0, z));
            }

            return cornerVertices;
        }

        internal void OnLODCountChanged(int oldLODCount, int newLODCount)
        {
            if (newLODCount > oldLODCount)
            {
                int delta = newLODCount - oldLODCount;
                for (int i = 0; i < delta; ++i)
                {
                    m_MeshGenerationParamForLODs.Add(new EditorRegionMeshGenerationParam(3, 0.3f, 10, false, ""));
                    var param = GetDefaultLODParam(false);
                    m_CurveRegionMeshGenerationParam.LODParams.Add(param);
                    m_CreatorsForLODs.Add(new CurveRegionCreator(m_Renderer.Root.transform, Width, Height));
                }
            }
            else
            {
                int delta = oldLODCount - newLODCount;
                for (int i = 0; i < delta; ++i)
                {
                    m_MeshGenerationParamForLODs.RemoveAt(m_MeshGenerationParamForLODs.Count - 1);
                    m_CurveRegionMeshGenerationParam.LODParams.RemoveAt(m_CurveRegionMeshGenerationParam.LODParams.Count - 1);
                    m_CreatorsForLODs[m_CreatorsForLODs.Count - 1].OnDestroy();
                    m_CreatorsForLODs.RemoveAt(m_CreatorsForLODs.Count - 1);
                }
            }
        }
    }
}
