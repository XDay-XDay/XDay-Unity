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

using UnityEngine;
using System;

namespace XDay.WorldAPI.House.Editor
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [SelectionBase]
    public class HouseBehaviour : MonoBehaviour
    {
        public void Initialize(int houseID, Action<DestroyHouseEvent> destroyHouseEventSender, Action<HousePositionChangeEvent> housePositionChangeEventSender)
        {
            m_HouseID = houseID;
            m_DestroyHouseEventSender = destroyHouseEventSender;
            m_HousePositionChangeEventSender = housePositionChangeEventSender;
        }

        private void Start()
        {
            m_LastUpdatePosition = gameObject.transform.position;
        }

        private void OnDestroy()
        {
            m_DestroyHouseEventSender?.Invoke(new DestroyHouseEvent()
            {
                HouseID = m_HouseID,
            });
        }

        private void Update()
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (m_LastUpdatePosition != gameObject.transform.position)
            {
                m_LastUpdatePosition = gameObject.transform.position;

                m_HousePositionChangeEventSender?.Invoke(new HousePositionChangeEvent()
                {
                    HouseID = m_HouseID,
                });
            }
        }

        private Vector3 m_LastUpdatePosition;
        private int m_HouseID;
        private Action<DestroyHouseEvent> m_DestroyHouseEventSender;
        private Action<HousePositionChangeEvent> m_HousePositionChangeEventSender;
    }
}

#endif