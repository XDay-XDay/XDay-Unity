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



using Cysharp.Threading.Tasks;
using System.IO;
using UnityEngine;

namespace XDay.AssetAPI
{
    public interface IAssetLoader
    {
        void OnDestroy();
        T Load<T>(string path) where T : Object;
        UniTask<T> LoadAsync<T>(string path) where T : Object;
        GameObject LoadGameObject(string path);
        UniTask<GameObject> LoadGameObjectAsync(string path);
        void LoadGameObjectAsync(string path, System.Action<GameObject> onLoaded);
        byte[] LoadBytes(string path);
        string LoadText(string path);
        Stream LoadTextStream(string path);
        bool Exists(string path);
        bool UnloadAsset(string path);
    }

    /// <summary>
    /// addressable asset system wrapper
    /// </summary>
    public interface IAddressableAssetSystem
    {
        static IAddressableAssetSystem Create()
        {
            return new AddressableAssetSystem();
        }

        void OnDestroy();

        /// <summary>
        /// load asset
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address">addressable asset address</param>
        /// <returns></returns>
        T Load<T>(string address) where T : Object;

        /// <summary>
        /// load asset async
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <returns></returns>
        UniTask<T> LoadAsync<T>(string address) where T : Object;

        /// <summary>
        /// load asset async
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <param name="onLoaded"></param>
        void LoadAsync<T>(string address, System.Action<T> onLoaded) where T : Object;

        /// <summary>
        /// load game object async
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        UniTask<GameObject> LoadGameObjectAsync(string address);

        /// <summary>
        /// load game object async
        /// </summary>
        /// <param name="address"></param>
        /// <param name="onLoaded"></param>
        void LoadGameObjectAsync(string address, System.Action<GameObject> onLoaded);

        /// <summary>
        /// unload asset
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        bool UnloadAsset(string address);

        /// <summary>
        /// check if asset exists
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        bool Exists(string address);
    }
}
