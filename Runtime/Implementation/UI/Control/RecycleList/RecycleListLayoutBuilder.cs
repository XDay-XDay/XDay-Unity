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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XDay.GUIAPI
{
    public abstract class RecycleListLayoutBuilder
    {
        public abstract int GlobalPointer { get; }

        public void Init(
            ScrollRect scrollRect,
            GameObject item,
            float leftPadding,
            float rightPadding,
            float topPadding,
            float bottomPadding, Vector2 spacing)
        {
            m_LeftPadding = leftPadding;
            m_RightPadding = rightPadding;
            m_TopPadding = topPadding;
            m_BottomPadding = bottomPadding;
            m_Spacing = spacing;
            m_ListItemPrefab = item;
            m_ScrollRect = scrollRect;
            m_ItemSize = GetItemSize();
            m_ItemPivot = (m_ListItemPrefab.transform as RectTransform).pivot;
            m_ListItemPrefab.SetActive(false);
            m_ScrollRect.onValueChanged.AddListener(OnScrollValueChanged);
            m_Content = m_ScrollRect.content;
            m_Viewport = m_ScrollRect.viewport;
            m_ViewportRect = m_ScrollRect.viewport.rect;
            m_LastScrollPosition = m_ScrollRect.normalizedPosition;
            m_ItemSizeWithSpacing = m_ItemSize + m_Spacing;
            float validWidth = m_ViewportRect.size.x - m_LeftPadding - m_RightPadding;
            float validHeight = m_ViewportRect.size.y - m_TopPadding - m_BottomPadding;

            OnInit(m_ItemSizeWithSpacing, validWidth, validHeight);
        }

        public void OnDestroy()
        {
            Clear();
            m_ScrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
        }

        public void SetData<View, Controller>(List<object> datas)
            where View : UIView
            where Controller : UIControllerBase
        {
            m_DataList = new(datas);
            m_ViewType = typeof(View);
            m_ControllerType = typeof(Controller);

            OnSetData();
        }

        public virtual void Clear()
        {
            foreach (var itemView in m_ItemViews)
            {
                itemView.Hide();
                itemView.OnDestroy();
            }
            m_ItemViews.Clear();
            m_DataList.Clear();
            m_ItemCoordinates.Clear();
            m_LastScrollPosition = Vector2.zero;
            m_ViewType = null;
            m_ControllerType = null;
        }

        public void SetScroll(Vector2 normalizedPosition)
        {
            m_ScrollRect.normalizedPosition = normalizedPosition;
        }

        public void MoveScroll(Vector2 normalizedPosition)
        {
            m_ScrollRect.normalizedPosition += normalizedPosition;
        }

        protected abstract void OnInit(Vector2 itemSizeWithSpacing, float validWidth, float validHeight);
        protected abstract void OnScrollValueChanged(Vector2 pos);
        protected abstract void OnSetData();

        //todo optimize
        protected void UpdateVisible()
        {
            foreach (var item in m_ItemViews)
            {
                var coord = m_ItemCoordinates[item];
                var idx = GetIndex(coord.x, coord.y);
                if (idx < m_DataList.Count)
                {
                    if (ItemInvisible(coord.x, coord.y))
                    {
                        if (item.IsActive)
                        {
                            item.Hide();
                        }
                    }
                    else
                    {
                        if (!item.IsActive)
                        {
                            item.Controller.SetData(GetData(coord.x, coord.y));
                            item.Show();
                        }
                    }
                }
            }
        }

        protected abstract object GetData(int x, int y);

        protected bool ItemInvisible(int x, int y)
        {
            GetBoundsInWorldSpace(x, y, out var worldMin, out var worldMax);
            Vector3 minLocal = m_Viewport.InverseTransformPoint(worldMin);
            Vector3 maxLocal = m_Viewport.InverseTransformPoint(worldMax);
            float itemMinX = Mathf.Min(minLocal.x, maxLocal.x);
            float itemMaxX = Mathf.Max(minLocal.x, maxLocal.x);
            float itemMinY = Mathf.Min(minLocal.y, maxLocal.y);
            float itemMaxY = Mathf.Max(minLocal.y, maxLocal.y);

            return m_ViewportRect.xMin > itemMaxX ||
                m_ViewportRect.yMin > itemMaxY ||
                itemMinX > m_ViewportRect.xMax ||
                itemMinY > m_ViewportRect.yMax;
        }

        protected void SetTopLeftPosition(UIView item, int x, int y)
        {
            var topLeftPos = GetItemPosition(x, y);
            m_ItemCoordinates[item] = new Vector2Int(x, y);
            (item.Root.transform as RectTransform).anchoredPosition = ToAnchorPosition(topLeftPos);
        }

        private Vector2 ToAnchorPosition(Vector2 pos)
        {
            var contentPos = ToContentPosition(pos);
            var x = contentPos.x + m_ItemSize.x * m_ItemPivot.x;
            var y = contentPos.y - m_ItemSize.y * (1 - m_ItemPivot.y);
            return new Vector2(x, y);
        }

        private Vector2 ToContentPosition(Vector2 pos)
        {
            return new Vector2(pos.x - m_ContentRect.width * 0.5f, m_ContentRect.height - pos.y - m_ContentRect.height * 0.5f);
        }

        private Vector2 GetItemSize()
        {
            return (m_ListItemPrefab.transform as RectTransform).rect.size;
        }

        /// <summary>
        /// 获取item矩形的左上角坐标
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private Vector2 GetItemPosition(int x, int y)
        {
            var posX = m_LeftPadding + (m_ItemSize.x + m_Spacing.x) * x;
            var posY = m_TopPadding + (m_ItemSize.y + m_Spacing.y) * y;
            return new Vector2(posX, posY);
        }

        /// <summary>
        /// 获取第X,Y个Item的World Bounds
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="worldMin"></param>
        /// <param name="worldMax"></param>
        private void GetBoundsInWorldSpace(int x, int y, out Vector2 worldMin, out Vector2 worldMax)
        {
            var anchorPos = ToAnchorPosition(GetItemPosition(x, y));
            //将anchor position转换成local position,才能被localToWorldMatrix修改
            var localPos = UIHelper.AnchorToLocalPosition(anchorPos, m_Content);
            var min = localPos - m_ItemSize * m_ItemPivot;
            var max = localPos + m_ItemSize * (Vector2.one - m_ItemPivot);
            var worldPos0 = m_Content.TransformPoint(min);
            var worldPos1 = m_Content.TransformPoint(max);
            worldMin = Vector3.Min(worldPos0, worldPos1);
            worldMax = Vector3.Max(worldPos0, worldPos1);
        }

        protected UIView CreateItemView(Type viewType, Type controllerType, int x, int y)
        {
            GameObject root = UnityEngine.Object.Instantiate(m_ListItemPrefab);
            var index = GetIndex(x, y);
            root.name = m_ListItemPrefab.name + $"_{index}";
            var itemView = Activator.CreateInstance(viewType, new object[] { root }) as UIView;
            var itemController = Activator.CreateInstance(controllerType, itemView) as UIControllerBase;
            itemView.SetController(itemController);

            var rectTransform = itemView.Root.transform as RectTransform;
            rectTransform.SetParent(m_Content, false);
            SetTopLeftPosition(itemView, x, y);

            m_ItemViews.Add(itemView);
            return itemView;
        }

        protected abstract int GetIndex(int x, int y);

        protected UIView GetItem(int x, int y)
        {
            return m_ItemViews[y * m_HorizontalDisplayItemCount + x];
        }

        protected Vector2 m_ItemSize;
        protected Vector2 m_ItemSizeWithSpacing;
        private Vector2 m_ItemPivot;
        protected int m_VerticalDisplayItemCount;
        protected int m_HorizontalDisplayItemCount;
        protected Rect m_ViewportRect;
        protected Rect m_ContentRect;

        protected RectTransform m_Content;
        protected RectTransform m_Viewport;
        protected ScrollRect m_ScrollRect;

        protected readonly List<UIView> m_ItemViews = new();

        protected List<object> m_DataList;

        protected Type m_ViewType;
        protected Type m_ControllerType;

        protected Vector2 m_LastScrollPosition;

        private GameObject m_ListItemPrefab;

        protected float m_LeftPadding;
        protected float m_RightPadding;
        protected float m_TopPadding;
        protected float m_BottomPadding;
        protected Vector2 m_Spacing;

        private readonly Dictionary<UIView, Vector2Int> m_ItemCoordinates = new();
    }
}