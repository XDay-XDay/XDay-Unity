/*
 * Copyright (c) 2024 XDay
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
    internal partial class BehaviourMouseZoom
    {
        internal class Receiver : BehaviourRequestReceiver
        {
            public override BehaviourType Type => BehaviourType.MouseZoom;

            public Receiver(CameraManipulator manipulator)
                : base(manipulator)
            {
            }

            protected override BehaviourState UpdateInternal(CameraTransform pos)
            {
                return BehaviourState.Over;
            }

            protected override void RespondInternal(BehaviourRequest request, CameraTransform pos)
            {
                if (Input.GetKey(KeyCode.LeftAlt))
                {
                    var req = request as Request;
                    var mousePos = req.TouchPosition;

                    if (req.State == MotionState.Start)
                    {
                        m_PrevMousePos = mousePos;
                    }

                    var deltaDis = (m_PrevMousePos.x - mousePos.x + m_PrevMousePos.y - mousePos.y) * 0.5f;
                    m_PrevMousePos = mousePos;

                    var scrollRate = m_Speed * deltaDis * pos.CurrentLogicPosition.y;
                    pos.CurrentLogicPosition += scrollRate * (pos.CurrentLogicRotation * Vector3.forward);
                }
            }

            private float m_Speed = 0.01f;
            private Vector2 m_PrevMousePos = Vector2.zero;
        }
    }
}

//XDay