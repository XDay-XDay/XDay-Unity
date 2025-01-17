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
using XDay.AssetAPI;

internal class GameAssetLoader : IAssetLoader
{
    public void OnDestroy()
    {
    }

    public bool Exists(string path)
    {
        return m_AssetSystem.Exists(path);
    }

    public T Load<T>(string path) where T : Object
    {
        return m_AssetSystem.Load<T>(path);
    }

    public byte[] LoadBytes(string path)
    {
        var asset = m_AssetSystem.Load<TextAsset>(path);
        if (asset != null)
        {
            return asset.bytes;
        }
        Debug.Assert(false, $"load bytes {path} failed");
        return null;
    }

    public GameObject LoadGameObject(string path)
    {
        var prefab = m_AssetSystem.Load<GameObject>(path);
        if (prefab != null)
        {
            return Object.Instantiate(prefab);
        }
        return null;
    }

    public string LoadText(string path)
    {
        var asset = m_AssetSystem.Load<TextAsset>(path);
        if (asset != null)
        {
            return asset.text;
        }
        Debug.Assert(false, $"load text {path} failed");
        return null;
    }

    public Stream LoadTextStream(string path)
    {
        return new MemoryStream(LoadBytes(path));
    }

    public async UniTask<T> LoadAsync<T>(string path) where T : Object
    {
        return await m_AssetSystem.LoadAsync<T>(path);
    }

    public async UniTask<GameObject> LoadGameObjectAsync(string path)
    {
        return await m_AssetSystem.LoadGameObjectAsync(path);
    }

    public void LoadGameObjectAsync(string path, System.Action<GameObject> onLoaded)
    {
        m_AssetSystem.LoadGameObjectAsync(path, onLoaded);
    }

    public bool UnloadAsset(string path)
    {
        return m_AssetSystem.UnloadAsset(path);
    }

    private readonly IAddressableAssetSystem m_AssetSystem = IAddressableAssetSystem.Create();
}