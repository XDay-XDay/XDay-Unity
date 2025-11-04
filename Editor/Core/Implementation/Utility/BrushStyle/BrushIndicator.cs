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

using UnityEditor;
using UnityEngine;
using XDay.WorldAPI;

namespace XDay.UtilityAPI.Editor
{
    internal class BrushIndicator : IBrushIndicator
    {
        public bool Enabled
        {
            set
            {
                if (m_GameObject != null)
                {
                    m_GameObject.SetActive(value);
                }
            }
        }

        public float Scale
        {
            set
            {
                if (m_GameObject != null)
                {
                    m_GameObject.transform.localScale = Vector3.one * value;
                }
            }
        }

        public Quaternion Rotation
        {
            set
            {
                if (m_GameObject != null)
                {
                    m_GameObject.transform.rotation = value;
                }
            }
        }

        public Vector3 Position
        {
            set
            {
                if (m_GameObject != null)
                {
                    m_GameObject.transform.position = value;
                }
            }
        }

        public Texture2D Texture
        {
            set
            {
                if (m_Texture != value)
                {
                    m_Texture = value;
                    Recreate(m_Texture);
                }
            }
        }

        public void OnDestroy()
        {
            Helper.DestroyUnityObject(m_GameObject);
            Helper.DestroyUnityObject(m_Mesh);
            Helper.DestroyUnityObject(m_Material);
        }

        private void Recreate(Texture2D texture)
        {
            Helper.DestroyUnityObject(m_GameObject);
            m_GameObject = null;

            if (texture != null)
            {
                m_GameObject = new GameObject("Brush Indicator");
                m_GameObject.SetActive(false);
                m_GameObject.tag = "EditorOnly";
                Helper.HideGameObject(m_GameObject, true);

                if (m_Mesh == null)
                {
                    var size = 0.5f;
                    m_Mesh = new Mesh
                    {
                        vertices = new Vector3[4]
                        {
                        new(-size, 0, -size),
                        new(-size, 0, size),
                        new(size, 0, size),
                        new(size, 0, -size),
                        },
                        uv = new Vector2[]
                        {
                        new(0, 0),
                        new(0, 1),
                        new(1, 1),
                        new(1, 0),
                        },
                        triangles = new int[] { 0, 1, 2, 0, 2, 3 }
                    };
                }
                var filter = m_GameObject.AddComponent<MeshFilter>();
                filter.sharedMesh = m_Mesh;

                if (m_Material == null)
                {
                    m_Material = new Material(AssetDatabase.LoadAssetAtPath<Shader>(WorldHelper.GetShaderPath("Brush.shader")));
                    m_Material.SetFloat("_Alpha", 0.5f);
                    m_Material.renderQueue = 4600;
                }
                m_Material.mainTexture = texture;
                var meshRenderer = m_GameObject.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = m_Material;
            }
        }

        private GameObject m_GameObject;
        private Material m_Material;
        private Mesh m_Mesh;
        private Texture2D m_Texture;
    }
}

//XDay