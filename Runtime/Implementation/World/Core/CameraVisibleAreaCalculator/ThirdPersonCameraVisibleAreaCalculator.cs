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

using XDay.UtilityAPI;
using UnityEngine;

namespace XDay.WorldAPI
{
    public class ThirdPersonCameraVisibleAreaCalculator : ICameraVisibleAreaCalculator
    {
        public Rect VisibleArea => m_VisibleArea;
        public Rect ExpandedArea => m_ExpandedArea;
        public Vector2 ExpandSize { get => m_ExpandSize; set => m_ExpandSize = value; }

        public ThirdPersonCameraVisibleAreaCalculator(Vector3 pos, Vector2 visibleSize)
        {
            m_Center = pos;
            m_VisibleSize = visibleSize;

            Calculate();
        }

        public void DebugDraw()
        {
            var oldColor = Gizmos.color;
            Gizmos.color = Color.red;

            Gizmos.DrawWireCube(m_ExpandedArea.center.ToVector3XZ(), m_ExpandedArea.size.ToVector3XZ());

            Gizmos.color = Color.green;

            Gizmos.DrawWireCube(m_VisibleArea.center.ToVector3XZ(), m_VisibleArea.size.ToVector3XZ());

            Gizmos.color = oldColor;
        }

        public void Update(Camera camera)
        {
            if (camera != null)
            {
                var cameraPos = camera.transform.position;
                if (cameraPos != m_LastCameraPos)
                {
                    m_LastCameraPos = cameraPos;
                    m_Center = camera.transform.position;

                    Calculate();
                }
            }
        }

        public Vector3 GetFocusPoint(Camera camera)
        {
            throw new System.NotImplementedException();
        }

        public Rect GetVisibleAreas(Camera camera)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 以梯形的下部分作为边框
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        public Rect GetNarrowVisibleAreas(Camera camera)
        {
            throw new System.NotImplementedException();
        }

        private void Calculate()
        {
            var center2 = m_Center.ToVector2();
            m_VisibleArea = new Rect(center2 - m_VisibleSize * 0.5f, m_VisibleSize);
            m_ExpandedArea = Helper.ExpandRect(m_VisibleArea, m_ExpandSize);
        }

        private Rect m_VisibleArea;
        private Rect m_ExpandedArea;
        private Vector2 m_ExpandSize;
        private Vector3 m_Center;
        private Vector2 m_VisibleSize;
        private Vector3 m_LastCameraPos;
    }
}

//XDay