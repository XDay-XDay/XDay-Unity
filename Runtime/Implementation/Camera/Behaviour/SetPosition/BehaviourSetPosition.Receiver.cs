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

using System;
using UnityEngine;

namespace XDay.CameraAPI
{
    internal partial class BehaviourSetPosition
    {
        internal class Receiver : BehaviourRequestReceiver
        {
            public override BehaviourMask Interrupter => m_Interrupters;
            public override BehaviourType Type => BehaviourType.SetPosition;

            public Receiver(CameraManipulator manipulator)
                : base(manipulator)
            {
            }

            protected override void RespondInternal(BehaviourRequest request, CameraTransform pos)
            {
                var req = request as Request;

                if (req.OverrideMovement || 
                    m_State == State.Idle)
                {
                    m_TargetPosition = req.Position;
                    m_Interrupters = req.Interrupters;

                    if (m_State != State.Idle && m_AlwaysInvokeCallback)
                    {
                        m_CameraReachTargetCallback?.Invoke();
                    }

                    m_AlwaysInvokeCallback = req.AlwaysInvokeCallback;
                    m_CameraReachTargetCallback = req.CameraReachTargetCallback;

                    SetState(State.SetPosition);
                }
            }

            protected override BehaviourState UpdateInternal(CameraTransform pos)
            {
                pos.CurrentLogicPosition = m_TargetPosition;
                return BehaviourState.Over;
            }

            public override void Over()
            {
                OverInternal(true);
            }

            public override void OnBeingInterrupted(BehaviourRequest request)
            {
                if (request is not Request)
                {
                    OverInternal(false);
                }
            }

            private void SetState(State state)
            {
                m_State = state;
            }

            private void OverInternal(bool triggerCallback)
            {
                SetState(State.Idle);

                if (triggerCallback && m_CameraReachTargetCallback != null && IsActive)
                {
                    m_CameraReachTargetCallback();
                }
            }

            private enum State
            {
                Idle,
                SetPosition,
            }

            private State m_State = State.Idle;
            private Action m_CameraReachTargetCallback;
            private Vector3 m_TargetPosition;
            private BehaviourMask m_Interrupters;
            private bool m_AlwaysInvokeCallback;
        }
    }
}

//XDay
