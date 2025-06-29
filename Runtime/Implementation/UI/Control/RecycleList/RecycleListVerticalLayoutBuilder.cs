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
    public class RecycleListVerticalLayoutBuilder : RecycleListLayoutBuilder
    {
        public override int GlobalPointer => m_GlobalPointer;

        protected override void OnInit(Vector2 itemSizeWithSpacing, float validWidth, float validHeight)
        {
            m_HorizontalDisplayItemCount = Mathf.FloorToInt(validWidth / itemSizeWithSpacing.x);
            m_VerticalDisplayItemCount = Mathf.CeilToInt(validHeight / itemSizeWithSpacing.y) + 2;
        }

        protected override void OnSetData()
        {
            Layout();
        }

        public override void Clear()
        {
            base.Clear();
            m_RowCount = 0;
            m_GlobalPointer = 0;
        }

        protected override void OnScrollValueChanged(Vector2 pos)
        {
			pos.x = Mathf.Clamp01(pos.x);
            pos.y = Mathf.Clamp01(pos.y);
            UpdateVisible();

            if (Mathf.Abs(m_LastScrollPosition.y - pos.y) >= m_NormalizedOneScreenHeight)
            {
                //直接重新设置
                foreach (var item in m_ItemViews)
                {
                    item.Hide();
                }

                m_GlobalPointer = CalculateGlobalPointerByScrollPos(pos.y);
                ShowItems();
            }
            else
            {
                if (m_LastScrollPosition.y > pos.y)
                {
                    UpdateScrollUp();
                }
                else
                {
                    UpdateScrollDown();
                }
            }
            m_LastScrollPosition = pos;
        }

        private void UpdateScrollUp()
        {
            while (true)
            {
                if (ItemInvisible(0, m_GlobalPointer))
                {
                    bool ok = MoveRowDown();
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

        private void UpdateScrollDown()
        {
            if (m_GlobalPointer == 0)
            {
                return;
            }

            while (true)
            {
                if (!ItemInvisible(0, m_GlobalPointer - 1))
                {
                    bool ok = MoveRowUp();
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

        //第一行Item移动到最后一行,并隐藏item
        private bool MoveRowDown()
        {
            if (m_GlobalPointer + m_VerticalDisplayItemCount >= m_RowCount)
            {
                return false;
            }

            var itemY = ToLocalPointer(m_GlobalPointer);
            for (var i = 0; i < m_HorizontalDisplayItemCount; ++i)
            {
                var lastRow = m_GlobalPointer + m_VerticalDisplayItemCount;
                UIView item = GetItem(i, itemY);
                var data = GetData(i, lastRow);
                SetTopLeftPosition(item, i, lastRow);
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

        //最后一行移动到第一行,并显示item
        private bool MoveRowUp()
        {
            if (m_GlobalPointer == 0)
            {
                return false;
            }

            var itemY = ToLocalPointer(m_GlobalPointer + m_VerticalDisplayItemCount - 1);
            for (var i = 0; i < m_HorizontalDisplayItemCount; ++i)
            {
                var firstRow = m_GlobalPointer - 1;
                UIView item = GetItem(i, itemY);
                var data = GetData(i, firstRow);
                SetTopLeftPosition(item, i, firstRow);
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
            CalculateVerticalLayoutContentSize();

            for (var i = 0; i < m_VerticalDisplayItemCount; ++i)
            {
                var globalY = i + m_GlobalPointer;
                for (var j = 0; j < m_HorizontalDisplayItemCount; ++j)
                {
                    var idx = GetIndex(j, globalY);
                    if (idx < m_DataList.Count)
                    {
                        var view = CreateItemView(m_ViewType, m_ControllerType, j, globalY);
                        view.Controller.SetData(m_DataList[idx]);
                        if (!ItemInvisible(j, globalY))
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
                var globalY = i + m_GlobalPointer;
                for (var j = 0; j < m_HorizontalDisplayItemCount; ++j)
                {
                    var idx = GetIndex(j, globalY);
                    if (idx < m_DataList.Count)
                    {
                        var itemIdx = ToLocalPointer(globalY);
                        var item = GetItem(j, itemIdx);
                        SetTopLeftPosition(item, j, globalY);
                        item.Controller.SetData(m_DataList[idx]);
                        if (!ItemInvisible(j, globalY))
                        {
                            item.Show();
                        }
                    }
                }
            }
        }

        private void CalculateVerticalLayoutContentSize()
        {
            m_RowCount = Mathf.CeilToInt((float)m_DataList.Count / m_HorizontalDisplayItemCount);
            m_Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, m_TopPadding + m_BottomPadding + m_RowCount * (m_ItemSize.y + m_Spacing.y) - m_Spacing.y);
            m_Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_ViewportRect.width);
            m_ContentRect = m_Content.rect;
            //一屏的高度
            float deltaHeight = m_ContentRect.size.y - m_Viewport.rect.height;
            if (Mathf.Approximately(deltaHeight, 0))
            {
                m_NormalizedOneScreenHeight = 0;
            }
            else
            {
                m_NormalizedOneScreenHeight = Mathf.Max(0, m_Viewport.rect.height / deltaHeight);
            }
        }

        protected int ToLocalPointer(int globalPointer)
        {
            return globalPointer % m_VerticalDisplayItemCount;
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
            return y * m_HorizontalDisplayItemCount + x;
        }

        private int CalculateGlobalPointerByScrollPos(float normalizedY)
        {
            float scrolledAbsHeight = (1 - normalizedY) * (m_ContentRect.height - m_ViewportRect.height);
            if (scrolledAbsHeight < m_TopPadding + m_ItemSizeWithSpacing.y)
            {
                return 0;
            }

            scrolledAbsHeight -= (m_TopPadding + m_ItemSizeWithSpacing.y);

            var y = Mathf.FloorToInt(scrolledAbsHeight / m_ItemSizeWithSpacing.y);
            return Mathf.Clamp(y + 1, 0, m_RowCount - m_VerticalDisplayItemCount);
        }

        private int m_GlobalPointer = 0;
        private int m_RowCount;
        private float m_NormalizedOneScreenHeight;
    }
}