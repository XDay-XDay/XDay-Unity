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



namespace XDay.InputAPI
{
    internal partial class PinchMotion
    {
        private abstract partial class Phase
        {
            public Phase(PinchMotion motion)
            {
                m_Motion = motion;
            }

            public virtual void Reset() { }
            public virtual void OnActivate(ITouch touch0, ITouch touch1) { }
            public virtual void OnDeactivate() { }
            public abstract bool Match(ITouch touch0, ITouch touch1);

            protected PinchMotion m_Motion;
        }

        private class Idle : Phase
        {
            public Idle(PinchMotion motion)
                : base(motion)
            {
            }

            public override bool Match(ITouch touch0, ITouch touch1)
            {
                var delta = touch0.Current - touch1.Current;
                delta.Normalize();
                m_Motion.CheckPhaseChange(touch0, touch1, delta);
                return false;
            }
        }
    }
}


//XDay