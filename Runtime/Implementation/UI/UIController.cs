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



namespace XDay.GUIAPI
{
    public abstract class UIControllerBase
    {
        public abstract void SetData(object data);
        public abstract object GetData();
        public abstract void Load();
        public abstract void OnDestroy();
        public abstract void Show();
        public abstract void Hide();
        public abstract void Update(float dt);
        public abstract void Refresh();
    }

    public class UIController<View> : UIControllerBase where View : UIView
    {
        public UIController(View view)
        {
            m_View = view;
        }

        public override void OnDestroy()
        {
        }

        public override void Load()
        {
            OnLoad();
        }

        public override void Show()
        {
            OnShow();
            Refresh();
        }

        public override void Hide()
        {
            OnHide();

            m_Data = null;
        }

        public override void SetData(object data)
        {
            m_Data = data;
            Refresh();
        }

        public override object GetData()
        {
            return m_Data;
        }

        public override void Refresh()
        {
            if (m_View.IsLoaded)
            {
                m_View.Refresh();
                OnRefresh();
            }
        }

        public override void Update(float dt)
        {
            OnUpdate(dt);
        }

        protected virtual void OnRefresh() { }
        protected virtual void OnLoad() { }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void OnUpdate(float dt) { }

        protected View m_View;
        protected object m_Data;
    }
}
