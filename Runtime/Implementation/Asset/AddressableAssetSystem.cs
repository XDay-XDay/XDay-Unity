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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if ENABLE_ADDRESSABLE
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace XDay.AssetAPI
{
    internal class AddressableAssetSystem : IAddressableAssetSystem
    {
        public void OnDestroy()
        {
            foreach (var kv in m_Assets)
            {
                UnloadAsset(kv.Key);
            }
        }

        public T Load<T>(string address) where T : Object
        {
#if ENABLE_ADDRESSABLE
            if (m_Assets.TryGetValue(address, out var asset))
            {
                return (T)asset.Asset;
            }

            var result = Addressables.LoadAssetAsync<T>(address).WaitForCompletion();
            if (result != null)
            {
                var newAseset = new AssetInfo()
                {
                    Address = address,
                    Asset = result,
                };
                m_Assets.Add(address, newAseset);
            }
            else
            {
                Debug.LogError($"Load asset {address} failed!");
            }
            return result;
#else
            Debug.LogError("Addressable system not enabled!");
            return null;
#endif
        }

        public async UniTask<T> LoadAsync<T>(string address) where T : Object
        {
#if ENABLE_ADDRESSABLE
            if (m_Assets.TryGetValue(address, out var asset))
            {
                return await UniTask.FromResult((T)asset.Asset);
            }

            var completionSource = AutoResetUniTaskCompletionSource<T>.Create();

            var operationHandle = Addressables.LoadAssetAsync<T>(address);
            operationHandle.Completed += (handle)=> 
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    var asset = new AssetInfo()
                    {
                        Address = address,
                        Asset = handle.Result,
                    };
                    m_Assets.Add(address, asset);

                    completionSource.TrySetResult(handle.Result);
                }
                else
                {
                    Debug.LogError($"Load asset {address} failed!");
                }
            };

            return await completionSource.Task;
#else
            Debug.LogError("Addressable system not enabled!");
            return await UniTask.FromResult<T>(null);
#endif
        }

        public async UniTask<GameObject> LoadGameObjectAsync(string address)
        {
            var prefab = await LoadAsync<GameObject>(address);
            if (prefab != null)
            {
                return Object.Instantiate(prefab);
            }
            return null;
        }

        public bool UnloadAsset(string address)
        {
#if ENABLE_ADDRESSABLE
            if (m_Assets.TryGetValue(address, out var asset))
            {
                Addressables.Release(asset.Asset);
                m_Assets.Remove(address);
                return true;
            }
#endif
            return false;
        }

        public bool Exists(string address)
        {
#if ENABLE_ADDRESSABLE
            var locations = Addressables.LoadResourceLocationsAsync(address).WaitForCompletion();
            return locations.Any();
#else
            return false;
#endif
        }

        private class AssetInfo
        {
            public string Address;
            public Object Asset;
        }

        private readonly Dictionary<string, AssetInfo> m_Assets = new();
    }
}