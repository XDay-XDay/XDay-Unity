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
using UnityEngine.UI;

namespace XDay.GUIAPI
{
    public enum ListDirection
    {
        Horizontal,
        Vertical,
    }

    /// <summary>
    /// 可重复使用item的滚动列表,挂在ScrollRect上
    /// TODO:
    ///     1.优化滚动时视野检查
    ///     2.自动计算Padding排版
    /// </summary>
    [AddComponentMenu("XDay/UI/Recycle List", 0)]
    public class UIRecycleList : MonoBehaviour
    {
        public GameObject ListItemPrefab;
        public float LeftPadding;
        public float RightPadding;
        public float TopPadding;
        public float BottomPadding;
        public Vector2 Spacing;
        public ListDirection Direction = ListDirection.Vertical;
        public int GlobalPointer => m_LayoutBuilder.GlobalPointer;

        private void Awake()
        {
            var scrollRect = GetComponent<ScrollRect>();
            if (Direction == ListDirection.Vertical)
            {
                m_LayoutBuilder = new RecycleListVerticalLayoutBuilder();
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
            }
            else if (Direction == ListDirection.Horizontal)
            {
                m_LayoutBuilder = new RecycleListHorizontalLayoutBuilder();
                scrollRect.horizontal = true;
                scrollRect.vertical = false;
            }
            m_LayoutBuilder.Init(scrollRect, ListItemPrefab, LeftPadding, RightPadding, TopPadding, BottomPadding, Spacing);
        }

        private void OnDestroy()
        {
            m_LayoutBuilder.OnDestroy();
        }

        public void SetData<View, Controller>(List<object> data)
            where View : UIView
            where Controller : UIControllerBase
        {
            m_LayoutBuilder.SetData<View, Controller>(data);
        }

        public void SetScroll(Vector2 normalizedPosition)
        {
            m_LayoutBuilder.SetScroll(normalizedPosition);
        }

        public void MoveScroll(Vector2 normalizedPosition)
        {
            m_LayoutBuilder.MoveScroll(normalizedPosition);
        }

        private RecycleListLayoutBuilder m_LayoutBuilder;
    }
}