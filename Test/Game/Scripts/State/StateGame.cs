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



using UnityEngine;

namespace XDay.TestGame
{
    internal class StateGame : State
    {
        struct TestEvent
        {
            public int a;
        }

        struct TestEvent1
        {
            public int b;
        }

        public override void OnEnter()
        {
            Debug.Log("Enter Game");
#if false
            m_TimerID0 = Global.XDayContext.TimerManager.FrameUpdate(10, () =>
            {
                Debug.Log($"FrameUpdate10: {Time.frameCount}");
            });

            m_TimerID0 = Global.XDayContext.TimerManager.FrameUpdate(2, () =>
            {
                Debug.Log($"FrameUpdate2: {Time.frameCount}");
            });

            m_TimerID1 = Global.XDayContext.TimerManager.TimeUpdate(5.0f, () =>
            {
                Debug.Log($"TimeUpdate5: {Time.time}");
            });

            m_TimerID1 = Global.XDayContext.TimerManager.TimeUpdate(1.0f, () =>
            {
                Debug.Log($"TimeUpdate1: {Time.time}");
            });
#endif
            Global.XDayContext.EventSystem.Register<TestEvent>(this, (e) => {
                Debug.LogError($"Receive event: {e.a}");
                Global.XDayContext.EventSystem.Unregister(this);
                Global.XDayContext.EventSystem.Register<TestEvent1>(this, (e) => {
                    Debug.LogError($"Receive event2: {e.b}");
                });
            });

            Global.XDayContext.EventSystem.Broadcast(new TestEvent() { a = 3 });
            Global.XDayContext.EventSystem.Broadcast(new TestEvent1() { b = 5 });
        }

        public override void Update(float dt)
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                Global.XDayContext.TickTimer.CancelTask(m_TimerID0);
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                Global.XDayContext.TickTimer.CancelTask(m_TimerID1);
            }
        }

        private int m_TimerID0;
        private int m_TimerID1;
    }
}
