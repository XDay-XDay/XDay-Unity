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



namespace XDay.WorldAPI.Tile
{
    internal partial class TileSystem
    {
        public float GetHeightAtPos(float x, float z)
        {
            var coord = RotatedPositionToCoordinate(x, z);
            var tile = GetTile(coord.x, coord.y);
            if (tile != null)
            {
                if (tile.VertexHeights == null)
                {
                    return 0;
                }

                float localX = coord.x * m_TileWidth + m_Origin.x;
                float localZ = coord.y * m_TileHeight + m_Origin.y;
                float gridSize = m_TileWidth / tile.MeshResolution;
                return tile.GetHeightAtPos(x - localX, z - localZ, gridSize);
            }

            return 0;
        }
    }
}
