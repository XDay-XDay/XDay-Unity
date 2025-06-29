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
using XDay.UtilityAPI;

namespace XDay.GUIAPI
{
    internal class UIWindowManager : IUIWindowManager
    {
        public static UIWindowManager Instance => m_Instance;
        public IAssetLoader AssetLoader => m_Loader;

        public UIWindowManager(IAssetLoader loader, GameObject windowRoot)
        {
            if (m_Instance != null)
            {
                Debug.LogError("UIWindowManager只能有一个Instance");
            }
            m_Instance = this;
            m_Loader = loader;
            m_UIRoot = windowRoot;
        }

        public void OnDestroy()
        {
            m_Instance = null;

            foreach (var window in m_ActiveWindows)
            {
                window.OnDestroy();
            }

            Helper.DestroyUnityObject(m_UIRoot);
        }

        public void Load<T>() where T : UIWindowBase, new()
        {
            var window = GetActive<T>();
            if (window == null)
            {
                window = GetCache<T>();
                if (window == null)
                {
                    window = new T();
                    window.Load(m_Loader, false, m_UIRoot);
                    m_CachedWindows.Add(window);
                }
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
                    window.LoadAsync(m_Loader, true, m_UIRoot);
                }
                else
                {
                    window.Show();
                }
                m_ActiveWindows.Add(window);
                if (window.Updatable)
                {
                    m_UpdatableActiveWindows.Add(window);
                }
            }
            return window;
        }

        public void Close<T>() where T : UIWindowBase, new()
        {
            for (var i = m_ActiveWindows.Count - 1; i >= 0; i--)
            {
                if (m_ActiveWindows[i].GetType() == typeof(T))
                {
                    Close(m_ActiveWindows[i]);
                    return;
                }
            }
            Log.Instance?.Warning($"Close window {typeof(T).Name} failed!");
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
                        m_CachedWindows.Remove(window);
                    }
                    m_ActiveWindows.RemoveAt(i);

                    if (window.Updatable)
                    {
                        bool removed = m_UpdatableActiveWindows.Remove(window);
                        Debug.Assert(removed);
                    }
                    return;
                }
            }
            Log.Instance?.Warning($"Close window failed!");
        }

        public void Update(float dt)
        {
            for (var i = m_UpdatableActiveWindows.Count - 1; i >= 0; --i)
            {
                if (m_UpdatableActiveWindows[i].IsLoaded)
                {
                    m_UpdatableActiveWindows[i].Update(dt);
                }
            }
        }

        public T GetActive<T>() where T : UIWindowBase, new()
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

        public bool IsOpen<T>() where T : UIWindowBase, new()
        {
            return GetActive<T>() != null;
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
        private readonly List<UIWindowBase> m_UpdatableActiveWindows = new();
        private readonly List<UIWindowBase> m_CachedWindows = new();
        private readonly GameObject m_UIRoot;
        private static UIWindowManager m_Instance;
    }
}
