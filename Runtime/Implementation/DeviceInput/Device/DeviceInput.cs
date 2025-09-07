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
using System;
using UnityEngine;

namespace XDay.InputAPI
{
    internal class DeviceInput : IDeviceInput
    {
        public bool UseConfigurableTouchAsSceneTouch { get => m_UseConfigurableTouchAsSceneTouch; set => m_UseConfigurableTouchAsSceneTouch = value; }
        public int TouchCount => m_Device.TouchCount;
        public int SceneTouchCount => m_Device.SceneTouchCount;
        public int ConfigurableTouchCount
        {
            get
            {
                if (m_UseConfigurableTouchAsSceneTouch)
                {
                    return TouchCountNotStartFromUI;
                }
                return UITouchCount;
            }
        }
        public int UITouchCount => m_Device.UITouchCount;
        public int TouchCountNotStartFromUI => m_Device.TouchCountNotStartFromUI;
        public int SceneTouchCountNotStartFromUI => m_Device.SceneTouchCountNotStartFromUI;
        public event Action<Vector2> EventAnyTouchBegin
        {
            add
            {
                m_Device.EventAnyTouchBegin -= value;
                m_Device.EventAnyTouchBegin += value;
            }
            remove => m_Device.EventAnyTouchBegin -= value;
        }
        public event Action<Vector2> EventAnySceneTouchBegin
        {
            add
            {
                m_Device.EventAnySceneTouchBegin -= value;
                m_Device.EventAnySceneTouchBegin += value;
            }
            remove => m_Device.EventAnySceneTouchBegin -= value;
        }

        public DeviceInput()
        {
            SetDeviceType(DeviceType.Mouse);
        }

        public IInertialDragMotion CreateInertialDragMotion(TouchID validTouchName, Plane plane, float moveThreshold, Camera camera, DeviceTouchType touchType)
        {
            return m_MotionSystem.CreateInertialDragMotion(validTouchName, plane, moveThreshold, camera, this, touchType);
        }

        public IDragMotion CreateDragMotion(TouchID validTouchName, float touchMovingThreshold, DeviceTouchType touchType)
        {
            return m_MotionSystem.CreateDragMotion(validTouchName, touchMovingThreshold, this, touchType);
        }

        public IScrollMotion CreateScrollMotion(float validInterval, DeviceTouchType touchType)
        {
            return m_MotionSystem.CreateScrollMotion(validInterval, this, touchType);
        }

        public IDoubleClickMotion CreateDoubleClickMotion(float validClickInterval, DeviceTouchType touchType)
        {
            return m_MotionSystem.CreateDoubleClickMotion(validClickInterval, this, touchType);
        }

        public IClickMotion CreateClickMotion(float moveThreshold, DeviceTouchType touchType)
        {
            return m_MotionSystem.CreateClickMotion(this, touchType, moveThreshold);
        }

        public IPinchMotion CreatePinchMotion(float minAltitude, float maxAltitude, float range, Camera camera, bool enableRotate, DeviceTouchType touchType)
        {
            return m_MotionSystem.CreatePinchMotion(minAltitude, maxAltitude, range, camera, enableRotate, this, touchType);
        }

        public ILongPressMotion CreateLongPressMotion(float tapHoldDuration, DeviceTouchType touchType)
        {
            return m_MotionSystem.CreateLongPressMotion(tapHoldDuration, this, touchType);
        }

        public IMouseScrollMotion CreateMouseScrollMotion(DeviceTouchType touchType)
        {
            return m_MotionSystem.CreateMouseScrollMotion(this, touchType);
        }

        public ITouch GetTouchNotStartFromUI(int index)
        {
            return m_Device.GetTouchNotStartFromUI(index);
        }

        public ITouch GetTouch(int index)
        {
            return m_Device.GetTouch(index);
        }

        public ITouch GetSceneTouchNotStartFromUI(int index)
        {
            return m_Device.GetSceneTouchNotStartFromUI(index);
        }

        public ITouch GetUITouch(int index)
        {
            return m_Device.GetUITouch(index);
        }

        public ITouch GetSceneTouch(int index)
        {
            return m_Device.GetSceneTouch(index);
        }

        public ITouch GetConfigurableTouch(int index)
        {
            if (m_UseConfigurableTouchAsSceneTouch)
            {
                return GetTouchNotStartFromUI(index);
            }
            return GetUITouch(index);
        }

        public TouchID QueryTouchID(int touchID)
        {
            return m_Device.QueryTouchName(touchID);
        }

        public ITouch QueryTouchAtPosition(Vector2 position)
        {
            return m_Device.QueryTouch(position);
        }

        public void SetDeviceType(DeviceType type)
        {
            if (type == DeviceType.Touch)
            {
                m_Device = new TouchDevice(this); 
            }
            else if (type == DeviceType.Mouse)
            {
                m_Device = new MouseDevice(this);
            }
            else
            {
                Debug.Assert(false, $"Unknown type: {type}");
            }
        }

        public void Update()
        {
            m_Device.Update();

            m_MotionSystem.Update();

            UpdateVoidSpaceClickCallbacks();
        }

        public void AddSceneClickCallback(Action callback)
        {
            m_VoidSpaceClickCallbacks.Add(callback);
        }

        public void RemoveSceneClickCallback(Action callback)
        {
            m_VoidSpaceClickCallbacks.Remove(callback);
        }

        public void RemoveMotion(int id)
        {
            m_MotionSystem.RemoveMotion(id);
        }

        public void RemoveMotion(IMotion motion)
        {
            if (motion != null)
            {
                m_MotionSystem.RemoveMotion(motion.ID);
            }
        }

        private void UpdateVoidSpaceClickCallbacks()
        {
            if (SceneTouchCountNotStartFromUI > 0)
            {
                var touch = GetSceneTouchNotStartFromUI(0);
                if (touch.State == TouchState.Start)
                {
                    for (var i = m_VoidSpaceClickCallbacks.Count - 1; i >= 0; i--)
                    {
                        m_VoidSpaceClickCallbacks[i].Invoke();
                    }
                }
            }
        }

        private IDevice m_Device;
        private MotionSystem m_MotionSystem = new();
        private List<Action> m_VoidSpaceClickCallbacks = new();
        private bool m_UseConfigurableTouchAsSceneTouch = true;
    }

    internal interface IDevice
    {
        event Action<Vector2> EventAnyTouchBegin;
        event Action<Vector2> EventAnySceneTouchBegin;
        int TouchCount { get; }
        int UITouchCount { get;}
        int TouchCountNotStartFromUI { get;}
        int SceneTouchCount { get; }
        int SceneTouchCountNotStartFromUI { get;}

        TouchID QueryTouchName(int touchID);
        ITouch QueryTouch(Vector2 position);
        ITouch GetUITouch(int index);
        ITouch GetTouchNotStartFromUI(int index);
        ITouch GetSceneTouchNotStartFromUI(int index);
        ITouch GetSceneTouch(int index);
        ITouch GetTouch(int index);
        void Update();
    }
}

//XDay