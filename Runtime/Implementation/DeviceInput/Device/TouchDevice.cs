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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace XDay.InputAPI
{
    internal class TouchDevice : IDevice
    {
        public event Action EventAnyTouchBegin;
        public int TouchCount => SceneTouchCount + UITouchCount;
        public int SceneTouchCount => m_SceneTouches.Count;
        public int UITouchCount => m_UITouches.Count;
        public int TouchCountNotStartFromUI => m_TouchesNotStartFromUI.Count;
        public int SceneTouchCountNotStartFromUI => m_SceneTouchesNotStartFromUI.Count;

        public TouchDevice(IDeviceInput input)
        {
            m_DeviceInput = input;

            m_TouchPool = new ObjectPool<DeviceTouch>(() => { return new DeviceTouch(); }, null, actionOnRelease:(tocuh) => { tocuh.Clear(); }, null, true, 10);
        }

        public TouchID QueryTouchName(int touchID)
        {
            return TouchID.Left;
        }

        public ITouch QueryTouch(Vector2 position)
        {
            for (var i = 0; i < m_Touches.Count; ++i)
            {
                if (m_Touches[i].Current == position)
                {
                    return m_Touches[i];
                }
            }
            Debug.Assert(false, $"touch not found at position: {position}");
            return null;
        }

        public void Update()
        {
            Clear();

            var touchBegin = false;
            var touchCount = Input.touchCount;
            for (var i = 0; i < touchCount; i++)
            {
                touchBegin |= UpdateTouch(i);
            }

            if (touchBegin)
            {
                EventAnyTouchBegin?.Invoke();
            }
        }

        public ITouch GetUITouch(int index)
        {
            return m_UITouches[index];
        }

        public ITouch GetTouchNotStartFromUI(int index)
        {
            return m_TouchesNotStartFromUI[index];
        }

        public ITouch GetSceneTouchNotStartFromUI(int index)
        {
            return m_SceneTouchesNotStartFromUI[index];
        }

        public ITouch GetSceneTouch(int index)
        {
            return m_SceneTouches[index];
        }

        private bool UpdateTouch(int index)
        {
            DeviceTouch deviceTouch = null;
            var touch = Input.GetTouch(index);
            if (touch.phase == TouchPhase.Began)
            {
                AddTouch(touch.fingerId);
            }
            else if (
                touch.phase == TouchPhase.Moved ||
                touch.phase == TouchPhase.Stationary)
            {
                deviceTouch = QueryTouch(touch.fingerId);
                if (deviceTouch != null)
                {
                    deviceTouch.State = TouchState.Touching;
                }
            }
            else if (
                touch.phase == TouchPhase.Ended ||
                touch.phase == TouchPhase.Canceled)
            {
                deviceTouch = QueryTouch(touch.fingerId);
                if (deviceTouch != null)
                {
                    Debug.Assert(!m_PendingTouchIndices.Contains(deviceTouch.ID));
                    m_PendingTouchIndices.Add(deviceTouch.ID);
                    deviceTouch.State = TouchState.Finish;
                }
            }
            else
            {
                Debug.Assert(false, $"unknown touch phase: {touch.phase}");
            }

            if (deviceTouch != null)
            {
                deviceTouch.Track(touch.position);
                var hit = UIRayCast.RayCast(touch.position, m_DeviceInput);
                if (touch.phase == TouchPhase.Began)
                {
                    deviceTouch.StartFromUI = hit;
                }

                if (hit)
                {
                    m_UITouches.Add(deviceTouch);
                }
                else
                {
                    if (!deviceTouch.StartFromUI)
                    {
                        m_SceneTouchesNotStartFromUI.Add(deviceTouch);
                    }
                    m_SceneTouches.Add(deviceTouch);
                }

                if (!deviceTouch.StartFromUI)
                {
                    m_TouchesNotStartFromUI.Add(deviceTouch);
                }
            }
            else
            {
                Debug.Assert(false, "wrong");
            }

            return touch.phase == TouchPhase.Began;
        }

        private DeviceTouch QueryTouch(int id)
        {
            for (var i = 0; i < m_Touches.Count; i++)
            {
                if (m_Touches[i].ID == id)
                {
                    return m_Touches[i];
                }
            }
            return null;
        }

        private DeviceTouch AddTouch(int id)
        {
#if UNITY_EDITOR
            Debug.Assert(QueryTouch(id) == null, "touch already added!");
#endif
            var deviceTouch = m_TouchPool.Get();
            deviceTouch.Init(id);
            m_Touches.Add(deviceTouch);
            return deviceTouch;
        }

        private void Clear()
        {
            m_SceneTouches.Clear();
            m_SceneTouchesNotStartFromUI.Clear();
            m_UITouches.Clear();
            m_TouchesNotStartFromUI.Clear();

            for (var i = 0; i < m_PendingTouchIndices.Count; i++)
            {
                var touch = QueryTouch(m_PendingTouchIndices[i]);
                m_Touches.Remove(touch);
                m_TouchPool.Release(touch);
            }
            m_PendingTouchIndices.Clear();
        }

        private ObjectPool<DeviceTouch> m_TouchPool;
        private List<int> m_PendingTouchIndices = new();
        private IDeviceInput m_DeviceInput;
        private List<DeviceTouch> m_UITouches = new();
        private List<DeviceTouch> m_TouchesNotStartFromUI = new();
        private List<DeviceTouch> m_SceneTouches = new();
        private List<DeviceTouch> m_SceneTouchesNotStartFromUI = new();
        private List<DeviceTouch> m_Touches = new();
    }
}

//XDay