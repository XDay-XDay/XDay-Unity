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
using System.Collections.Generic;
using XDay.UtilityAPI;

namespace XDay.NetworkAPI
{
    internal class ArrayPool : IArrayPool
    {
        public ArrayPool(int defaultLength)
        {
            m_DefaultLength = defaultLength;
        }

        public void Release(byte[] array)
        {
            lock (m_Lock)
            {
                if (m_Pools.TryGetValue(array.Length, out var pool))
                {
                    pool.Release(array);
                }
                else
                {
                    Debug.LogError($"query pool of size {array.Length} failed!");
                }
            }
        }

        public byte[] Get(int length = 0)
        {
            lock (m_Lock)
            {
                length = length > 0 ? length : m_DefaultLength;
                m_Pools.TryGetValue(length, out var pool);
                if (pool == null)
                {
                    pool = IStructArrayPool<byte>.Create(10);
                    m_Pools.Add(length, pool);
                }
                var array = pool.Get(length);
                Debug.Assert(array.Length >= length);
                return array;
            }
        }

        private readonly Dictionary<int, IStructArrayPool<byte>> m_Pools = new();
        private readonly object m_Lock = new();
        private readonly int m_DefaultLength;
    }
}

//XDay