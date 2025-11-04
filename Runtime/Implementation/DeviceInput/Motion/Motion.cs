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
using System.Collections.Generic;
using UnityEngine;

namespace XDay.InputAPI
{
    internal abstract class Motion : IMotion
    {
        public int ID => m_ID;
        public bool Enabled { get => m_Enabled; set=> m_Enabled = value; }
        public abstract MotionType Type { get; }

        public Motion(int id, IDeviceInput device, DeviceTouchType touchType)
        {
            m_ID = id;
            m_Device = device;
            m_TouchType = touchType;
        }

        public virtual void Update()
        {
            if (m_Enabled)
            {
                if (Match())
                {
                    OnMatch();

                    OnAfterMatch();
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

            foreach (var callback in m_MatchCallbacks)
            {
                var stop = callback.Action.Invoke(this, m_State.GetValueOrDefault());
                if (stop)
                {
                    break;
                }
            }
            if (m_State == MotionState.Start)
            {
                m_State = MotionState.Running;
            }
        }

        protected virtual void OnAfterMatch() { }

        public void AddMatchCallback(Func<IMotion, MotionState, bool> callback, int priority)
        {
            m_MatchCallbacks.Add(new Callback() { Action = callback, Priority = priority});
            m_MatchCallbacks.Sort((a, b) => { return a.Priority.CompareTo(b.Priority); });
        }

        public void RemoveMatchCallback(Func<IMotion, MotionState, bool> callback)
        {
            for (var i = 0; i < m_MatchCallbacks.Count; ++i)
            {
                if (m_MatchCallbacks[i].Action == callback)
                {
                    m_MatchCallbacks.RemoveAt(i);
                    return;
                }
            }
        }

        protected int GetTouchCount()
        {
            switch (m_TouchType)
            {
                case DeviceTouchType.Configurable:
                    return m_Device.ConfigurableTouchCount;
                case DeviceTouchType.SceneTouch:
                    return m_Device.SceneTouchCount;
                case DeviceTouchType.UITouch:
                    return m_Device.UITouchCount;
                case DeviceTouchType.Touch:
                    return m_Device.TouchCount;
                case DeviceTouchType.TouchNotStartFromUI:
                    return m_Device.TouchCountNotStartFromUI;
                case DeviceTouchType.SceneTouchNotStartFromUI:
                    return m_Device.SceneTouchCountNotStartFromUI;
                default:
                    Debug.Assert(false, $"Unknown touch type: {m_TouchType}");
                    break;
            }
            return 0;
        }

        protected ITouch GetTouch(int index)
        {
            switch (m_TouchType)
            {
                case DeviceTouchType.Configurable:
                    return m_Device.GetConfigurableTouch(index);
                case DeviceTouchType.SceneTouch:
                    return m_Device.GetSceneTouch(index);
                case DeviceTouchType.UITouch:
                    return m_Device.GetUITouch(index);
                case DeviceTouchType.Touch:
                    return m_Device.GetTouch(index);
                case DeviceTouchType.TouchNotStartFromUI:
                    return m_Device.GetTouchNotStartFromUI(index);
                case DeviceTouchType.SceneTouchNotStartFromUI:
                    return m_Device.GetSceneTouchNotStartFromUI(index);
                default:
                    Debug.Assert(false, $"Unknown touch type: {m_TouchType}");
                    break;
            }
            return null;
        }

        protected abstract void OnReset();
        protected abstract bool Match();

        private readonly int m_ID;
        private bool m_Enabled = true;
        private readonly List<Callback> m_MatchCallbacks = new();
        private MotionState? m_State;
        protected DeviceTouchType m_TouchType;
        protected IDeviceInput m_Device;

        private class Callback
        {
            public int Priority = 0;
            //返回true会让Priority值更大的Action不被调用
            public Func<IMotion, MotionState, bool> Action;
        }
    }
}

//XDay