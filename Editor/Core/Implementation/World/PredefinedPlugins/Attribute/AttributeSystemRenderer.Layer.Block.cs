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

using System.Buffers;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Attribute.Editor
{
    public partial class AttributeSystemRenderer
    {
        private class BlockRenderer
        {
            public int StartGridX => m_StartGridX;
            public int StartGridY => m_StartGridY;
            public int EndGridX => m_EndGridX;
            public int EndGridY => m_EndGridY;

            public BlockRenderer(int startGridX, int startGridY, int endGridX, int endGridY, AttributeSystem.LayerBase layer, Transform parent, ArrayPool<Color32> pool)
            {
                m_StartGridX = startGridX;
                m_StartGridY = startGridY;
                m_EndGridX = endGridX;
                m_EndGridY = endGridY;
                m_Layer = layer;
                m_DirtyMinX = m_StartGridX;
                m_DirtyMinY = m_StartGridY;
                m_DirtyMaxX = m_EndGridX;
                m_DirtyMaxY = m_EndGridY;
                m_Pool = pool;

                CreateRoot(parent);

                UpdateColors(true, false);
            }

            public void Uninitialize()
            {
                Helper.DestroyUnityObject(m_Texture);
                Helper.DestroyUnityObject(m_Material);
                Helper.DestroyUnityObject(m_Root);
            }

            public bool UpdateGrid(int minX, int minY, int maxX, int maxY)
            {
                var validRange = CheckRange(minX, minY, maxX, maxY,
                    out var validMinX, out var validMinY, out var validMaxX, out var validMaxY);

                if (validRange)
                {
                    m_Dirty = true;
                    m_DirtyMinX = validMinX;
                    m_DirtyMinY = validMinY;
                    m_DirtyMaxX = validMaxX;
                    m_DirtyMaxY = validMaxY;
                    return true;
                }

                return false;
            }

            public void UpdateColors(bool forceUpdate, bool updateAll)
            {
                if (updateAll)
                {
                    m_DirtyMinX = m_StartGridX;
                    m_DirtyMinY = m_StartGridY;
                    m_DirtyMaxX = m_EndGridX;
                    m_DirtyMaxY = m_EndGridY;
                }

                var validRange = CheckRange(m_DirtyMinX, m_DirtyMinY, m_DirtyMaxX, m_DirtyMaxY, 
                    out var validMinX, out var validMinY, out var validMaxX, out var validMaxY);
                if (!validRange)
                {
                    return;
                }

                if (m_Dirty || forceUpdate)
                {
                    m_Dirty = false;

                    var bw = validMaxX - validMinX + 1;
                    var bh = validMaxY - validMinY + 1;
                    var colors = m_Pool.Rent(bw * bh);

                    var idx = 0;
                    for (var y = validMinY; y <= validMaxY; ++y)
                    {
                        for (var x = validMinX; x <= validMaxX; ++x)
                        {
                            colors[idx] = m_Layer.GetColor(x, y);
                            ++idx;
                        }
                    }

                    m_Texture.SetPixels32(validMinX - m_StartGridX, validMinY - m_StartGridY, validMaxX - validMinX + 1, validMaxY - validMinY + 1, colors);
                    m_Texture.Apply();

                    m_Pool.Return(colors);
                }
            }

            private bool CheckRange(int minX, int minY, int maxX, int maxY,
                        out int validMinX, out int validMinY, out int validMaxX, out int validMaxY)
            {
                validMinX = 0;
                validMinY = 0;
                validMaxX = 0;
                validMaxY = 0;

                if (minX > m_EndGridX ||
                    minY > m_EndGridY ||
                    maxX < m_StartGridX ||
                    maxY < m_StartGridY)
                {
                    return false;
                }

                validMinX = Mathf.Max(minX, m_StartGridX);
                validMinY = Mathf.Max(minY, m_StartGridY);
                validMaxX = Mathf.Min(maxX, m_EndGridX);
                validMaxY = Mathf.Min(maxY, m_EndGridY);

                return true;
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
                        new(0, 0, 0),
                        new(0, 0, height),
                        new(width, 0, height),
                        new(width, 0, 0),
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
            private int m_DirtyMinX = -1;
            private int m_DirtyMinY = -1;
            private int m_DirtyMaxX = -1;
            private int m_DirtyMaxY = -1;
            private bool m_Dirty = true;
            private ArrayPool<Color32> m_Pool;
        }
    }
}
