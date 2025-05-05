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

namespace XDay.UtilityAPI
{
    public class GridMesh
    {
        public GridMesh(string name, 
            int horizontalGridCount, 
            int verticalGridCount, 
            float gridSize, 
            Material material,
            Color32 color,
            Transform parent = null, 
            bool hideGameObject = false,
            int renderQueue = 0,
            float height = 0.05f,
            string colorPropertyName = "_Color")
        {
            CreateLine(name, horizontalGridCount, verticalGridCount, gridSize, parent, material, hideGameObject, renderQueue, height, color, colorPropertyName);
        }

        public GridMesh(string name,
            int horizontalGridCount,
            int verticalGridCount,
            float gridSize,
            Material material,
            Transform parent = null,
            bool hideGameObject = false,
            int renderQueue = 0,
            float height = 0.05f,
            string colorPropertyName = "_Color")
            : this(name, horizontalGridCount, verticalGridCount, gridSize, material, Color.white, parent, hideGameObject, renderQueue, height, colorPropertyName)
        {
        }

        public void OnDestroy()
        {
            Helper.DestroyUnityObject(m_Mesh);
            Helper.DestroyUnityObject(m_GameObject);
            Helper.DestroyUnityObject(m_Material);
            m_GameObject = null;
            m_Mesh = null;
            m_Material = null;
        }

        public void SetActive(bool show)
        {
            m_GameObject.SetActive(show);
        }

        private void CreateLine(string name, 
            int horizontalGridCount, 
            int verticalGridCount, 
            float gridSize, 
            Transform parent, 
            Material material, 
            bool hideGameObject,
            int renderQueue,
            float height, 
            Color color, 
            string colorPropertyName)
        {
            m_GameObject = new GameObject(name);
            m_GameObject.transform.SetParent(parent, false);
            m_GameObject.transform.localPosition = new Vector3(0, height, 0);
            if (hideGameObject)
            {
                Helper.HideGameObject(m_GameObject);
            }

            var renderer = m_GameObject.AddComponent<MeshRenderer>();
            m_Material = Object.Instantiate(material);
            m_Material.SetColor(colorPropertyName, color);
            if (renderQueue > 0)
            {
                m_Material.renderQueue = renderQueue;
            }
            renderer.sharedMaterial = m_Material;
            var filter = m_GameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = CreateLineMesh(horizontalGridCount, verticalGridCount, gridSize);
        }

        private Mesh CreateLineMesh(int horizontalGridCount, int verticalGridCount, float gridSize)
        {
            var hResolution = horizontalGridCount + 1;
            var vResolution = verticalGridCount + 1;
            var vertices = new Vector3[hResolution * 2 + vResolution * 2];
            var indices = new int[vertices.Length];

            var width = horizontalGridCount * gridSize;
            var height = verticalGridCount * gridSize;

            //horizontal line
            for (var i = 0; i < vResolution; ++i)
            {
                vertices[i * 2] = new Vector3(0, 0, i * gridSize);
                vertices[i * 2 + 1] = new Vector3(width, 0, i * gridSize);
            }

            //vertical line
            var offset = vResolution * 2;
            for (var i = 0; i < hResolution; ++i)
            {
                vertices[offset + i * 2] = new Vector3(i * gridSize, 0, 0);
                vertices[offset + i * 2 + 1] = new Vector3(i * gridSize, 0, height);
            }

            for (var i = 0; i < indices.Length; ++i)
            {
                indices[i] = i;
            }

            m_Mesh = new Mesh
            {
                vertices = vertices
            };
            m_Mesh.SetIndices(indices, MeshTopology.Lines, 0);
            m_Mesh.RecalculateBounds();
            m_Mesh.UploadMeshData(markNoLongerReadable: true);

            return m_Mesh;
        }

        private GameObject m_GameObject;
        private Mesh m_Mesh;
        private Material m_Material;
    }
}
