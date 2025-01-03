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
    internal class DeviceTouch : ITouch
    {
        public int ID => m_ID;
        public Vector2 Start => m_Tracker.Start;
        public Vector2 Previous => m_Tracker.Previous;
        public Vector2 Current => m_Tracker.Current;
        public float Scroll => 0;
        public bool StartFromUI { get => m_IsHitUIWhenTouchBegin; set => m_IsHitUIWhenTouchBegin = value; }
        public TouchState State { get => m_State; set => m_State = value; }

        public DeviceTouch()
        {
            m_Tracker = new(3, 6);
        }

        public void Init(int id)
        {
            m_ID = id;
            m_State = TouchState.Start;
            m_Tracker.Clear();
        }

        public bool MovedMoreThan(float distanceSqr)
        {
            return (Current - Previous).sqrMagnitude >= distanceSqr;
        }

        public Vector2 GetTouchPosition(int index)
        {
            return m_Tracker.GetTouchPosition(index);
        }

        public void Track(Vector2 pos)
        {
            m_Tracker.Track(pos);
        }

        public void Clear()
        {
            m_ID = -1;
            m_IsHitUIWhenTouchBegin = false;
            m_Tracker.Clear();
        }

        private int m_ID;
        private bool m_IsHitUIWhenTouchBegin;
        private TouchTracker m_Tracker;
        private TouchState m_State;
    }
}

//XDay