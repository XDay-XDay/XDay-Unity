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
using UnityEngine.EventSystems;

namespace XDay.InputAPI
{
    internal class MouseDevice : IDevice
    {
        public event Action<Vector2> EventAnyTouchBegin;
        public event Action<Vector2> EventAnySceneTouchBegin;
        public int TouchCount => SceneTouchCount + UITouchCount;
        public int UITouchCount => m_UITouches.Count;
        public int SceneTouchCount => m_SceneTouches.Count;
        public int TouchCountNotStartFromUI => m_TouchesNotStartFromUI.Count;
        public int SceneTouchCountNotStartFromUI => m_SceneTouchesNotFromUI.Count;

        public MouseDevice(IDeviceInput deviceInput)
        {
            m_DeviceInput = deviceInput;
            m_Touches = new MouseTouch[4];
            for (var i = 0; i < m_Touches.Length; i++)
            {
                m_Touches[i] = new(ButtonToTouchID(i));
            }
        }

        public ITouch QueryTouch(Vector2 position)
        {
            for (var i = 0; i < m_Touches.Length; ++i)
            {
                if (m_Touches[i].Current == position)
                {
                    return m_Touches[i];
                }
            }
            Debug.LogWarning($"touch not found at position: {position}");
            return null;
        }

        public TouchID QueryTouchName(int touchID)
        {
            if (touchID == PointerInputModule.kMouseLeftId)
            {
                return TouchID.Left;
            }

            if (touchID == PointerInputModule.kMouseRightId)
            {
                return TouchID.Right;
            }

            if (touchID == PointerInputModule.kMouseMiddleId)
            {
                return TouchID.Middle;
            }

            if (touchID == PointerInputModule.kFakeTouchesId)
            {
                return TouchID.Virtual;
            }

            Debug.Assert(false, $"unknown touchID: {touchID}");
            return TouchID.Unknown;
        }

        public void Update()
        {
            Clear();

            var scroll = Input.mouseScrollDelta;
            var mousePos = Input.mousePosition;
            var virtualKeyPos = m_DeviceInput.VirtualKeyPosition;

            var anyStart = false;
            for (var i = 0; i < m_Touches.Length; ++i)
            {
                //down和up可能同一帧触发,应该是个unity的bug,这样就忽略这次点击
                var validTouch = true;
                var start = false;
                var state = ButtonState.Idle;

                //check虚拟键
                bool press;
                if (i == 3)
                {
                    press = Input.GetKeyDown(KeyCode.Space);
                }
                else
                {
                    press = Input.GetMouseButtonDown(i);
                }

                bool up;
                if (i == 3)
                {
                    up = Input.GetKeyUp(KeyCode.Space);
                }
                else
                {
                    up = Input.GetMouseButtonUp(i);
                }

                bool down;
                if (i == 3)
                {
                    down = Input.GetKey(KeyCode.Space);
                }
                else
                {
                    down = Input.GetMouseButton(i);
                }

                if (press)
                {
                    anyStart = true;
                    start = true;
                    state = ButtonState.Start;
                }

                if (up)
                {
                    if (start) 
                    {
                        validTouch = false;
                    }
                    state = ButtonState.Finish;
                }
                else if (down)
                {
                    if (!start)
                    {
                        state = ButtonState.Touching;
                    }
                }

                if (validTouch)
                {
                    if (state != ButtonState.Idle || (scroll != Vector2.zero && i == 2))
                    {
                        UpdateTouch(i, i == 3 ? virtualKeyPos : mousePos, state, scroll, m_DeviceInput);
                    }
                }
            }

            bool anySceneTouchStart = false;
            foreach (var touch in m_Touches)
            {
                if (touch.IsActive)
                {
                    var uiHit = UIRayCast.RayCast(mousePos, m_DeviceInput);
                    if (!touch.StartFromUI)
                    {
                        m_TouchesNotStartFromUI.Add(touch);
                        if (!uiHit)
                        {
                            m_SceneTouchesNotFromUI.Add(touch);
                        }
                    }

                    if (uiHit)
                    {
                        m_UITouches.Add(touch);
                    }
                    else
                    {
                        m_SceneTouches.Add(touch);
                        if (touch.State == TouchState.Start)
                        {
                            anySceneTouchStart = true;
                        }
                    }

                    m_ActiveTouches.Add(touch);
                }
            }

            if (anyStart)
            {
                EventAnyTouchBegin?.Invoke(mousePos);
            }

            if (anySceneTouchStart)
            {
                EventAnySceneTouchBegin?.Invoke(mousePos);
            }
        }

