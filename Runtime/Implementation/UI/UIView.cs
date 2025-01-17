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
using XDay.UtilityAPI;

namespace XDay.GUIAPI
{
    public abstract class UIView
    {
        public GameObject Root => m_Root;
        public UIControllerBase Controller => m_Controller;
        public bool IsLoaded => Root != null;

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
            OnDestroyInternal();

            m_Controller.OnDestroy();

            Object.Destroy(m_Root);
            m_Root = null;
        }

        public void Show()
        {
            Root.SetActive(true);
            m_Controller.Show();
            OnShow();
        }

        public void Hide()
        {
            OnHide();
            m_Controller.Hide();
            Root.SetActive(false);
        }

        public void SetController(UIControllerBase controller)
        {
            m_Controller = controller;
            if (m_Root != null)
            {
                m_Controller.Load();
            }
        }

        protected GameObject QueryGameObject(string path)
        {
            return Root.FindChildInHierarchy(path, false);
        }

        protected T QueryComponent<T>(string path, int index) where T : Component
        {
            var obj = QueryGameObject(path);
            return obj.GetComponentAtIndex<T>(index);
        }

        public abstract string GetPath();
        protected virtual void OnLoad() { }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void OnDestroyInternal() { }

        private void SetRoot(GameObject root)
        {
            Debug.Assert(root != null, $"{GetType().Name} root is null");
            m_Root = root;
            if (root != null)
            {
                OnLoad();
            }
        }

        private GameObject m_Root;
        protected UIControllerBase m_Controller;
    }
}
