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


using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace XDay.GUIAPI
{
    [AddComponentMenu("XDay/UI/Event Listener", 0)]
    public class UIEventListener : MonoBehaviour, 
        IPointerClickHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IDragHandler,
        IBeginDragHandler,
        IEndDragHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            m_OnClick?.Invoke(eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            m_OnDown?.Invoke(eventData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            m_OnEnter?.Invoke(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            m_OnExit?.Invoke(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            m_OnUp?.Invoke(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            m_OnDrag?.Invoke(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            m_OnBeginDrag?.Invoke(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            m_OnEndDrag?.Invoke(eventData);
        }

        public void AddClickEvent(Action<PointerEventData> handler)
        {
            m_OnClick += handler;
        }

        public void RemoveClickEvent(Action<PointerEventData> handler)
        {
            m_OnClick -= handler;
        }

        public void AddUpEvent(Action<PointerEventData> handler)
        {
            m_OnUp += handler;
        }

        public void RemoveUpEvent(Action<PointerEventData> handler)
        {
            m_OnUp -= handler;
        }

        public void AddDownEvent(Action<PointerEventData> handler)
        {
            m_OnDown += handler;
        }

        public void RemoveDownEvent(Action<PointerEventData> handler)
        {
            m_OnDown -= handler;
        }

        public void AddEnterEvent(Action<PointerEventData> handler)
        {
            m_OnEnter += handler;
        }

        public void RemoveEnterEvent(Action<PointerEventData> handler)
        {
            m_OnEnter -= handler;
        }

        public void AddExitEvent(Action<PointerEventData> handler)
        {
            m_OnExit += handler;
        }

        public void RemoveExitEvent(Action<PointerEventData> handler)
        {
            m_OnExit -= handler;
        }

        public void AddDragEvent(Action<PointerEventData> handler)
        {
            m_OnDrag += handler;
        }

        public void RemoveDragEvent(Action<PointerEventData> handler)
        {
            m_OnDrag -= handler;
        }

        public void AddBeginDragEvent(Action<PointerEventData> handler)
        {
            m_OnBeginDrag += handler;
        }

        public void RemoveBeginDragEvent(Action<PointerEventData> handler)
        {
            m_OnBeginDrag -= handler;
        }

        public void AddEndDragEvent(Action<PointerEventData> handler)
        {
            m_OnEndDrag += handler;
        }

        public void RemoveEndDragEvent(Action<PointerEventData> handler)
        {
            m_OnEndDrag -= handler;
        }

        private event Action<PointerEventData> m_OnClick;
        private event Action<PointerEventData> m_OnUp;
        private event Action<PointerEventData> m_OnDown;
        private event Action<PointerEventData> m_OnEnter;
        private event Action<PointerEventData> m_OnExit;
        private event Action<PointerEventData> m_OnDrag;
        private event Action<PointerEventData> m_OnBeginDrag;
        private event Action<PointerEventData> m_OnEndDrag;
    }
}
