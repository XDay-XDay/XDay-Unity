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

namespace XDay.NavigationAPI
{
    public class NavMeshRenderer
    {
        public NavMeshRenderer(string name, Vector3[] vertices, int[] indices, Color[] colors, Transform parent)
        {
            m_Mesh = new LargeMesh(vertices, indices, null, colors);

            m_GameObject = new GameObject(name);
            m_GameObject.transform.SetParent(parent, false);
            var renderer = m_GameObject.AddComponent<MeshRenderer>();
            m_Material = new Material(Shader.Find("XDay/VertexColorTransparent"))
            {
                renderQueue = 3999
            };
            m_Material.SetInt("_CompareFunc", (int)UnityEngine.Rendering.CompareFunction.Always);
            
            renderer.sharedMaterial = m_Material;
            var filter = m_GameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = m_Mesh.Mesh;
        }

        public void OnDestroy()
        {
            m_Mesh.OnDestroy();
            Helper.DestroyUnityObject(m_Material);
            Helper.DestroyUnityObject(m_GameObject);
        }

        private readonly GameObject m_GameObject;
        private readonly LargeMesh m_Mesh;
        private readonly Material m_Material;
    }
}
