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

namespace XDay.WorldAPI.City.Editor
{
    class DisplayGridCost
    {
        public DisplayGridCost(int horizontalGridCount, int verticalGridCount)
        {
            m_HorizontalGridCount = horizontalGridCount;
            m_VerticalGridCount = verticalGridCount;
        }

        public void Render(Grid grid)
        {
            if (IsActive)
            {
                for (var i = 0; i < m_VerticalGridCount; ++i)
                {
                    for (var j = 0; j < m_HorizontalGridCount; ++j)
                    {
                        var position = grid.CoordinateToGridCenterPosition(j, i);
                        var cost = grid.GetGridCost(j, i);
                        Handles.Label(position, cost.ToString());
                    }
                }
            }
        }

        public bool IsActive { set; get; } = false;

        readonly int m_HorizontalGridCount;
        readonly int m_VerticalGridCount;
    }
}