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

namespace XDay.UtilityAPI
{
    internal class Mover : IMover
    {
        public Vector2 OldPosition => m_OldPosition;
        public Vector2 NewPosition => m_NewPosition;

        public Mover()
        {
            Reset();
        }

        public void Update(Vector2 newPos)
        {
            if (m_OldPosition.x < INVALID_VALUE + 10.0f)
            {
                m_OldPosition = newPos;
            }
            else
            {
                m_OldPosition = m_NewPosition;
            }
            m_NewPosition = newPos;
        }

        public void Reset()
        {
            m_OldPosition.Set(INVALID_VALUE, INVALID_VALUE);
            m_NewPosition = m_OldPosition;
        }

        public Vector2 GetMovement()
        {
            if (m_OldPosition.x == INVALID_VALUE)
            {
                return Vector2.zero;
            }
            return m_NewPosition - m_OldPosition;
        }

        private Vector2 m_OldPosition;
        private Vector2 m_NewPosition;
        private const float INVALID_VALUE = -100000;
    };
}
