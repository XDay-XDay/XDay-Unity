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
    public class FloatBounds2D
    {
        public bool IsEmpty => m_Min.y > m_Max.y || m_Min.x > m_Max.x;
        public Vector2 Min => m_Min;
        public Vector2 Max => m_Max;
        public Vector2 Size => new(m_Max.x - m_Min.x, m_Max.y - m_Min.y);
        public Vector2 Center => (m_Min + m_Max) / 2;

        public FloatBounds2D(Vector2 min, Vector2 max)
        {
            SetMinMax(min, max);
        }

        public FloatBounds2D()
        {
            Reset();
        }

        public void AddRect(Rect r)
        {
            AddPoint(r.x, r.y);
            AddPoint(r.x + r.width - 1, r.y + r.height - 1);
        }

        public void AddRect(float x, float y, float width, float height)
        {
            AddPoint(x, y);
            AddPoint(x + width - 1, y + height - 1);
        }

        public void AddPoint(float x, float y)
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

        public void SetMinMax(Vector2 min, Vector2 max)
        {
            m_Min = min;
            m_Max = max;
        }

        public void Reset()
        {
            m_Max = new Vector2(float.MinValue, float.MinValue);
            m_Min = new Vector2(float.MaxValue, float.MaxValue);
        }

        private Vector2 m_Min;
        private Vector2 m_Max;
    }
}

//XDay
