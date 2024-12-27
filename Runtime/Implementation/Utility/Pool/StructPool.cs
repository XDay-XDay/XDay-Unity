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

using System.Collections.Generic;


namespace XDay.UtilityAPI
{
    internal class StructListPool<T> : IStructListPool<T> where T : struct
    {
        public StructListPool(int capacity)
        {
            m_Cache = new(capacity);
        }

        public void Release(List<T> list)
        {
            list.Clear();
            m_Cache.Add(list);
        }

        public List<T> Get()
        {
            if (m_Cache.Count > 0)
            {
                var list = m_Cache[^1];
                m_Cache.RemoveAt(m_Cache.Count - 1);
                return list;
            }

            return new List<T>();
        }

        private List<List<T>> m_Cache;
    }

    internal class StructArrayPool<T> : IStructArrayPool<T> where T : struct
    {
        public int Count => m_Cache.Count;

        public StructArrayPool(int capacity = 0)
        {
            m_Cache = new(capacity);
        }

        public void Release(T[] array)
        {
            m_Cache.Add(array);
        }

        public T[] Get(int length)
        {
            T[] array = null;

            var n = m_Cache.Count;
            if (n > 0)
            {
                array = m_Cache[n - 1];
                m_Cache.RemoveAt(n - 1);
            }

            array ??= new T[length];            

            return array;
        }

        private List<T[]> m_Cache;
    }
    
}

//XDay