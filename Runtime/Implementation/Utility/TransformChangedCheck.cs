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

namespace XDay.UtilityAPI
{
    public class TransformChangedCheck
    {
        public bool IsDirty { get => m_Dirty; set => m_Dirty = value; }

        public void Update(Transform transform)
        {
            if (!m_Inited)
            {
                m_LastPosition = transform.position;
                m_LastScale = transform.lossyScale;
                m_LastRotation = transform.rotation;
                m_Inited = true;
            }
            else
            {
                if (transform.position != m_LastPosition)
                {
                    m_LastPosition = transform.position;
                    m_Dirty = true;
                }

                if (transform.rotation != m_LastRotation)
                {
                    m_LastRotation = transform.rotation;
                    m_Dirty = true;
                }

                if (transform.lossyScale != m_LastScale)
                {
                    m_LastScale = transform.lossyScale;
                    m_Dirty = true;
                }
            }
        }

        private Vector3 m_LastPosition;
        private Vector3 m_LastScale;
        private Quaternion m_LastRotation;
        private bool m_Inited = false;
        private bool m_Dirty = true;
    }
}
