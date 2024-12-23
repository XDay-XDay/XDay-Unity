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

using UnityEngine;

namespace XDay.CameraAPI
{
    internal class UniformShake : CameraTransformPostProcessor
    {
        public UniformShake(CameraManipulator manipulator) 
            : base(manipulator) 
        { 
        }

        public void Shake(float amplitude, float frequency, bool changeAltitude, float duration)
        {
            ShakeInternal(duration, changeAltitude, frequency, amplitude, null, null);
        }

        public void Shake(AnimationCurve amplitude, AnimationCurve frequency, bool changeAltitude, float duration)
        {
            ShakeInternal(duration, changeAltitude, 0, 0, frequency, amplitude);
        }

        protected override bool UpdateInternal(
            Vector3 oldPos, 
            Quaternion oldRot, 
            out Vector3 newPos,
            out Quaternion newRot)
        {
            newRot = oldRot;
            var dt = Time.deltaTime;

            if (m_State == State.Stopped)
            {
                m_IsActive = false;
                newPos = oldPos;
                return false;
            }
            
            m_Timer += dt;
            if (m_Timer >= m_Duration)
            {
                m_State = State.Stopped;
                m_Target = Vector3.zero;
            }

            var frequency = m_FrequencyCurve != null ? m_FrequencyCurve.Evaluate(m_Timer / m_Duration) : m_Frequency;
            m_Movement = Vector3.MoveTowards(m_Movement, m_Target, frequency * dt);
            if (m_Movement == m_Target)
            {
                if (m_State != State.Restoring)
                {
                    m_Target = CalculateMovement();
                }
                else
                {
                    m_State = State.Stopped;
                }
            }

            newPos = oldPos + m_Movement;

            return true;
        }

        private void ShakeInternal(float duration, bool changeAmplitude, float frequency, float amplitude, AnimationCurve frequencyCurve, AnimationCurve amplitudeCurve)
        {
            if (duration <= 0)
            {
                return;
            }

            m_Timer = 0;
            m_ChangeAltitude = changeAmplitude;
            m_Movement = Vector3.zero;
            m_Duration = duration;
            m_IsActive = true;
            m_FrequencyCurve = frequencyCurve;
            m_Frequency = frequency;
            m_AmplitudeCurve = amplitudeCurve;
            m_Amplitude = amplitude;
            m_State = State.Shaking;
            m_Target = CalculateMovement();
        }

        private Vector3 CalculateMovement()
        {
            var amplitude = m_AmplitudeCurve != null ? m_AmplitudeCurve.Evaluate(m_Timer / m_Duration) : m_Amplitude;

            if (!m_ChangeAltitude)
            {
                var angle = Mathf.Deg2Rad * Random.Range(0, 360);
                return amplitude * new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
            }
            return amplitude * Random.onUnitSphere;
        }

        private enum State
        {
            Shaking,
            Restoring,
            Stopped,
        }

        private bool m_ChangeAltitude;
        private float m_Timer;
        private float m_Duration;
        private float m_Amplitude;
        private float m_Frequency;
        private AnimationCurve m_AmplitudeCurve;
        private AnimationCurve m_FrequencyCurve;
        private Vector3 m_Target;
        private Vector3 m_Movement;
        private State m_State;
    }
}

//XDay