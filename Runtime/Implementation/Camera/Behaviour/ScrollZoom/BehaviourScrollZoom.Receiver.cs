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


namespace XDay.CameraAPI
{
    internal partial class BehaviourScrollZoom
    {
        internal class Receiver : BehaviourRequestReceiver
        {
            public override BehaviourType Type => BehaviourType.ScrollZoom;

            public Receiver(CameraManipulator manipulator, float zoomSpeed)
                : base(manipulator)
            {
                m_ZoomSpeed = zoomSpeed;
            }

            protected override BehaviourState UpdateInternal(CameraTransform pos)
            {
                return BehaviourState.Over;
            }

            protected override void RespondInternal(BehaviourRequest request, CameraTransform pos)
            {
                var req = request as Request;
                if (req.ScrollDelta != 0)
                {
                    var newZoomFactor = m_Manipulator.ZoomFactor - req.ScrollDelta * m_ZoomSpeed;

                    m_Manipulator.Setup.AltitudeManager.DecomposeZoomFactor(newZoomFactor, out var focalLength, out _);
                    pos.CurrentLogicPosition = m_Manipulator.FocusPoint - m_Manipulator.Forward * focalLength;
                }
            }

            private float m_ZoomSpeed;
        }
    }
}

//XDay