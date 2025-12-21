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
    /// <summary>
    /// touch button id
    /// </summary>
    [Flags]
    public enum TouchID
    {
        Unknown = -1,
        /// <summary>
        /// middle button
        /// </summary>
        Middle = 1,
        /// <summary>
        /// left button
        /// </summary>
        Left = 2,
        /// <summary>
        /// right button
        /// </summary>
        Right = 4,

        /// <summary>
        /// Ä£Äâ¼ü
        /// </summary>
        Virtual = 8,

        All = Middle | Left | Right | Virtual,
    }

    public enum DeviceType
    {
        /// <summary>
        /// used on touch devices
        /// </summary>
        Touch,  
        
        /// <summary>
        /// used on pc
        /// </summary>
        Mouse,
    }

    public enum MotionType
    {
        Click,
        LongPress,
        Drag,
        InertialDrag,
        DoubleClick,
        MouseScroll,
        Pinch,
        MultiClick,
    }

    public interface IDeviceInput
    {
        static IDeviceInput Create()
        {
            return new DeviceInput();
        }

        /// <summary>
        /// event triggered when any touch pressed
        /// </summary>
        event Action<Vector2> EventAnyTouchBegin;

        /// <summary>
        /// event triggered when any scene touch pressed
        /// </summary>
        event Action<Vector2> EventAnySceneTouchBegin;

        bool UseConfigurableTouchAsSceneTouch { get; set; }

        Vector3 VirtualKeyPosition { get; set; }

        /// <summary>
        /// number of touches
        /// </summary>
        int TouchCount { get; }

        /// <summary>
        /// number of touches which are on scene elements now
        /// </summary>
        int SceneTouchCount { get; }

        /// <summary>
        /// scene touch or ui touch count
        /// </summary>
        int ConfigurableTouchCount { get; }

        /// <summary>
        /// touch currently on UI elements
        /// </summary>
        int UITouchCount { get; }

        /// <summary>
        /// number of touches which are not start from UI elements
        /// </summary>
        int TouchCountNotStartFromUI { get; }

        /// <summary>
        /// number of touches which are on scene elements but not start from UI elements
        /// </summary>
        int SceneTouchCountNotStartFromUI { get; }

        /// <summary>
        /// add scene click callbacks
        /// </summary>
        /// <param name="callback"></param>
        void AddSceneClickCallback(Action callback);

        /// <summary>
        /// remove scene click callbacks
        /// </summary>
        /// <param name="callback"></param>
        void RemoveSceneClickCallback(Action callback);

        /// <summary>
        /// remove motion by motion id
        /// </summary>
        /// <param name="id"></param>
        void RemoveMotion(int id);

        /// <summary>
        /// remove motion by instance
        /// </summary>
        /// <param name="motion"></param>
        void RemoveMotion(IMotion motion);

        /// <summary>
        /// create a pinch gesture motion
        /// </summary>
        /// <param name="minHeight">min height which enables camera rotation</param>
        /// <param name="maxHeight">max height which enables camera rotation</param>
        /// <param name="range">camera rotation range</param>
        /// <param name="camera">camera</param>
        /// <param name="enableRotate">whether rotation is enabled</param>
        /// <returns></returns>
        IPinchMotion CreatePinchMotion(float minHeight, float maxHeight, float range, Camera camera, bool enableRotate, DeviceTouchType touchType = DeviceTouchType.TouchNotStartFromUI);

        /// <summary>
        /// create a mouse scroll gesture motion
        /// </summary>
        /// <returns></returns>
        IMouseScrollMotion CreateMouseScrollMotion(DeviceTouchType touchType = DeviceTouchType.Configurable);

        /// <summary>
        /// create a long press gesture motion
        /// </summary>
        /// <param name="pressDuration">press time when long press action should be triggered</param>
        /// <returns></returns>
        ILongPressMotion CreateLongPressMotion(float pressDuration, float moveThreshold, DeviceTouchType touchType = DeviceTouchType.Configurable);

        /// <summary>
        /// create inertial drag gesture motion
        /// </summary>
        /// <param name="validTouchName">valid button</param>
        /// <param name="camera">camera</param>
        /// <returns></returns>
        IInertialDragMotion CreateInertialDragMotion(TouchID validTouchName, Plane plane, float moveThreshold, Camera camera, DeviceTouchType touchType = DeviceTouchType.Configurable);

        /// <summary>
        /// create a one finger scroll gesture motion
        /// </summary>
        /// <param name="validInterval"></param>
        /// <returns></returns>
        IScrollMotion CreateScrollMotion(float validInterval, DeviceTouchType touchType = DeviceTouchType.UITouch);

        /// <summary>
        /// create a double click gesture motion
        /// </summary>
        /// <param name="validClickInterval">min time interval when double click take effects</param>
        /// <returns></returns>
        IDoubleClickMotion CreateDoubleClickMotion(float validClickInterval, DeviceTouchType touchType = DeviceTouchType.Configurable);

        /// <summary>
        /// create a single click gesture motion
        /// </summary>
        /// <returns></returns>
        IClickMotion CreateClickMotion(float moveThreshold, DeviceTouchType touchType = DeviceTouchType.Configurable);

        IMultiClickMotion CreateMultiClickMotion(float moveThreshold, DeviceTouchType touchType = DeviceTouchType.Configurable);

        /// <summary>
        /// create a drag gesture motion
        /// </summary>
        /// <param name="validTouchName"></param>
        /// <param name="touchMovingThreshold"></param>
        /// <returns></returns>
        IDragMotion CreateDragMotion(TouchID validTouchName, float touchMovingThreshold, DeviceTouchType touchType = DeviceTouchType.TouchNotStartFromUI);

        /// <summary>
        /// set touch device type
        /// </summary>
        /// <param name="type"></param>
        void SetDeviceType(DeviceType type);

        /// <summary>
        /// get touch at position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        ITouch QueryTouchAtPosition(Vector2 position);
        ITouch GetSceneTouchNotStartFromUI(int index);
        ITouch GetSceneTouch(int index);
        ITouch GetConfigurableTouch(int index);
        ITouch GetTouchNotStartFromUI(int index);
        ITouch GetUITouch(int index);
        ITouch GetTouch(int index);
        TouchID QueryTouchID(int touchID);

        void Update();
    }

    public enum TouchState
    {
        Start,
        Touching,
        Finish,
    }

    public interface ITouch
    {
        /// <summary>
        /// touch id
        /// </summary>
        int ID { get; }

        /// <summary>
        /// if start from ui
        /// </summary>
        bool StartFromUI { get; }

        /// <summary>
        /// mouse scroll
        /// </summary>
        float Scroll { get; }

        /// <summary>
        /// touch start position
        /// </summary>
        Vector2 Start { get; }

        /// <summary>
        /// touch previous position
        /// </summary>
        Vector2 Previous { get; }

        /// <summary>
        /// touch current position
        /// </summary>
        Vector2 Current { get; }

        /// <summary>
        /// touch current state
        /// </summary>
        TouchState State { get; }

        /// <summary>
        /// get touch history position 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        Vector2 GetTouchTrackedPosition(int index);

        /// <summary>
        /// if touch moved more than specified distance
        /// </summary>
        /// <param name="distanceSquare">square distance to test</param>
        /// <returns></returns>
        bool MovedMoreThan(float distanceSquare);
    }

    public enum MotionState
    {
        Start,
        Running,
        End,
    }

    public enum DeviceTouchType
    {
        SceneTouch,
        UITouch,
        Touch,
        TouchNotStartFromUI,
        SceneTouchNotStartFromUI,
        Configurable,
    }

    /// <summary>
    /// motion base interface
    /// </summary>
    public interface IMotion
    {
        int ID { get; }
        bool Enabled { get; set; }

        /// <summary>
        /// add triggers when motion pattern match
        /// </summary>
        /// <param name="trigger"></param>
        void AddMatchCallback(Func<IMotion, MotionState, bool> trigger, int priority = 0);
        void RemoveMatchCallback(Func<IMotion, MotionState, bool> trigger);
    }

    /// <summary>
    /// a scroll gesture motion
    /// </summary>
    public interface IScrollMotion : IMotion
    {
        /// <summary>
        /// scrolled distance
        /// </summary>
        float MoveDistance { get; }

        /// <summary>
        /// scroll direction
        /// </summary>
        Vector2 MoveDirection { get; }
    }

    /// <summary>
    /// mouse middle button scroll
    /// </summary>
    public interface IMouseScrollMotion : IMotion
    {
        /// <summary>
        /// mouse position
        /// </summary>
        Vector2 Position { get; }

        /// <summary>
        /// scroll delta
        /// </summary>
        float Delta { get; }
    }

    /// <summary>
    /// a drag gesture motion
    /// </summary>
    public interface IDragMotion : IMotion
    {
        /// <summary>
        /// touch start position
        /// </summary>
        Vector2 Start { get; }

        /// <summary>
        /// touch previous position
        /// </summary>
        Vector2 Previous { get; }

        /// <summary>
        /// touch current position
        /// </summary>
        Vector2 Current { get; }
    }

    /// <summary>
    /// single click gesture motion
    /// </summary>
    public interface IClickMotion : IMotion
    {
        /// <summary>
        /// touch start position
        /// </summary>
        Vector2 Start { get; }
    }

    public class MultiClickData
    {
        public int TouchID;
        //touch position
        public Vector2 Start;
    }

    public interface IMultiClickMotion : IMotion
    {
        List<MultiClickData> Clicks { get; }
    }

    /// <summary>
    /// long press gesture motion
    /// </summary>
    public interface ILongPressMotion : IMotion
    {
        /// <summary>
        /// touch start position
        /// </summary>
        Vector2 Start { get; }

        /// <summary>
        /// touch last time
        /// </summary>
        float Duration { get; }
    }

    /// <summary>
    /// double click gesture motion
    /// </summary>
    public interface IDoubleClickMotion : IMotion
    {
        /// <summary>
        /// touch start position
        /// </summary>
        Vector2 Start { get; }
    }

    public interface IInertialDragMotion : IMotion
    {
        float DragTime { get; }
        float MovedDistance { get; }
        int TouchCount { get; }
        Vector3 DraggedOffset { get; }
        Vector3 SlideDirection { get; }
    }

    public interface IPinchMotion : IMotion
    {
        float SlideDirection { get; }
        float DragDistance { get; }
        bool IsRotating { get; }
        float ZoomRate { get; }
        Vector2 Center { get; }
    }
}
