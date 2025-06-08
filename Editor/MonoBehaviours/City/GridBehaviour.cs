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

#if UNITY_EDITOR

using System;
using UnityEngine;

namespace XDay.WorldAPI.City.Editor
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class GridBehaviour : MonoBehaviour
    {
        public void Initialize(int gridID, Action<DestroyGridEvent> destroyGridEventSender)
        {
            m_GridID = gridID;
            m_DestroyGridEventSender = destroyGridEventSender;
        }

        private void OnDestroy()
        {
            m_DestroyGridEventSender?.Invoke(new DestroyGridEvent()
            {
                ID = m_GridID
            });
        }

        private int m_GridID;
        private Action<DestroyGridEvent> m_DestroyGridEventSender;
    }
}

#endif