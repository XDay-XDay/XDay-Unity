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

namespace XDay.WorldAPI.Tile
{
    internal class NormalTileData
    {
        public bool Visible { get => m_Visible; set => m_Visible = value; }
        public bool HasHeightData => m_HasHeightData;
        public float[] VertexHeights => m_VertexHeights;
        public int MeshResolution => m_MeshResolution;

        public NormalTileData(string path, float[] vertexHeights, bool hasHeightData)
        {
            m_Path = path;
            m_VertexHeights = vertexHeights;
            m_HasHeightData = hasHeightData;
            if (vertexHeights != null)
            {
                m_MeshResolution = (int)Mathf.Sqrt(vertexHeights.Length) - 1;
            }
        }

        public void Init(IResourceDescriptorSystem system)
        {
            m_Descriptor = system.QueryDescriptor(m_Path);
            m_Path = null;
        }

        public string GetPath(int lod)
        {
            return m_Descriptor.GetPath(lod);
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

        private string m_Path;
        private IResourceDescriptor m_Descriptor;
        private bool m_Visible = false;
        private bool m_HasHeightData = false;
        private readonly float[] m_VertexHeights;
        private int m_MeshResolution;
    }
}

//XDay