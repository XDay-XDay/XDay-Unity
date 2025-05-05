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
    public class TextureGrid
    {
        public Vector3 Position 
        {
            get => m_GameObject.transform.position; 
            set => m_GameObject.transform.position = new Vector3(value.x, m_Y, value.z); }

        public TextureGrid(string name,
            Material material,
            Color32 color,
            Transform parent = null,
            bool hideGameObject = false,
            int renderQueue = 0,
            float height = 0.05f,
            bool centerAlign = false,
            string colorPropertyName = "_Color")
        {
            Create(name, parent, material, hideGameObject, renderQueue, height, centerAlign, color, colorPropertyName);
        }

        public TextureGrid(string name,
            Material material,
            Transform parent = null,
            bool hideGameObject = false,
            int renderQueue = 0,
            float height = 0.05f,
            bool centerAlign = false,
            string colorPropertyName = "_Color")
            : this(name, material, Color.white, parent, hideGameObject, renderQueue, height, centerAlign, colorPropertyName)
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

        public void SetScale(Vector2 uvScale, Vector3 scale)
        {
            m_GameObject.transform.localScale = scale;
            m_Material.SetTextureScale("_MainTex", uvScale);
        }

        public void SetActive(bool show)
        {
            m_GameObject.SetActive(show);
        }

        private void Create(string name,
            Transform parent,
            Material material,
            bool hideGameObject,
            int renderQueue,
            float height,
            bool centerAlign,
            Color color,
            string colorPropertyName)
        {
            m_Y = height;
            m_GameObject = new GameObject(name);
            m_GameObject.transform.SetParent(parent, false);
            m_GameObject.transform.localPosition = new Vector3(0, m_Y, 0);
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
            filter.sharedMesh = CreateMesh(centerAlign);
        }

        private Mesh CreateMesh(bool centerAlign)
        {
            float width = 1;
            float height = 1;

            var offset = Vector3.zero;
            if (centerAlign)
            {
                offset = new Vector3(0.5f, 0, 0.5f);
            }

            var vertices = new Vector3[]
            {
                new Vector3(0, 0, 0) - offset,
                new Vector3(0, 0, height) - offset,
                new Vector3(width, 0, height) - offset,
                new Vector3(width, 0, 0) - offset,
            };

            var uvs = new Vector2[]
            {
                Vector2.zero,
                new Vector2(0, 1),
                new Vector2(1, 1),
                new Vector2(1, 0),
            };

            var indices = new int[]
            {
                0,1,2,0,2,3,
            };

            m_Mesh = new Mesh
            {
                vertices = vertices,
                uv = uvs,
            };
            m_Mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            m_Mesh.RecalculateBounds();
            m_Mesh.UploadMeshData(markNoLongerReadable: true);

            return m_Mesh;
        }

        private GameObject m_GameObject;
        private Mesh m_Mesh;
        private Material m_Material;
        private float m_Y;
    }
}
