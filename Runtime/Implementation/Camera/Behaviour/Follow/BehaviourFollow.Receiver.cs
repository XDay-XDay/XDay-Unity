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

namespace XDay.CameraAPI
{
    internal partial class BehaviourFollow
    {
        internal class Receiver : BehaviourRequestReceiver
        {
            public override BehaviourMask Interrupter => m_Interrupters;
            public override BehaviourType Type => BehaviourType.Follow;

            public Receiver(CameraManipulator manipulator)
                : base(manipulator)
            {
                m_LineMovement = new(manipulator);
                m_HorizontalAndVerticalMovement = new(manipulator);
            }

            protected override void RespondInternal(BehaviourRequest request, CameraTransform pos)
            {
                Stop();

                if (request is RequestFollow req)
                {
                    Execute(req, pos);
                }
            }

            public override void OnBeingInterrupted(BehaviourRequest request)
            {
                Stop();
            }

            public override void Over()
            {
                Stop();
            }

            private void Execute(RequestFollow request, CameraTransform transform)
            {
                m_Interrupters = request.Interrupters;

                m_MovementType = request.Param.MovementType;
                if (m_MovementType == FocusMovementType.HorizontalAndVertical)
                {
                    m_CurrentMovement = m_HorizontalAndVerticalMovement;
                }
                else if (m_MovementType == FocusMovementType.Line)
                {
                    m_CurrentMovement = m_LineMovement;
                }
                else
                {
                    Debug.Assert(false, "Todo");
                }

                m_CurrentMovement.Respond(request.Param.Target, request, transform);
            }

            protected override BehaviourState UpdateInternal(CameraTransform pose)
            {
                if (m_CurrentMovement == null)
                {
                    return BehaviourState.Over;
                }
                return m_CurrentMovement.Update(pose);
            }

            private void Stop()
            {
                m_CurrentMovement?.Over();
                m_CurrentMovement = null;
            }
            
            private BehaviourMask m_Interrupters;
            private LineMovement m_LineMovement;
            private HorizontalAndVertical m_HorizontalAndVerticalMovement;
            private Movement m_CurrentMovement;
            private FocusMovementType m_MovementType = FocusMovementType.Line;
        }
    }
}

//XDay