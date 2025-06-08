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
    [ExecuteInEditMode]
    public class DisableTransformChange : MonoBehaviour
    {
        public bool Enabled = true;

        private void Update()
        {
            if (Enabled)
            {
                if (m_StartPosition == null)
                {
                    m_StartPosition = gameObject.transform.position;
                }

                if (m_StartRotation == null)
                {
                    m_StartRotation = gameObject.transform.rotation;
                }

                if (m_StartScale == null)
                {
                    m_StartScale = gameObject.transform.localScale;
                }

                if (transform.position != m_StartPosition.Value)
                {
                    transform.position = m_StartPosition.Value;
                }

                if (transform.rotation != m_StartRotation.Value)
                {
                    transform.rotation = m_StartRotation.Value;
                }

                if (transform.localScale != m_StartScale.Value)
                {
                    transform.localScale = m_StartScale.Value;
                }
            }
        }

        private Vector3? m_StartPosition;
        private Vector3? m_StartScale;
        private Quaternion? m_StartRotation;
    }
}

