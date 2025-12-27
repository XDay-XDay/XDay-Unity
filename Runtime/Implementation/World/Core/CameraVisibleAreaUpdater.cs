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

namespace XDay.WorldAPI
{
    internal class CameraVisibleAreaUpdater : ICameraVisibleAreaUpdater
    {
        public Rect CurrentArea => m_CurrentArea;
        public Rect PreviousArea => m_PreviousArea;

        public CameraVisibleAreaUpdater(IWorld world)
        {
            Reset();
            m_World = world;
        }

        public void Reset()
        {
            m_PreviousArea = new Rect(-30000, 30000, 1, 1);
            m_CurrentArea = m_PreviousArea;
        }

        public bool BeginUpdate()
        {
            m_AreaChanged = false;

            var expandedArea = m_World.CameraVisibleAreaCalculator.ExpandedArea;

            if (Helper.GT(Mathf.Abs(expandedArea.x - m_CurrentArea.x), m_DistanceThreshold) ||
                Helper.GT(Mathf.Abs(expandedArea.y - m_CurrentArea.y), m_DistanceThreshold) ||
                Helper.GT(Mathf.Abs(expandedArea.xMax - m_CurrentArea.xMax), m_DistanceThreshold) ||
                Helper.GT(Mathf.Abs(expandedArea.yMax - m_CurrentArea.yMax), m_DistanceThreshold))
            {
                m_AreaChanged = m_CurrentArea != expandedArea;
                m_CurrentArea = expandedArea;
            }
            return m_AreaChanged;
        }

        public void EndUpdate()
        {
            if (m_AreaChanged)
            {
                m_PreviousArea = m_CurrentArea;
            }
        }

        public void SetDistanceThreshold(float distance)
        {
            m_DistanceThreshold = distance;
        }

        private IWorld m_World;
        private Rect m_PreviousArea;
        private Rect m_CurrentArea;
        private float m_DistanceThreshold = 0;
        private bool m_AreaChanged = false;
    }
}


//XDay