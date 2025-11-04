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
using XDay.InputAPI;

namespace XDay.CameraAPI
{
    internal partial class BehaviourDrag
    {
        internal class Sender : BehaviourRequestSender
        {
            public Sender(CameraManipulator manipulator, IDeviceInput deviceInput, TouchID touchID, float moveThreshold)
                : base(manipulator, deviceInput)
            {
                m_Motion = deviceInput.CreateInertialDragMotion(touchID,
                    manipulator.Setup.Direction == CameraDirection.XZ ? new Plane(Vector3.up, 0) : new Plane(Vector3.back, 0),
                    moveThreshold, manipulator.Camera, DeviceTouchType.TouchNotStartFromUI);
                SetActive(true);
            }

            protected override bool OnMatch(IMotion motion, MotionState state)
            {
                var req = Request.Create(
                    layer: 0, 
                    priority: 1,
                    state, 
                    RequestQueueType.Replace,
                    m_Motion.TouchCount,
                    m_Motion.DraggedOffset, 
                    m_Motion.DragTime, 
                    m_Motion.MovedDistance, 
                    m_Motion.SlideDirection);

                m_Manipulator.AddRequest(req);

                return false;
            }

            protected override IMotion Motion => m_Motion;

            private IInertialDragMotion m_Motion;
        }
    }
}

//XDay