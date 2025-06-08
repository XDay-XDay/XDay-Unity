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

namespace XDay.WorldAPI.House
{
    internal class WalkableStateDebugger : GridStateDebugger
    {
        public WalkableStateDebugger(string name, int horizontalGridCount, int verticalGridCount, float gridSize, float gridHeight, bool[] walkable)
            : base(name, horizontalGridCount, verticalGridCount, gridSize, gridHeight)
        {
            m_Walkable = new bool[verticalGridCount * horizontalGridCount];
            for (var i = 0; i < m_Walkable.Length; ++i)
            {
                m_Walkable[i] = walkable[i];
            }
        }

        public void SetWalkable(int x, int y, int width, int height, bool walkable)
        {
            var maxX = x + width - 1;
            var maxY = y + height - 1;
            for (var i = y; i <= maxY; ++i)
            {
                for (var j = x; j <= maxX; ++j)
                {
                    if (IsValidCoordinate(j, i))
                    {
                        m_Walkable[i * HorizontalGridCount + j] = walkable;
                    }
                }
            }

            UpdateColors();
        }

        public bool IsWalkable(int x, int y, int width = 1, int height = 1)
        {
            var maxX = x + width - 1;
            var maxY = y + height - 1;
            for (var i = y; i <= maxY; ++i)
            {
                for (var j = x; j <= maxX; ++j)
                {
                    if (IsValidCoordinate(j, i))
                    {
                        if (!m_Walkable[i * HorizontalGridCount + j])
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public void SetAll(bool set)
        {
            for (var i = 0; i < m_Walkable.Length; ++i)
            {
                m_Walkable[i] = set;
            }
            UpdateColors();
        }

        protected override Color GetColor(int x, int y)
        {
            return IsWalkable(x, y, 1, 1) ? Green : Black;
        }

        private bool[] m_Walkable;
        private static Color32 Green = new(35, 232, 85, 255);
        private static Color32 Black = new(30, 30, 30, 255);
    }
}
