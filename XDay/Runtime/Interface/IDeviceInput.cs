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
using UnityEngine;

namespace XDay.InputAPI
{
    [Flags]
    public enum TouchID
    {
        Unknown = -1,
        Middle = 1,
        Left = 2,
        Right = 4,

        All = Middle | Left | Right,
    }

    public enum DeviceType
    {
        Touch,
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
    }

    public interface IDeviceInput
    {
        static IDeviceInput Create()
        {
            return new DeviceInput();
        }

        event Action EventAnyTouchBegin;

        int TouchCount { get; }
        int SceneTouchCount { get; }
        int UITouchCount { get; }
        int TouchCountNotStartFromUI { get; }
        int SceneTouchCountNotStartFromUI { get; }

        void AddVoidSpaceClickCallback(Action callback);
        void RemoveVoidSpaceClickCallback(Action callback);

        void RemoveMotion(int id);
        void RemoveMotion(IMotion motion);
        IPinchMotion CreatePinchMotion(float minHeight, float maxHeight, float range, Camera camera, bool enableRotate);
        IMouseScrollMotion CreateMouseScrollMotion();
        ILongPressMotion CreateLongPressMotion(float pressDuration);
        IInertialDragMotion CreateInertialDragMotion(TouchID validTouchName, Camera camera);
        IScrollMotion CreateSlideMotion(float validInterval = 0.2f);
        IDoubleClickMotion CreateDoubleClickMotion(float validClickInterval);
        IClickMotion CreateClickMotion();
        IDragMotion CreateDragMotion(TouchID validTouchName, float touchMovingThreshold);

        void SetDeviceType(DeviceType type);

        ITouch QueryTouchAtPosition(Vector2 position);
        ITouch GetSceneTouchNotStartFromUI(int index);
        ITouch GetSceneTouch(int index);
        ITouch GetTouchNotStartFromUI(int index);
        ITouch GetUITouch(int index);
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
        int ID { get; }
        bool StartFromUI { get; }
        float Scroll { get; }
        Vector2 Start { get; }
        Vector2 Previous { get; }
        Vector2 Current { get; }
        TouchState State { get; }

        Vector2 GetTouchPosition(int index);
        bool MovedMoreThan(float distanceSquare);
    }

    public enum MotionState
    {
        Start,
        Running,
        Finish,
    }

    public interface IMotion
    {
        int ID { get; }
        bool Enabled { get; set; }

        void AddMatchCallback(Action<IMotion, MotionState> trigger);
        void RemoveMatchCallback(Action<IMotion, MotionState> trigger);
    }

    public interface IScrollMotion : IMotion
    {
        float Distance { get; }
        Vector2 Direction { get; }
    }

    public interface IMouseScrollMotion : IMotion
    {
        Vector2 Position { get; }
        float Delta { get; }
    }

    public interface IDragMotion : IMotion
    {
        Vector2 Start { get; }
        Vector2 Previous { get; }
        Vector2 Current { get; }
    }

    public interface IInertialDragMotion : IMotion
    {
        float DragTime { get; }
        float MovedDistance { get; }
        int TouchCount { get; }
        Vector3 DraggedOffset { get; }
        Vector3 SlideDirection { get; }
    }

    public interface IClickMotion : IMotion
    {
        Vector2 Start { get; }
    }

    public interface IPinchMotion : IMotion
    {
        float SlideDirection { get; }
        float DragDistance { get; }
        bool IsRotating { get; }
        float ZoomRate { get; }
        Vector2 Center { get; }
    }

    public interface ILongPressMotion : IMotion
    {
        float Duration { get; }
        Vector2 Start { get; }
    }

    public interface IDoubleClickMotion : IMotion
    {
        Vector2 Start { get; }
    }
}
