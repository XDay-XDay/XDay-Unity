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

namespace XDay.InputAPI
{
    internal class MultiClickMotion : Motion, IMultiClickMotion
    {
        public override MotionType Type => MotionType.MultiClick;
        public List<MultiClickData> Clicks => m_ValidClicks;

        public MultiClickMotion(int id, IDeviceInput device, DeviceTouchType touchType, float threshold)
            : base(id, device, touchType)
        {
            m_MovingThreshold = threshold;
        }

        protected override void OnReset()
        {
            m_ValidClicks.Clear();
        }

        protected override bool Match()
        {
            var touchCount = GetTouchCount();
            if (touchCount == 0)
            {
                Reset(true);
                return false;
            }

            var valid = false;

            for (var i = 0; i < touchCount; ++i)
            {
                var touch = GetTouch(i);

                if (touch.State == TouchState.Finish)
                {
                    var distance = (touch.Start - touch.Current).sqrMagnitude;
                    if (distance <= m_MovingThreshold * m_MovingThreshold)
                    {
                        AddValidClick(touch);
                        valid = true;
                    }
                }
            }

            return valid;
        }

        private void AddValidClick(ITouch touch)
        {
            foreach (var click in m_ValidClicks)
            {
                if (click.TouchID == touch.ID)
                {
                    click.Start = touch.Start;
                    return;
                }
            }

            m_ValidClicks.Add(new MultiClickData()
            {
                TouchID = touch.ID,
                Start = touch.Start,
            });
        }

        protected override void OnAfterMatch()
        {
            m_ValidClicks.Clear();
        }

        private readonly float m_MovingThreshold;
        private readonly List<MultiClickData> m_ValidClicks = new();
    }
}

//XDay