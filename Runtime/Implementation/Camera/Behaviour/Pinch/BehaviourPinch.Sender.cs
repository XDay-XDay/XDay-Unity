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



using XDay.InputAPI;

namespace XDay.CameraAPI
{
    internal partial class BehaviourPinch
    {
        internal class Sender : BehaviourRequestSender
        {
            public Sender(CameraManipulator manipulator, IDeviceInput deviceInput, float minHeight, float maxHeight, float range, bool enableRotation)
                : base(manipulator, deviceInput)
            {
                m_Motion = m_DeviceInput.CreatePinchMotion(minHeight, maxHeight, range, manipulator.Camera, enableRotation);
                SetActive(true);
            }

            protected override bool OnMatch(IMotion motion, MotionState state)
            {
                var request = Request.Create(layer: 0, priority: 0, RequestQueueType.Replace, m_Motion.Center, m_Motion.ZoomRate, state, m_Motion.IsRotating, m_Motion.DragDistance, m_Motion.SlideDirection);

                m_Manipulator.AddRequest(request);

                return false;
            }

            protected override IMotion Motion => m_Motion;

            private IPinchMotion m_Motion;
        }
    }
}


//XDay