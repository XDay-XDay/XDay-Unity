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

namespace XDay.RenderingAPI
{
    public class StripeEffect
    {
        public StripeEffect()
        {
            CreateMesh();
            m_GameObject = new GameObject("Stripe");
            m_GameObject.AddComponent<MeshRenderer>();
            var filter = m_GameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = m_Mesh;
        }

        public void OnDestroy()
        {
            Helper.DestroyUnityObject(m_GameObject);
            m_GameObject = null;
            Helper.DestroyUnityObject(m_Mesh);
            m_Mesh = null;
            Helper.DestroyUnityObject(m_Material);
            m_Material = null;
        }

        public void Start(Transform start, Transform end, Vector3 startPos, Vector3 endPos, Material material, float width)
        {
            m_Start = start;
            m_End = end;
            m_StartPos = startPos;
            m_EndPos = endPos;
            m_Width = width;
            m_Material = Object.Instantiate(material);
            m_GameObject.GetComponent<MeshRenderer>().sharedMaterial = m_Material;
        }

        public void Update()
        {
            var startPos = m_Start != null ? m_Start.position : m_StartPos;
            var endPos = m_End != null ? m_End.position : m_EndPos;
            var dir = endPos - startPos;
            var distance = dir.magnitude;
            dir /= distance;
            var pos = (startPos + endPos) * 0.5f;
            //wzw temp
            pos.y = 1.0f;
            m_GameObject.transform.position = pos;
            m_GameObject.transform.localScale = new Vector3(distance, 1, m_Width);
            m_GameObject.transform.right = dir;
        }

        private void CreateMesh()
        {
            var vertices = new Vector3[8]
            {
                new Vector3(-0.5f, 0, -0.5f),
                new Vector3(-0.5f, 0, 0.5f),
                new Vector3(0.5f, 0, 0.5f),
                new Vector3(0.5f, 0, -0.5f),
                new Vector3(-0.5f, -0.5f, 0),
                new Vector3(-0.5f, 0.5f, 0),
                new Vector3(0.5f, 0.5f, 0),
                new Vector3(0.5f, -0.5f, 0),
            };

            var uvs = new Vector2[8]
            {
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(1, 1),
                new Vector2(1, 0),
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(1, 1),
                new Vector2(1, 0),
            };

            var indices = new int[]
            {
                0,1,2,0,2,3,
                6,5,4,7,6,4,
            };

            m_Mesh = new Mesh()
            {
                vertices = vertices,
                uv = uvs,
                triangles = indices,
            };
        }

        private Mesh m_Mesh;
        private GameObject m_GameObject;
        private Material m_Material;
        private Transform m_Start;
        private Transform m_End;
        private Vector3 m_StartPos;
        private Vector3 m_EndPos;
        private float m_Width;
    }
}
