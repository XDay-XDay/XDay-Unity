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
using UnityEngine;

namespace XDay.AnimationAPI
{
    internal class InstanceAnimatorSet : IInstanceAnimatorSet
    {
        public Quaternion WorldRotation
        {
            set
            {
                if (m_Rot != value)
                {
                    m_Rot = value;
                    Dirty();
                }
            }
        }

        public Vector3 WorldPosition
        {
            set
            {
                if (m_Pos != value)
                {
                    m_Pos = value;
                    Dirty();
                }
            }
        }

        public Vector3 Transform(Vector3 localPos)
        {
            return m_Pos + m_Rot * localPos;
        }

        public Quaternion Transform(Quaternion localRot)
        {
            return m_Rot * localRot;
        }

        public Vector3 InverseTransform(Vector3 worldPos)
        {
            return Quaternion.Inverse(m_Rot) * (worldPos - m_Pos);
        }

        public Quaternion InverseTransform(Quaternion worldRot)
        {
            return Quaternion.Inverse(m_Rot) * worldRot;
        }

        public void RemoveAnimator(IInstanceAnimator instance)
        {
            m_Animators.Remove(instance);
        }

        public void AddAnimator(IInstanceAnimator instance)
        {
            Debug.Assert(!m_Animators.Contains(instance));
            m_Animators.Add(instance);
        }

        private void Dirty()
        {
            foreach (var instance in m_Animators)
            {
                instance.Dirty();
            }
        }

        private Vector3 m_Pos;
        private Quaternion m_Rot = Quaternion.identity;
        private List<IInstanceAnimator> m_Animators = new();
    }
}

//XDay