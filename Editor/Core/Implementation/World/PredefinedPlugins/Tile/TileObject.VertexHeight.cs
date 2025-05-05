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
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Tile.Editor
{
    internal sealed partial class TileObject
    {
        public float[] VertexHeights => m_VertexHeights;
        public int MeshResolution => m_MeshResolution;
        public bool IsMeshDirty
        {
            get
            {
                return m_IsMeshDirty;
            }

            set
            {
                m_IsMeshDirty = value;
            }
        }
        public Vector2[] UVs { get => m_UVs; set => m_UVs = value; }

        public float GetHeight(int x, int y)
        {
            if (m_VertexHeights == null)
            {
                return 0;
            }
            int idx = y * (m_MeshResolution + 1) + x;
            return m_VertexHeights[idx];
        }

        public float GetHeightAtPos(float localPosX, float localPosZ, float gridSize)
        {
            var heights = m_VertexHeights;
            if (heights == null)
            {
                return 0;
            }

            var resolution = m_MeshResolution;
            int x = (int)(localPosX / gridSize);
            int y = (int)(localPosZ / gridSize);
            localPosX = localPosX - x * gridSize;
            localPosZ = localPosZ - y * gridSize;
            float height0 = heights[y * (resolution + 1) + x];
            float height2 = heights[(y + 1) * (resolution + 1) + x + 1];
            if (localPosX > localPosZ)
            {
                float height3 = heights[y * (resolution + 1) + x + 1];
                //lower triangle
                Vector3 bc = Helper.CalculateBaryCentricCoord(0, 0, gridSize, gridSize, gridSize, 0, localPosX, localPosZ);
                return bc.x * height0 + bc.y * height2 + bc.z * height3;
            }
            else
            {
                float height1 = heights[(y + 1) * (resolution + 1) + x];
                //upper triangle
                Vector3 bc = Helper.CalculateBaryCentricCoord(0, 0, 0, gridSize, gridSize, gridSize, localPosX, localPosZ);
                return bc.x * height0 + bc.y * height1 + bc.z * height2;
            }
        }

        public bool SetResolution(int resolution)
        {
            if (resolution != m_MeshResolution)
            {
                if (resolution == 0)
                {
                    m_VertexHeights = null;
                    m_MeshResolution = 0;
                }
                else
                {
                    m_VertexHeights = new float[(resolution + 1) * (resolution + 1)];
                    m_MeshResolution = resolution;
                }
                m_IsMeshDirty = true;
                return true;
            }
            return false;
        }

        public void SetHeight(int minX, int minY, int maxX, int maxY, int resolution, List<float> heights, float height, bool dontChangeEdgeVertexHeight)
        {
            if (resolution != m_MeshResolution)
            {
                if (resolution == 0)
                {
                    m_VertexHeights = null;
                    m_MeshResolution = 0;
                }
                else
                {
                    m_VertexHeights = new float[(resolution + 1) * (resolution + 1)];
                    m_MeshResolution = resolution;
                }
            }
            if (m_VertexHeights != null)
            {
                if (dontChangeEdgeVertexHeight)
                {
                    minY = Mathf.Max(1, minY);
                    minX = Mathf.Max(1, minX);
                    maxY = Mathf.Min(resolution - 1, maxY);
                    maxX = Mathf.Min(resolution - 1, maxX);
                }

                if (heights != null && heights.Count > 0)
                {
                    int width = maxX - minX + 1;
                    for (int i = minY; i <= maxY; ++i)
                    {
                        for (int j = minX; j <= maxX; ++j)
                        {
                            int srcIdx = i * (resolution + 1) + j;
                            int dstIdx = (i - minY) * width + j - minX;
                            m_VertexHeights[srcIdx] = heights[dstIdx];
                        }
                    }
                }
                else
                {
                    for (int i = minY; i <= maxY; ++i)
                    {
                        for (int j = minX; j <= maxX; ++j)
                        {
                            int idx = i * (resolution + 1) + j;
                            m_VertexHeights[idx] = height;
                        }
                    }
                }
            }
            m_IsMeshDirty = true;
        }

#if ENABLE_CLIP_MASK
        public void InitClipMask(string name, float tileWidth, float tileHeight, ArrayPool<Color32> arrayPool, bool[,] clipGrids)
        {
            DestroyClipMask();
            if (mResolution > 1)
            {
                mClipMask = new ClipMask(name, mResolution, tileWidth, tileHeight, GetPosition(), arrayPool, clipGrids);
            }
        }

        public void DestroyClipMask()
        {
            mClipMask?.OnDestroy();
            mClipMask = null;
        }

        public bool IsClipped(int x, int y)
        {
            if (mClipMask == null)
            {
                return false;
            }
            return mClipMask.IsClipped(x, y);
        }

        public void ClearClipMask()
        {
            if (mClipMask != null)
            {
                mClipMask.Clear();
            }
        }

        public void ShowClipMask(bool show)
        {
            if (mClipMask != null)
            {
                mClipMask.Show(show);
            }
        }
#endif
        [SerializeField]
        private float[] m_VertexHeights;
        //格子数,顶点数要+1
        [SerializeField]
        private int m_MeshResolution;
        private Vector2[] m_UVs;
        private bool m_IsMeshDirty = true;
        //裁剪数据
#if ENABLE_CLIP_MASK
        private ClipMask ClipMask;
        [SerializeField]
        private bool Clipped = false;
#endif

    }
}
