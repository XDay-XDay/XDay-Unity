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
using UnityEngine.EventSystems;

namespace XDay.InputAPI
{
    public static class UIRayCast
    {
        public static bool RayCast(Vector2 pos, IDeviceInput device)
        {
            if (GUIUtility.hotControl != 0)
            {
                return true;
            }

            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                Debug.LogError("No event system!");
                return false;
            }

            var module = UnityEngine.EventSystems.EventSystem.current.currentInputModule as CustomInputModule;
            if (module == null)
            {
                return RayCastInternal(pos) != null;
            }

            var touch = device.QueryTouchAtPosition(pos);
            if (touch == null)
            {
                return RayCastInternal(pos) != null;
            }

            var data = module.QueryTouchPointerData(touch.ID);
            if (data == null)
            {
                return RayCastInternal(pos) != null;
            }

            var gameObject = data.pointerCurrentRaycast.gameObject;
            if (gameObject != null && gameObject.transform is RectTransform)
            {
                return true;
            }
            return false;
        }

        private static Transform RayCastInternal(Vector2 touchPos)
        {
#if UNITY_EDITOR
            //Debug.LogWarning("Using RayCastInternal");
#endif

            if (m_EventSystem != UnityEngine.EventSystems.EventSystem.current)
            {
                m_PointerData = null;
                m_EventSystem = UnityEngine.EventSystems.EventSystem.current;
            }

            if (m_EventSystem != null)
            {
                m_PointerData ??= new PointerEventData(m_EventSystem);
                m_PointerData.pressPosition = touchPos;
                m_PointerData.position = touchPos;

                m_RayCastResults.Clear();
                m_EventSystem.RaycastAll(m_PointerData, m_RayCastResults);
                foreach (var result in m_RayCastResults)
                {
                    if (result.gameObject.transform is RectTransform transform)
                    {
                        return transform;
                    }
                }
            }

            return null;
        }

        private static UnityEngine.EventSystems.EventSystem m_EventSystem;
        private static List<RaycastResult> m_RayCastResults = new();
        private static PointerEventData m_PointerData;
    }
}

//XDay