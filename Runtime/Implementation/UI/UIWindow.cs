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
using XDay.AssetAPI;

namespace XDay.GUIAPI
{
    public abstract class UIWindowBase
    {
        public virtual bool CacheWhenClose => true;
        public virtual bool Updatable => false;
        public abstract bool IsLoaded { get; }

        public abstract void LoadAsync(IAssetLoader loader, bool showWhenLoaded, GameObject uiRoot);
        public abstract void Load(IAssetLoader loader, bool showWhenLoaded, GameObject uiRoot);
        public abstract void OnDestroy();
        public abstract void SetData(object data);
        public abstract object GetData();
        public abstract void Show();
        public abstract void Hide();
        public abstract void Update(float dt);
        public abstract T GetController<T>() where T : UIControllerBase;
        public abstract void SetCanvasOrder(int order);
    }

    public class UIWindow<View, Controller> : UIWindowBase
        where View : UIView 
        where Controller : UIController<View>
    {
        public override bool IsLoaded => m_View.IsLoaded;
        public View WindowView => m_View;

        public UIWindow()
        {
            m_View = Activator.CreateInstance(typeof(View)) as View;
            m_Controller = Activator.CreateInstance(typeof(Controller), m_View) as Controller;
            m_View.SetController(m_Controller);
        }

        public override void OnDestroy()
        {
            m_View.OnDestroy();
            m_View = null;
            m_IsLoading = false;
        }

        public override void Load(IAssetLoader loader, bool showWhenLoaded, GameObject uiRoot)
        {
            m_IsLoading = true;
            var gameObject = loader.LoadGameObject(m_View.GetPath());
            var parent = GetParent(uiRoot);
            gameObject.transform.SetParent(parent, false);
            OnLoaded(gameObject, showWhenLoaded);
        }

        public override void LoadAsync(IAssetLoader loader, bool showWhenLoaded, GameObject uiRoot)
        {
            m_IsLoading = true;
            loader.LoadGameObjectAsync(m_View.GetPath(), (gameObject) =>
            {
                var parent = GetParent(uiRoot);
                gameObject.transform.SetParent(parent, false);
                OnLoaded(gameObject, showWhenLoaded);
            });
        }

        public override void Show()
        {
            m_View.Show();
        }

        public override void Hide()
        {
            m_View.Hide();
            m_IsLoading = false;
        }

        public override void SetData(object data)
        {
            m_Controller.SetData(data);
        }

        public override object GetData()
        {
            return m_Controller.GetData();
        }

        public override void Update(float dt)
        {
            m_View.Update(dt);
        }

        public override T GetController<T>() 
        {
            return m_Controller as T;
        }

        public override void SetCanvasOrder(int order)
        {
            m_View.SetCanvasOrder(order);
        }

        private void OnLoaded(GameObject gameObject, bool showWhenLoaded)
        {
            if (m_View != null)
            {
                m_View.Init(gameObject);

                if (m_IsLoading && showWhenLoaded)
                {
                    Show();
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        private Transform GetParent(GameObject uiRoot)
        {
            var layer = m_View.Layer;
            Transform parent = uiRoot.transform;
            if (layer != null)
            {
                var obj = uiRoot.transform.Find(layer.GetValueOrDefault().ToString());
                if (obj == null)
                {
                    Debug.LogError($"Layer {layer.GetValueOrDefault()} not found");
                }
                else
                {
                    parent = obj.transform;
                }
            }
            return parent;
        }

        private View m_View;
        protected readonly Controller m_Controller;
        private bool m_IsLoading = false;
    }
}
