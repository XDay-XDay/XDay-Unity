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

namespace XDay.GUIAPI
{
    /// <summary>
    /// 需要借助Layout组件自动排列List元素
    /// </summary>
    [AddComponentMenu("XDay/UI/List", 0)]
    public class UIList : MonoBehaviour
    {
        public GameObject ListItemPrefab;

        private void Awake()
        {
            if (m_ListItemPrefab != null)
            {
                m_ListItemPrefab.SetActive(false);
            }
        }

        public void OnDestroy()
        {
            Clear();
        }

        public void SetData<View, Controller>(List<object> datas, Transform parent)
            where View : UIView
            where Controller : UIControllerBase
        {
            m_DataList = new(datas);
            m_ViewType = typeof(View);
            m_ControllerType = typeof(Controller);

            for (var i = 0; i < datas.Count; ++i)
            {
                var view = CreateItemView(m_ViewType, m_ControllerType, i, parent);
                view.Controller.SetData(m_DataList[i]);
                view.Show();
            }
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
            m_ViewType = null;
            m_ControllerType = null;
        }

        protected UIView CreateItemView(Type viewType, Type controllerType, int idx, Transform parent)
        {
            GameObject root = UnityEngine.Object.Instantiate(m_ListItemPrefab);
            root.name = m_ListItemPrefab.name + $"_{idx}";
            var itemView = Activator.CreateInstance(viewType, new object[] { root }) as UIView;
            var itemController = Activator.CreateInstance(controllerType, itemView) as UIControllerBase;
            itemView.SetController(itemController);

            var rectTransform = itemView.Root.transform as RectTransform;
            rectTransform.SetParent(parent, false);

            m_ItemViews.Add(itemView);
            return itemView;
        }

        public UIView GetItem(int index)
        {
            if (index >= 0 && index < m_ItemViews.Count)
            {
                return m_ItemViews[index];
            }
            return null;
        }

        protected readonly List<UIView> m_ItemViews = new();

        protected List<object> m_DataList;

        protected Type m_ViewType;
        protected Type m_ControllerType;

        private GameObject m_ListItemPrefab;
    }
}