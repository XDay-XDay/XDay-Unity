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

namespace XDay.GUIAPI
{
    /// <summary>
    /// 向下继续传递消息
    /// </summary>
    [AddComponentMenu("XDay/UI/Event Pass", 0)]
    public class UIEventPass : MonoBehaviour,
        IPointerClickHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IBeginDragHandler
    {
        public GameObject LastDragObject;
        public float ClickMinDistanceSqr = 5 * 5;

        /// <summary>
        /// 点击事件只穿透到第一个对象
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerClick(PointerEventData eventData)
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            eventSystem.RaycastAll(eventData, m_Results);
            for (var i = 1; i < m_Results.Count;)
            {
                var result = m_Results[i];
                eventData.pointerCurrentRaycast = result;
                eventData.pointerClick = result.gameObject;
                //Debug.LogError($"Pointer Click {result.gameObject}");
                ExecuteEvents.Execute(result.gameObject, eventData, ExecuteEvents.pointerClickHandler);
                break;
            }
        }

        /// <summary>
        /// 点击事件只穿透到第一个对象
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerDown(PointerEventData eventData)
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            eventSystem.RaycastAll(eventData, m_Results);
            for (var i = 1; i < m_Results.Count;)
            {
                var result = m_Results[i];
                eventData.pointerCurrentRaycast = result;
                eventData.pointerPress = result.gameObject;
                //Debug.LogError($"Pointer Down {result.gameObject}");
                ExecuteEvents.Execute(result.gameObject, eventData, ExecuteEvents.pointerDownHandler);
                break;
            }
        }

        /// <summary>
        /// 点击事件只穿透到第一个对象
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerUp(PointerEventData eventData)
        {
            var delta = eventData.pressPosition - eventData.position;
            if (delta.sqrMagnitude > ClickMinDistanceSqr)
            {
                return;
            }
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            eventSystem.RaycastAll(eventData, m_Results);
            for (var i = 1; i < m_Results.Count;)
            {
                var result = m_Results[i];
                eventData.pointerCurrentRaycast = result;
                eventData.pointerPress = result.gameObject;
                //Debug.LogError($"Pointer Up {result.gameObject}");
                ExecuteEvents.Execute(result.gameObject, eventData, ExecuteEvents.pointerUpHandler);
                break;
            }
        }

        /// <summary>
        /// 拖拽事件穿透到指定对象
        /// </summary>
        /// <param name="eventData"></param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            eventSystem.RaycastAll(eventData, m_Results);
            for (int i = 1; i < m_Results.Count; ++i)
            {
                var result = m_Results[i];
                eventData.pointerCurrentRaycast = result;
                //设置了pointerDrag后就不会在这里处理Drag和EndDrag了
                eventData.pointerDrag = result.gameObject;
                ExecuteEvents.Execute(result.gameObject, eventData, ExecuteEvents.beginDragHandler);
                if (Object.ReferenceEquals(result.gameObject, LastDragObject))
                {
                    break;
                }
            }
        }

        private readonly List<RaycastResult> m_Results = new();
    }
}
