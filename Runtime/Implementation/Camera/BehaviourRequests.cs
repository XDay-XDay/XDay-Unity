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

namespace XDay.CameraAPI
{
    public enum BehaviourType
    {
        SetPosition = 0,
        Pinch = 1,
        Focus = 2,
        Follow = 3,
        StopFollow = 4,
        Drag = 5,
        //鼠标中键缩放
        ScrollZoom = 6,
        //ctrl+鼠标右键缩放
        MouseZoom = 7,
    }

    [Flags]
    public enum BehaviourMask : uint
    {
        None = 0,
        All = 0xffffffff,
    }

    public static class BehaviourMaskExtensions
    {
        public static BehaviourMask Add(this BehaviourMask value, BehaviourType type)
        {
            return value |= ((BehaviourMask)(1 << (int)type));
        }

        public static BehaviourMask Remove(this BehaviourMask value, BehaviourType type)
        {
            return value &= ~((BehaviourMask)(1 << (int)type));
        }
    }

    internal enum BehaviourState
    {
        Running,
        Over,
    }

    internal abstract class BehaviourRequest
    {
        public abstract BehaviourType Type { get; }
        public int Layer => m_Layer;
        public int Priority => m_Priority;
        public RequestQueueType QueueType => m_QueueType;
        public BehaviourMask Interrupters { get => m_Interrupters; set => m_Interrupters = value; }

        protected void Init(int layer, int priority, RequestQueueType queueType)
        {
            m_Layer = layer;
            m_Priority = priority;
            m_QueueType = queueType;
        }

        public abstract void OnDestroy(bool overridden);

        private int m_Layer;
        private int m_Priority;
        private RequestQueueType m_QueueType;
        private BehaviourMask m_Interrupters = BehaviourMask.All;
    }
}

//XDay