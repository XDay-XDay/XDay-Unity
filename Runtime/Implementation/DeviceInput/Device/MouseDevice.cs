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
        public event Action EventAnyTouchBegin;
        public int TouchCount => SceneTouchCount + UITouchCount;
        public int UITouchCount => m_UITouches.Count;
        public int SceneTouchCount => m_SceneTouches.Count;
        public int TouchCountNotStartFromUI => m_TouchesNotStartFromUI.Count;
        public int SceneTouchCountNotStartFromUI => m_SceneTouchesNotFromUI.Count;

        public MouseDevice(IDeviceInput deviceInput)
        {
            m_DeviceInput = deviceInput;
            m_Touches = new MouseTouch[3];
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
            Debug.Assert(false, $"touch not found at position: {position}");
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

            Debug.Assert(false, $"unknown touchID: {touchID}");
            return TouchID.Unknown;
        }

        public void Update()
        {
            Clear();

            var scroll = Input.mouseScrollDelta;
            var mousePos = Input.mousePosition;

            var start = false;
            for (var i = 0; i < m_Touches.Length; ++i)
            {
                var state = ButtonState.Idle;
                if (Input.GetMouseButtonDown(i))
                {
                    start = true;
                    state = ButtonState.Start;
                }
                else if (Input.GetMouseButton(i))
                {
                    state = ButtonState.Touching;
                }
                else if (Input.GetMouseButtonUp(i))
                {
                    state = ButtonState.Finish;
                }

                if (state != ButtonState.Idle ||(scroll != Vector2.zero && i == 2))
                {
                    UpdateTouch(i, mousePos, state, scroll, m_DeviceInput);
                }
            }

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
                    }
                }
            }

            if (start)
            {
                EventAnyTouchBegin?.Invoke();
            }
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
            }
            Debug.Assert(false, $"Unknown button: {button}");
            return 0;
        }

        private enum ButtonState
        {
            Idle = 0,
            Start,
            Touching,
            Finish,
        }

        private List<MouseTouch> m_TouchesNotStartFromUI = new();
        private List<MouseTouch> m_UITouches = new();
        private List<MouseTouch> m_SceneTouches = new();
        private List<MouseTouch> m_SceneTouchesNotFromUI = new();
        private List<int> m_PendingTouchIndices = new();
        private MouseTouch[] m_Touches;
        private IDeviceInput m_DeviceInput;
    }
}


//XDay