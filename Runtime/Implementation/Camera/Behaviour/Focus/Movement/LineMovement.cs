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

namespace XDay.CameraAPI
{
    internal partial class BehaviourFocus
    {
        internal class LineMovement : Movement
        {
            public LineMovement(CameraManipulator manipulator)
                : base(manipulator)
            {
            }

            public override void Over()
            {
                SetState(State.Idle);
            }

            public override void Respond(Request request, CameraTransform transform)
            {
                m_ReachDistance = request.FocusParam.ReachDistance;
                m_StartPos = transform.StartPosition;
                m_EndPos = Helper.FromFocusPoint(m_Manipulator.Camera, request.FocusParam.FocusPoint, request.FocusParam.TargetAltitude);
                m_Speed = request.FocusParam.MoveModulator;
                m_Duration = request.FocusParam.MoveDuration;
                m_Timer = 0;
                SetState(State.Moving);
                if (Vector3.Distance(m_EndPos, m_StartPos) < m_ReachDistance)
                {
                    SetState(State.ReachTarget);
                }
            }

            public override BehaviourState Update(CameraTransform pose)
            {
                if (m_StartPos != m_EndPos)
                {
                    m_Timer += Time.deltaTime;
                    return Move(pose);
                }

                pose.CurrentLogicPosition = m_EndPos;
                return BehaviourState.Over;
            }

            private BehaviourState Move(CameraTransform pose)
            {
                var state = BehaviourState.Running;
                if (m_State == State.Moving)
                {
                    var t = 1.0f;
                    if (!Mathf.Approximately(m_Duration, 0))
                    {
                        t = Mathf.Clamp(m_Timer, 0, m_Duration) / m_Duration;
                        if (m_Speed != null)
                        {
                            t = Mathf.Clamp01(m_Speed.Evaluate(t));
                        }
                    }
                    if (m_Timer >= m_Duration)
                    {
                        state = BehaviourState.Over;
                    }

                    var x = Mathf.SmoothStep(m_StartPos.x, m_EndPos.x, t);
                    var y = Mathf.SmoothStep(m_StartPos.y, m_EndPos.y, t);
                    var z = Mathf.SmoothStep(m_StartPos.z, m_EndPos.z, t);

                    pose.CurrentLogicPosition = new Vector3(x, y, z);
                }
                else if (m_State == State.ReachTarget)
                {
                    pose.CurrentLogicPosition = m_EndPos;
                    state = BehaviourState.Over;
                }

                return state;
            }

            private void SetState(State state)
            {
                m_State = state;
            }

            private enum State
            {
                Idle,
                Moving,
                ReachTarget,
            }

            private State m_State = State.Idle;
            private float m_ReachDistance;
            private float m_Duration;
            private Vector3 m_StartPos;
            private Vector3 m_EndPos;
            private float m_Timer;
            private AnimationCurve m_Speed;
        }
    }
}

//XDay