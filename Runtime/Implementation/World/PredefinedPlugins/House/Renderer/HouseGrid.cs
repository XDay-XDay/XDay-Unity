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
using XDay.UtilityAPI;

namespace XDay.WorldAPI.House
{
    public class HouseGrid
    {
        public GameObject RootGameObject { get; private set; }
        public string Name { get => RootGameObject.name; set => RootGameObject.name = value; }
        public bool IsLineActive { get => m_Line.activeSelf; set => m_Line.SetActive(value); }
        public float Width => m_Width;
        public float Height => m_Height;
        public int HorizontalGridCount => m_HorizontalGridCount;
        public int VerticalGridCount => m_VerticalGridCount;

        public HouseGrid(string name, int horizontalGridCount, int verticalGridCount, float gridSize, Transform parent, float height, Vector3 origin)
        {
            m_HorizontalGridCount = horizontalGridCount;
            m_VerticalGridCount = verticalGridCount;
            m_GridSize = gridSize;
            m_Width = m_GridSize * m_HorizontalGridCount;
            m_Height = m_GridSize * m_VerticalGridCount;
            m_Origin = origin;

            CreateRoot(name, parent);
            CreateLine(height);
        }

        public void OnDestroy()
        {
            Helper.DestroyUnityObject(m_LineMesh);
            Helper.DestroyUnityObject(RootGameObject);
            Helper.DestroyUnityObject(m_LineMaterial);
        }

        public void SetHeight(float height)
        {
            m_Line.transform.localPosition = new Vector3(0, height, 0) + m_Origin;
        }

        private void CreateRoot(string name, Transform parent)
        {
            RootGameObject = new GameObject(name);
            RootGameObject.transform.SetParent(parent, worldPositionStays: false);
#if UNITY_EDITOR
            RootGameObject.AddComponent<NoKeyDeletion>();
            UnityEditor.Selection.activeGameObject = RootGameObject;
#endif
            Helper.HideGameObject(RootGameObject);
        }

        private void CreateLine(float height)
        {
            m_Line = new GameObject("Line");
#if UNITY_EDITOR
            m_Line.AddComponent<NoKeyDeletion>();
#endif
            m_Line.transform.SetParent(RootGameObject.transform, false);
            m_Line.transform.localPosition = new Vector3(0, height, 0) + m_Origin;

            var renderer = m_Line.AddComponent<MeshRenderer>();
            m_LineMaterial = new Material(Shader.Find("XDay/Grid"));
            renderer.sharedMaterial = m_LineMaterial;
            m_LineMaterial.SetColor("_Color", new Color32(42, 142, 232, 255));
            var filter = m_Line.AddComponent<MeshFilter>();
            filter.sharedMesh = CreateLineMesh();
        }

        private Mesh CreateLineMesh()
        {
            var hResolution = m_HorizontalGridCount + 1;
            var vResolution = m_VerticalGridCount + 1;
            var vertices = new Vector3[hResolution * 2 + vResolution * 2];
            var indices = new int[vertices.Length];

            //horizontal line
            for (var i = 0; i < vResolution; ++i)
            {
                vertices[i * 2] = new Vector3(0, 0, i * m_GridSize);
                vertices[i * 2 + 1] = new Vector3(m_Width, 0, i * m_GridSize);
            }

            //vertical line
            var offset = vResolution * 2;
            for (var i = 0; i < hResolution; ++i)
            {
                vertices[offset + i * 2] = new Vector3(i * m_GridSize, 0, 0);
                vertices[offset + i * 2 + 1] = new Vector3(i * m_GridSize, 0, m_Height);
            }

            for (var i = 0; i < indices.Length; ++i)
            {
                indices[i] = i;
            }

            m_LineMesh = new Mesh
            {
                vertices = vertices
            };
            m_LineMesh.SetIndices(indices, MeshTopology.Lines, 0);
            m_LineMesh.RecalculateBounds();
            m_LineMesh.UploadMeshData(markNoLongerReadable: true);

            return m_LineMesh;
        }

        private GameObject m_Line;
        private Mesh m_LineMesh;
        private Material m_LineMaterial;
        private readonly int m_HorizontalGridCount;
        private readonly int m_VerticalGridCount;
        private readonly float m_GridSize;
        private readonly float m_Width;
        private readonly float m_Height;
        private Vector3 m_Origin;
    }
}

