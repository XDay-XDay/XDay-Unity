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

using System;

namespace XDay.InputAPI
{
    internal abstract class Motion : IMotion
    {
        public int ID => m_ID;
        public bool Enabled { get => m_Enabled;set=>m_Enabled = value; }
        public abstract MotionType Type { get; }

        public Motion(int id, IDeviceInput device)
        {
            m_ID = id;
            m_Device = device;
        }

        public virtual void Update()
        {
            if (m_Enabled)
            {
                if (Match())
                {
                    OnMatch();
                }
            }
        }

        protected void Reset(bool setFinish)
        {
            if (m_State != null)
            {
                if (setFinish)
                {
                    m_State = MotionState.End;
                    OnMatch();
                }
                m_State = null;
            }

            OnReset();
        }

        protected void OnMatch()
        {
            m_State ??= MotionState.Start;
            EventMatch?.Invoke(this, m_State.GetValueOrDefault());
            if (m_State == MotionState.Start)
            {
                m_State = MotionState.Running;
            }
        }

        public void AddMatchCallback(Action<IMotion, MotionState> callback)
        {
            EventMatch -= callback;
            EventMatch += callback;
        }

        public void RemoveMatchCallback(Action<IMotion, MotionState> callback)
        {
            EventMatch -= callback;
        }

        protected abstract void OnReset();
        protected abstract bool Match();

        private int m_ID;
        private bool m_Enabled = true;
        private event Action<IMotion, MotionState> EventMatch;
        private MotionState? m_State;
        protected IDeviceInput m_Device;
    }
}

//XDay