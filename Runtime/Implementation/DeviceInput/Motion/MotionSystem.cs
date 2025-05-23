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
    internal class MotionSystem
    {
        public void Update()
        {
            foreach (var motion in m_Motions)
            {
                motion.Update();
            }
        }

        public ScrollMotion CreateScrollMotion(float interval, IDeviceInput device)
        {
            return AddMotion(new ScrollMotion(AllocateID, 16.0f, interval, TouchID.Left, device));
        }

        public InertialDragMotion CreateInertialDragMotion(TouchID name, Plane plane, Camera camera, IDeviceInput device)
        {
            return AddMotion(new InertialDragMotion(AllocateID, name, camera, plane, device));
        }

        public PinchMotion CreatePinchMotion(float minHeight, float maxHeight, float range, Camera camera, bool enableRotation, IDeviceInput device)
        {
            return AddMotion(new PinchMotion(AllocateID, minHeight, maxHeight, range, camera, enableRotation, device));
        }

        public MouseScrollMotion CreateMouseScrollMotion(IDeviceInput device)
        {
            return AddMotion(new MouseScrollMotion(AllocateID, device));
        }

        public DoubleClickMotion CreateDoubleClickMotion(float clickInterval, IDeviceInput device)
        {
            return AddMotion(new DoubleClickMotion(AllocateID, clickInterval, device));
        }

        public LongPressMotion CreateLongPressMotion(float duration, IDeviceInput device)
        {
            return AddMotion(new LongPressMotion(AllocateID, duration, device));
        }

        public ClickMotion CreateClickMotion(IDeviceInput device)
        {
            return AddMotion(new ClickMotion(AllocateID, device));
        }

        public DragMotion CreateDragMotion(TouchID name, float moveThreshold, IDeviceInput device)
        {
            return AddMotion(new DragMotion(AllocateID, name, moveThreshold, device));
        }

        public void RemoveMotion(int id)
        {
            for (var i = 0; i < m_Motions.Count; i++)
            {
                if (m_Motions[i].ID == id)
                {
                    m_Motions.RemoveAt(i);
                    return;
                }
            }
            Debug.Assert(false, $"Remove motion failed with id: {id}");
        }

        private T AddMotion<T>(T motion) where T : Motion
        {
            m_Motions.Add(motion);
            return motion;
        }

        private int AllocateID => ++m_NextID;

        private int m_NextID;
        private List<Motion> m_Motions = new();
    }
}


//XDay