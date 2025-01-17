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
using XDay.AssetAPI;

namespace XDay.GUIAPI
{
    internal class UIWindowManager : IUIWindowManager
    {
        public UIWindowManager(IAssetLoader loader)
        {
            m_Loader = loader;
        }

        public void OnDestroy()
        {
            foreach (var window in m_ActiveWindows)
            {
                window.OnDestroy();
            }
        }

        public T Open<T>() where T : UIWindowBase, new()
        {
            var window = GetActive<T>();
            if (window == null)
            {
                window = GetCache<T>();
                if (window == null)
                {
                    window = new T();
                    window.Load(m_Loader);
                }
                else
                {
                    window.Show();
                }
                m_ActiveWindows.Add(window);
            }
            return window;
        }

        public void Close(UIWindowBase window)
        {
            for (var i = m_ActiveWindows.Count - 1; i >= 0; i--)
            {
                if (window == m_ActiveWindows[i])
                {
                    window.Hide();
                    if (window.CacheWhenClose)
                    {
                        m_CachedWindows.Add(window);
                    }
                    else
                    {
                        window.OnDestroy();
                    }
                    m_ActiveWindows.RemoveAt(i);
                    return;
                }
            }
            Debug.LogError("Close window failed!");
        }

        private T GetActive<T>() where T : UIWindowBase, new()
        {
            foreach (var window in m_ActiveWindows)
            {
                if (window is T w)
                {
                    return w;
                }
            }
            return null;
        }

        private T GetCache<T>() where T : UIWindowBase, new()
        {
            foreach (var window in m_CachedWindows)
            {
                if (window is T w)
                {
                    return w;
                }
            }
            return null;
        }

        private readonly IAssetLoader m_Loader;
        private readonly List<UIWindowBase> m_ActiveWindows = new();
        private readonly List<UIWindowBase> m_CachedWindows = new();
    }
}
