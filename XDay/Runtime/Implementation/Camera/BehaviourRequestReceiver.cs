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



namespace XDay.CameraAPI
{
    internal abstract class BehaviourRequestReceiver
    {
        public bool IsActive { get => m_IsActive; set => m_IsActive = value; }
        public abstract BehaviourType Type { get; }
        public CameraManipulator Controller => m_Manipulator;
        public virtual BehaviourMask Interrupter => BehaviourMask.All;

        public BehaviourRequestReceiver(CameraManipulator manipulator)
        {
            m_Manipulator = manipulator;
        }

        public void Respond(BehaviourRequest request, CameraTransform transform)
        {
            if (m_IsActive)
            {
                RespondInternal(request, transform);
            }
        }

        public BehaviourState Update(CameraTransform transform)
        {
            var state = m_IsActive ? UpdateInternal(transform) : BehaviourState.Over;
            if (state == BehaviourState.Over)
            {
                Over();
            }
            return state;
        }

        public bool CanBeInterruptedBy(BehaviourRequest request)
        {
            var mask = (uint)Interrupter;
            return (mask & (1 << (int)request.Type)) != 0;
        }

        public virtual void Over() { }
        public virtual void OnBeingInterrupted(BehaviourRequest request) { }

        protected abstract void RespondInternal(BehaviourRequest request, CameraTransform pos);
        protected abstract BehaviourState UpdateInternal(CameraTransform pos);

        private bool m_IsActive = true;
        protected CameraManipulator m_Manipulator;
    }
}


//XDay