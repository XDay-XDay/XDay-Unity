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

namespace XDay.GUIAPI
{
    public class RecycleListHorizontalLayoutBuilder : RecycleListLayoutBuilder
    {
        public override int GlobalPointer => m_GlobalPointer;

        protected override void OnInit(Vector2 itemSizeWithSpacing, float validWidth, float validHeight)
        {
            m_HorizontalDisplayItemCount = Mathf.CeilToInt(validWidth / itemSizeWithSpacing.x) + 2;
            m_VerticalDisplayItemCount = Mathf.FloorToInt(validHeight / itemSizeWithSpacing.y);
        }

        protected override void OnSetData()
        {
            Layout();
        }

        public override void Clear()
        {
            base.Clear();
            m_ColumnCount = 0;
            m_GlobalPointer = 0;
        }

        protected override void OnScrollValueChanged(Vector2 pos)
        {
			pos.x = Mathf.Clamp01(pos.x);
            pos.y = Mathf.Clamp01(pos.y);
            UpdateVisible();

            if (Mathf.Abs(m_LastScrollPosition.x - pos.x) >= m_NormalizedOneScreenWidth)
            {
                //直接重新设置
                foreach (var item in m_ItemViews)
                {
                    item.Hide();
                }

                m_GlobalPointer = CalculateGlobalPointerByScrollPos(pos.x);
                ShowItems();
            }
            else
            {
                if (m_LastScrollPosition.x < pos.x)
                {
                    UpdateScrollLeft();
                }
                else
                {
                    UpdateScrollRight();
                }
            }
            m_LastScrollPosition = pos;
        }

        private void UpdateScrollLeft()
        {
            while (true)
            {
                if (ItemInvisible(m_GlobalPointer, 0))
                {
                    bool ok = MoveColumnRight();
                    if (!ok)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        private void UpdateScrollRight()
        {
            if (m_GlobalPointer == 0)
            {
                return;
            }

            while (true)
            {
                if (!ItemInvisible(m_GlobalPointer - 1, 0))
                {
                    bool ok = MoveColumnLeft();
                    if (!ok)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        //第一列Item移动到最后一列,并隐藏item
        private bool MoveColumnRight()
        {
            if (m_GlobalPointer + m_HorizontalDisplayItemCount >= m_ColumnCount)
            {
                return false;
            }

            var itemX = ToLocalPointer(m_GlobalPointer);
            for (var i = 0; i < m_VerticalDisplayItemCount; ++i)
            {
                var lastColumn = m_GlobalPointer + m_HorizontalDisplayItemCount;
                UIView item = GetItem(itemX, i);
                var data = GetData(lastColumn, i);
                SetTopLeftPosition(item, lastColumn, i);
                if (data != null)
                {
                    item.Controller.SetData(data);
                    if (!item.IsActive)
                    {
                        item.Show();
                    }
                }
                else
                {
                    item.Hide();
                }
            }
            ++m_GlobalPointer;

            return true;
        }

        //最后一列移动到第一列,并显示item
        private bool MoveColumnLeft()
        {
            if (m_GlobalPointer == 0)
            {
                return false;
            }

            var itemX = ToLocalPointer(m_GlobalPointer + m_HorizontalDisplayItemCount - 1);
            for (var i = 0; i < m_VerticalDisplayItemCount; ++i)
            {
                var firstColumn = m_GlobalPointer - 1;
                UIView item = GetItem(itemX, i);
                var data = GetData(firstColumn, i);
                SetTopLeftPosition(item, firstColumn, i);
                if (data != null)
                {
                    item.Controller.SetData(data);
                    if (!item.IsActive)
                    {
                        item.Show();
                    }
                }
                else
                {
                    item.Hide();
                }
            }
            --m_GlobalPointer;
            return true;
        }

        private void Layout()
        {
            CalculateHorizontalLayoutContentSize();

            for (var i = 0; i < m_VerticalDisplayItemCount; ++i)
            {
                for (var j = 0; j < m_HorizontalDisplayItemCount; ++j)
                {
                    var globalX = j + m_GlobalPointer;
                    var idx = GetIndex(globalX, i);
                    if (idx < m_DataList.Count)
                    {
                        var view = CreateItemView(m_ViewType, m_ControllerType, globalX, i);
                        view.Controller.SetData(m_DataList[idx]);
                        if (!ItemInvisible(globalX, i))
                        {
                            view.Show();
                        }
                    }
                }
            }
        }

        private void ShowItems()
        {
            for (var i = 0; i < m_VerticalDisplayItemCount; ++i)
            {
                for (var j = 0; j < m_HorizontalDisplayItemCount; ++j)
                {
					var globalX = j + m_GlobalPointer;
                    var idx = GetIndex(globalX, i);
                    if (idx < m_DataList.Count)
                    {
                        var item = GetItem(ToLocalPointer(globalX), i);
                        SetTopLeftPosition(item, globalX, i);
                        item.Controller.SetData(m_DataList[idx]);
                        if (!ItemInvisible(globalX, i))
                        {
                            item.Show();
                        }
                    }
                }
            }
        }
        private void CalculateHorizontalLayoutContentSize()
        {
            m_ColumnCount = Mathf.CeilToInt((float)m_DataList.Count / m_VerticalDisplayItemCount);
            m_Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_LeftPadding + m_RightPadding + m_ColumnCount * (m_ItemSize.x + m_Spacing.x) - m_Spacing.x);
            m_Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, m_ViewportRect.height);
            m_ContentRect = m_Content.rect;
            float deltaWidth = m_ContentRect.size.x - m_Viewport.rect.width;
            if (Mathf.Approximately(deltaWidth, 0))
            {
                m_NormalizedOneScreenWidth = 0;
            }
            else
            {
                m_NormalizedOneScreenWidth = Mathf.Max(0, m_Viewport.rect.width / deltaWidth);
            }
        }

        protected int ToLocalPointer(int globalPointer)
        {
            return globalPointer % m_HorizontalDisplayItemCount;
        }

        protected override object GetData(int x, int y)
        {
            var idx = GetIndex(x, y);
            if (idx >= m_DataList.Count || idx < 0)
            {
                return null;
            }
            return m_DataList[idx];
        }

        protected override int GetIndex(int x, int y)
        {
            return y * m_ColumnCount + x;
        }

        private int CalculateGlobalPointerByScrollPos(float normalizedX)
        {
            float scrolledAbsWidth = normalizedX * (m_ContentRect.width - m_ViewportRect.width);
            if (scrolledAbsWidth < m_LeftPadding + m_ItemSizeWithSpacing.x)
            {
                return 0;
            }

            scrolledAbsWidth -= (m_LeftPadding + m_ItemSizeWithSpacing.x);

            var x = Mathf.FloorToInt(scrolledAbsWidth / m_ItemSizeWithSpacing.x);
            return Mathf.Clamp(x + 1, 0, m_ColumnCount - m_HorizontalDisplayItemCount);
        }
        private int m_GlobalPointer = 0;
        private int m_ColumnCount;
        private float m_NormalizedOneScreenWidth;
    }
}