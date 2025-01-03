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

namespace XDay.UtilityAPI.Math
{
    public class IntBounds2D
    {
        public bool IsEmpty => m_Min.y > m_Max.y || m_Min.x > m_Max.x;
        public Vector2Int Min => m_Min;
        public Vector2Int Max => m_Max;
        public Vector2Int Size => new(m_Max.x - m_Min.x + 1, m_Max.y - m_Min.y + 1);
        public Vector2Int Center => (m_Min + m_Max) / 2;

        public IntBounds2D(Vector2Int min, Vector2Int max)
        {
            SetMinMax(min, max);
        }

        public IntBounds2D()
        {
            Reset();
        }

        public void AddRect(int x, int y, int width, int height)
        {
            AddPoint(x, y);
            AddPoint(x + width - 1, y + height - 1);
        }

        public void AddPoint(int x, int y)
        {
            if (x < m_Min.x)
            {
                m_Min.x = x;
            }
            if (x > m_Max.x)
            {
                m_Max.x = x;
            }
            if (y < m_Min.y)
            {
                m_Min.y = y;
            }
            if (y > m_Max.y)
            {
                m_Max.y = y;
            }
        }

        public void SetMinMax(Vector2Int min, Vector2Int max)
        {
            m_Min = min;
            m_Max = max;
        }

        public void Reset()
        {
            m_Max = new Vector2Int(int.MinValue, int.MinValue);
            m_Min = new Vector2Int(int.MaxValue, int.MaxValue);
        }

        private Vector2Int m_Min;
        private Vector2Int m_Max;
    }
}

//XDay
