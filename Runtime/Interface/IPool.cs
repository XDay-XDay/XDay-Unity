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
    public interface IValueListPool<T> where T : struct
    {
        static IValueListPool<T> Create(int capacity)
        {
            return new ValueListPool<T>(capacity);
        }

        List<T> Get();
        void Release(List<T> list);
    }

    public interface IConcurrentObjectPool<T> where T : class
    {
        static IConcurrentObjectPool<T> Create(
            Func<T> createFunc,
            int capacity = 10,
            Action<T> actionOnDestroy = null,
            Action<T> actionOnGet = null,
            Action<T> actionOnRelease = null)
        {
            return new ConcurrentObjectPool<T>(createFunc, capacity, actionOnDestroy, actionOnGet, actionOnRelease);
        }

        void OnDestroy();
        void Clear();
        T Get();
        void Release(T obj);
    }

    public interface IConcurrentValueListPool<T> where T : struct
    {
        static IConcurrentValueListPool<T> Create(int capacity = 100)
        {
            return new ConcurrentValueListPool<T>(capacity);
        }

        List<T> Get();
        void Release(List<T> list);
    }

    public interface IGameObjectPool
    {
        static IGameObjectPool Create(Transform parent, Func<string, GameObject> createFunc, Action<string, GameObject> returnToPoolFunc = null, bool hideRoot = true)
        {
            return new GameObjectPool(parent, createFunc, returnToPoolFunc, hideRoot);
        }

        void OnDestroy();

        GameObject Get(string path);
        void Release(string path, GameObject obj);
    }
}
