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

using System.Collections.Generic;
using UnityEngine;

namespace XDay.GUIAPI
{
    internal class DynamicAtlasPage
    {
        public int Index => m_Index;
        public Texture2D Texture => m_Texture;
        public List<IntegerRectangle> FreeAreasList => m_FreeAreasList;

        unsafe public DynamicAtlasPage(int index, int width, int height, string name)
        {
            m_Index = index;
            m_Width = width;
            m_Height = height;

            m_Texture = new Texture2D(width, height, AtlasHelper.GetTextureFormat(), false, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = string.Format("DynamicAtlas-{0}-{1}*{2}-{3}", name, width, height, index),
            };
            m_Texture.Apply(updateMipmaps:false, makeNoLongerReadable:true);
            if (DynamicAtlasMgr.S.ClearTexture != null)
            {
                Graphics.CopyTexture(DynamicAtlasMgr.S.ClearTexture, m_Texture);
            }

            var area = new IntegerRectangle(0, 0, m_Width, m_Height);
            m_FreeAreasList.Add(area);
        }

        public Vector2 AddTexture(int posX, int posY, Sprite sprite, int alignment)
        {
            //可以把一张贴图画到另一张贴图上
            var rect = sprite.textureRect;
            var alignedRect = AlignRect(rect, alignment, m_Texture.width, m_Texture.height, posX, posY);
            var minOffset = rect.min - alignedRect.min;

            if (sprite.texture != null)
            {
                Graphics.CopyTexture(sprite.texture, 0, 0, srcX: (int)alignedRect.x, srcY: (int)alignedRect.y, (int)alignedRect.width, (int)alignedRect.height, m_Texture, 0, 0, posX, posY);
            }
            else
            {
                Debug.LogError($"Sprite {sprite.name} texture is null");
            }
            return minOffset;
        }

        private Rect AlignRect(Rect rect, int alignment, int maxWidth, int maxHeight, int targetX, int targetY)
        {
            var minX = AtlasHelper.FloorRound(rect.x, alignment);
            var minY = AtlasHelper.FloorRound(rect.y, alignment);
            var maxX = AtlasHelper.CeilRound(rect.xMax, alignment);
            var maxY = AtlasHelper.CeilRound(rect.yMax, alignment);
            if (maxX >= maxWidth)
            {
                maxX = maxWidth - 1;
            }
            if (maxY >= maxHeight)
            {
                maxY = maxHeight - 1;
            }

            var width = maxX - minX;
            var height = maxY - minY;

            var targetMaxX = targetX + width;
            if (targetMaxX >= maxWidth)
            {
                width -= (targetMaxX - maxWidth + 1);
            }

            width = AtlasHelper.FloorRound(width, alignment);

            var targetMaxY = targetY + height;
            if (targetMaxY >= maxHeight)
            {
                height -= (targetMaxY - maxHeight + 1);
            }

            height = AtlasHelper.FloorRound(height, alignment);

            return new Rect(minX, minY, width, height);
        }

        public void RemoveTexture(Rect rect)
        {
            int width = (int)rect.width;
            int height = (int)rect.height;
            Color32[] colors = new Color32[width * height];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }

            m_Texture.SetPixels32((int)rect.x, (int)rect.y, width, height, colors);
            m_Texture.Apply();
        }

        /// <summary>
        /// 搜寻空白区域
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public int GetFreeAreaIndex(int width, int height, int padding, int alignment)
        {
            if (width > m_Width || height > m_Height)
            {
                Debug.LogError("To Large Texture for atlas");
                return -1;
            }

            IntegerRectangle best = new IntegerRectangle(m_Width + 1, m_Height + 1, 0, 0);
            int index = -1;

            int paddedWidth = width + padding;
            int paddedHeight = height + padding;

            for (int i = m_FreeAreasList.Count - 1; i >= 0; i--)
            {
                IntegerRectangle free = m_FreeAreasList[i];

                // if (free.x < width || free.y < height)
                {
                    if (free.x < best.x && paddedWidth <= free.width && paddedHeight <= free.height)//如果这个Free大小可以容纳目标大小的话
                    {
                        index = i;
                        if ((paddedWidth == free.width && free.width <= free.height && free.Right < m_Width) || (paddedHeight == free.height && free.height <= free.width))//如果这个区域正好可以放得下
                            break;
                        best = free;
                    }
                    else
                    {
                        // Outside the current packed area, no padding required
                        if (free.x < best.x && width <= free.width && height <= free.height)//如果不算padding距离也可以放得下的话，也可以放进去
                        {
                            index = i;
                            if ((width == free.width && free.width <= free.height && free.Right < m_Width) || (height == free.height && free.height <= free.width))
                                break;

                            best = free;
                        }
                    }
                }
            }

            return index;
        }

        public void AddFreeArea(IntegerRectangle area)
        {
            m_FreeAreasList.Add(area);
        }

        public void RemoveFreeArea(IntegerRectangle area)
        {
            m_FreeAreasList.Remove(area);
        }

        internal void OnDestroy()
        {
            UnityEngine.Object.Destroy(m_Texture);
            m_Texture = null;
        }

        internal unsafe void Clear()
        {
            m_FreeAreasList.Clear();
            var area = new IntegerRectangle(0, 0, m_Width, m_Height);
            m_FreeAreasList.Add(area);

            if (!DynamicAtlasMgr.S.Pause)
            {
                ClearTexture();
            }
        }

        private void ClearTexture()
        {
            if (m_Texture != null)
            {
                Graphics.CopyTexture(DynamicAtlasMgr.S.ClearTexture, m_Texture);
            }
        }

        private readonly int m_Index;
        private Texture2D m_Texture;
        private readonly List<IntegerRectangle> m_FreeAreasList = new();
        private readonly int m_Width;
        private readonly int m_Height;
    }
}
