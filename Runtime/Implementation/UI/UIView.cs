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
using XDay.UtilityAPI;

namespace XDay.GUIAPI
{
    public abstract class UIView
    {
        public GameObject Root => m_Root;
        public UIControllerBase Controller => m_Controller;
        public bool IsLoaded => Root != null;
        public bool IsActive => m_Visible;
        public int SubViewCount => m_Subviews.Count;
        public virtual UIWindowLayer? Layer => null;

        public UIView()
        {
        }

        public UIView(GameObject root)
        {
            SetRoot(root);
        }

        public void Init(GameObject gameObject)
        {
            SetRoot(gameObject);
            m_Controller.Load();
        }

        public void OnDestroy()
        {
            if (m_Visible)
            {
                Hide();
            }

            OnDestroyInternal();

            foreach (var subView in m_Subviews)
            {
                subView.OnDestroy();
            }

            m_Controller.OnDestroy();

            Object.Destroy(m_Root);
            m_Root = null;
        }

        public void Show()
        {
            if (m_Visible)
            {
                return;
            }

            m_Visible = true;
            Root.SetActive(true);
            OnShow();
            foreach (var subView in m_Subviews)
            {
                subView.Show();
            }
            m_Controller.Show();
        }

        public void Hide()
        {
            if (!m_Visible)
            {
                return;
            }

            foreach (var subView in m_Subviews)
            {
                subView.Hide();
            }
            OnHide();
            m_Controller.Hide();

            if (Root != null)
            {
                Root.SetActive(false);
            }

            m_Visible = false;
        }

        public void Update(float dt)
        {
            foreach (var subView in m_Subviews)
            {
                subView.Update(dt);
            }

            OnUpdate(dt);

            m_Controller?.Update(dt);
        }

        public void Refresh()
        {
            foreach (var subView in m_Subviews)
            {
                subView.Controller?.Refresh();
                subView.Refresh();
            }
        }

        public void SetController(UIControllerBase controller)
        {
            m_Controller = controller;
            if (m_Root != null)
            {
                m_Controller.Load();
            }
        }

        public ViewType AddSubView<ViewType, ControllerType>(GameObject subviewRoot) 
            where ViewType : UIView
            where ControllerType : UIControllerBase
        {
            var view = System.Activator.CreateInstance(typeof(ViewType), (object)subviewRoot) as ViewType;
            var controller = System.Activator.CreateInstance(typeof(ControllerType), view) as ControllerType;
            view.SetController(controller);
            view.Show();
            m_Subviews.Add(view);
            return view;
        }

        public void RemoveSubView(int index)
        {
            if (index >= 0 && index < m_Subviews.Count)
            {
                m_Subviews[index].OnDestroy();
                m_Subviews.RemoveAt(index);
            }
            else
            {
                Log.Instance?.Error($"Remove subview failed!");
            }
        }

        public void RemoveSubView(UIView view)
        {
            var index = m_Subviews.IndexOf(view);
            RemoveSubView(index);
        }

        public void RemoveAllSubViews()
        {
            foreach (var subView in m_Subviews) 
            {
                subView.OnDestroy(); 
            }
            m_Subviews.Clear();
        }

        public void RemoveSubViewsOfType<T>() where T : UIView
        {
            for (var i = m_Subviews.Count - 1; i >= 0; i--)
            {
                if (m_Subviews[i] is T)
                {
                    m_Subviews[i].OnDestroy();
                    m_Subviews.RemoveAt(i);
                }
            }
        }

        public UIView GetSubView(int index)
        {
            if (index >= 0 && index < m_Subviews.Count)
            {
                return m_Subviews[index];
            }
            return null;
        }

        public T GetController<T>() where T : UIControllerBase
        {
            return Controller as T;
        }

        public void SetCanvasOrder(int order)
        {
            if (m_Canvas != null)
            {
                m_Canvas.sortingOrder = order;
            }
        }

        protected GameObject QueryGameObject(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return Root;
            }
            return Root.FindChildInHierarchy(path, false);
        }

        protected T QueryComponent<T>(string path, int index) where T : Component
        {
            var obj = QueryGameObject(path);
            if (obj == null)
            {
                Log.Instance?.Error($"QueryComponent failed: {path}, {index}");
            }
            return obj.GetComponentAtIndex<T>(index);
        }

        public abstract string GetPath();
        protected virtual void OnLoad() { }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void OnUpdate(float dt) { }
        protected virtual void OnDestroyInternal() { }

        private void SetRoot(GameObject root)
        {
            Debug.Assert(root != null, $"{GetType().Name} root is null");
            m_Root = root;
            if (root != null)
            {
                m_Canvas = root.GetComponentInChildren<Canvas>();

                OnLoad();
            }
        }

        private bool m_Visible = false;
        private GameObject m_Root;
        private List<UIView> m_Subviews = new();
        protected UIControllerBase m_Controller;
        protected Canvas m_Canvas;
    }
}
