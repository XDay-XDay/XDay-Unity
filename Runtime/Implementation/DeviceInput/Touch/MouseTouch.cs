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

namespace XDay.InputAPI
{
    internal class MouseTouch : ITouch
    {
        public int ID => m_ID;
        public Vector2 Start => m_Tracker.Start;
        public Vector2 Previous => m_Tracker.Previous;
        public Vector2 Current => m_Tracker.Current;
        public float Scroll { get => m_Scroll; set => m_Scroll = value; }
        public bool IsActive { get => m_IsActive; set => m_IsActive = value; }
        public bool StartFromUI { get => m_StartFromUI; set => m_StartFromUI = value; }
        public TouchState State { get => m_State; set => m_State = value; }

        public MouseTouch(int id)
        {
            m_ID = id;
            m_Tracker = new(3, 5);
        }

        public void Clear()
        {
            m_IsActive = false;
            m_Tracker.Clear();
            m_StartFromUI = false;
        }

        public bool MovedMoreThan(float distanceSquare)
        {
            return (Current - Previous).sqrMagnitude >= distanceSquare;
        }

        public void Track(Vector2 position)
        {
            m_Tracker.Track(position);
        }

        public Vector2 GetTouchPosition(int index)
        {
            return m_Tracker.GetTouchPosition(index);
        }

        private int m_ID;
        private bool m_IsActive;
        private bool m_StartFromUI;
        private float m_Scroll;
        private TouchTracker m_Tracker;
        private TouchState m_State;
    }
}

//XDay