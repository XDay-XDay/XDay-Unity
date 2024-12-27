/*
 * Copyright (c) 2024 XDay
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
using System.Collections.Generic;
using UnityEngine;


namespace XDay.UtilityAPI
{
    internal class GameObjectPool : IGameObjectPool
    {
        public GameObjectPool(Transform parent, Func<string, GameObject> createFunc, Action<string, GameObject> actionOnRelease, bool hideRoot)
        {
            m_ActionOnRelease = actionOnRelease;
            m_CreateFunc = createFunc;
            m_PoolRoot = new GameObject("GameObjectPool");
            m_PoolRoot.transform.SetParent(parent);
            if (hideRoot)
            {
                Helper.HideGameObject(m_PoolRoot, true);
            }
        }

        public void OnDestroy()
        {
            ClearAll();

            Helper.DestroyUnityObject(m_PoolRoot);
            m_PoolRoot = null;
        }

        public void ClearAll()
        {
            List<string> keys = new();
            foreach (var key in m_Pool.Keys)
            {
                keys.Add(key);
            }
            foreach (var key in keys)
            {
                Clear(key);
            }
        }

        public GameObject Get(string path)
        {
            var result = GetOrCreate(path);

            if (result != null)
            {
                result.SetActive(true);
            }
            return result;
        }

        public void Get(string path, Vector3 localPosition)
        {
            var result = Get(path);
            if (result != null)
            {
                result.transform.localPosition = localPosition;
                result.SetActive(true);
            }
        }

        public void Get(string path, Vector3 localPosition, Quaternion localRotation)
        {
            var result = Get(path);
            if (result != null)
            {
                result.transform.SetLocalPositionAndRotation(localPosition, localRotation);
                result.SetActive(true);
            }
        }

        public void Get(string path, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            var result = Get(path);
            if (result != null)
            {
                result.transform.localScale = localScale;
                result.transform.SetLocalPositionAndRotation(localPosition, localRotation);
                result.SetActive(true);
            }
        }

        public void Release(string key, GameObject obj)
        {
            if (string.IsNullOrEmpty(key))
            {
                Helper.DestroyUnityObject(obj);
            }
            else
            {
                if (obj != null)
                {
                    m_Pool.TryGetValue(key, out var objList);
                    if (objList == null)
                    {
                        objList = new();
                        m_Pool[key] = objList;
                    }
                    objList.Add(obj);
                    obj.SetActive(false);
                    if (m_PoolRoot != null)
                    {
                        obj.transform.SetParent(m_PoolRoot.transform, false);
                    }
                    m_ActionOnRelease?.Invoke(key, obj);
                }
            }
        }

        public void Clear(string key)
        {
            m_Pool.TryGetValue(key, out var cacheList);
            if (cacheList != null)
            {
                foreach (var obj in cacheList)
                {
                    Helper.DestroyUnityObject(obj);
                }
                m_Pool.Remove(key);
            }
        }

        private GameObject GetOrCreate(string path)
        {
            GameObject result = null;

            bool found = m_Pool.TryGetValue(path, out var objectList);
            if (found)
            {
                int n = objectList.Count;
                if (n > 0)
                {
                    result = objectList[n - 1];
                    objectList.RemoveAt(n - 1);
                }
            }
            else
            {
                objectList = new List<GameObject>();
                m_Pool[path] = objectList;
            }

            result ??= m_CreateFunc(path);

            return result;
        }

        private GameObject m_PoolRoot;
        private Dictionary<string, List<GameObject>> m_Pool = new();
        private Action<string, GameObject> m_ActionOnRelease;
        private Func<string, GameObject> m_CreateFunc;
    }
}

//XDay