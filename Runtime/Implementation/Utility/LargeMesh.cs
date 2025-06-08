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
using System.Collections.Generic;

namespace XDay.UtilityAPI
{
    public class LargeMesh
    {
        public LargeMesh(Vector3[] vertices, int[] indices, Vector2[] uvs, Color[] colors)
        {
            SetData(vertices, indices, uvs, colors);
            m_Mesh.UploadMeshData(markNoLongerReadable: true);
        }

        public void OnDestroy()
        {
            Helper.DestroyUnityObject(m_Mesh);
            m_Mesh = null;
        }

        private void SetData(Vector3[] vertices, int[] indices, Vector2[] uvs, Color[] colors)
        {
            var maxIndexCount = 65535;
            m_Mesh = new Mesh
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
                vertices = vertices
            };

            if (uvs != null)
            {
                m_Mesh.uv = uvs;
            }

            if (colors != null)
            {
                m_Mesh.colors = colors;
            }

            var submeshes = new List<List<int>>();
            var offset = 0;
            while (true)
            {
                var submesh = new List<int>();
                var maxCount = Mathf.Min(maxIndexCount, indices.Length - offset);
                for (var i = 0; i < maxCount; ++i)
                {
                    submesh.Add(indices[i + offset]);
                }

                submeshes.Add(submesh);

                offset += maxCount;

                if (indices.Length - offset == 0)
                {
                    break;
                }
            }

            m_Mesh.subMeshCount = submeshes.Count;
            for (var i = 0; i < submeshes.Count; ++i)
            {
                m_Mesh.SetIndices(submeshes[i], MeshTopology.Triangles, i);
            }

            m_Mesh.RecalculateBounds();
            m_Mesh.UploadMeshData(markNoLongerReadable: true);
        }

        public Mesh Mesh => m_Mesh;

        private Mesh m_Mesh;
    }
}
