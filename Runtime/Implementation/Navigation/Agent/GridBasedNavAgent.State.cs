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

namespace XDay.NavigationAPI
{
    partial class GridBasedNavAgent
    {
        private abstract class State
        {
            public State(GridBasedNavAgent agent)
            {
                m_Agent = agent;
            }

            public virtual void OnEnter() { }
            public virtual void OnExit() { }
            public virtual void Update(float dt) { }

            protected GridBasedNavAgent m_Agent;
        }

        private class Idle : State
        {
            public Idle(GridBasedNavAgent agent)
                : base(agent)
            {
                m_Agent = agent;
            }
        }

        private class Move : State
        {
            public Move(GridBasedNavAgent agent)
                : base(agent)
            {
            }

            public override void OnEnter()
            {
                if (m_Agent.Path == null)
                {
                    Debug.LogError("No path");
                    return;
                }

                m_CurrentPathIndex = 0;
                m_Path = m_Agent.Path;
            }

            public override void Update(float dt)
            {
                if (m_CurrentPathIndex >= m_Agent.Path.Count)
                {
                    SetIdle();
                    return;
                }
                var position = m_Agent.Position;
                var rotation = m_Agent.Rotation;

                var endPos = m_Agent.Path[m_CurrentPathIndex];
                position = Vector3.MoveTowards(position, endPos, m_Agent.MoveSpeed * dt);

                var dir = endPos - position;
                if (dir != Vector3.zero)
                {
                    rotation = Quaternion.RotateTowards(rotation, Quaternion.LookRotation(dir), m_Agent.RotateSpeed * dt);
                }

                if (position == endPos)
                {
                    ++m_CurrentPathIndex;
                    if (m_CurrentPathIndex >= m_Path.Count)
                    {
                        SetIdle();
                    }
                }
                else
                {
                    m_Agent.Position = position;
                    m_Agent.Rotation = rotation;
                    m_Agent.EventPositionChange?.Invoke();
                }
            }

            private void SetIdle()
            {
                m_Agent.SetState<Idle>();
                m_Agent.EventStopMove?.Invoke();
            }

            private int m_CurrentPathIndex;
            private List<Vector3> m_Path;
        }
    }
}


//XDay