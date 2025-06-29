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
using XDay.AssetAPI;

namespace XDay.GUIAPI
{
    public enum UIWindowLayer
    {
        Layer0,
        Layer1,
        Layer2,
        Layer3,
        Layer4,
    }

    public interface IUIWindowManager
    {
        static IUIWindowManager Create(IAssetLoader loader, GameObject windowRoot)
        {
            return new UIWindowManager(loader, windowRoot);
        }

        void OnDestroy();
        /// <summary>
        /// only load window, but don't show it
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        void Load<T>() where T : UIWindowBase, new();
        T Open<T>() where T : UIWindowBase, new();
        T GetActive<T>() where T : UIWindowBase, new();
        bool IsOpen<T>() where T : UIWindowBase, new();
        void Close<T>() where T : UIWindowBase, new();
        void Close(UIWindowBase window);
        void Update(float dt);
    }
}
