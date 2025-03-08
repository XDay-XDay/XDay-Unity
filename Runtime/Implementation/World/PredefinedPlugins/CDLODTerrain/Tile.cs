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


namespace XDay.WorldAPI.CDLODTerrain
{
    internal class Tile
    {
        public int X => m_X;
        public int Y => m_Y;
        public int Index => m_Index;
        public QuadTree QuadTree => m_QuadTree;
        public TerrainSystem Terrain => m_Terrain;
        public string MaterialPath => m_MaterialPath;
        public bool IsVisible { get => m_Visible; set => m_Visible = value; }

        public Tile(string materialPath, int x, int y, int index)
        {
            m_X = x;
            m_Y = y;
            m_Index = index;
            m_MaterialPath = materialPath;
        }

        public void Init(TerrainSystem terrain)
        {
            m_Terrain = terrain;
            var lodCount = m_Terrain.LODCount;
            var tileSize = (int)m_Terrain.TileWidth;
            m_QuadTree = new QuadTree(m_X * tileSize, m_Y * tileSize, tileSize, tileSize, lodCount - 1, m_Terrain.GetHeight);
        }

        private readonly int m_X;
        private readonly int m_Y;
        private readonly int m_Index;
        private QuadTree m_QuadTree;
        private TerrainSystem m_Terrain;
        private readonly string m_MaterialPath;
        private bool m_Visible = false;
    }
}

//XDay