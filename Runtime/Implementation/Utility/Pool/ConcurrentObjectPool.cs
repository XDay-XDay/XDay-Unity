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
using UnityEngine.Pool;

namespace XDay.UtilityAPI
{
    internal class ConcurrentObjectPool<T> : IConcurrentObjectPool<T> where T : class
    {
        public ConcurrentObjectPool(
            Func<T> createFunc,
            int capacity,
            Action<T> actionOnDestroy, 
            Action<T> actionOnGet, 
            Action<T> actionOnRelease)
        {
            m_Pool = new ObjectPool<T>(createFunc, actionOnGet, actionOnRelease, actionOnDestroy, true, capacity);
        }

        public void OnDestroy()
        {
            lock (m_Lock)
            {
                m_Pool.Dispose();
            }
        }

        public T Get()
        {
            lock (m_Lock)
            {
                return m_Pool.Get();
            }
        }

        public void Release(T obj)
        {
            lock (m_Lock)
            {
                m_Pool.Release(obj);
            }
        }

        public void Clear()
        {
            lock (m_Lock)
            {
                m_Pool.Clear();
            }
        }

        private object m_Lock = new();
        private ObjectPool<T> m_Pool;
    }
}

//XDay