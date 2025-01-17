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
using XDay.AssetAPI;

namespace XDay.GUIAPI
{
    public abstract class UIWindowBase
    {
        public virtual bool CacheWhenClose => true;

        public abstract void Load(IAssetLoader loader);
        public abstract void OnDestroy();
        public abstract void SetData(object data);
        public abstract void Show();
        public abstract void Hide();
    }

    public class UIWindow<View, Controller> : UIWindowBase
        where View : UIView 
        where Controller : UIController<View>
    {
        public UIWindow()
        {
            m_View = Activator.CreateInstance(typeof(View)) as View;
            m_Controller = Activator.CreateInstance(typeof(Controller), m_View) as Controller;
            m_View.SetController(m_Controller);
        }

        public override void OnDestroy()
        {
            m_View.OnDestroy();
        }

        public override void Load(IAssetLoader loader)
        {
            loader.LoadGameObjectAsync(m_View.GetPath(), (gameObject) =>
            {
                m_View.Init(gameObject);
                Show();
            });
        }

        public override void Show()
        {
            m_View.Show();
        }

        public override void Hide()
        {
            m_View.Hide();
        }

        public override void SetData(object data)
        {
            m_Controller.SetData(data);
        }

        private readonly View m_View;
        private readonly Controller m_Controller;
    }
}
