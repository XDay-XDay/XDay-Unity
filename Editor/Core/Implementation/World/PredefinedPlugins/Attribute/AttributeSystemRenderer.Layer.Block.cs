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

namespace XDay.WorldAPI.Attribute.Editor
{
    public partial class AttributeSystemRenderer
    {
        private class BlockRenderer
        {
            public BlockRenderer(int startGridX, int startGridY, int endGridX, int endGridY, AttributeSystem.LayerBase layer, Transform parent)
            {
                m_StartGridX = startGridX;
                m_StartGridY = startGridY;
                m_EndGridX = endGridX;
                m_EndGridY = endGridY;
                m_Layer = layer;
                var width = m_EndGridX - m_StartGridX + 1;
                var height = m_EndGridY - m_StartGridY + 1;
                m_Buffer = new Color32[width * height];

                CreateRoot(parent);

                UpdateColors(true);
            }

            public void Uninitialize()
            {
                Helper.DestroyUnityObject(m_Texture);
                Helper.DestroyUnityObject(m_Material);
                Helper.DestroyUnityObject(m_Root);
            }

            public bool UpdateGrid(int x, int y)
            {
                if (x >= m_StartGridX && x <= m_EndGridX &&
                    y >= m_StartGridY && y <= m_EndGridY)
                {
                    m_Dirty = true;
                    return true;
                }

                return false;
            }

            public void UpdateColors(bool forceUpdate)
            {
                if (m_Dirty || forceUpdate)
                {
                    m_Dirty = false;
                    var width = m_EndGridX - m_StartGridX + 1;
                    var height = m_EndGridY - m_StartGridY + 1;
                    for (var y = m_StartGridY; y <= m_EndGridY; ++y)
                    {
                        for (var x = m_StartGridX; x <= m_EndGridX; ++x)
                        {
                            var idx = (y - m_StartGridY) * height + x - m_StartGridX;
                            m_Buffer[idx] = m_Layer.GetColor(x, y);
                        }
                    }

                    m_Texture.SetPixels32(0, 0, width, height, m_Buffer);
                    m_Texture.Apply();
                }
            }

            private void CreateRoot(Transform parent)
            {
                m_Root = new GameObject("Block");
                Helper.HideGameObject(m_Root);

                var filter = m_Root.AddComponent<MeshFilter>();
                var width = m_EndGridX - m_StartGridX + 1;
                var height = m_EndGridY - m_StartGridY + 1;
                 filter.sharedMesh = CreateMesh(width * m_Layer.GridWidth, height * m_Layer.GridHeight);

                var renderer = m_Root.AddComponent<MeshRenderer>();
                m_Material = new Material(Shader.Find("XDay/TextureTransparent"));
                m_Material.renderQueue = 3000 + m_Layer.ObjectIndex * 5;
                renderer.sharedMaterial = m_Material;
                CreateTexture();
                m_Material.SetTexture("_MainTex", m_Texture);

                m_Root.transform.SetParent(parent, worldPositionStays: false);
                m_Root.transform.localPosition = m_Layer.CoordinateToPosition(m_StartGridX, m_StartGridY);
            }

            private Mesh CreateMesh(float width, float height)
            {
                var mesh = new Mesh
                {
                    vertices = new Vector3[]
                    {
                        new Vector3(0, 0, 0),
                        new Vector3(0, 0, height),
                        new Vector3(width, 0, height),
                        new Vector3(width, 0, 0),
                    },
                    uv = new Vector2[]
                    {
                        new Vector2(0, 0),
                        new Vector2(0, 1),
                        new Vector2(1, 1),
                        new Vector2(1, 0),
                    },
                    triangles = new int[] { 0, 1, 2, 0, 2, 3 }
                };

                return mesh;
            }

            private void CreateTexture()
            {
                var width = m_EndGridX - m_StartGridX + 1;
                var height = m_EndGridY - m_StartGridY + 1;

                m_Texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Point
                };
            }

            private readonly int m_StartGridX;
            private readonly int m_StartGridY;
            private readonly int m_EndGridX;
            private readonly int m_EndGridY;
            private GameObject m_Root;
            private Texture2D m_Texture;
            private Material m_Material;
            private readonly AttributeSystem.LayerBase m_Layer;
            private readonly Color32[] m_Buffer;
            private bool m_Dirty = true;
        }
    }
}