        public ITouch GetTouch(int index)
        {
            return m_ActiveTouches[index];
        }

        public ITouch GetSceneTouchNotStartFromUI(int index)
        {
            return m_SceneTouchesNotFromUI[index];
        }

        public ITouch GetTouchNotStartFromUI(int index)
        {
            return m_TouchesNotStartFromUI[index];
        }

        public ITouch GetUITouch(int index)
        {
            return m_UITouches[index];
        }

        public ITouch GetSceneTouch(int index)
        {
            return m_SceneTouches[index];
        }

        private void UpdateTouch(int touchIndex, Vector3 mousePos, ButtonState state, Vector2 scroll, IDeviceInput input)
        {
            switch (state)
            {
                case ButtonState.Idle:
                case ButtonState.Start:
                    {
                        m_Touches[touchIndex].IsActive = true;
                        m_Touches[touchIndex].State = TouchState.Start;
                    }
                    break;
                case ButtonState.Touching:
                    {
                        m_Touches[touchIndex].State = TouchState.Touching;
                    }
                    break;
                case ButtonState.Finish:
                    {
                        m_Touches[touchIndex].State = TouchState.Finish;
                        Debug.Assert(!m_PendingTouchIndices.Contains(touchIndex));
                        m_PendingTouchIndices.Add(touchIndex);
                    }
                    break;
                default:
                    break;
            }

            m_Touches[touchIndex].Track(mousePos);

            if (state == ButtonState.Start)
            {
                m_Touches[touchIndex].StartFromUI = UIRayCast.RayCast(mousePos, input);
            }

            if (2 == touchIndex)
            {
                if (0 != scroll.y)
                {
                    m_Touches[touchIndex].Scroll = scroll.y;
                    m_Touches[touchIndex].IsActive = true;
                }

                if (state == ButtonState.Idle)
                {
                    Debug.Assert(m_PendingTouchIndices.IndexOf(touchIndex) == -1);
                    m_PendingTouchIndices.Add(touchIndex);
                }
            }
        }

        private void Clear()
        {
            m_UITouches.Clear();
            m_SceneTouches.Clear();
            m_TouchesNotStartFromUI.Clear();
            m_SceneTouchesNotFromUI.Clear();
            m_ActiveTouches.Clear();

            for (var i = 0; i < m_PendingTouchIndices.Count; ++i)
            {
                m_Touches[m_PendingTouchIndices[i]].Clear();
            }
            m_PendingTouchIndices.Clear();
            m_Touches[2].Scroll = 0;
        }

        private int ButtonToTouchID(int button)
        {
            switch (button)
            {
                case 0:
                    return PointerInputModule.kMouseLeftId;
                case 1:
                    return PointerInputModule.kMouseRightId;
                case 2:
                    return PointerInputModule.kMouseMiddleId;
                case 3:
                    return PointerInputModule.kFakeTouchesId;
            }
            Debug.Assert(false, $"Unknown button: {button}");
            return 0;
        }

        private readonly List<MouseTouch> m_TouchesNotStartFromUI = new();
        private readonly List<MouseTouch> m_UITouches = new();
        private readonly List<MouseTouch> m_SceneTouches = new();
        private readonly List<MouseTouch> m_SceneTouchesNotFromUI = new();
        private readonly List<MouseTouch> m_ActiveTouches = new();
        private readonly List<int> m_PendingTouchIndices = new();
        private readonly MouseTouch[] m_Touches;
        private readonly IDeviceInput m_DeviceInput;

        private enum ButtonState
        {
            Idle = 0,
            Start,
            Touching,
            Finish,
        }
    }
}


//XDay