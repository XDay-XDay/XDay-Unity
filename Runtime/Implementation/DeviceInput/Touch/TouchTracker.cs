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

namespace XDay.InputAPI
{
    internal class TouchData
    {
        public Vector2 Position;
        public float Time;
        public float Movement;
    }

    internal class TouchTracker
    {
        public int Count => m_Touches.Count;
        public Vector2 Start => m_Start;
        public Vector2 Current => m_Current;
        public Vector2 Previous => m_Previous;

        public TouchTracker(float minMovementThreshold, int maxCount)
        {
            m_Touches = new List<Vector2>(maxCount);
            m_MaxCount = maxCount;
            m_MinMovementThreshold = minMovementThreshold;
        }

        public Vector2 GetTouchPosition(int index)
        {
            return m_Touches[index];
        }

        public void Track(Vector2 position)
        {
            var moved = false;
            if (m_Previous != Vector2.zero &&
                (position - m_Previous).sqrMagnitude > m_MinMovementThreshold * m_MinMovementThreshold)
            {
                moved = true;
                m_Previous = m_Current;
                m_Current = position;
            }

            if (!moved)
            {
                m_Previous = position;
                m_Current = position;
            }

            if (m_Start == Vector2.zero)
            {
                m_Start = position;
            }

            if (m_Touches.Count == 0 ||
                m_Touches[^1] != position)
            {
                if (m_Touches.Count >= m_MaxCount)
                {
                    m_Touches.RemoveAt(0);
                }
                m_Touches.Add(position);
            }
        }

        public void Clear()
        {
            m_Touches.Clear();
            m_Start = Vector2.zero;
            m_Current = Vector2.zero;
            m_Previous = Vector2.zero;
        }

        private float m_MinMovementThreshold;
        private Vector2 m_Start;
        private Vector2 m_Previous;
        private Vector2 m_Current = Vector2.zero;
        private int m_MaxCount;
        private List<Vector2> m_Touches;
    }
}

//XDay