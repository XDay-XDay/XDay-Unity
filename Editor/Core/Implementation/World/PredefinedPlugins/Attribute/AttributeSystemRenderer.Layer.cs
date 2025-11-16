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
        internal class LayerRenderer
        {
            public GameObject Root => m_Root;
            public int LayerID => m_Layer.ID;

            public LayerRenderer(AttributeSystem.LayerBase layer, Transform parent, ArrayPool<Color32> pool)
            {
                m_Layer = layer;

                m_Root = new GameObject(layer.Name);
                m_Root.AddComponent<NoKeyDeletion>();
                m_Root.transform.SetParent(parent, false);
                m_Root.tag = "EditorOnly";
                m_Root.SetActive(layer.IsActive);

                var horizontalGridCount = layer.HorizontalGridCount;
                var verticalGridCount = layer.VerticalGridCount;
                var blockWidth = Mathf.CeilToInt(horizontalGridCount / layer.HorizontalBlockCount);
                var blockHeight = Mathf.CeilToInt(verticalGridCount / layer.VerticalBlockCount);
                m_BlockRenderers = new BlockRenderer[layer.HorizontalBlockCount * layer.VerticalBlockCount];
                for (var y = 0; y < layer.VerticalBlockCount; ++y)
                {
                    for (var x = 0; x < layer.HorizontalBlockCount; ++x)
                    {
                        var startGridX = x * blockWidth;
                        var startGridY = y * blockHeight;
                        var endGridX = Mathf.Min(startGridX + blockWidth, horizontalGridCount) - 1;
                        var endGridY = Mathf.Min(startGridY + blockHeight, verticalGridCount) - 1;

                        m_BlockRenderers[y * layer.HorizontalBlockCount + x] = new BlockRenderer(startGridX, startGridY, endGridX, endGridY, m_Layer, m_Root.transform, pool);
                    }
                }

                var shader = Shader.Find("XDay/Grid");
                var material = new Material(shader);
                m_GridMesh = new GridMesh(layer.Name, layer.Origin, layer.HorizontalGridCount, layer.VerticalGridCount, layer.GridWidth, layer.GridHeight, material, new Color32(255, 190, 65, 150), m_Root.transform, true, 3000 + m_Layer.ObjectIndex * 5 + 1);
                ShowGrid(layer.GridVisible);
                Object.DestroyImmediate(material);
            }

            public void Uninitialize()
            {
                m_GridMesh.OnDestroy();

                foreach (var block in m_BlockRenderers)
                {
                    block.Uninitialize();
                }
                m_BlockRenderers = null;

                Helper.DestroyUnityObject(m_Root);
                m_Root = null;
            }

            public void UpdateGrid(int minX, int minY, int maxX, int maxY)
            {
                foreach (var block in m_BlockRenderers)
                {
                    block.UpdateGrid(minX, minY, maxX, maxY);
                }
            }

            public void Update(bool forceUpdate, bool updateAll)
            {
                foreach (var block in m_BlockRenderers)
                {
                    block.UpdateColors(forceUpdate, updateAll);
                }
            }

            public void ShowGrid(bool show)
            {
                m_GridMesh.SetActive(show);
            }

            private GameObject m_Root;
            private BlockRenderer[] m_BlockRenderers;
            private AttributeSystem.LayerBase m_Layer;
            private GridMesh m_GridMesh;
        }
    }
}
