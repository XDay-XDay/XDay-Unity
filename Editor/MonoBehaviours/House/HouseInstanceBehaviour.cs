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
    public class HouseInstanceBehaviour : MonoBehaviour
    {
        public void Initialize(int houseID, Action<DestroyHouseInstanceEvent> destroyHouseInstanceEventSender, Action<HouseInstancePositionChangeEvent> houseInstancecPositionChangeEventSender)
        {
            m_HouseInstanceID = houseID;
            m_DestroyHouseInstanceEventSender = destroyHouseInstanceEventSender;
            m_HouseInstancecPositionChangeEventSender = houseInstancecPositionChangeEventSender;
        }

        private void Start()
        {
            m_LastUpdatePosition = gameObject.transform.position;
        }

        private void OnDestroy()
        {
            m_DestroyHouseInstanceEventSender?.Invoke(new DestroyHouseInstanceEvent()
            {
                HouseInstanceID = m_HouseInstanceID,
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

                m_HouseInstancecPositionChangeEventSender?.Invoke(new HouseInstancePositionChangeEvent()
                {
                    HouseID = m_HouseInstanceID,
                });
            }
        }

        private Vector3 m_LastUpdatePosition;
        private int m_HouseInstanceID;
        private Action<DestroyHouseInstanceEvent> m_DestroyHouseInstanceEventSender;
        private Action<HouseInstancePositionChangeEvent> m_HouseInstancecPositionChangeEventSender;
    }
}

#endif