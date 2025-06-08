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



using XDay.UtilityAPI;
using UnityEngine;
using XDay.AssetAPI;
using Cysharp.Threading.Tasks;

namespace XDay.WorldAPI
{
    public class WorldRenderer
    {
        public GameObject Root => m_Root;
        public IGameObjectPool Cache => m_Cache;

        public WorldRenderer(string name, IAssetLoader assetLoader)
        {
            m_AssetLoader = assetLoader;

            m_Root = new GameObject(name);
            m_Root.tag = WorldDefine.EDITOR_ONLY_TAG;

            m_Cache = IGameObjectPool.Create("GameObjectPool-WorldRenderer", m_Root.transform, 
                (prefabPath) => { return CreateNew(prefabPath); },
                (prefabPath) => { return CreateNewAsync(prefabPath); }
                );
        }

        public void OnDestroy()
        {
            m_AssetLoader = null;

            m_Cache.OnDestroy();
            m_Cache = null;

            Helper.DestroyUnityObject(m_Root);
            m_Root = null;
        }

        private GameObject CreateNew(string prefabPath)
        {
            GameObject result = null;
            if (!string.IsNullOrEmpty(prefabPath))
            {
                result = m_AssetLoader.LoadGameObject(prefabPath);
            }
            if (result == null)
            {
                result = CreatePlaceholder(prefabPath);
            }
            return result;
        }

        private async UniTask<GameObject> CreateNewAsync(string prefabPath)
        {
            GameObject result = null;
            if (!string.IsNullOrEmpty(prefabPath))
            {
                result = await m_AssetLoader.LoadGameObjectAsync(prefabPath);
            }
            if (result == null)
            {
                result = CreatePlaceholder(prefabPath);
            }
            return result;
        }

        private GameObject CreatePlaceholder(string prefabPath)
        {
            var result = new GameObject(prefabPath);
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            primitive.transform.SetParent(result.transform, true);
            primitive.transform.localScale = 10.0f * Vector3.one;
            return result;
        }

        private GameObject m_Root;
        private IGameObjectPool m_Cache;
        private IAssetLoader m_AssetLoader;
    }
}

//XDay