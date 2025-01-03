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

namespace XDay.WorldAPI
{
    internal class CameraVisibleAreaUpdater : ICameraVisibleAreaUpdater
    {
        public Rect CurrentArea => m_CurrentArea;
        public Rect PreviousArea => m_PreviousArea;

        public CameraVisibleAreaUpdater(ICameraVisibleAreaCalculator calculator)
        {
            Reset();
            m_CameraVisibleAreaCalculator = calculator;
        }

        public void Reset()
        {
            m_PreviousArea = new Rect(-30000, 30000, 1, 1);
            m_CurrentArea = m_PreviousArea;
        }

        public bool BeginUpdate()
        {
            var areaChanged = m_CurrentArea != m_CameraVisibleAreaCalculator.ExpandedArea;
            m_CurrentArea = m_CameraVisibleAreaCalculator.ExpandedArea;
            return areaChanged;
        }

        public void EndUpdate()
        {
            m_PreviousArea = m_CurrentArea;
        }

        private ICameraVisibleAreaCalculator m_CameraVisibleAreaCalculator;
        private Rect m_PreviousArea;
        private Rect m_CurrentArea;
    }
}


//XDay